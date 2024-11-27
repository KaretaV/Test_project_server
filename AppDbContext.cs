using Microsoft.EntityFrameworkCore;
using MyLibrary;
using System;
using CounterLibrary;

namespace API_Tranzit_Interface
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
		public DbSet<Product> Products { get; set; }
		public DbSet<ProductCounter> Counters { get; set; }
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Product>().HasKey(p => p.ProductID);
			modelBuilder.Entity<MeatProduct>().HasBaseType<Product>();
			modelBuilder.Entity<MilkProduct>().HasBaseType<Product>();
			modelBuilder.Entity<SweetProduct>().HasBaseType<Product>();
		}
	}
}
