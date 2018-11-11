using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Migration.WIContract;

namespace Migration.Common
{
    public class AbortMigrationException : Exception 
    {
        public AbortMigrationException(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; private set; }
        public WiRevision Revision { get; set; }
    }
}
