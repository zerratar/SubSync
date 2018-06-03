using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SubSyncLib
{
    public static class Arguments
    {
        public static T Parse<T>(string[] args) where T : new()
        {
            args = ApplyQuoteBugFix(args);

            var settings = new T();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var arg = property.GetCustomAttribute<StartupArgumentAttribute>();
                if (arg != null)
                {
                    var argName = $"-{arg.ArgumentName}";
                    var isFlag = property.PropertyType == typeof(bool);
                    if (isFlag)
                    {
                        var index = Array.FindIndex(args, x => x.EndsWith(argName, StringComparison.OrdinalIgnoreCase));
                        if (index != -1)
                        {
                            SetArgumentValue(property, settings, true);
                        }
                        continue;
                    }

                    if (arg.UseArgumentName)
                    {
                        var index = Array.FindIndex(args, x => x.EndsWith(argName, StringComparison.OrdinalIgnoreCase));
                        if (index == -1 || index + 1 >= args.Length)
                        {
                            if (!string.IsNullOrEmpty(arg.DefaultValue))
                            {
                                SetArgumentValue(property, settings, arg.DefaultValue);
                            }
                            continue;
                        }

                        SetArgumentValueByIndex(args, index + 1, property, ref settings);
                    }
                    else
                    {
                        if (arg.ArgumentIndex >= args.Length)
                        {
                            if (!string.IsNullOrEmpty(arg.DefaultValue))
                            {
                                SetArgumentValue(property, settings, arg.DefaultValue);
                            }
                            continue;
                        }

                        SetArgumentValueByIndex(args, arg.ArgumentIndex, property, ref settings);
                    }
                }
            }

            return settings;
        }

        private static string[] ApplyQuoteBugFix(string[] args)
        {
            var newArgs = new List<string>();
            foreach (var arg in args)
            {
                string tmpArg = "";
                if (arg.Contains("\""))
                {
                    // anything afterwards will be part of the string, so we have to break up this string
                    // by its spaces.
                    var values = arg.Split('"');
                    newArgs.Add(values[0]);
                    tmpArg = values[1];
                }
                else if (arg.Contains("'"))
                {
                    var values = arg.Split('\'');
                    newArgs.Add(values[0]);
                    tmpArg = values[1];
                }
                else newArgs.Add(arg);

                if (!string.IsNullOrEmpty(tmpArg))
                {
                    newArgs.AddRange(tmpArg.Split(' '));
                }
            }

            return newArgs.ToArray();
        }

        private static void SetArgumentValueByIndex<T>(
            string[] args,
            int index,
            PropertyInfo property,
            ref T settings) where T : new()
        {
            var value = args[index];
            SetArgumentValue(property, settings, value);
        }

        private static void SetArgumentValue<T>(PropertyInfo property, T settings, string value)
        {
            if (property.PropertyType == typeof(int))
            {
                if (int.TryParse(value, out var v))
                    property.SetValue(settings, v);
            }
            else if (property.PropertyType == typeof(bool))
            {
                if (bool.TryParse(value, out var v))
                    property.SetValue(settings, v);
            }
            else if (property.PropertyType == typeof(string))
            {
                property.SetValue(settings, value);
            }
            else if (property.PropertyType == typeof(HashSet<string>))
            {
                property.SetValue(settings, ParseList(value));
            }
            else
            {
                try
                {
                    var tc = TypeDescriptor.GetConverter(property.PropertyType);
                    var objValue = tc.ConvertFromString(null, CultureInfo.InvariantCulture, value);
                    property.SetValue(settings, objValue, null);
                }
                catch { }
            }
        }

        private static void SetArgumentValue<T>(PropertyInfo property, T settings, object value)
        {
            property.SetValue(settings, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static HashSet<string> ParseList(string s)
        {
            return new HashSet<string>(s
                .Split(';')
                .Select(x => x.Trim())
                .Select(x => x.StartsWith("*") ? x.Substring(1) : x));
        }
    }

    public class StartupArgumentAttribute : Attribute
    {
        public StartupArgumentAttribute(string argName, string defaultValue = null)
        {
            this.ArgumentName = argName;
            DefaultValue = defaultValue;
        }

        public StartupArgumentAttribute(int argIndex, string defaultValue = null)
        {
            this.ArgumentIndex = argIndex;
            DefaultValue = defaultValue;
        }

        public int ArgumentIndex { get; set; } = -1;
        public string DefaultValue { get; }
        public string ArgumentName { get; set; }
        public bool UseArgumentName => !string.IsNullOrEmpty(ArgumentName) || ArgumentIndex == -1;
    }
}