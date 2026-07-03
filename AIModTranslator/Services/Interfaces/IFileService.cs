using System.Collections.ObjectModel;
using AIModTranslator.Models;

namespace AIModTranslator.Services.Interfaces;

public interface IFileService
{
    Task<ObservableCollection<TranslationEntry>> LoadFileAsync(string filePath);
    Task SaveFileAsync(string filePath, IEnumerable<TranslationEntry> entries);
    string[] SupportedExtensions { get; }
}
