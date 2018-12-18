using System;
using System.Runtime.Serialization;
using Migration.WIContract;

namespace Migration.Common
{
    [Serializable]
    public class AbortMigrationException : Exception 
    {
        protected AbortMigrationException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {

        }

        public AbortMigrationException(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; private set; }
        public WiRevision Revision { get; set; }
    }
}