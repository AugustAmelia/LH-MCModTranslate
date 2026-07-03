# План реализации: 7 фич из TECHNICAL_SPECIFICATION

---

## Фича 1: 🔄 Retry + Auto-Fix (Приоритет: ВЫСОКИЙ)

**Проблема:** Если ИИ вернул невалидный JSON — программа падает с ошибкой. Особенно часто ломается на локальных моделях.

**Решение:** До 3 попыток повтора + программная починка JSON между попытками.

### Изменения:

#### [MODIFY] [OpenAITranslationService.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/Services/OpenAITranslationService.cs)
- Обернуть вызов AI в цикл `for (int attempt = 0; attempt < maxRetries; attempt++)`
- При `JsonException` — попытка Auto-Fix:
  - Удалить всё до первого `[` и после последнего `]`
  - Заменить одинарные кавычки на двойные
  - Исправить незакрытые строки
  - Убрать trailing comma перед `]`
- Между попытками: задержка `await Task.Delay(1000 * attempt)` (Exponential Backoff)
- Если все 3 попытки провалились — fallback: вернуть оригинальные строки (а не крашить)

#### [MODIFY] [AppConfig.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/Models/AppConfig.cs)
- Добавить `public int MaxRetries { get; set; } = 3;`

> [!TIP]
> Эта фича напрямую решит ошибку "AI returned invalid array length" которую вы видели.

---

## Фича 2: 📊 ETA и детальный прогресс (Приоритет: СРЕДНИЙ)

**Проблема:** Сейчас только простой счётчик. Непонятно, сколько ждать и что происходит.

**Решение:** Показывать ETA, скорость (строк/мин), текущий файл.

### Изменения:

#### [MODIFY] [MainViewModel.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/ViewModels/MainViewModel.cs)
- Добавить свойства:
  - `string CurrentFileName` — текущий обрабатываемый файл
  - `string EtaText` — "~2 мин 30 сек"
  - `string SpeedText` — "12 строк/мин"
- В `TranslateAllAsync()`: засечь `Stopwatch` при старте, после каждого батча пересчитывать:
  - `speed = translatedSoFar / elapsed.TotalMinutes`
  - `eta = (total - translatedSoFar) / speed`

#### [MODIFY] [MainWindow.xaml](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/Views/MainWindow.xaml)
- В overlay загрузки (Blur-эффект) добавить:
  - TextBlock с `EtaText`
  - TextBlock с `SpeedText`
  - TextBlock с `CurrentFileName`

---

## Фича 3: 🧠 Гибридный режим Local + Cloud (Приоритет: НИЗКИЙ)

**Проблема:** Либо всё через Ollama (медленно), либо всё через OpenAI (платно).

**Решение:** Настройка порога — короткие строки (≤ N слов) → локально, длинные → облако.

### Изменения:

#### [MODIFY] [AppConfig.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/Models/AppConfig.cs)
- `public bool UseHybridMode { get; set; } = false;`
- `public string CloudProvider { get; set; } = "OpenAI";`
- `public int HybridWordThreshold { get; set; } = 10;` (строки длиннее 10 слов → облако)

#### [MODIFY] [MainViewModel.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/ViewModels/MainViewModel.cs)
- В `TranslateAllAsync()`: разделить `untranslated` на два списка:
  - `shortStrings` (≤ threshold слов) → отправить в Ollama
  - `longStrings` (> threshold) → отправить в OpenAI
- Нужен второй экземпляр `ITranslationService` с облачными настройками

#### [MODIFY] [SettingsWindow.xaml](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/Views/SettingsWindow.xaml)
- Чекбокс "Гибридный режим" + слайдер порога слов
- Поля для настройки обоих провайдеров одновременно

> [!WARNING]
> Эта фича самая сложная — потребует рефакторинг DI для двух экземпляров сервиса. Рекомендую реализовать последней.

---

## Фича 4: 📝 Панель логов в реальном времени (Приоритет: СРЕДНИЙ)

**Проблема:** Ошибки видны только в MessageBox. Нет истории действий.

**Решение:** Лог-панель внизу главного окна (как Output в VS Code).

### Изменения:

#### [NEW] Services/LogService.cs
```csharp
public class LogService
{
    public ObservableCollection<LogEntry> Logs { get; } = new();
    
    public void Info(string message) => Add("INFO", message);
    public void Warn(string message) => Add("WARN", message);
    public void Error(string message) => Add("ERROR", message);
    
    private void Add(string level, string message) =>
        App.Current.Dispatcher.Invoke(() => 
            Logs.Add(new LogEntry(DateTime.Now, level, message)));
}

public record LogEntry(DateTime Time, string Level, string Message);
```

#### [MODIFY] [MainViewModel.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/ViewModels/MainViewModel.cs)
- Инжектировать `LogService`
- Заменить все `MessageBox.Show(error)` на `_log.Error(error)` (MessageBox оставить только для критических)
- Добавить логи: "Загружено 45 строк из en_us.json", "Батч 1/5 переведён за 12 сек", и т.д.

#### [MODIFY] [MainWindow.xaml](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/Views/MainWindow.xaml)
- Добавить сворачиваемую панель внизу (Expander или GridSplitter):
  ```xml
  <Expander Header="📝 Логи" Grid.Row="2">
      <ListBox ItemsSource="{Binding LogService.Logs}" MaxHeight="150">
          <!-- Цвет по Level: INFO=серый, WARN=жёлтый, ERROR=красный -->
      </ListBox>
  </Expander>
  ```

