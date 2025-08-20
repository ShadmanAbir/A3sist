using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    public interface IFileSystem
    {
        Task WriteAllTextAsync(string path, string content);
        Task  ReadAllTextAsync(string path);
        Task FileExistsAsync(string path);
    }
}