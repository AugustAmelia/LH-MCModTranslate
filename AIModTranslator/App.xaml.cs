using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AIModTranslator.Data;
using AIModTranslator.Services;
using AIModTranslator.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System;
using System.Linq;
using Material.Styles.Themes;
using Material.Colors;

namespace AIModTranslator;

public partial class App : Application
{
    public new static App? Current => Application.Current as App;
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            var tmService = Services.GetRequiredService<ITranslationMemoryService>();
            tmService.EnsureCreatedAsync().ConfigureAwait(false);

            var settingsService = Services.GetRequiredService<ISettingsService>();
            ApplyTheme(settingsService.LoadConfig());

            desktop.MainWindow = new Views.MainWindow
            {
                DataContext = Services.GetRequiredService<ViewModels.MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        string dbPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "AIModTranslator", "TranslationMemory.db");
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<HttpClient>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGlossaryService, GlossaryService>();
        services.AddSingleton<ITranslationMemoryService, TranslationMemoryService>();
        services.AddSingleton<LogService>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddTransient<IFileService, JsonFileService>();
        services.AddTransient<IFileService, LangFileService>();
        services.AddTransient<IFileService, TomlFileService>();
        services.AddTransient<IFileService, SnbtFileService>();

        services.AddTransient<ITranslationService, OpenAITranslationService>();
        services.AddTransient<QAService>();

        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.TranslationMemoryViewModel>();
        services.AddTransient<ViewModels.SettingsViewModel>();
        services.AddTransient<ViewModels.GlossaryViewModel>();
        services.AddTransient<ViewModels.GitHubViewModel>();
        
