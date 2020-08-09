using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace HouseofCat.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Extension method of getting the Description value to string.
        /// </summary>
        /// <param name="value"></param>
        public static string Description(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0
                ? ((DescriptionAttribute)attributes[0]).Description
                : string.Empty;
        }
    }
}
