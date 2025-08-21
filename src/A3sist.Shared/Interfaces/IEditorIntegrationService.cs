using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    public interface IEditorIntegrationService
    {
        Task RefreshEditorView(object filePath);
    }
}