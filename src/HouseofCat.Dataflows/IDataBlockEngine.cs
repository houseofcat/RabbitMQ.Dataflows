using System.Threading.Tasks;

namespace HouseofCat.Dataflows;

public interface IDataBlockEngine<in TIn>
{
    ValueTask EnqueueWorkAsync(TIn data);
}