namespace SubSync
{
    public abstract class XmlRpcValueObject : XmlRpcObjectBase, IXmlRpcObjectValue
    {
        public override XmlRpcObjectBase FindRecursive(string name)
        {
            return null;
        }

        public abstract object GetValue();
    }
}