using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Migration.Common
{
    [Serializable]
    public class FieldMapping<TRevision> : Dictionary<string, Func<TRevision, (bool, object)>> where TRevision : ISourceRevision
    {
        public FieldMapping()
        {

        }

        protected FieldMapping(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {

        }
    } 
}