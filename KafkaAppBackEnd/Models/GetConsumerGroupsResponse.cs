using Confluent.Kafka;

namespace KafkaAppBackEnd.Models
{
    public class GetConsumerGroupsResponse
    {
        public string? Group { get; set; }
        public int? Members { get; set; }
        public Error? Error { get; set; }
        public string? State { get; set; }
        public int? BrokerId { get; set; }
        public string? Host { get; set; }
        public int? Port { get; set; }
        public List<string>? AssignedTopics { get; set; }
        public long? OverallLag { get; set; }
    }
}
