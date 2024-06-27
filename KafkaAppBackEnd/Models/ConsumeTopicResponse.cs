using Avro;
using Confluent.Kafka;

namespace KafkaAppBackEnd.Models
{
    public class ConsumeTopicResponse
    {
        public Message<string, string>? Message { get; set; }
        public int Partition { get; set; }
        public long Offset { get; set; }
        public string? HeaderValue { get; set; }
    }
}
