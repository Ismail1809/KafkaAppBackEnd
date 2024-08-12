namespace KafkaAppBackEnd.Models
{
    public class UpdateConnectionRequest
    {
        public int ConnectionId { get; set; }
        public string? ConnectionName { get; set; }
        public string? BootStrapServer { get; set; }
    }
}
