using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using SmartHomeWeb.Models.AppModels;

namespace SmartHomeWeb.Models.DBModels
{
    public class Sensor
    {
        [Key]
        public Guid Id { get; set; }
        [Required, StringLength(55)]
        public required string Name { get; set; }
        public eSensorsType Type { get; set; }
        [Required, StringLength(10)]
        public required string Unit { get; set; }
        [Required]
        public required Guid RoomId { get; set; }
        public double RecommendedValMin { get; set; }
        public double RecommendedValMax { get; set; }
        [Required, StringLength(25)]
        public required string DisplayText { get; set; }
        public int? PosX { get; set; }
        public int? PosY { get; set; }



        [ForeignKey(nameof(RoomId))]
        public required Room Room { get; set; }

        public ICollection<Measurement>? Measurements { get; set; }
    }
}
