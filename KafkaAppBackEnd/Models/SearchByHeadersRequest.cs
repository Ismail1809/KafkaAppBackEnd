namespace KafkaAppBackEnd.Models
{
    public class SearchByHeadersRequest
    {
        public List<string>? ListOfStrings { get; set; }
        public string? Topic { get; set; }
        public int SearchOption { get; set; }
    }
}
