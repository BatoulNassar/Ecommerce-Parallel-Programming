


using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .Property(p => p.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Laptop",
                    Price = 500,
                    Stock = 200
                },
                new Product
                {
                    Id = 2,
                    Name = "Phone",
                    Price = 300,
                    Stock = 800
                },
                new Product
                {
                    Id = 3,
                    Name = "Headphones",
                    Price = 120,
                    Stock = 1500
                }
            );


            modelBuilder.Entity<User>().HasData(
    new User
    {
        Id = 1,
        Name = "Ali",
        Email = "ali@test.com"
    },
    new User
    {
        Id = 2,
        Name = "Sara",
        Email = "sara@test.com"
    }
);
        }
    }
}