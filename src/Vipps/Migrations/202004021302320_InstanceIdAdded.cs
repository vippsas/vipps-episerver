namespace Vipps.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InstanceIdAdded : DbMigration
    {
        public override void Up()
        {
            AddColumn(
                "dbo.VippsPollingEntities",
                "InstanceId",
                c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.VippsPollingEntities", "InstanceId");
        }
    }
}
