using System.Threading.Tasks;

namespace HouseofCat.Dataflows
{
    public interface IDataBlockEngine<TIn>
    {
        ValueTask EnqueueWorkAsync(TIn data);
    }
}