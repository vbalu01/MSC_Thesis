using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SmartHomeWeb.Models.DBModels
{
    public class Measurement
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public required Guid SensorId { get; set; }
        public DateTime Time { get; set; }
        public double Value { get; set; }



        [ForeignKey(nameof(SensorId))]
        public required Sensor Sensor { get; set; }
    }
}