#### [MODIFY] [App.xaml.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/App.xaml.cs)
- Зарегистрировать `services.AddSingleton<LogService>();`

---

## Фича 5: 🔍 Автоопределение кодировки (Приоритет: НИЗКИЙ)

**Проблема:** Старые моды (Minecraft 1.7-1.12) используют `.lang` файлы в Latin-1 или Windows-1251.

**Решение:** Определять кодировку автоматически перед чтением файла.

### Изменения:

#### Установка NuGet пакета
```
dotnet add package UTF.Unknown
```

#### [MODIFY] [LangFileService.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/Services/LangFileService.cs)
- В `LoadFileAsync()`:
  ```csharp
  var bytes = await File.ReadAllBytesAsync(filePath);
  var detected = CharsetDetector.DetectFromBytes(bytes);
  var encoding = detected.Detected?.Encoding ?? Encoding.UTF8;
  string[] lines = encoding.GetString(bytes).Split('\n');
  ```
- В `SaveFileAsync()` — всегда сохранять в UTF-8 (стандарт)

---

## Фича 6: 🎯 Drag & Drop загрузка (Приоритет: СРЕДНИЙ)

**Проблема:** Нужно нажимать кнопку и выбирать файл через диалог.

**Решение:** Перетаскивание `.jar`/`.json`/папки прямо на окно программы.

### Изменения:

#### [MODIFY] [MainWindow.xaml](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/Views/MainWindow.xaml)
- На корневом `Grid` или Dashboard-зоне:
  ```xml
  <Grid AllowDrop="True" Drop="OnFileDrop" DragOver="OnDragOver">
  ```

#### [MODIFY] [MainWindow.xaml.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/Views/MainWindow.xaml.cs)
```csharp
private async void OnFileDrop(object sender, DragEventArgs e)
{
    if (e.Data.GetDataPresent(DataFormats.FileDrop))
    {
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var path = files[0];
        var vm = (MainViewModel)DataContext;
        
        if (Directory.Exists(path))
            await vm.LoadDroppedFolderAsync(path);
        else if (path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
            await vm.LoadDroppedJarAsync(path);
        else
            await vm.LoadDroppedFileAsync(path);
    }
}

private void OnDragOver(object sender, DragEventArgs e)
{
    e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) 
        ? DragDropEffects.Copy : DragDropEffects.None;
    e.Handled = true;
}
```

#### [MODIFY] [MainViewModel.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/ViewModels/MainViewModel.cs)
- Добавить публичные методы:
  - `LoadDroppedFileAsync(string path)`
  - `LoadDroppedFolderAsync(string path)` 
  - `LoadDroppedJarAsync(string path)`
- Они переиспользуют существующую логику загрузки

#### [MODIFY] Dashboard в MainWindow.xaml
- Заменить текст "Загрузите мод" на:
  ```
  "📂 Перетащите .jar файл сюда или выберите через кнопки выше"
  ```
- Добавить визуальный эффект при DragOver (подсветка границы)

---

## Фича 7: ⚡ Параллельные запросы (Приоритет: СРЕДНИЙ)

**Проблема:** Батчи отправляются последовательно. С OpenAI это медленно.

**Решение:** Отправлять N батчей параллельно с учётом rate limit.

### Изменения:

#### [MODIFY] [AppConfig.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/Models/AppConfig.cs)
- `public int MaxParallelRequests { get; set; } = 1;` (1 = последовательно, 2-5 = параллельно)

#### [MODIFY] [MainViewModel.cs](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/ViewModels/MainViewModel.cs)
- В `TranslateAllAsync()`:
  ```csharp
  var semaphore = new SemaphoreSlim(config.MaxParallelRequests);
  var tasks = batches.Select(async batch => {
      await semaphore.WaitAsync();
      try { /* translate batch */ }
      finally { semaphore.Release(); }
  });
  await Task.WhenAll(tasks);
  ```

#### [MODIFY] [SettingsWindow.xaml](file:///c:/Projects-for-AI-coding/AI-Mod-Translator/AIModTranslator/Views/SettingsWindow.xaml)
- Слайдер "Параллельные запросы" (1-5) в провайдер-вкладке
- Подсказка: "1 для Ollama, 3-5 для OpenAI"

> [!IMPORTANT]
> Для Ollama ставить 1 (модель обрабатывает по одному). Для OpenAI можно 3-5, но с учётом rate limit (60 RPM на бесплатном тарифе).

---

## Порядок реализации (Рекомендация)

| Очередь | Фича | Сложность | Время |
|---------|-------|-----------|-------|
| 1 | 🔄 Retry + Auto-Fix | Лёгкая | ~15 мин |
| 2 | 🎯 Drag & Drop | Лёгкая | ~15 мин |
| 3 | 📝 Панель логов | Средняя | ~30 мин |
| 4 | 📊 ETA и прогресс | Средняя | ~20 мин |
| 5 | ⚡ Параллельные запросы | Средняя | ~20 мин |
| 6 | 🔍 Автоопределение кодировки | Лёгкая | ~10 мин |
| 7 | 🧠 Гибридный режим | Сложная | ~45 мин |

## Open Questions

> [!IMPORTANT]
> 1. Реализовать все 7 фич или выбрать конкретные?
> 2. Гибридный режим (фича 3) значительно усложняет архитектуру — стоит ли его реализовывать сейчас, или отложить на будущее?
