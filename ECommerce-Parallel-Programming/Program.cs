using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Use_Cases;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Repositories;
using ECommerce_Parallel_Programming;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyConnection")));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<GetProductsUseCase>();

builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<AddToCartUseCase>();

builder.Services.AddScoped<BuyProductUseCase>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
