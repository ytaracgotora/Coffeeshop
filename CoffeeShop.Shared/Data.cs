using Microsoft.EntityFrameworkCore;

namespace CoffeeShop.Shared;

public record OrderRequest(string Customer, string Details);

public class Order { 
    public int Id { get; set; } 
    public string Customer { get; set; } = "";
    public string Drink { get; set; } = "";
}

public class OutboxMessage { 
    public int Id { get; set; } 
    public string Payload { get; set; } = "";
    public bool Processed { get; set; } 
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class CoffeeContext(DbContextOptions<CoffeeContext> options) : DbContext(options) {
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
}
