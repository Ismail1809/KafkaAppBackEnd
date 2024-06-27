using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaAppBackEnd.Models;

namespace KafkaAppBackEnd.Services
{
    public interface IAdminClientService
    {
        IEnumerable<GetTopicsResponse> GetTopics(bool hideInternal);
        TopicDescription GetTopic(string topicName);
        List<GetConsumerGroupsResponse> GetConsumerGroups();
        Task CreateTopic(CreateTopicRequest topicRequest);
        Task CloneTopic(string oldTopicName, string newTopicName);
        Task RenameTopicAsync(string oldTopicName, string newTopicName);
        Task ProduceMessage(Message<string,string> message, string topic);
        List<ConsumeResult<string, string>> GetMessagesFromX(string topic, int x);
        List<ConsumeResult<string, string>> GetSpecificPages(string topic, int pageSize, int pageNumber);
        IEnumerable<ConsumeResult<string, string>> SearchByKeys(string topic, List<string> listOfKeys, Choices choice);
        IEnumerable<ConsumeResult<string, string>> SearchByHeaders(string topic, List<string> listOfPairs, Choices choice);
        IEnumerable<ConsumeResult<string, string>> SearchByTimeStamps(string topic, DateTime? time1, DateTime? time2);
        IEnumerable<ConsumeResult<string, string>> SearchByPartitions(string topic, int partitions);
        Task ProduceRandomNumberOfMessages(int numberOfMessages, string topic);
        Task DeleteTopic(string topicName);
        string SetAddress(string address);  
    }
}
