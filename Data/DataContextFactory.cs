using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServiceApotheke.API.Data
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            
            // Switch to Postgres for design-time migrations to match production
            optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=serviceapotheke-db;Username=appuser;Password=ServiceApotheke2026Strong");

            return new DataContext(optionsBuilder.Options);
        }
    }
}