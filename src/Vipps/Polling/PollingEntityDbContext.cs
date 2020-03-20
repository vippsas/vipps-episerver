using System.Data.Entity;

namespace Vipps.Polling
{
    public class PollingEntityDbContext : DbContext
    {
        public PollingEntityDbContext() : base("EcfSqlConnection")
        {
            
        }

        public DbSet<VippsPollingEntity> PollingEntities { get; set; }
    }
}
