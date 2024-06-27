using KafkaAppBackEnd.Models;

namespace KafkaAppBackEnd.Contracts
{
    public interface IConnectionRepository
    {
        Task<IEnumerable<Connection>> GetAllAsync();
        Task<Connection?> GetById(int id);
        Task<Connection> AddAsync(Connection connection);
        Task UpdateAsync(Connection updatedConnection);
        Task DeleteAsync(int updatedConnection);
    }
}
