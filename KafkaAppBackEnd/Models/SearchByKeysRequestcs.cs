namespace KafkaAppBackEnd.Models
{
    public class SearchByKeysRequestcs
    {
        public List<string>? ListOfKeys { get; set; }
        public string? Topic { get; set; }
        public int SearchOption { get; set; }
    }
}
