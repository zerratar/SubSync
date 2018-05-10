namespace SubSyncLib.Logic.XmlRpc
{
    public class XmlRpcArray : XmlRpcObjectBase
    {
        public XmlRpcObjectBase[] Items { get; }

        public XmlRpcArray(XmlRpcObjectBase[] items)
        {
            Items = items;
        }

        public override XmlRpcObjectBase FindRecursive(string name)
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