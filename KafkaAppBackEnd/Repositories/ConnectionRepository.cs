using Confluent.Kafka;
using KafkaAppBackEnd.Contracts;
using KafkaAppBackEnd.DbContent;
using KafkaAppBackEnd.Models;
using KafkaAppBackEnd.Services;
using Microsoft.EntityFrameworkCore;

namespace KafkaAppBackEnd.Repositories
{
    public class ConnectionRepository: IConnectionRepository
    {
        //private readonly ILogger<ConnectionRepository> _logger;
        private readonly DatabaseContext _context;

        public ConnectionRepository(DatabaseContext context)
        {
            //_logger = logger;
            _context = context;
        }
        public async Task<IEnumerable<Connection>> GetAllAsync()
        {
            return await _context.Connections.ToListAsync();
        }

        public async Task<Connection?> GetById(int id)
        {
            var connection = await _context.Connections.FindAsync(id);

            if (connection != null)
            {
                return connection;
            }

            return null;
        }

        public async Task<Connection> AddAsync(Connection connection)
        {
            await _context.Connections.AddAsync(connection);
            await _context.SaveChangesAsync();
            return connection;

        }

        public async Task UpdateAsync(Connection updatedConnection)
        {

            if (updatedConnection != null)
            {
                _context.Connections.Entry(updatedConnection).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var connection = await _context.Connections.FindAsync(id);

            if (connection != null)
            {
                //This will mark the Entity State as Deleted
                _context.Connections.Remove(connection);
            }

            await _context.SaveChangesAsync();
        }

    }
}
