namespace Intaller.Wpf
{
    public class PhoneModel
    {
        public int Id { get; }
        public string Name { get; }
        public static PhoneModel Lumia950 = new PhoneModel(1, "Microsoft Lumia 950");
        public static PhoneModel Lumia950Xl = new PhoneModel(2, "Microsoft Lumia 950 XL");
        
        private PhoneModel(int id, string name)
        {
            Id = id;
            Name = name;
        }

        protected bool Equals(PhoneModel other)
        {
            return Id == other.Id;
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
            return Id;
        }
    }
}