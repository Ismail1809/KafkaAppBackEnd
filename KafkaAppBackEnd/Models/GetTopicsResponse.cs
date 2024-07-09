using Confluent.Kafka;

namespace KafkaAppBackEnd.Models
{
    public class GetTopicsResponse
    {
        public string? Name { get; set; }
        public Uuid? TopicId { get; set; }
        public List<TopicPartitionInfo>? Partitions { get; set; }
        public Error? Error { get; set; }
        public bool? IsInternal { get; set; }
    }
}
