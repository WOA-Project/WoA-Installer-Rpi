using System;

namespace Installer.Core
{
    public class PartitionType
    {
        private const string EspGuid = "C12A7328-F81F-11D2-BA4B-00A0C93EC93B";
        private const string BasicGuid = "EBD0A0A2-B9E5-4433-87C0-68B6B72699C7";
        private const string ReservedGuid = "ebd0a0a2-b9e5-4433-87c0-68b6b72699c7";

        public Guid Guid { get; }

        public static PartitionType Reserved = new PartitionType(Guid.Parse(ReservedGuid));
        public static PartitionType Esp = new PartitionType(Guid.Parse(EspGuid));
        public static PartitionType Basic = new PartitionType(Guid.Parse(BasicGuid));

        private PartitionType(Guid guid)
        {
            Guid = guid;
        }

        public static PartitionType FromGuid(Guid guid)
        {
            return new PartitionType(guid);
        }

        protected bool Equals(PartitionType other)
        {
            return Guid.Equals(other.Guid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((PartitionType) obj);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }
}