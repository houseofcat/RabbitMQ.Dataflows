using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace HouseofCat.Reflection;

public static class Generics
{
    /// <summary>
    /// A high performing New generic object instance creator.
    /// <para>Found this bit of cleverness on StackOverflow while dealing low performance using Generic instantiation.</para>
    /// <para>https://stackoverflow.com/questions/6582259/fast-creation-of-objects-instead-of-activator-createinstancetype</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("FormatterServices is no longer supported by Microsoft.")]
    public static class New<T>
    {
        public static readonly Func<T> Instance = Creator();

        private static Func<T> Creator()
        {
            Type t = typeof(T);
            if (t == typeof(string))
            { return Expression.Lambda<Func<T>>(Expression.Constant(string.Empty)).Compile(); }

            if (t.HasDefaultConstructor())
            { return Expression.Lambda<Func<T>>(Expression.New(t)).Compile(); }

            return () => (T)FormatterServices.GetUninitializedObject(t);
        }
    }

    public static bool HasDefaultConstructor(this Type t)
    {
        return t.IsValueType || t.GetConstructor(Type.EmptyTypes) != null;
    }
}
