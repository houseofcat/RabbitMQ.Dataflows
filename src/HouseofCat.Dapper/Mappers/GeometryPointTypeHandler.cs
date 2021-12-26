using Dapper;
using Microsoft.Spatial;
using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HouseofCat.Dapper
{
    public class GeometryPointTypeHandler : SqlMapper.TypeHandler<GeometryPoint>
    {
        private Regex _regex = new Regex(@"^(POINT \()(.+)(\))", RegexOptions.CultureInvariant);

        public override GeometryPoint Parse(object value)
        {
            if (value == null) return null;

            if (!_regex.IsMatch(value.ToString()))
            { throw new ArgumentException("Value is not a Geometry Point"); }

            var geometryPoints = value.ToString().Split('(', ')')[1];
            var geometryValues = geometryPoints.Split(' ');

            var x = ConvertToDouble(geometryValues[0]);
            var y = ConvertToDouble(geometryValues[1]);

            double? z = null;
            if (geometryValues.Length >= 3)
            { z = ConvertToDouble(geometryValues[2]); }

            double? m = null;
            if (geometryValues.Length >= 4)
            { m = ConvertToDouble(geometryValues[3]); }

            return GeometryPoint.Create(x, y, z, m);
        }

        public override void SetValue(IDbDataParameter parameter, GeometryPoint value)
        {
            throw new NotImplementedException();
        }

        private double ConvertToDouble(string value)
        {
            return double.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}
