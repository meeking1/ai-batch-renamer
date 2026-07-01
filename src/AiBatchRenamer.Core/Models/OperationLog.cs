using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AiBatchRenamer.Core.Models
{
    [DataContract]
    public class OperationLog
    {
        public OperationLog()
        {
            Items = new List<OperationLogItem>();
        }

        [DataMember(Order = 1)]
        public string OperationId { get; set; }

        [DataMember(Order = 2)]
        public string CreatedAt { get; set; }

        [DataMember(Order = 3)]
        public List<OperationLogItem> Items { get; set; }
    }

    [DataContract]
    public class OperationLogItem
    {
        [DataMember(Order = 1)]
        public string OldPath { get; set; }

        [DataMember(Order = 2)]
        public string NewPath { get; set; }

        [DataMember(Order = 3)]
        public string Status { get; set; }

        [DataMember(Order = 4)]
        public string Message { get; set; }
    }
}
