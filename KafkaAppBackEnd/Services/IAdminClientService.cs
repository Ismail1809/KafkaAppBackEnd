using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaAppBackEnd.Models;

namespace KafkaAppBackEnd.Services
{
    public interface IAdminClientService
    {
        Task<IEnumerable<GetTopicResponse>> GetTopics(bool hideInternal);
        Task<IEnumerable<GetTopicSizeResponse>> GetTopicsSizeInfo(bool hideInternal);
        Task<TopicDescription> GetTopic(string topicName);
        Task<int> GetTopicRecordsCount(string topicName);
        Task<long> GetTopicRecordsCountKafka(string topicName);
        Task<List<DescribeConfigsResult>> GetTopicConfig(string topicName);
        Task<List<GetConsumerGroupsResponse>> GetConsumerGroups();
        long GetOverAllLag(List<MemberDescription> members);
        Task CreateTopic(CreateTopicRequest topicRequest);
        Task CreateTopics(List<CreateTopicRequest> topicsRequests);
        Task CloneTopic(string oldTopicName, string newTopicName);
        Task RenameTopicAsync(string oldTopicName, string newTopicName);
        Task ProduceMessage(string key, string value, Headers headers, string topic);
        Task ProduceMessageWithCustomHeaders(string key, string value, List<HeaderRequest> headers, string topic);
        Task<List<ConsumeResult<string, string>>> GetMessagesFromBeginning(string topic);
        Task<List<ConsumeResult<string, string>>> GetMessagesFromX(string topic, int x);
        Task<List<ConsumeResult<string, string>>> GetSpecificPages(string topic, int pageSize, int pageNumber);
        Task<IEnumerable<ConsumeResult<string, string>>> SearchByKeys(string topic, List<string> listOfKeys, int choice);
        Task<IEnumerable<ConsumeResult<string, string>>> SearchByHeaders(string topic, List<string> listOfPairs, int choice);
        Task<IEnumerable<ConsumeResult<string, string>>> SearchByTimeStamps(string topic, DateTime? time1, DateTime? time2);
        Task<IEnumerable<ConsumeResult<string, string>>> SearchByPartitions(string topic, int partition);
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
   