using Confluent.Kafka;

namespace KafkaAppBackEnd.Models
{
    public class GetTopicResponse
    {
        public string? Name { get; set; }
        public Uuid? TopicId { get; set; }
        public List<KafkaTopicPartition>? Partitions { get; set; }
        public Error? Error { get; set; }
        public bool? IsInternal { get; set; }
        public int ReplicationFactor { get; internal set; }
        public int RecordsCount { get; set; }
    }

    public class KafkaTopicPartition
    {
        public string? PartitionNumber { get; set; }
        public int? Size { get; set; }
    }
}
