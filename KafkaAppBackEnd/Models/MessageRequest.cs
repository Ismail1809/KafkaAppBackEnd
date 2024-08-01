using Confluent.Kafka;

namespace KafkaAppBackEnd.Models
{
    public class MessageRequest
    {
        public string Topic { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public List<HeaderRequest> Headers { get; set; }
    }
    public class HeaderRequest
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
