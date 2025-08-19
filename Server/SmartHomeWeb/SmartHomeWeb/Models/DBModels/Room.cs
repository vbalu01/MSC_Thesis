using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SmartHomeWeb.Models.DBModels
{
    public class Room
    {
        [Key]
        public Guid Id { get; set; }
        [Required, StringLength(55)]
        public required string RoomName { get; set; }
        [Required]
        public Guid HouseId { get; set; }
        [Required, StringLength(45)]
        public required string Color { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int? SensorPosX { get; set; }
        public int? SensorPosY { get; set; }
        public byte IsPartRoom { get; set; } = 0;




        [ForeignKey(nameof(HouseId))]
        public required Home Home { get; set; }

        public ICollection<Sensor>? Sensors { get; set; }
    }
}
