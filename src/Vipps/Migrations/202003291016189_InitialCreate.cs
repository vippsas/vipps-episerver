namespace Vipps.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.VippsPollingEntities",
                c => new
                    {
                        OrderId = c.String(nullable: false, maxLength: 128),
                        ContactId = c.Guid(nullable: false),
                        CartName = c.String(),
                        MarketId = c.String(),
                        Created = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.OrderId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.VippsPollingEntities");
        }
    }
}
