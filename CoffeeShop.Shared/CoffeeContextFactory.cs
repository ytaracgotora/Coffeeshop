using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoffeeShop.Shared;

public class CoffeeContextFactory : IDesignTimeDbContextFactory<CoffeeContext>
{
    public CoffeeContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CoffeeContext>();
        
        // This 'args' check is what the efbundle uses when you pass --connection
        string connectionString = args.Length > 0 ? args[0] : "Host=localhost;Database=CoffeeDB;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new CoffeeContext(optionsBuilder.Options);
    }
}
