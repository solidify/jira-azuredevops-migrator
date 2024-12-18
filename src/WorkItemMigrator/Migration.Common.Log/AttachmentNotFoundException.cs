using System;
using System.Runtime.Serialization;

namespace Migration.Common.Log
{
    [Serializable]
    public class AttachmentNotFoundException : Exception
    {
        protected AttachmentNotFoundException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {

        }

        public AttachmentNotFoundException(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; private set; }
    }
}