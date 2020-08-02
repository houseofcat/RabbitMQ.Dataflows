using System.Linq;
using System.Reflection;

namespace HouseofCat.Reflection
{
    public static class Constructor
    {
        public static T Construct<T>(params object[] p)
        {
            var ctor = typeof(T)
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(c => c)
                .Where(c => c.GetParameters().Length == p.Length)
                .FirstOrDefault();

            return (T)ctor?.Invoke(p);
        }
    }
}
