using System.Collections.Generic;

namespace SubSync
{
    public class XmlRpcRoot : XmlRpcObject
    {
        internal readonly List<XmlRpcObject> children = new List<XmlRpcObject>();

        internal void Add(XmlRpcObject child)
        {
            children.Add(child);
        }

        public T GetValue<T>(string name)
        {
            if (this.children.Count > 0 && !string.IsNullOrEmpty(name))
            {
                foreach (var child in children)
                {
                    var node = child.FindRecursive(name);
                    if (node is XmlRpcMember member)
                    {
                        node = member.Value;
                    }
                    if (node is IXmlRpcObjectValue valueNode)
                    {
                        return (T)valueNode.GetValue();
                    }
                }
            }
            return default(T);
        }

        public override XmlRpcObject FindRecursive(string name)
        {
            foreach (var child in children)
            {
                var found = child.FindRecursive(name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}