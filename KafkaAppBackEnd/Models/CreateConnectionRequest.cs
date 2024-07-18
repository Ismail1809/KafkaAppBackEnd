namespace KafkaAppBackEnd.Models
{
    public class CreateConnectionRequest
    {
        public string? ConnectionName { get; set; }
        public string? BootStrapServer { get; set; }
    }
}
