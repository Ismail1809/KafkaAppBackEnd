namespace KafkaAppBackEnd.Models
{
    public class TopicRequest
    {
        public string? Name { get; set; }
        public short ReplicationFactor { get; set; }
        public int Partitions { get; set; }
    }
}
