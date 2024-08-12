namespace KafkaAppBackEnd.Models
{
    public class GetTopicSizeResponse
    {
        public string? Name { get; set; }
        public List<KafkaTopicPartition>? Partitions { get; set; }
    }
}
