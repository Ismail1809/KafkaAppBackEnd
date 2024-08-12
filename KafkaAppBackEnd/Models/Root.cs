namespace KafkaAppBackEnd.Models
{
    public class KafkaBroker
    {
        public int broker { get; set; }
        public List<KafkaLogDir> logDirs { get; set; }
    }

    public class KafkaLogDir
    {
        public string logDir { get; set; }
        public object error { get; set; }
        public List<LogPartition> partitions { get; set; }
    }

    public class LogPartition
    {
        public string partition { get; set; }
        public int size { get; set; }
        public int offsetLag { get; set; }
        public bool isFuture { get; set; }
    }

    public class Root
    {
        public int version { get; set; }
        public List<KafkaBroker> brokers { get; set; }
    }
}
