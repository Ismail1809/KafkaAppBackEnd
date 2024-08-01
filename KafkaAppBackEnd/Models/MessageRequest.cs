using Confluent.Kafka;

namespace KafkaAppBackEnd.Models
{
    public class MessageRequest
    {
        public string Topic { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public Headers Headers { get; set; }
    }
}
