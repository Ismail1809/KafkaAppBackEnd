namespace KafkaAppBackEnd.Models
{
    public class SearchByHeadersRequest
    {
        public List<string>? ListOfKeys { get; set; }
        public string? Topic { get; set; }
        public int SearchOption { get; set; }
    }
}
