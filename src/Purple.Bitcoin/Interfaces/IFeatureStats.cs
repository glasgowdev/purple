using System.Text;

namespace Purple.Bitcoin.Interfaces
{
    public interface IFeatureStats
    {
        void AddFeatureStats(StringBuilder benchLog);
    }
}
