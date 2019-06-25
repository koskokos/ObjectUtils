using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MiscellaneousUtils
{
    public static class EnvironmentHelper
    {
        public static T Read<T>() where T : new()
        {
            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.SetProperty);

            if (props.Any(p => p.PropertyType != typeof(string)))
            {
                throw new ArgumentException("T must contain only string properties");
            }

            var res = new T();

            foreach (var item in props)
            {
                var shi = Regex.Replace(item.Name, @"[A-Z]", s => "_" + s.ToString().ToLower()).TrimStart('_');
                var v = Environment.GetEnvironmentVariable(shi);
                if (!string.IsNullOrEmpty(v))
                {
                    item.SetValue(res, v);
                }
                else if (item.GetValue(res) == null)
                {
                    throw new ApplicationException($"Environment variable {shi} is not defined");
                }
            }

            return res;
        }
    }
}
