namespace KafkaAppBackEnd.Models
{
    public class PublishBatchRequest
    {
        public List<string>? ListOfMessages { get; set; }
        public string? Topic { get; set; }
        public string? Key { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public int? partitionId { get; set; }
    }
}
