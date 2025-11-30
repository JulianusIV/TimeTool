using Microsoft.EntityFrameworkCore;

namespace TimeTool
{
    internal class SupaContext : DbContext
    {
        public DbSet<Timezone> Timezones { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
            => optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("CONNECTION_STRING"));
    }
}
