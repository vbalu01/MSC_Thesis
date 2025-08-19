using System.ComponentModel.DataAnnotations;

namespace SmartHomeWeb.Models.DBModels
{
    public class Home
    {
        [Key]
        public Guid Id { get; set; }
        [Required, StringLength(55)]
        public required string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }



        public ICollection<UserHome>? UserHomes { get; set; }
        public ICollection<Room>? Rooms { get; set; }
    }
}
