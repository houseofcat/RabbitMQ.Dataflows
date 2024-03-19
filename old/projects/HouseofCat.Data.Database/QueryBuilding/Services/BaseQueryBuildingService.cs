using HouseofCat.Utilities.Extensions;
using SqlKata;
using System.Data;
using System.Linq;
using static HouseofCat.Data.Database.Enums;

namespace HouseofCat.Data.Database.QueryBuilding.Services;

public abstract class BaseQueryBuildingService
{
    protected virtual Query BuildQueryFromStatement(Statement statement, Case casing)
    {
        var query = StartQuery(statement);

        switch (statement.StatementType)
        {
            case StatementType.Select:
                SetSelect(query, statement, casing);
                if (statement.Joins?.Count > 0)
                {
                    SetJoins(query, statement, casing);
                }
                if (statement.Wheres?.Count > 0)
                {
                    SetWhere(query, statement);
                }
                if (statement.SelectRawStatements?.Length > 0
                    && statement.GroupByFields?.Length > 0)
                {
                    SetGroupBy(query, statement);
                }
                if (statement.Havings?.Count > 0)
                {
                    SetHavings(query, statement);
                }
                if (statement.OrderBys?.Count > 0)
                {
                    SetOrderBy(query, statement);
                }
                if (statement.Limit.HasValue)
                {
                    query.Limit(statement.Limit.Value);
                }
                if (statement.Offset.HasValue)
                {
                    query.Offset(statement.Offset.Value);
                }
                if (statement.Distinct.HasValue)
                {
                    query.IsDistinct = statement.Distinct.Value;
                }
                break;
            case StatementType.Insert:
                SetInsert(query, statement, casing);
                break;
            case StatementType.Update:
                SetUpdate(query, statement, casing);
                if (statement.Wheres?.Count > 0)
                {
                    SetWhere(query, statement);
                }
                break;
            case StatementType.Delete:
                SetDelete(query, statement, casing);
                if (statement.Wheres?.Count > 0)
                {
                    SetWhere(query, statement);
                }
                break;
            default: break;
        }

        if (statement.UnionStatement != null)
        {
            query.Union(BuildQueryFromStatement(statement.UnionStatement, casing));
        }

        return query;
    }

    protected virtual Query StartQuery(Statement statement)
    {
        var query = new Query(statement.Table.GetNameWithSchemaAndAlias());

        if (string.IsNullOrWhiteSpace(statement.QueryAlias))
        {
            query.QueryAlias = statement.QueryAlias;
        }

        return query;
    }

    protected virtual void SetSelect(Query query, Statement statement, Case casing)
    {
        if (statement.Fields != null)
        {
            foreach (var field in statement.Fields)
            {
                query.Select(field.GetNameWithAlias());
            }
        }
    }

    protected virtual void SetUpdate(Query query, Statement statement, Case casing)
    {
        query.AsUpdate(
            statement.Fields.Select(f => f.Name),
            statement.Fields.Select(f => f.Value));
    }

    protected virtual void SetInsert(Query query, Statement statement, Case casing)
    {
        query.AsInsert(
            statement.Fields.Select(f => f.Name),
            statement.Fields.Select(f => f.Value));
    }

    protected virtual void SetDelete(Query query, Statement statement, Case casing)
    {
        query.AsDelete();
    }

    protected virtual void SetJoins(Query query, Statement statement, Case casing)
    {
        foreach (var join in statement.Joins)
        {
            switch (join.Type)
            {
                case JoinType.Left:
                    query.LeftJoin(
                        join.ToTable.GetNameWithSchemaAndAlias(),
                        join.Field,
                        join.OnField);
                    break;
                case JoinType.Right:
                    query.RightJoin(
                        join.ToTable.GetNameWithSchemaAndAlias(),
                        join.Field,
                        join.OnField);
                    break;
                case JoinType.Cross:
                    query.CrossJoin(
                        join.ToTable.GetNameWithSchemaAndAlias());
                    break;
                case JoinType.Inner:
                    query.Join(
                        join.ToTable.GetNameWithSchemaAndAlias(),
                        join.Field,
                        join.OnField);
                    break;
            }

        }
    }

