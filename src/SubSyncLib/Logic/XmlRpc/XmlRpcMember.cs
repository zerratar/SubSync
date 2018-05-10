using System;

namespace SubSyncLib.Logic.XmlRpc
{
    public class XmlRpcMember : XmlRpcObjectBase
    {
        public XmlRpcString Name { get; }
        public XmlRpcObjectBase Value { get; }

        public XmlRpcMember(XmlRpcString name, XmlRpcObjectBase value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return "'" + Name + "' => '" + Value + "'";
        }

        public override XmlRpcObjectBase FindRecursive(string name)
        {
            if (name.Equals(this.Name.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return this;
            }

            return null;
        }
    }
}