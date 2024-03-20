namespace HouseofCat.Data.Database;

public class Field
{
    public string Name { get; set; }
    public string Alias { get; set; }
    public string Value { get; set; }

    private static readonly string _aliasTemplate = "{0} as {1}";

    public string GetNameWithAlias()
    {
        if (string.IsNullOrWhiteSpace(Alias))
        { return Name; }
        else
        { return string.Format(_aliasTemplate, Name, Alias); }
    }
}
