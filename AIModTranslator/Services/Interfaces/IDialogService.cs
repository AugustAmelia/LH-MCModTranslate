using System.Threading.Tasks;

namespace AIModTranslator.Services.Interfaces;

public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);
    Task<bool> ShowConfirmAsync(string title, string message);
}
