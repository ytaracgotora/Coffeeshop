using CoffeeShop.Shared;
using CoffeeShop.Worker;
using Microsoft.EntityFrameworkCore;

using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug() // Catch everything!
    .WriteTo.Console(new CompactJsonFormatter())
    //.WriteTo.File("logs/coffee-shop-.log", 
    //    rollingInterval: RollingInterval.Day,
    //    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog(); // Replaces default logger with Serilog

builder.Services.AddDbContextPool<CoffeeContext>(opt => 
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<OutboxProcessor>();

var host = builder.Build();
await host.RunAsync();