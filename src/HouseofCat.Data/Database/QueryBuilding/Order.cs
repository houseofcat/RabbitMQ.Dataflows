using static HouseofCat.Data.Database.Enums;

namespace HouseofCat.Data.Database;

public class Order
{
    public string Field { get; set; }
    public OrderDirection? Direction { get; set; }
}
