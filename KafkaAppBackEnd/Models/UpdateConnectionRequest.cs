namespace KafkaAppBackEnd.Models
{
    public class UpdateConnectionRequest
    {
        public int Id { get; set; }
        public string? ConnectionName { get; set; }
        public string? BootStrapServer { get; set; }
    }
}
