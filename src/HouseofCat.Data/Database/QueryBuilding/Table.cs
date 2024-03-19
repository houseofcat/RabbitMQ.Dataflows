namespace HouseofCat.Data.Database;

public class Table
{
    public string SchemaName { get; set; }
    public string Name { get; set; }
    public string Alias { get; set; }

    public string GetNameOrAlias()
    {
        if (string.IsNullOrWhiteSpace(Alias))
        { return Name; }
        else
        { return Alias; }
    }

    private static readonly string _aliasTemplate = "{0} as {1}";
    private static readonly string _schemaTemplate = "{0}.{1}";
    private static readonly string _aliasSchemaTemplate = "{0}.{1} as {2}";

    public string GetNameWithSchemaAndAlias()
    {
        if (string.IsNullOrWhiteSpace(Alias))
        {
            if (string.IsNullOrWhiteSpace(SchemaName))
            {
                return Name;
            }
            else
            {
                return string.Format(_schemaTemplate, SchemaName, Name);
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(SchemaName))
            {
                return string.Format(_aliasTemplate, Name, Alias);
            }
            else
            {
                return string.Format(_aliasSchemaTemplate, SchemaName, Name, Alias);
            }
        }
    }
}
