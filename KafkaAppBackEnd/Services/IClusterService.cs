using KafkaAppBackEnd.Models;
using Microsoft.AspNetCore.Mvc;

namespace KafkaAppBackEnd.Services
{
    public interface IClusterService
    {
        Task<IEnumerable<Connection>> GetConnections();
        Task<Connection?> GetConnection(int id);
        Task UpdateConnection(int id, ConnectionRequest connection);
        Task<Connection> PostConnection(CreateConnectionRequest connection);
        Task<IEnumerable<string?>> GetBootStrapServers();
        void CheckConnection(string address);
        void SetAddress(string address);
        Task DeleteConnection(int id);
    }
}
