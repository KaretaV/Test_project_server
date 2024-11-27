using Microsoft.AspNetCore.Mvc;
using MyLibrary;
using Microsoft.EntityFrameworkCore;
using CounterLibrary;
using System;
using System.IO;


namespace API_Tranzit_Interface.Controllers
{
	
		[ApiController]
		[Route("[controller]")]
		public class SalesDataStorageServerController : ControllerBase
		{
			private static List<Product> _products = new List<Product>();

			private readonly ILogger<SalesDataStorageServerController> _logger;

			private readonly AppDbContext _dbContext;


			public SalesDataStorageServerController(ILogger<SalesDataStorageServerController> logger , AppDbContext context)
			{
				_dbContext = context;
				_logger = logger;	
			}

			[HttpPost("ProcessLogFile")]
			public async Task<IActionResult> ProcessLogFile([FromBody] List<int> productIds)
			{
				if (productIds == null || !productIds.Any())
				{
					return BadRequest("File is empty or invalid.");
				}

				try
				{
					var validProductIds = productIds.Where(id => id > 0).ToList();

					if (!validProductIds.Any())
					{
						return BadRequest("No valid product IDs found in the file.");
					}

					
					foreach (var productId in validProductIds)
					{
						var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.ProductID == productId);
						if (product == null)
						{
							
							continue;
						}

						var counter = await _dbContext.Counters.FirstOrDefaultAsync(c => c.ProductID == productId);

						if (counter != null)
						{
							counter.Count++;
						}
						else
						{
							_dbContext.Counters.Add(new ProductCounter { ProductID = productId, Count = 1 });
						}
					}

					await _dbContext.SaveChangesAsync();

					return Ok("File processed successfully.");
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Error processing file: {ex.Message}");
				}
			}

