using KafkaAppBackEnd.Models;
using Microsoft.EntityFrameworkCore;


namespace KafkaAppBackEnd.DbContent
{
    public class DatabaseContext: DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DatabaseContext() { }

        public virtual DbSet<Connection> Connections { get; set; }
    }
}
