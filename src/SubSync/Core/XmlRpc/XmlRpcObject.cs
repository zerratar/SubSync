using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace SubSync
{
    public class XmlRpcObject : XmlRpcObjectBase
    {
        internal readonly List<XmlRpcObjectBase> children = new List<XmlRpcObjectBase>();

        internal void Add(XmlRpcObjectBase child)
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

        public override XmlRpcObjectBase FindRecursive(string name)
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

        public T Deserialize<T>()
        {
            var dataRoot = "data";
            var type = typeof(T);
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return DeserializeArray<T>(dataRoot, elementType, elementType.GetProperties());
            }

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                return DeserializeGeneric<T>(dataRoot, genericTypeDefinition, genericTypeDefinition.GetProperties());
            }

            return Deserialize<T>(dataRoot, type.GetProperties(BindingFlags.Public));
        }


        private T Deserialize<T>(string dataRoot, PropertyInfo[] properties)
        {
            throw new NotImplementedException();
            return default(T);
        }

        private T DeserializeGeneric<T>(string dataRoot, Type genericType, PropertyInfo[] properties)
        {
            throw new NotImplementedException();
            return default(T);
        }

        private T DeserializeArray<T>(string dataRoot, Type elementType, PropertyInfo[] properties)
        {
            dynamic list = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            if (FindRecursive(dataRoot) is XmlRpcMember member)
            {
                if (member.Value is XmlRpcArray array)
                {
                    foreach (var item in array.Items)
                    {
                        dynamic value = Deserialize(elementType, item, properties);
                        list.Add(value);
                    }
                }
            }

            return list.ToArray();
        }

        private object Deserialize(Type targetType, XmlRpcObjectBase item, PropertyInfo[] properties)
        {
            if (item is XmlRpcStruct strct)
            {
                return DeserializeStruct(targetType, strct, properties);
            }

            // todo: implement any other types that may be supported like serialize as strings, integers, doubles, etc
            throw new NotImplementedException();

            return default(object);
        }

        private object DeserializeStruct(Type structType, XmlRpcStruct structData, PropertyInfo[] properties)
        {
            var instance = Activator.CreateInstance(structType);
            foreach (var prop in properties)
            {
                if (structData.FindRecursive(prop.Name) is XmlRpcMember member)
                {
                    if (member.Value is IXmlRpcObjectValue val)
                    {
                        dynamic v = val.GetValue();
                        prop.SetValue(instance, v);
                    }
                }
            }
            return instance;
        }
    }
}