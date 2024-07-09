using System.ComponentModel.DataAnnotations;

namespace KafkaAppBackEnd.Models
{
    public class PublishBatchFromFile
    {
        public string? Topic { get; set; }
        public string? Separator { get; set; }
        public string? Key { get; set; }
        public int? partitionId { get; set; }
    }
}
