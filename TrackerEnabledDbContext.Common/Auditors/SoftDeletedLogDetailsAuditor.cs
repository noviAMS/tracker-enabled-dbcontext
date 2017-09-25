using System.Data.Entity.Infrastructure;
using TrackerEnabledDbContext.Common.Models;

namespace TrackerEnabledDbContext.Common.Auditors
{
    public class SoftDeletedLogDetailsAuditor : ChangeLogDetailsAuditor
    {
        public SoftDeletedLogDetailsAuditor(DbEntityEntry dbEntry, AuditLog log, DbPropertyValues dbEntryDbValues) : base(dbEntry, log, dbEntryDbValues)
        {
        }
    }
}
