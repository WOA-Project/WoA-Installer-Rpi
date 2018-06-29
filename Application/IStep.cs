using System.Threading.Tasks;
using ReactiveUI;

namespace Install
{
    public interface IStep
    {
        string Name { get; }
        ReactiveCommand RunCommand { get; }
        Task Execute();
    }
}