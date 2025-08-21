using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    public interface IFileSystem
    {
        Task WriteAllTextAsync(string path, string content);
        Task<string>  ReadAllTextAsync(string path);
        Task<bool> FileExistsAsync(string path);
    }
}