			//KRUD для товаров, кроме R
			[HttpPost("AddProduct")]
			public ActionResult AddProduct([FromBody] Product newProduct)
			{
				if (newProduct == null)
				{
					return BadRequest("Product data is invalid.");
				}
				if (string.IsNullOrEmpty(newProduct.Name) || newProduct.Weight <= 0 || newProduct.Volume <= 0)
				{
					return BadRequest("Invalid product fields.");
				}

				try
				{
					_dbContext.Products.Add(newProduct);
					_dbContext.SaveChanges();

					return CreatedAtAction(nameof(GetProductInfoByIdOrName), new { id = newProduct.ProductID }, newProduct);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}

			[HttpPut("UpdateProduct/{id}")]
			public ActionResult UpdateProduct(int id, [FromBody] Product updatedProduct)
			{
				try
				{
					var existingProduct = _dbContext.Products.FirstOrDefault(p => p.ProductID == id);
					if (existingProduct == null)
					{
						return NotFound($"Product with ID {id} not found.");
					}

					existingProduct.Name = updatedProduct.Name;
					existingProduct.Weight = updatedProduct.Weight;
					existingProduct.Volume = updatedProduct.Volume;
					existingProduct.TypeOfProduct = updatedProduct.TypeOfProduct;

					if (existingProduct is MeatProduct existingMeatProduct && updatedProduct is MeatProduct updatedMeatProduct)
					{
						existingMeatProduct.Nature = updatedMeatProduct.Nature;
					}
					else if (existingProduct is MilkProduct existingMilkProduct && updatedProduct is MilkProduct updatedMilkProduct)
					{
						existingMilkProduct.ProductionDay = updatedMilkProduct.ProductionDay;
					}
					else if (existingProduct is SweetProduct existingSweetProduct && updatedProduct is SweetProduct updatedSweetProduct)
					{
						existingSweetProduct.AmountOfSugar = updatedSweetProduct.AmountOfSugar;
					}

					_dbContext.SaveChanges();

					return Ok($"Product with ID {id} updated successfully.");
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
			[HttpDelete("DeleteProduct/{id}")]
			public ActionResult DeleteProduct(int id)
			{
				try
				{
					var delProduct = _dbContext.Products.FirstOrDefault(p => p.ProductID == id);

					if (delProduct == null)
					{
						return NotFound($"Product with ID {id} not found.");
					}

					_dbContext.Products.Remove(delProduct);

					_dbContext.SaveChanges();

					return Ok($"Product with ID {id} deleted successfully.");
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
			//Запрос инфы о товарах
			[HttpGet("GetAllProducts")]
			public ActionResult<IEnumerable<Product>> GetAllProducts()
			{
				try
				{
					var products = _dbContext.Products.ToList();

					if (products == null || !products.Any())
					{
						return NotFound("No products found.");
					}

					return Ok(products);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
			[HttpGet("GetProductByName/{name}")]
			public ActionResult<IEnumerable<Product>> GetProductsByName(string name)
			{
				try
				{
					var products = _dbContext.Products.Where(p => p.Name == name).ToList();

					if (!products.Any())
					{
						return NotFound($"No products found with name '{name}'.");
					}

					return Ok(products);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
			[HttpGet("GetProductNameById/{id}")]
			public ActionResult<string> GetProductNameById(int id)
			{
				try
				{
					var product = _dbContext.Products.FirstOrDefault(p => p.ProductID == id);

					if (product == null)
					{
						return NotFound($"Product with ID {id} not found.");
					}

					return Ok(product.Name);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
			[HttpGet("GetProductInfoByIdOrName/{input}")]
			public ActionResult<Product> GetProductInfoByIdOrName(string input)
			{
				try
				{
					Product? product;

					if (int.TryParse(input, out int productId))
					{
						product = _dbContext.Products.FirstOrDefault(p => p.ProductID == productId);
					}
					else
					{
						product = _dbContext.Products.FirstOrDefault(p => p.Name == input);
					}

					if (product == null)
					{
						return NotFound($"Product with input '{input}' not found.");
					}

					return Ok(product);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
			//Запрос инфы о продажах - Counters
			[HttpGet("GetCountersSummary")]
			public ActionResult<IEnumerable<ProductCounter>> GetCountersSummary()
			{
				try
				{
					var countersSummary = _dbContext.Counters
						.Join(_dbContext.Products,
							  counter => counter.ProductID,
							  product => product.ProductID,
							  (counter, product) => new
							  {
								  ProductID = product.ProductID,
								  ProductName = product.Name,
								  Count = counter.Count
							  })
						.ToList();

					if (countersSummary == null || !countersSummary.Any())
					{
						return NotFound("No counters found.");
					}

					return Ok(countersSummary);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
			[HttpGet("GetCounterById/{id}")]
			public ActionResult<ProductCounter> GetCounterById(int id)
			{
				try
				{
					var counter = _dbContext.Counters.FirstOrDefault(c => c.ProductID == id);

					if (counter == null)
					{
						return NotFound($"Counter for ProductID {id} not found.");
					}

					return Ok(counter);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
			[HttpGet("GetTotalRevenue")]
			public ActionResult<decimal> GetTotalRevenue()
			{
				try
				{
					var counters = _dbContext.Counters.ToList();

					if (counters == null || !counters.Any())
					{
						return NotFound("No sales data available.");
					}

					var products = _dbContext.Products.ToList();

					decimal totalRevenue = counters
						.Join(products,
							  counter => counter.ProductID,
							  product => product.ProductID,
							  (counter, product) => counter.Count * product.Prise)
						.Sum();

					return Ok(totalRevenue);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
			[HttpGet("GetRevenueByCategory/{category}")]
			public ActionResult<decimal> GetRevenueByCategory(string category)
			{
				try
				{
					var productsInCategory = _dbContext.Products
						.Where(p => p.TypeOfProduct == category)
						.ToList();

					if (!productsInCategory.Any())
					{
						return NotFound($"No products found in the '{category}' category.");
					}

					var counters = _dbContext.Counters
						.Where(c => productsInCategory.Any(p => p.ProductID == c.ProductID))
						.ToList();

					if (!counters.Any())
					{
						return NotFound($"No sales data available for the '{category}' category.");
					}

					decimal revenueByCategory = counters
						.Join(productsInCategory,
							  counter => counter.ProductID,
							  product => product.ProductID,
							  (counter, product) => counter.Count * product.Prise)
						.Sum();

					return Ok(revenueByCategory);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
			[HttpGet("GetRevenueByCounter/{id}")]
			public ActionResult<decimal> GetRevenueByCounter(int id)
			{
				try
				{
					var counter = _dbContext.Counters.FirstOrDefault(c => c.ProductID == id);

					if (counter == null)
					{
						return NotFound($"Counter with ProductID {id} not found.");
					}

					var product = _dbContext.Products.FirstOrDefault(p => p.ProductID == id);

					if (product == null)
					{
						return NotFound($"Product with ID {id} not found.");
					}

					decimal revenue = counter.Count * product.Prise;

					return Ok(revenue);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}
			//К интерйфейсу



		}
	}
	

