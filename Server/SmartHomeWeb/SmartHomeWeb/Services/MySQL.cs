using Microsoft.EntityFrameworkCore;
using SmartHomeWeb.Models.DBModels;
using System;

namespace SmartHomeWeb.Services
{
    public class MySQL : DbContext
    {
        public MySQL(DbContextOptions<MySQL> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserHome> UserHomes { get; set; }
        public DbSet<Home> Homes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Sensor> Sensors { get; set; }
        public DbSet<Measurement> Measurements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserHome>()
                .HasKey(uh => new { uh.UserId, uh.HomeId });

            base.OnModelCreating(modelBuilder);
        }
    }
}
