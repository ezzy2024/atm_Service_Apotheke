import sys

filepath = r"c:\Users\ezzel\Projects\ServiceApotheke_MegaProject\ServiceApotheke.API\Program.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

target1 = """// Apply EF Core Migrations Automatically on Startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServiceApotheke.API.Data.DataContext>();
    try
    {
        Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.Migrate(db.Database);
        Console.WriteLine("[EF Core] Production database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[EF Core] Fatal error applying migrations: {ex.Message}");
    }
}"""

replacement1 = """// Apply EF Core Migrations Automatically on Startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServiceApotheke.API.Data.DataContext>();
    if (db.Database.IsRelational())
    {
        try 
        {
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(db.Database, @"
                INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                VALUES ('20260713040543_MobileGateway', '8.0.0') ON CONFLICT DO NOTHING;

                ALTER TABLE ""Pharmacists"" ALTER COLUMN ""IsApprobationVerified"" DROP DEFAULT;
                ALTER TABLE ""Pharmacists"" ALTER COLUMN ""IsApprobationVerified"" TYPE boolean USING ""IsApprobationVerified"" = 1;
            ");
            Console.WriteLine("Successfully applied manual DB fix.");
        }
        catch (Exception ex) { Console.WriteLine($"Manual DB fix error: {ex.Message}"); }
    }
    try
    {
        Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.Migrate(db.Database);
        Console.WriteLine("[EF Core] Production database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[EF Core] Fatal error applying migrations: {ex.Message}");
    }
}"""

target2 = """using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
await Task.Delay(3000); // Wait for Cloud SQL proxy

    db.Database.EnsureCreated();
    if (db.Database.IsRelational())
    {
        try 
        {
            db.Database.ExecuteSqlRaw(@"
                INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                VALUES ('20260713040543_MobileGateway', '8.0.0') ON CONFLICT DO NOTHING;

                ALTER TABLE ""Pharmacists"" ALTER COLUMN ""IsApprobationVerified"" DROP DEFAULT;
                ALTER TABLE ""Pharmacists"" ALTER COLUMN ""IsApprobationVerified"" TYPE boolean USING ""IsApprobationVerified"" = 1;
            ");
            Console.WriteLine("Successfully applied manual DB fix.");
        }
        catch (Exception ex) { Console.WriteLine($"Manual DB fix error: {ex.Message}"); }

        try 
        {
            await db.Database.MigrateAsync();
            Console.WriteLine("Successfully applied pending EF Core migrations.");
        }
        catch (Exception ex) { Console.WriteLine($"EF Migration error: {ex.Message}"); }
    }
}"""

# Use replace, careful with whitespace
import re

# Fix CRLF issues if any
content = content.replace('\r\n', '\n')
target1 = target1.replace('\r\n', '\n')
replacement1 = replacement1.replace('\r\n', '\n')
target2 = target2.replace('\r\n', '\n')

content = content.replace(target1, replacement1)
content = content.replace(target2, "")

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Applied fixes via script.")
