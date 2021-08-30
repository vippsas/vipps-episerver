using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Vipps.Polling;

namespace Vipps.Extensions
{
    public static class PollingEntityDbContextExtensions
    {
        public static int SaveChangesDatabaseWins(this PollingEntityDbContext context, int retries = 1)
        {
            return Save(context, retries, (e) => e.Reload());
        }

        public static int SaveChangesClientWins(this PollingEntityDbContext context, int retries = 1)
        {
            return Save(context, retries, (e) => e.OriginalValues.SetValues(e.GetDatabaseValues()));
        }

        private static int Save(PollingEntityDbContext context, int retries, Action<DbEntityEntry> resolver)
        {
            DbUpdateConcurrencyException exception;
            bool saveFailed;
            
            do
            {
                saveFailed = false;

                try
                {
                    return context.SaveChanges();                    
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    exception = ex;
                    saveFailed = true;

                    if (retries-- < 1)
                        throw exception;

                    var entries = ex.Entries.ToArray();

                    foreach (var entry in entries)
                    {
                        resolver(entry);
                    }
                }

            } while (saveFailed);

            throw exception;
        }
    }
}
