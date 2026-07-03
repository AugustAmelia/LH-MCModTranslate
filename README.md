# LH's MCModsTranslate

![Avalonia UI](https://img.shields.io/badge/UI-Avalonia-purple)
![.NET 8](https://img.shields.io/badge/.NET-8.0-blueviolet)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey)
![License](https://img.shields.io/badge/License-GPLv3-green)

**LH's MCModsTranslate** is a premium, cross-platform AI localization tool specifically designed to automatically translate Minecraft mods and modpacks. Built with C# and Avalonia UI, it brings high-quality AI translation capabilities (OpenAI, Gemini, Claude, and Local LLMs) directly to your desktop.

## ✨ Features

* 🤖 **Multi-LLM Support**: Translate seamlessly using GPT-4, Google Gemini, Anthropic Claude, or entirely free local models via Ollama / LM Studio.
* ⚖️ **Hybrid Mode**: Optimize costs and speed! Automatically route short strings to local/free models and complex/long texts to premium cloud models.
* 📦 **FTB Quests Support**: Built-in support for modern FTB Quests `.snbt` localization files. It smartly extracts only the translatable strings without breaking the quest structure.
* 🧠 **Translation Memory (TM)**: Automatically saves previous translations locally in an SQLite database. Provides fuzzy-matching suggestions and prevents paying to re-translate identical strings.
* 🛡️ **Tag Protection**: Specialized regex engine prevents AI models from accidentally translating or corrupting Minecraft formatting codes, variables, and HTML tags.
* 🎨 **Premium UI & Theming**: Beautiful, modern UI with customizable backgrounds, corner radiuses, and popular color palettes (Dracula, Nord, Solarized, Sakura, Neon Cyberpunk, Deep Blue, and more).
* 🌐 **Cross-Platform**: Runs flawlessly on Windows, Linux, and macOS.

## 📥 Installation

1. Go to the [Releases](https://github.com/AugustAmelia/LH-MCModTranslate/releases) tab.
2. Download the version for your operating system (Windows or Linux).
3. Unzip and run the executable!

## 🛠️ Building from Source

Ensure you have the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

```bash
git clone https://github.com/AugustAmelia/LH-MCModTranslate.git
cd LH-MCModTranslate/AIModTranslator

# Build the project
dotnet build

# Run the project
dotnet run
```

## 🤝 Contributing
Contributions, issues, and feature requests are welcome! Feel free to check the issues page.

## 📝 License
This project is open-source and available under the GNU GPL v3 License.
