namespace SubSync
{
    public class XmlRpcArray : XmlRpcObject
    {
        public XmlRpcObject[] Items { get; }

        public XmlRpcArray(XmlRpcObject[] items)
        {
            Items = items;
        }

        public override XmlRpcObject FindRecursive(string name)
        {
            foreach (var item in Items)
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