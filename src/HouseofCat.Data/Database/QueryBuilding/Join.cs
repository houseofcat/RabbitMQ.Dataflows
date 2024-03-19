using static HouseofCat.Data.Database.Enums;

namespace HouseofCat.Data.Database;

public class Join
{
    public JoinType Type { get; set; }

    public Table ToTable { get; set; }

    public string Field { get; set; }
    public string OnField { get; set; }
}
