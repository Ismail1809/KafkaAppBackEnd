using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KafkaAppBackEnd.Models
{
    public class Connection
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int ConnectionId { get; set; }
        public string? ConnectionName { get; set; }
        public string? BootStrapServer { get; set; }
    }
}
