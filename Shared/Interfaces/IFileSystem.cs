using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    public interface IFileSystem
    {
        Task WriteAllTextAsync(string path, string content);
        Task&lt;string&gt; ReadAllTextAsync(string path);
        Task&lt;bool&gt; FileExistsAsync(string path);
    }
}