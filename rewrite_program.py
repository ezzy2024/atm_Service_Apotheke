import sys
import re

filepath = r"c:\Users\ezzel\Projects\ServiceApotheke_MegaProject\ServiceApotheke.API\Program.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# Fix CRLF
content = content.replace('\r\n', '\n')

# Find and remove old migration block
migration_pattern = re.compile(r'// Apply EF Core Migrations Automatically on Startup\nusing \(var scope = app\.Services\.CreateScope\(\)\)\n\{.*?Console\.WriteLine\(\$"\[EF Core\] Fatal error applying migrations: \{ex\.Message\}"\);\n    \}\n\}\n', re.DOTALL)
content = migration_pattern.sub('', content)

# Find and remove old seeding block
seeding_pattern = re.compile(r'using \(var scope = app\.Services\.CreateScope\(\)\)\n\{\n    var context = scope\.ServiceProvider\.GetRequiredService<DataContext>\(\);\n    if \(\!context\.PharmacyRegistries\.Any\(\)\)\n    \{.*?Console\.WriteLine\(\$"Seeded \{batch\.Count\} pharmacies\."\);\n        \}\n    \}\n\}\n', re.DOTALL)
content = seeding_pattern.sub('', content)

# Background task code to insert
background_task_code = """
// Apply EF Core Migrations and Seeding Automatically in Background
_ = Task.Run(() => 
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ServiceApotheke.API.Data.DataContext>();
        
        // Manual schema drift fix
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
        
        // EF Core Migrations
        try
        {
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.Migrate(db.Database);
            Console.WriteLine("[EF Core] Production database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EF Core] Fatal error applying migrations: {ex.Message}");
        }
        
        // Seeding
        try
        {
            if (!System.Linq.Enumerable.Any(db.PharmacyRegistries))
            {
                var csvPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, "scripts", "pharmacy_registry.csv");
                if (File.Exists(csvPath))
                {
                    var lines = File.ReadAllLines(csvPath).Skip(1);
                    var batch = new System.Collections.Generic.List<ServiceApotheke.API.Models.PharmacyRegistry>();
                    foreach (var line in lines)
                    {
                        var parts = line.Split(',');
                        if (parts.Length >= 6)
                        {
                            batch.Add(new ServiceApotheke.API.Models.PharmacyRegistry
                            {
                                Name = parts[0].Trim('"'),
                                Street = parts[1].Trim('"'),
                                PLZ = parts[2].Trim('"'),
                                City = parts[3].Trim('"'),
                                Phone = parts[4].Trim('"'),
                                Email = parts[5].Trim('"')
                            });
                        }
                    }
                    db.AddRange(batch);
                    db.SaveChanges();
                    Console.WriteLine($"Seeded {batch.Count} pharmacies.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EF Core] Fatal error seeding: {ex.Message}");
        }
    }
});
"""

# Insert background task just before app.UseExceptionHandler or app.Use(...)
# We can just insert it right after builder.Build() actually.
# Wait, let's insert it after app.UseForwardedHeaders
insert_marker = "app.UseForwardedHeaders(forwardedHeadersOptions);"
if insert_marker in content:
    content = content.replace(insert_marker, insert_marker + "\n" + background_task_code)
else:
    print("Could not find insert marker!")

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Program.cs successfully rewritten.")
