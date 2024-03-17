using System.ComponentModel.DataAnnotations;

namespace HouseofCat.Data.Database;

public static class Enums
{
    public enum Database
    {
        Default,
        SQLite,
        OleDb,
        Odbc,
        LegacySqlServer,
        SqlServer,
        PostgreSql,
        MySql,
        OracleSql,
        Firebird,
    }

    public enum Case
    {
        AsIs,
        SnakeCase
    }

    public enum OrderDirection
    {
        Asc,
        Desc
    }

    public enum JoinType
    {
        Left,
        Right,
        Inner,
        Cross
    }

    public enum WhereLogic
    {
        [Display(Name = "AND")]
        AND,
        [Display(Name = "OR")]
        OR
    }

    public enum WhereAction
    {
        [Display(Name = "QUERY")]
        Query,
        [Display(Name = "IN")]
        In,
        [Display(Name = "NOT IN")]
        NotIn,
        [Display(Name = "NULL")]
        Null,
        [Display(Name = "NOT NULL")]
        NotNull,
        [Display(Name = "TRUE")]
        True,
        [Display(Name = "FALSE")]
        False,
        [Display(Name = "STARTS")]
        Starts,
        [Display(Name = "ENDS")]
        Ends,
        [Display(Name = "LIKE")]
        Like,
        [Display(Name = "NOT LIKE")]
        NotLike,
        [Display(Name = "BETWEEN")]
        Between,
        [Display(Name = "NOT BETWEEN")]
        NotBetween,
        [Display(Name = "CONTAINS")]
        Contains,
        [Display(Name = "NOT CONTAINS")]
        NotContains,
        [Display(Name = "DATE")]
        Date,
        [Display(Name = "NOT DATE")]
        NotDate,
        [Display(Name = "DATEPART")]
        DatePart,
        [Display(Name = "NOT DATEPART")]
        NotDatePart,
        [Display(Name = "TIME")]
        Time,
        [Display(Name = "NOT TIME")]
        NotTime,
        [Display(Name = ">")]
        GreaterThan,
        [Display(Name = ">=")]
        GreaterThanOrEqual,
        [Display(Name = "<")]
        LessThan,
        [Display(Name = "<=")]
        LessThanOrEqual,
        [Display(Name = "=")]
        Equal,
        [Display(Name = "RAW")]
        Raw
    }

    public enum UnitOfTime
    {
        Millisecond,
        Second,
        Minute,
        Hour,
        Day
    }
}
