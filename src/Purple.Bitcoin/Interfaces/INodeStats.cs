using System.Text;

namespace Purple.Bitcoin.Interfaces
{
    public interface INodeStats
    {
        void AddNodeStats(StringBuilder benchLog);
    }
}
