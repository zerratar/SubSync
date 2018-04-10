namespace SubSync
{
    public class XmlRpcMember : XmlRpcObject
    {
        public XmlRpcString Name { get; }
        public XmlRpcObject Value { get; }

        public XmlRpcMember(XmlRpcString name, XmlRpcObject value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return "'" + Name + "' => '" + Value + "'";
        }

        public override XmlRpcObject FindRecursive(string name)
        {
            if (name == this.Name.ToString())
            {
                return this;
            }

            return null;
        }
    }
}