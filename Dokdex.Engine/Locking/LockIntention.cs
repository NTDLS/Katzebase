using System;
using static Dokdex.Engine.Constants;

namespace Dokdex.Engine.Locking
{
    public class LockIntention
    {
        public LockType Type { get; set; }
        public LockOperation Operation { get; set; }
        public string DiskPath { get; set; }

        public LockIntention()
        {
            DiskPath = string.Empty;
        }

        public string ObjectName
        {
            get
            {
                return string.Format("{0}:{1}", Type, DiskPath);
            }
        }

        public bool IsObjectEqual(LockIntention intention)
        {
            return (intention.Type == this.Type
                && intention.DiskPath == this.DiskPath);
        }

        public bool IsEqual(LockIntention intention)
        {
            return (intention.Type == this.Type
                && intention.Operation == this.Operation
                && intention.DiskPath == this.DiskPath);
        }
    }
}
