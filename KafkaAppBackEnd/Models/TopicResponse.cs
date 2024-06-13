using Confluent.Kafka;

namespace KafkaAppBackEnd.Models
{
    public class TopicResponse
    {
        public string? Topic { get; set; }
        public int? Partitions { get; set; }
        public Error? Error { get; set; }
        public string? Type { get; set; }
    }
}
