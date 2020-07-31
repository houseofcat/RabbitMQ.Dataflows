using System;
using System.ComponentModel;

namespace HouseofCat.RabbitMQ
{
    public static class Enums
    {
        /// <summary>
        /// Allows for quickling setting ContentType for RabbitMQ IBasicProperties.
        /// </summary>
        public enum ContentType
        {
            /// <summary>
            /// ContentType.Javascript
            /// </summary>
            [Description("application/javascript;")]
            Javascript,

            /// <summary>
            /// ContentType.Json
            /// </summary>
            [Description("application/json;")]
            Json,

            /// <summary>
            /// ContentType.Urlencoded
            /// </summary>
            [Description("application/x-www-form-urlencoded;")]
            Urlencoded,

            /// <summary>
            /// ContentType.Xml
            /// </summary>
            [Description("application/xml;")]
            Xml,

            /// <summary>
            /// ContentType.Zip
            /// </summary>
            [Description("application/zip;")]
            Zip,

            /// <summary>
            /// ContentType.Pdf
            /// </summary>
            [Description("application/pdf;")]
            Pdf,

            /// <summary>
            /// ContentType.Sql
            /// </summary>
            [Description("application/sql;")]
            Sql,

            /// <summary>
            /// ContentType.Graphql
            /// </summary>
            [Description("application/graphql;")]
            Graphql,

            /// <summary>
            /// ContentType.Ldjson
            /// </summary>
            [Description("application/ld+json;")]
            Ldjson,

            /// <summary>
            /// ContentType.Msword
            /// </summary>
            [Description("application/msword(.doc);")]
            Msword,

            /// <summary>
            /// ContentType.Openword
            /// </summary>
            [Description("application/vnd.openxmlformats-officedocument.wordprocessingml.document(.docx);")]
            Openword,

            /// <summary>
            /// ContentType.Excel
            /// </summary>
            [Description("application/vnd.ms-excel(.xls);")]
            Excel,

            /// <summary>
            /// ContentType.Openexcel
            /// </summary>
            [Description("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet(.xlsx);")]
            Openexcel,

            /// <summary>
            /// ContentType.Powerpoint
            /// </summary>
            [Description("application/vnd.ms-powerpoint(.ppt);")]
            Powerpoint,

            /// <summary>
            /// ContentType.Openpowerpoint
            /// </summary>
            [Description("application/vnd.openxmlformats-officedocument.presentationml.presentation(.pptx);")]
            Openpowerpoint,

            /// <summary>
            /// ContentType.Opendocument
            /// </summary>
            [Description("application/vnd.oasis.opendocument.text(.odt);")]
            Opendocument,

            /// <summary>
            /// ContentType.Audiompeg
            /// </summary>
            [Description("audio/mpeg;")]
            Audiompeg,

            /// <summary>
            /// ContentType.Audiovorbis
            /// </summary>
            [Description("audio/vorbis;")]
            Audiovorbis,

            /// <summary>
            /// ContentType.Multiformdata
            /// </summary>
            [Description("multipart/form-data;")]
            Multiformdata,

            /// <summary>
            /// ContentType.Textcss
            /// </summary>
            [Description("text/css;")]
            Textcss,

            /// <summary>
            /// ContentType.Texthtml
            /// </summary>
            [Description("text/html;")]
            Texthtml,

            /// <summary>
            /// ContentType.Textcsv
            /// </summary>
            [Description("text/csv;")]
            Textcsv,

            /// <summary>
            /// ContentType.Textplain
            /// </summary>
            [Description("text/plain;")]
            Textplain,

            /// <summary>
            /// ContentType.Png
            /// </summary>
            [Description("image/png;")]
            Png,

            /// <summary>
            /// ContentType.Jpeg
            /// </summary>
            [Description("image/jpeg;")]
            Jpeg,

            /// <summary>
            /// ContentType.Gif
            /// </summary>
            [Description("image/gif;")]
            Gif
        }

        /// <summary>
        /// Allows for quickling combining Charset with ContentType for RabbitMQ IBasicProperties.
        /// </summary>
        public enum Charset
        {
            /// <summary>
            /// Charset.Utf8
            /// </summary>
            [Description("charset=utf-8")]
            Utf8,

            /// <summary>
            /// Charset.Utf16
            /// </summary>
            [Description("charset=utf-16")]
            Utf16,

            /// <summary>
            /// Charset.Utf32
            /// </summary>
            [Description("charset=utf-32")]
            Utf32,
        }
    }

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
                : "Description Not Found";
        }
    }
}
