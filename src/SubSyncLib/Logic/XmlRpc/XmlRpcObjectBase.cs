using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SubSyncLib.Logic.XmlRpc
{
    public abstract class XmlRpcObjectBase
    {
        protected XmlRpcObjectBase()
        {
        }

        public static XmlRpcObject Parse(string data)
        {
            var doc = XDocument.Parse(data);
            if (doc.Root == null)
            {
                throw new ArgumentException("Argument is not a valid xml document.", nameof(data));
            }

            if (doc.Root.Name == "methodResponse")
            {
                return ParseMethodResponse(doc.Root);
            }

            return new XmlRpcObject();
        }

        private static XmlRpcObject ParseMethodResponse(XElement doc)
        {
            var values = doc.Element("params");
            if (values == null)
            {
                throw new Exception("Xmlrpc response is invalid, missing params element.");
            }

            var resultObject = new XmlRpcObject();
            var parameters = values.Elements("param");
            foreach (var param in parameters)
            {
                var result = ParseParam(param);
                resultObject.Add(result);
            }

            return resultObject;
        }


        public abstract XmlRpcObjectBase FindRecursive(string name);

        private static XmlRpcObjectBase ParseParam(XElement param)
        {
            return ParseValue(param.Element("value"));
        }

        private static XmlRpcObjectBase ParseValue(XElement value)
        {
            foreach (var elm in value.Elements())
            {
                if (elm.Name == "struct")
                {
                    return ParseStruct(elm);
                }
                if (elm.Name == "array")
                {
                    return ParseArray(elm);
                }
                if (elm.Name == "string")
                {
                    return ParseString(elm);
                }
                if (elm.Name == "double")
                {
                    return ParseDouble(elm);
                }
                if (elm.Name == "int")
                {
                    return ParseInt(elm);
                }
            }

            return ParseString(value);
        }

        private static XmlRpcInt ParseInt(XElement elm)
        {
            int.TryParse(elm.Value, out var value);
            return new XmlRpcInt(value);
        }

        private static XmlRpcDouble ParseDouble(XElement elm)
        {
            double.TryParse(elm.Value, out var value);
            return new XmlRpcDouble(value);
        }

        private static XmlRpcObjectBase ParseString(XElement elm)
        {
            return new XmlRpcString(elm.Value);
        }

        private static XmlRpcArray ParseArray(XElement elm)
        {
            var items = new List<XmlRpcObjectBase>();
            var dataElement = elm.Element("data");
            var elements = dataElement.Elements("value");
            foreach (var elmData in elements)
            {
                var item = ParseValue(elmData);
                items.Add(item);
            }

            return new XmlRpcArray(items.ToArray());
        }

        private static XmlRpcObjectBase ParseStruct(XElement elm)
        {
            var foundMembers = new List<XmlRpcMember>();
            var members = elm.Elements("member");
            foreach (var member in members)
            {
                var name = ParseValue(member.Element("name")) as XmlRpcString;
                var value = ParseValue(member.Element("value"));
                foundMembers.Add(new XmlRpcMember(name, value));
            }
            return new XmlRpcStruct(foundMembers);
        }

        public static T Deserialize<T>(string data)
        {
            // aint gonna write a xml parser today
            // 1. parse as xml or xdoc
            // 2. get properties of type T
            // 3. create instance of T
            // 4. assign all properties with values matching same name parsed from data.
            // 

            return Parse(data).Deserialize<T>();
        }
    }
}