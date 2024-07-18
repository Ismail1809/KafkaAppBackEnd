using Confluent.Kafka.Admin;
using KafkaAppBackEnd.Models;
using Microsoft.AspNetCore.Mvc;

namespace KafkaAppBackEnd.Services
{
    public interface IClusterService
    {
        Task<List<DescribeConfigsResult>> GetClusterConfig();
        Task<IEnumerable<Connection>> GetConnections();
        Task<Connection?> GetConnection(int id);
        Task UpdateConnection(int id, UpdateConnectionRequest connection);
        Task<Connection> PostConnection(CreateConnectionRequest connection);
        Task<IEnumerable<string?>> GetBootStrapServers();
        void CheckConnection(string address);
        void SetAddress(string address);
        Task DeleteConnection(int id);
    }
}
