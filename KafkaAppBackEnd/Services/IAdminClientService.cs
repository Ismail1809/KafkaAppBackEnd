using Confluent.Kafka;
using KafkaAppBackEnd.Models;

namespace KafkaAppBackEnd.Services
{
    public interface IAdminClientService
    {
        IEnumerable<string> GetTopics();
        List<ConsumerResponse> GetConsumerGroup();
        Task CreateTopic(TopicRequest topicRequest);
        Task DeleteTopic(string topicName);
    }
}
