namespace Intaller.Wpf.ViewModels
{
    public class PhoneModel
    {
        public static readonly PhoneModel Lumia950 = new PhoneModel(PhoneType.Lumia950, "Microsoft Lumia 950");
        public static readonly PhoneModel Lumia950Xl = new PhoneModel(PhoneType.Lumia950Xl, "Microsoft Lumia 950 XL");

        private PhoneModel(PhoneType phoneType, string name)
        {
            PhoneType = phoneType;
            Name = name;
        }

        public PhoneType PhoneType { get; }
        public string Name { get; }

        protected bool Equals(PhoneModel other)
        {
            return PhoneType == other.PhoneType;
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

            return Equals((PhoneModel) obj);
        }

        public override int GetHashCode()
        {
            return (int) PhoneType;
        }
    }

    public enum PhoneType
    {
        Lumia950Xl,
        Lumia950
    }
}