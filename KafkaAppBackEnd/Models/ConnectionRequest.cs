﻿namespace KafkaAppBackEnd.Models
{
    public class ConnectionRequest
    {
        public int Id { get; set; }
        public string? ConnectionName { get; set; }
        public string? BootStrapServer { get; set; }
    }
}
