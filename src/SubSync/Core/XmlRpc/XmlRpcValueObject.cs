namespace SubSync
{
    public abstract class XmlRpcValueObject : XmlRpcObject, IXmlRpcObjectValue
    {
        public override XmlRpcObject FindRecursive(string name)
        {
            return null;
        }

        public abstract object GetValue();
    }
}