namespace SubSync
{
    public class XmlRpcDouble : XmlRpcValueObject
    {
        public double Value { get; }

        public XmlRpcDouble(double value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator double(XmlRpcDouble val)
        {
            return val.Value;
        }

        public override object GetValue()
        {
            return Value;
        }
    }
}