using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Client.Models
{

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<FeedingEvent> FeedingEvents { get; set; }
        public DbSet<EventComment> EventComments { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<FeedingSchedule> FeedingSchedules { get; set; }
        public DbSet<FeedingStats> FeedingStats { get; set; }
        public DbSet<WarningLog> WarningsLog { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            var connectionString = config.GetConnectionString("DefaultConnection");
            options.UseMySql(
               connectionString,
               ServerVersion.AutoDetect(connectionString)
           );
        }
    }
}
