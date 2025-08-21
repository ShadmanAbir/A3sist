using A3sist.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    public interface INotificationService
    {
        Task<bool> ApplyQuickActionAsync(string actionType, string target);
        Task<List<Notification>> GetRecentNotificationsAsync(int count);
    }
}