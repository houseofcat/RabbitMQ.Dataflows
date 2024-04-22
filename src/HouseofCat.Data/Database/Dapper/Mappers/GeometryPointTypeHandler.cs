using Dapper;
using Microsoft.Spatial;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace HouseofCat.Database.Dapper;

public partial class GeometryPointTypeHandler : SqlMapper.TypeHandler<GeometryPoint>
{
    [GeneratedRegex(@"^(POINT \()(.+)(\))", RegexOptions.IgnoreCase)]
    private static partial Regex PointRegex();
    private static readonly Regex _regex = PointRegex();

    public override GeometryPoint Parse(object value)
    {
        if (value == null) return null;

        if (!_regex.IsMatch(value.ToString()))
        { throw new ArgumentException("Value is not a Geometry Point"); }

        var geometryPoints = value.ToString().Split('(', ')', StringSplitOptions.RemoveEmptyEntries)[1];
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

    private static double ConvertToDouble(string value)
    {
        return double.Parse(value);
    }
}
