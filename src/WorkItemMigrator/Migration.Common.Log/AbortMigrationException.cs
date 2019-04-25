using System;
using System.Runtime.Serialization;

namespace Migration.Common.Log
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
    }
}