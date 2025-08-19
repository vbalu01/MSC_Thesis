using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SmartHomeWeb.Models.AppModels;

namespace SmartHomeWeb.Models.DBModels
{
    public class UserHome
    {
        [Key, Column(Order = 0)]
        [StringLength(75)]
        public required string UserId { get; set; }
        [Key, Column(Order = 1)]
        public Guid HomeId { get; set; }
        public eHomePermission Role { get; set; }



        [ForeignKey(nameof(UserId))]
        public required User User { get; set; }
        [ForeignKey(nameof(HomeId))]
        public required Home Home { get; set; }
    }
}
