using System.Threading.Tasks;
using Avalonia.Controls.Notifications;

namespace AIModTranslator.Services.Interfaces;

public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);
    Task<bool> ShowConfirmAsync(string title, string message);
    void ShowToast(string title, string message, NotificationType type = NotificationType.Information, int expirationSeconds = 3);
}
