using CoffeeShop.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public class OutboxProcessor(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<OutboxProcessor> logger)
    : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Ensure the trigger exists before we start listening
        await EnsureTriggerExists(ct);

        // Use a persistent connection for LISTENING
        await using var conn = new NpgsqlConnection(config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync(ct);

        // Tell Postgres we are listening for the 'outbox_updated' channel
        await using (var cmd = new NpgsqlCommand("LISTEN outbox_updated", conn))
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }

        logger.LogInformation("[WORKER] Passive listening mode activated. Zzz...");

        while (!ct.IsCancellationRequested)
        {
            // Wait here indefinitely until Postgres sends a notification
            await conn.WaitAsync(ct);

            logger.LogInformation("[WORKER] Ping received! Waking up to check the Log Book.");

            // Drain the Outbox
            bool hasMoreWork = true;
            while (hasMoreWork)
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CoffeeContext>();

                var tasks = await db.Outbox
                    .Where(m => !m.Processed)
                    .OrderBy(m => m.Id)
                    .Take(10)
                    .ToListAsync(ct);

                if (tasks.Any())
                {
                    foreach (var msg in tasks)
                    {
                        logger.LogInformation($"[KITCHEN] Notifying for: {msg.Payload}");
                        msg.Processed = true;
                    }
                    await db.SaveChangesAsync(ct);
                }
                else
                {
                    hasMoreWork = false; // Log Book is empty, go back to sleep
                }
            }
        }
    }
    
    private async Task EnsureTriggerExists(CancellationToken ct) {
    using var scope = scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CoffeeContext>();
    
    // 🔥 SENIOR MOVE: Run the raw SQL to "self-heal" the database trigger
    var sql = @"
        CREATE OR REPLACE FUNCTION notify_outbox_change() RETURNS trigger AS $$
        BEGIN
          PERFORM pg_notify('outbox_updated', 'new_order');
          RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;

        DROP TRIGGER IF EXISTS outbox_insert_trigger ON ""Outbox"";
        CREATE TRIGGER outbox_insert_trigger
        AFTER INSERT ON ""Outbox""
        FOR EACH ROW EXECUTE FUNCTION notify_outbox_change();";

    await db.Database.ExecuteSqlRawAsync(sql, ct);
}
}