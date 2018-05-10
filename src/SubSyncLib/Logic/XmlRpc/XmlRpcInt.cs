namespace SubSyncLib.Logic.XmlRpc
{
    public class XmlRpcInt : XmlRpcValueObject
    {
        public int Value { get; }

        public XmlRpcInt(int value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator int(XmlRpcInt val)
        {
            return val.Value;
        }

        public override object GetValue()
        {
            return Value;
        }
    }
}