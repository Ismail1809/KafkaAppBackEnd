using KafkaAppBackEnd.DbContent;
using Microsoft.EntityFrameworkCore;

namespace KafkaAppBackEnd.Extensions
{
    public static class MigrationExtensions
    {
        public static void ApplyMigrations(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            dbContext.Database.Migrate();
        }
    }
}
