namespace SubSync
{
    public class XmlRpcString : XmlRpcValueObject
    {
        public string Value { get; }

        public XmlRpcString(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(XmlRpcString val)
        {
            return val.Value;
        }

        public override object GetValue()
        {
            return Value;
        }
    }
}