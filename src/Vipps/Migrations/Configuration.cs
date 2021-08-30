namespace Vipps.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<Vipps.Polling.PollingEntityDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "Vipps.Polling.PollingEntityDbContext";
        }

        protected override void Seed(Vipps.Polling.PollingEntityDbContext context)
        {

        }
    }
}
