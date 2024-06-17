using Confluent.Kafka;

namespace KafkaAppBackEnd.Models
{
    public class ConsumerGroupResponse
    {
        public string? Group { get; set; }
        public Error? Error { get; set; }
        public string? State { get; set; }
        public int? BrokerId { get; set; }
        public string? Host { get; set; }
        public int? Port { get; set; }
        public string? ProtocolType { get; set; }
        public string? Protocol { get; set; }
    }
}
