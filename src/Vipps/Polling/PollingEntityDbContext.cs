using System.Data.Entity;

namespace Vipps.Polling
{
    public class PollingEntityDbContext : DbContext
    {
        public PollingEntityDbContext() : base("EcfSqlConnection")
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<PollingEntityDbContext, Vipps.Migrations.Configuration>());
        }

        public DbSet<VippsPollingEntity> PollingEntities { get; set; }
    }
}
