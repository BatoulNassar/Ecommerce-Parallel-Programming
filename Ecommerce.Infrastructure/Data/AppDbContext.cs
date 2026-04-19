using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Data
{
    public class AppDbContext :DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }
            public DbSet<Product> Products { get; set; }
            public DbSet<User> Users { get; set; }
            public DbSet<CartItem> CartItems { get; set; }
            public DbSet<Order> Orders { get; set; }
    }
}
