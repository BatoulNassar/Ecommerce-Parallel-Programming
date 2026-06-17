
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Use_Cases;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ======================
// DB Context
// ======================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyConnection")));

// ======================
// Repositories
// ======================
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// ======================
// Use Cases
// ======================
builder.Services.AddScoped<GetProductsUseCase>();
builder.Services.AddScoped<AddToCartUseCase>();
builder.Services.AddScoped<BuyProductUseCase>();

// ======================
// Batch / Background Service
// ======================
builder.Services.AddScoped<SalesBatchProcessingService>();

// ======================
// Background Queue (AOP idea part)
// ======================
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

// Distributed Cache (Redis)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
                            ?? "localhost:6379";
    options.InstanceName = "Ecommerce:";
});

// HttpClient for the Round Robin distributor
builder.Services.AddHttpClient();

// ======================
// Controllers + Swagger
// ======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ======================
// Swagger (IMPORTANT)
// ======================
app.UseSwagger();
app.UseSwaggerUI();

// ======================
// Test endpoint (اختياري للتأكد)
// ======================
app.MapGet("/", () => "API IS WORKING");

// ======================
// Middleware
// ======================

// app.UseHttpsRedirection(); // ممكن تتركيها أو تشيليها إذا عم تعمل مشاكل

app.UseAuthorization();

app.MapControllers();

app.Run();