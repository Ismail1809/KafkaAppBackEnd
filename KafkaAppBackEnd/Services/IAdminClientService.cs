using Confluent.Kafka;
using KafkaAppBackEnd.Models;

namespace KafkaAppBackEnd.Services
{
    public interface IAdminClientService
    {
        IEnumerable<TopicResponse> GetTopics(bool hideInternal);
        List<ConsumerGroupResponse> GetConsumerGroup();
        Task CreateTopic(TopicRequest topicRequest);
        Task DeleteTopic(string topicName);
    }
}
