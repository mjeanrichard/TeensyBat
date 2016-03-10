using System.ComponentModel;
using System.Threading.Tasks;

namespace TeensyBatMap.Common
{
    public interface IBaseViewModel : INotifyPropertyChanged
    {
        Task Initialize();
        bool IsBusy { get; }
    }
}