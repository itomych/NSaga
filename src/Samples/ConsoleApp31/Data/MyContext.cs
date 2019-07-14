using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSaga.SqlServer.Model;

namespace ConsoleApp31
{
    public class MyContext : DbContext
    {
        public DbSet<SagaData> SagaData { get; set; }
        public DbSet<SagaHeaders> SagaHeaders { get; set; }

        public MyContext(DbContextOptions options) : base(options)
        {
        }

        public static readonly LoggerFactory _myLoggerFactory =
            new LoggerFactory(new[] {
            new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider()
            });

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(_myLoggerFactory);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SagaData>()
                .HasKey(i => i.CorrelationId);

            modelBuilder.Entity<SagaData>()
              .Property(i => i.CorrelationId)
              .ValueGeneratedOnAdd();

            modelBuilder.Entity<SagaHeaders>()
                .HasKey(i => i.CorrelationId);

            modelBuilder.Entity<SagaHeaders>()
              .Property(i => i.CorrelationId)
              .ValueGeneratedOnAdd();
        }
    }
}
