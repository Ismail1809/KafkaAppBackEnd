namespace KafkaAppBackEnd.Models
{
    public class ConnectionRequest
    {
        public string? ConnectionName { get; set; }
        public string? BootStrapServer { get; set; }
    }
}
