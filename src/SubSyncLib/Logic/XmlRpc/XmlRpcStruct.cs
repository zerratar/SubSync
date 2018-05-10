using System.Collections.Generic;

namespace SubSyncLib.Logic.XmlRpc
{
    public class XmlRpcStruct : XmlRpcObjectBase
    {
        public List<XmlRpcMember> Members { get; }

        public XmlRpcStruct(List<XmlRpcMember> members)
        {
            Members = members;
        }

        public override XmlRpcObjectBase FindRecursive(string name)
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