        // Register Views for Dialogs
        services.AddTransient<Views.SettingsWindow>();
        services.AddTransient<Views.GlossaryWindow>();
        services.AddTransient<Views.GitHubWindow>();
        services.AddTransient<Views.TermExtractorWindow>();
        services.AddTransient<Views.TranslationMemoryWindow>();
    }

    public void ApplyTheme(Models.AppConfig config)
    {
        if (Current == null) return;
        
        Current.Resources["AppCornerRadius"] = new Avalonia.CornerRadius(config.UICornerRadius);

        Current.RequestedThemeVariant = config.IsDarkMode ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;

        Avalonia.Media.Color windowColor;
        Avalonia.Media.Color panelColor;
        Avalonia.Media.Color cardColor;

        bool isDark = config.IsDarkMode;

        switch (config.ThemePalette)
        {
            case "Deep Blue":
                windowColor = isDark ? Avalonia.Media.Color.FromRgb(11, 15, 25) : Avalonia.Media.Color.FromRgb(240, 244, 248);
                panelColor = isDark ? Avalonia.Media.Color.FromRgb(17, 24, 39) : Avalonia.Media.Color.FromRgb(226, 236, 245);
                cardColor = isDark ? Avalonia.Media.Color.FromRgb(31, 41, 55) : Avalonia.Media.Color.FromRgb(255, 255, 255);
                break;
            case "Emerald":
                windowColor = isDark ? Avalonia.Media.Color.FromRgb(6, 16, 14) : Avalonia.Media.Color.FromRgb(242, 247, 245);
                panelColor = isDark ? Avalonia.Media.Color.FromRgb(11, 30, 27) : Avalonia.Media.Color.FromRgb(230, 240, 236);
                cardColor = isDark ? Avalonia.Media.Color.FromRgb(20, 50, 45) : Avalonia.Media.Color.FromRgb(255, 255, 255);
                break;
            case "Amber":
                windowColor = isDark ? Avalonia.Media.Color.FromRgb(16, 12, 6) : Avalonia.Media.Color.FromRgb(247, 245, 240);
                panelColor = isDark ? Avalonia.Media.Color.FromRgb(28, 21, 10) : Avalonia.Media.Color.FromRgb(240, 234, 224);
                cardColor = isDark ? Avalonia.Media.Color.FromRgb(45, 35, 20) : Avalonia.Media.Color.FromRgb(255, 255, 255);
                break;
            case "Neon Cyberpunk":
                windowColor = isDark ? Avalonia.Media.Color.FromRgb(10, 9, 21) : Avalonia.Media.Color.FromRgb(249, 247, 252);
                panelColor = isDark ? Avalonia.Media.Color.FromRgb(18, 14, 37) : Avalonia.Media.Color.FromRgb(241, 236, 248);
                cardColor = isDark ? Avalonia.Media.Color.FromRgb(30, 24, 60) : Avalonia.Media.Color.FromRgb(255, 255, 255);
                break;
            case "Dracula":
                windowColor = isDark ? Avalonia.Media.Color.FromRgb(30, 31, 41) : Avalonia.Media.Color.FromRgb(232, 232, 242);
                panelColor = isDark ? Avalonia.Media.Color.FromRgb(40, 42, 54) : Avalonia.Media.Color.FromRgb(241, 241, 248);
                cardColor = isDark ? Avalonia.Media.Color.FromRgb(52, 55, 70) : Avalonia.Media.Color.FromRgb(255, 255, 255);
                break;
            case "Nord":
                windowColor = isDark ? Avalonia.Media.Color.FromRgb(46, 52, 64) : Avalonia.Media.Color.FromRgb(216, 222, 233);
                panelColor = isDark ? Avalonia.Media.Color.FromRgb(59, 66, 82) : Avalonia.Media.Color.FromRgb(229, 233, 240);
                cardColor = isDark ? Avalonia.Media.Color.FromRgb(76, 86, 106) : Avalonia.Media.Color.FromRgb(255, 255, 255);
                break;
            case "Solarized":
                windowColor = isDark ? Avalonia.Media.Color.FromRgb(0, 43, 54) : Avalonia.Media.Color.FromRgb(253, 246, 227);
                panelColor = isDark ? Avalonia.Media.Color.FromRgb(7, 54, 66) : Avalonia.Media.Color.FromRgb(238, 232, 213);
                cardColor = isDark ? Avalonia.Media.Color.FromRgb(88, 110, 117) : Avalonia.Media.Color.FromRgb(255, 255, 255);
                break;
            case "Sakura":
                windowColor = isDark ? Avalonia.Media.Color.FromRgb(30, 10, 20) : Avalonia.Media.Color.FromRgb(255, 240, 245);
                panelColor = isDark ? Avalonia.Media.Color.FromRgb(45, 15, 30) : Avalonia.Media.Color.FromRgb(255, 228, 237);
                cardColor = isDark ? Avalonia.Media.Color.FromRgb(75, 25, 50) : Avalonia.Media.Color.FromRgb(255, 255, 255);
                break;
            case "Monochrome":
                windowColor = isDark ? Avalonia.Media.Color.FromRgb(18, 18, 18) : Avalonia.Media.Color.FromRgb(245, 245, 245);
                panelColor = isDark ? Avalonia.Media.Color.FromRgb(30, 30, 30) : Avalonia.Media.Color.FromRgb(255, 255, 255);
                cardColor = isDark ? Avalonia.Media.Color.FromRgb(45, 45, 45) : Avalonia.Media.Color.FromRgb(240, 240, 240);
                break;
            case "Vanilla":
            default:
                windowColor = isDark ? Avalonia.Media.Color.FromRgb(18, 18, 18) : Avalonia.Media.Color.FromRgb(245, 245, 245);
                panelColor = isDark ? Avalonia.Media.Color.FromRgb(30, 30, 30) : Avalonia.Media.Color.FromRgb(255, 255, 255);
                cardColor = isDark ? Avalonia.Media.Color.FromRgb(37, 37, 38) : Avalonia.Media.Color.FromRgb(240, 240, 240);
                break;
        }

        byte alpha = (byte)(config.UIBackgroundOpacity * 255);
        Current.Resources["AppPanelBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, panelColor.R, panelColor.G, panelColor.B));
        Current.Resources["AppWindowBackground"] = new Avalonia.Media.SolidColorBrush(windowColor);
        Current.Resources["AppCardBackground"] = new Avalonia.Media.SolidColorBrush(cardColor);

        var materialTheme = Current.Styles.OfType<Material.Styles.Themes.MaterialTheme>().FirstOrDefault();
        if (materialTheme != null)
        {
            materialTheme.BaseTheme = config.IsDarkMode ? Material.Styles.Themes.Base.BaseThemeMode.Dark : Material.Styles.Themes.Base.BaseThemeMode.Light;

            switch (config.ThemePalette)
            {
                case "Deep Blue":
                    materialTheme.PrimaryColor = Material.Colors.PrimaryColor.Blue;
                    materialTheme.SecondaryColor = Material.Colors.SecondaryColor.LightBlue;
                    break;
                case "Emerald":
                    materialTheme.PrimaryColor = Material.Colors.PrimaryColor.Teal;
                    materialTheme.SecondaryColor = Material.Colors.SecondaryColor.Green;
                    break;
                case "Amber":
                    materialTheme.PrimaryColor = Material.Colors.PrimaryColor.Amber;
                    materialTheme.SecondaryColor = Material.Colors.SecondaryColor.Orange;
                    break;
                case "Neon Cyberpunk":
                    materialTheme.PrimaryColor = Material.Colors.PrimaryColor.DeepPurple;
                    materialTheme.SecondaryColor = Material.Colors.SecondaryColor.Pink;
                    break;
                case "Monochrome":
                    materialTheme.PrimaryColor = Material.Colors.PrimaryColor.Grey;
                    materialTheme.SecondaryColor = Material.Colors.SecondaryColor.Indigo;
                    break;
                case "Dracula":
                    materialTheme.PrimaryColor = Material.Colors.PrimaryColor.DeepPurple;
                    materialTheme.SecondaryColor = Material.Colors.SecondaryColor.Pink;
                    break;
                case "Nord":
                    materialTheme.PrimaryColor = Material.Colors.PrimaryColor.BlueGrey;
                    materialTheme.SecondaryColor = Material.Colors.SecondaryColor.Cyan;
                    break;
                case "Solarized":
                    materialTheme.PrimaryColor = Material.Colors.PrimaryColor.Cyan;
                    materialTheme.SecondaryColor = Material.Colors.SecondaryColor.Amber;
                    break;
                case "Sakura":
                    materialTheme.PrimaryColor = Material.Colors.PrimaryColor.Pink;
                    materialTheme.SecondaryColor = Material.Colors.SecondaryColor.DeepPurple;
                    break;
                case "Vanilla":
                default:
                    materialTheme.PrimaryColor = Material.Colors.PrimaryColor.Purple;
                    materialTheme.SecondaryColor = Material.Colors.SecondaryColor.Indigo;
                    break;
            }
        }
    }
}
