namespace HouseofCat.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    public static class GenericTypeExtensions
    {
        private static readonly HashSet<Type> _numericTypes = new HashSet<Type>
        {
            typeof(int), typeof(double), typeof(decimal),
            typeof(long), typeof(short), typeof(sbyte),
            typeof(byte), typeof(ulong), typeof(ushort),
            typeof(uint), typeof(float), typeof(BigInteger)
        };

        /// <summary>
        /// Use this method for the general use case of is defined type a non-nullable numeric.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsNumeric<T>(this T input)
        {
            if (input is null) return false;

            return _numericTypes.Contains(typeof(T));
        }

        /// <summary>
        /// Handles situations where the type declaration maybe differ at runtime.
        /// <para>Use when object type maybe different from declared type i.e. (object)int</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsNumericAtRuntime<T>(this T input)
        {
            if (input is null) return false;

            return _numericTypes.Contains(input.GetType());
        }

        /// <summary>
        /// Identifies whether or not this object is a numeric or nullable numeric type.
        /// <para>Examples</para>
        /// <para />int value = 0; true
        /// <para />var objValue = (object)(int)0; true
        /// <para />int? value = 0; true
        /// <para />int? value = null; true
        /// <para />var objValue = (object)(int?)0; true
        /// <para />var objValue = (object)(int?)(null); false - because (int?) is totally lost.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsNullableNumeric<T>(this T input)
        {
            if (input is null)
            {
                return _numericTypes.Contains(Nullable.GetUnderlyingType(typeof(T))); // see what the inner base type is
            }

            return _numericTypes.Contains(input.GetType());
        }

        /// <summary>
        /// Allows you to enhance this extension's dataset by adding other object types that implement the same C# number interface conventions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_"></param>
        public static void AddCustomNumericType<T>(this T _) where T : IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable
        {
            _numericTypes.Add(typeof(T));
        }

        /// <summary>
        /// Allows you to enhance this extension's dataset by adding other number types that implement the same C# number interface conventions.
        /// <para>Returns true if the value was a numeric type and added/duplicate.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        public static bool TryAddCustomNumeric<T>(T input)
        {
            Type type;
            if (input is null)
            {
                type = Nullable.GetUnderlyingType(typeof(T));
                if (type is null) return false;
            }
            else
            { type = input.GetType(); }

            if (_numericTypes.Contains(type)) return true;

            var interfaces = type.GetInterfaces();
            var count = 0;

            for (var i = 0; i < interfaces.Length; i++)
            {
                switch (interfaces[i])
                {
                    case IComparable:
                    case IComparable<T>:
                    case IConvertible:
                    case IEquatable<T>:
                    case IFormattable:
                        count++;
                        break;
                    default: continue;
                }
            }

            if (count != 5) return false;

            _numericTypes.Add(type);
            return true;
        }

        /// <summary>
        /// Allows you to enhance this extension's dataset by adding other number types that conform to the C# number interface conventions.
        /// <para>Returns true if the value was a numeric type and added/duplicate.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool TryAddCustomNumericType<T>(Type type)
        {
            if (type is null) return false;

            if (_numericTypes.Contains(type)) return true;

            var interfaces = type.GetInterfaces();
            var count = 0;

            for (var i = 0; i < interfaces.Length; i++)
            {
                switch (interfaces[i])
                {
                    case IComparable:
                    case IComparable<T>:
                    case IConvertible:
                    case IEquatable<T>:
                    case IFormattable:
                        count++;
                        break;
                    default: continue;
                }
            }

            if (count != 5) return false;

            _numericTypes.Add(type);
            return true;
        }
    }
}