    protected virtual void SetWhere(Query query, Statement statement)
    {
        foreach (var where in statement.Wheres)
        {
            switch (where.Action)
            {
                case WhereAction.Query:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhere(where.Field, new Query(where.Parameters[0])); }
                        else
                        { query.Where(where.Field, new Query(where.Parameters[0])); }
                    }
                    break;
                case WhereAction.In:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereIn(where.Field, where.Parameters); }
                        else
                        { query.WhereIn(where.Field, where.Parameters); }
                    }
                    break;
                case WhereAction.NotIn:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereNotIn(where.Field, where.Parameters); }
                        else
                        { query.WhereNotIn(where.Field, where.Parameters); }
                    }
                    break;
                case WhereAction.Null:
                    if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                    { query.OrWhereNull(where.Field); }
                    else
                    { query.WhereNull(where.Field); }
                    break;
                case WhereAction.NotNull:
                    if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                    { query.OrWhereNotNull(where.Field); }
                    else
                    { query.WhereNotNull(where.Field); }
                    break;
                case WhereAction.True:
                    if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                    { query.OrWhereTrue(where.Field); }
                    else
                    { query.WhereTrue(where.Field); }
                    break;
                case WhereAction.False:
                    if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                    { query.OrWhereFalse(where.Field); }
                    else
                    { query.WhereFalse(where.Field); }
                    break;
                case WhereAction.Starts:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereStarts(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                        else
                        { query.WhereStarts(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                    }
                    break;
                case WhereAction.Ends:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereEnds(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                        else
                        { query.WhereEnds(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                    }
                    break;
                case WhereAction.Like:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereLike(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                        else
                        { query.WhereLike(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                    }
                    break;
                case WhereAction.NotLike:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereNotLike(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                        else
                        { query.WhereNotLike(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                    }
                    break;
                case WhereAction.Between:
                    if (where.Parameters?.Length > 1)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereBetween(where.Field, where.Parameters[0], where.Parameters[1]); }
                        else
                        { query.WhereBetween(where.Field, where.Parameters[0], where.Parameters[1]); }
                    }
                    break;
                case WhereAction.NotBetween:
                    if (where.Parameters?.Length > 1)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereNotBetween(where.Field, where.Parameters[0], where.Parameters[1]); }
                        else
                        { query.WhereNotBetween(where.Field, where.Parameters[0], where.Parameters[1]); }
                    }
                    break;
                case WhereAction.Contains:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereContains(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                        else
                        { query.WhereContains(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                    }
                    break;
                case WhereAction.NotContains:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereNotContains(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                        else
                        { query.WhereNotContains(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                    }
                    break;
                case WhereAction.Date:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereDate(where.Field, where.Parameters[0]); }
                        else
                        { query.WhereDate(where.Field, where.Parameters[0]); }
                    }
                    break;
                case WhereAction.NotDate:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereNotDate(where.Field, where.Parameters[0]); }
                        else
                        { query.WhereNotDate(where.Field, where.Parameters[0]); }
                    }
                    break;
                case WhereAction.DatePart:
                    if (where.Parameters?.Length > 1)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereDatePart(where.Parameters[0], where.Field, where.Parameters[1]); }
                        else
                        { query.WhereDatePart(where.Parameters[0], where.Field, where.Parameters[1]); }
                    }
                    break;
                case WhereAction.NotDatePart:
                    if (where.Parameters?.Length > 1)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereNotDatePart(where.Parameters[0], where.Field, where.Parameters[1]); }
                        else
                        { query.WhereNotDatePart(where.Parameters[0], where.Field, where.Parameters[1]); }
                    }
                    break;
                case WhereAction.Time:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereTime(where.Field, where.Parameters[0]); }
                        else
                        { query.WhereTime(where.Field, where.Parameters[0]); }
                    }
                    break;
                case WhereAction.NotTime:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereNotTime(where.Field, where.Parameters[0]); }
                        else
                        { query.WhereNotTime(where.Field, where.Parameters[0]); }
                    }
                    break;
                case WhereAction.Raw:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhereRaw(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                        else
                        { query.WhereRaw(where.Field, where.Parameters[0], where.CaseSensitive, where.EscapeCharacter); }
                    }
                    break;
                default:
                    if (where.Parameters?.Length > 0)
                    {
                        if (where.Logic.HasValue && where.Logic.Value == WhereLogic.OR)
                        { query.OrWhere(where.Field, where.Action.Description(), where.Parameters); }
                        else
                        { query.Where(where.Field, where.Action.Description(), where.Parameters); }
                    }
                    break;
            }
        }
    }

    protected virtual void SetGroupBy(Query query, Statement statement)
    {
        foreach (var aggStatement in statement.SelectRawStatements)
        {
            query.SelectRaw(aggStatement);
        }

        foreach (var field in statement.GroupByFields)
        {
            if (statement.Fields.Any(f => f.Name == field))
            {
                query.GroupBy(statement.GroupByFields);
            }
        }
    }

    protected virtual void SetHavings(Query query, Statement statement)
    {
        foreach (var having in statement.Wheres)
        {
            switch (having.Action)
            {
                case WhereAction.Query:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHaving(having.Field, new Query(having.Parameters[0])); }
                        else
                        { query.Having(having.Field, new Query(having.Parameters[0])); }
                    }
                    break;
                case WhereAction.In:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingIn(having.Field, having.Parameters); }
                        else
                        { query.HavingIn(having.Field, having.Parameters); }
                    }
                    break;
                case WhereAction.NotIn:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingNotIn(having.Field, having.Parameters); }
                        else
                        { query.HavingNotIn(having.Field, having.Parameters); }
                    }
                    break;
                case WhereAction.Null:
                    if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                    { query.OrHavingNull(having.Field); }
                    else
                    { query.HavingNull(having.Field); }
                    break;
                case WhereAction.NotNull:
                    if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                    { query.OrHavingNotNull(having.Field); }
                    else
                    { query.HavingNotNull(having.Field); }
                    break;
                case WhereAction.True:
                    if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                    { query.OrHavingTrue(having.Field); }
                    else
                    { query.HavingTrue(having.Field); }
                    break;
                case WhereAction.False:
                    if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                    { query.OrHavingFalse(having.Field); }
                    else
                    { query.HavingFalse(having.Field); }
                    break;
                case WhereAction.Starts:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingStarts(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                        else
                        { query.HavingStarts(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                    }
                    break;
                case WhereAction.Ends:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingEnds(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                        else
                        { query.HavingEnds(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                    }
                    break;
                case WhereAction.Like:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingLike(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                        else
                        { query.HavingLike(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                    }
                    break;
                case WhereAction.NotLike:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingNotLike(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                        else
                        { query.HavingNotLike(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                    }
                    break;
                case WhereAction.Between:
                    if (having.Parameters?.Length > 1)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingBetween(having.Field, having.Parameters[0], having.Parameters[1]); }
                        else
                        { query.HavingBetween(having.Field, having.Parameters[0], having.Parameters[1]); }
                    }
                    break;
                case WhereAction.NotBetween:
                    if (having.Parameters?.Length > 1)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingNotBetween(having.Field, having.Parameters[0], having.Parameters[1]); }
                        else
                        { query.HavingNotBetween(having.Field, having.Parameters[0], having.Parameters[1]); }
                    }
                    break;
                case WhereAction.Contains:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingContains(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                        else
                        { query.HavingContains(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                    }
                    break;
                case WhereAction.NotContains:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingNotContains(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                        else
                        { query.HavingNotContains(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                    }
                    break;
                case WhereAction.Date:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingDate(having.Field, having.Parameters[0]); }
                        else
                        { query.HavingDate(having.Field, having.Parameters[0]); }
                    }
                    break;
                case WhereAction.NotDate:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingNotDate(having.Field, having.Parameters[0]); }
                        else
                        { query.HavingNotDate(having.Field, having.Parameters[0]); }
                    }
                    break;
                case WhereAction.DatePart:
                    if (having.Parameters?.Length > 1)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingDatePart(having.Parameters[0], having.Field, having.Parameters[1]); }
                        else
                        { query.HavingDatePart(having.Parameters[0], having.Field, having.Parameters[1]); }
                    }
                    break;
                case WhereAction.NotDatePart:
                    if (having.Parameters?.Length > 1)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingNotDatePart(having.Parameters[0], having.Field, having.Parameters[1]); }
                        else
                        { query.HavingNotDatePart(having.Parameters[0], having.Field, having.Parameters[1]); }
                    }
                    break;
                case WhereAction.Time:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingTime(having.Field, having.Parameters[0]); }
                        else
                        { query.HavingTime(having.Field, having.Parameters[0]); }
                    }
                    break;
                case WhereAction.NotTime:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingNotTime(having.Field, having.Parameters[0]); }
                        else
                        { query.HavingNotTime(having.Field, having.Parameters[0]); }
                    }
                    break;
                case WhereAction.Raw:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHavingRaw(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                        else
                        { query.HavingRaw(having.Field, having.Parameters[0], having.CaseSensitive, having.EscapeCharacter); }
                    }
                    break;
                default:
                    if (having.Parameters?.Length > 0)
                    {
                        if (having.Logic.HasValue && having.Logic.Value == WhereLogic.OR)
                        { query.OrHaving(having.Field, having.Action.Description(), having.Parameters); }
                        else
                        { query.Having(having.Field, having.Action.Description(), having.Parameters); }
                    }
                    break;
            }
        }
    }

    protected virtual void SetOrderBy(Query query, Statement statement)
    {
        foreach (var order in statement.OrderBys)
        {
            if (order.Direction.HasValue && order.Direction.Value == OrderDirection.Asc)
            {
                if (statement.Fields.Any(f => f.Name == order.Field))
                {
                    query.OrderBy(order.Field);
                }
            }
            else
            {
                if (statement.Fields.Any(f => f.Name == order.Field))
                {
                    query.OrderByDesc(order.Field);
                }
            }
        }
    }
}
