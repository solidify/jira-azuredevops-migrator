using System;
using System.Collections.Generic;

namespace Migration.Common
{
    public class FieldMapping<TRevision> : Dictionary<string, Func<TRevision, (bool, object)>> where TRevision : ISourceRevision { } 
}