using System.ComponentModel.DataAnnotations;

namespace SmartHomeWeb.Models.DBModels
{
    public class User
    {
        [Key]
        [StringLength(75)]
        public required string Email { get; set; }
        [Required, StringLength(155)]
        public required string Password { get; set; }
        [Required, StringLength(30)]
        public required string FName { get; set; }
        [Required, StringLength(30)]
        public required string LName { get; set; }



        public ICollection<UserHome>? UserHomes { get; set; }
    }
}
