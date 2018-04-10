using System.Collections.Generic;

namespace SubSync
{
    public class XmlRpcStruct : XmlRpcObject
    {
        public List<XmlRpcMember> Members { get; }

        public XmlRpcStruct(List<XmlRpcMember> members)
        {
            Members = members;
        }

        public override XmlRpcObject FindRecursive(string name)
        {
            foreach (var item in Members)
            {
                var found = item.FindRecursive(name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }
    }
}