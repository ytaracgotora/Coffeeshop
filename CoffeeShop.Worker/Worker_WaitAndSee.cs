using CoffeeShop.Shared;
using Microsoft.EntityFrameworkCore;

namespace CoffeeShop.Worker;

// The Outbox Logic
public class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger) 
    : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken ct) {
        while (!ct.IsCancellationRequested) {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoffeeContext>();

            var tasks = await db.Outbox.Where(m => !m.Processed).Take(5).ToListAsync(ct);
            foreach (var msg in tasks) {
                logger.LogInformation($"[WORKER] Processing: {msg.Payload}");
                msg.Processed = true;
            }

            await db.SaveChangesAsync(ct);
            await Task.Delay(5000, ct);
        }
    }
}
