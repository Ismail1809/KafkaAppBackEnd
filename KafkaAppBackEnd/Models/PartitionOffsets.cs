namespace KafkaAppBackEnd.Models
{
    public class PartitionOffsets
    {
        public int Partition { get; set; }
        public long Offset { get; set; }
    }
}
