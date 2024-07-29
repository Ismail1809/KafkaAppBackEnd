using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaAppBackEnd.Models;

namespace KafkaAppBackEnd.Services
{
    public interface IAdminClientService
    {
        Task<IEnumerable<GetTopicResponse>> GetTopics(bool hideInternal);
        TopicDescription GetTopic(string topicName);
        Task<List<DescribeConfigsResult>> GetTopicConfig(string topicName);
        Task<List<LogPartition>> GetTopicSize(string? param);
        int GetTopicRecordsCount(string topicName);
        Task<List<GetConsumerGroupsResponse>> GetConsumerGroups();
        long GetOverAllLag(List<MemberDescription> members);
        Task CreateTopic(CreateTopicRequest topicRequest);
        Task CreateTopics(List<CreateTopicRequest> topicsRequests);
        Task CloneTopic(string oldTopicName, string newTopicName);
        Task RenameTopicAsync(string oldTopicName, string newTopicName);
        Task ProduceMessage(Message<string,string> message, string topic);
        List<ConsumeResult<string, string>> GetMessagesFromX(string topic, int x);
        List<ConsumeResult<string, string>> GetSpecificPages(string topic, int pageSize, int pageNumber);
        IEnumerable<ConsumeResult<string, string>> SearchByKeys(string topic, List<string> listOfKeys, SearchOption choice);
        IEnumerable<ConsumeResult<string, string>> SearchByHeaders(string topic, List<string> listOfPairs, SearchOption choice);
        IEnumerable<ConsumeResult<string, string>> SearchByTimeStamps(string topic, DateTime? time1, DateTime? time2);
        IEnumerable<ConsumeResult<string, string>> SearchByPartitions(string topic, int partitions);
        Task<List<string>> ProduceAvroMessage(string topic);
        Task BatchMessages(string topic, List<string> listOfMessages, string key, Dictionary<string, string> headers, int? partitionId);
        Task BatchMessagesFromFile(string topic, IFormFile formFile, string separator, string key, Dictionary<string, string> headers, int? partitionId);
        List<string> CompareMessageSizes(string jsonTopic);
        Task<string> CompareSizes();
        Task ProduceRandomNumberOfMessages(int numberOfMessages, string topic);
        Task DeleteTopic(string topicName);
        string SetAddress(string address);
        Task<GetTopicResponse> GetTopicInfo(string topicName);
    }
}
   