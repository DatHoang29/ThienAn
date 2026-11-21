using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Utility.Helpers
{
    public class ListHelper
    {
        public static List<T> CreateList<T>(params T[] items)
        {
            return new List<T>(items);
        }

        //return value in Dictionary with key 


    }

    public static class ListStaticHelper
    {
        public static object? GetDictionaryValue(this Dictionary<string, object> dictionary, string key)
        {

            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }

            return null;
        }

        public static T? GetDictionaryValue<T>(this Dictionary<string, object> dictionary, string key)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                if (value is T castedValue) // Direct cast if already the correct type
                    return castedValue;

                try
                {
                    if (typeof(T).IsEnum) // Handle enums separately
                        return (T?)Enum.Parse(typeof(T), value.ToString() ?? "", true);

                    return (T?)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default; // Return default(T) in case of failure
                }
            }
            return default;
        }

        public static string GetEnumDescription<T>(this T enumValue) where T : Enum
        {
            FieldInfo? field = enumValue.GetType().GetField(enumValue.ToString());
        
            if (field != null)
            {
                DescriptionAttribute? attribute = field.GetCustomAttribute<DescriptionAttribute>();
                return attribute?.Description ?? enumValue.ToString();
            }
        
            return enumValue.ToString(); // Fallback to enum name if no description is found
        }
        

    }
   
}
