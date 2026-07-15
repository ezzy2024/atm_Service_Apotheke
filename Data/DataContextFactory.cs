using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServiceApotheke.API.Data
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            
            // Use Postgres locally
            optionsBuilder.UseNpgsql("Host=localhost;Database=dummy;Username=test;Password=test");

            return new DataContext(optionsBuilder.Options);
        }
    }
}