using static HouseofCat.Data.Database.Enums;

namespace HouseofCat.Data.Database.QueryBuilding;

public class Order
{
    public string Field { get; set; }
    public OrderDirection? Direction { get; set; }
}
