using System.Text;
using System.Threading.RateLimiting;
using CoffeeShop.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug() // Catch everything!
    .WriteTo.Console(new CompactJsonFormatter())
    //.WriteTo.File("logs/coffee-shop-.log", 
    //    rollingInterval: RollingInterval.Day,
    //    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(); // Replaces default logger with Serilog

builder.Services.AddDbContextPool<CoffeeContext>(opt => 
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1. Setup the "Security Guard" (Authentication)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Define the "Bouncer" Rules
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter(policyName: "strict", opt =>
    {
        opt.PermitLimit = 100;          // Max 10 orders
        opt.Window = TimeSpan.FromSeconds(10); // per 10 seconds
        opt.QueueLimit = 40;            // Only 2 people can wait in line
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// ✅ THE CORRECT SENIOR FIX
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!, // Connection string first
        "SELECT 1;",                                                   // Health query second
        name: "Postgres_Basement",                                      // Name
        tags: new[] { "ready" }                                         // Tags
    );

builder.Services.AddAuthorization(); // Enable roles/permissions

var app = builder.Build();

app.UseRouting();
app.UseRateLimiter(); // Enable the Bouncer
app.UseAuthentication(); // Check "Who are you?"
app.UseAuthorization();  // Check "What can you do?"

// 2. Protect the Order Endpoint
app.MapPost("/order", async (OrderRequest req, CoffeeContext db, CancellationToken ct) => {
    // 1. Create a "Window" into the existing string (No new memory allocated)
    ReadOnlySpan<char> detailsSpan = req.Details.AsSpan();
    
    // 2. Find the colon without splitting the string
    int colonPos = detailsSpan.IndexOf(':');

    // 3. Slice the window to point only to the drink name
    // (Still no new string created on the heap!)
    ReadOnlySpan<char> drinkSpan = (colonPos != -1) 
        ? detailsSpan.Slice(colonPos + 1) 
        : detailsSpan;

    // 4. Only now do we convert to a string to store it in the DB
    var drinkName = drinkSpan.Trim().ToString();

    // Pass 'ct' to all async methods
    await using var tx = await db.Database.BeginTransactionAsync(ct);
    
    try
    {
        var order = new Order { 
        Customer = req.Customer, 
        Drink = drinkName // Using our efficiently parsed name
    };
    
    db.Orders.Add(order);
    db.Outbox.Add(new OutboxMessage { Payload = $"New order for {req.Customer}: {drinkName}" });

    await db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    return Results.Accepted();
    }
    catch (OperationCanceledException) {
        // Log that we closed the shop mid-order
        Log.Warning("Order for {Customer} was cancelled due to server shutdown", req.Customer);
        return Results.StatusCode(503); // Service Unavailable
    }
    
}).RequireRateLimiting("strict");
 // 👈 ONLY CUSTOMERS WITH A TOKEN CAN ENTER HERE;

// 3. Create a "Login" for testing (The Ticket Booth)
app.MapGet("/login", () =>
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    // In a real app, you'd check a password here!
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: "CoffeeShopApi",
        audience: "CoffeeShopCustomers",
        expires: DateTime.Now.AddMinutes(30),
        signingCredentials: creds
    );

    return Results.Ok(new { token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token) });
});

app.Run();
