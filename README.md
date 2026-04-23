# DS Project

A cross-platform mobile application built with **.NET MAUI** (Multi-platform App UI), supporting Android, iOS, macOS, and Windows platforms.

## 📱 Supported Platforms

- **Android** (API 21+)
- **iOS** (15.0+)
- **macOS Catalyst** (15.0+)
- **Windows** (10.0.17763.0+)

## 🛠 Technology Stack

- **Framework**: .NET MAUI (Multi-platform App UI)
- **.NET Version**: .NET 10.0
- **Language**: C# with XAML
- **Architecture**: MVVM (Model-View-ViewModel)
- **Key Dependencies**:
  - Microsoft.Maui.Controls
  - Microsoft.Extensions.Logging.Debug
  - Plugin.Maui.Audio

## 📁 Project Structure

```
MauiApp3/
├── Views/                  # XAML UI pages and user controls
├── ViewModels/             # ViewModel classes for MVVM pattern
├── Models/                 # Data models and business entities
├── Services/               # Application services (API, database, etc.)
├── Converters/             # XAML value converters
├── Helpers/                # Utility and helper classes
├── Platforms/              # Platform-specific code
│   ├── Android/
│   ├── iOS/
│   ├── MacCatalyst/
│   └── Windows/
├── Resources/              # Images, fonts, styles, and raw assets
│   ├── AppIcon/
│   ├── Splash/
│   ├── Images/
│   ├── Fonts/
│   └── Raw/
├── MauiProgram.cs          # Application startup and DI configuration
├── App.xaml                # Application-level resources and styles
├── AppShell.xaml           # Shell navigation structure
└── MauiApp3.csproj        # Project file with dependencies
```

## 🚀 Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- **IDE**: Visual Studio 2022 (17.0+) or Visual Studio Code
- **Platform-specific requirements**:
  - **Android**: Android SDK (API 21+), Android Emulator or physical device
  - **iOS**: Xcode 14.0+ (macOS only)
  - **macOS**: Xcode 14.0+ and MAUI workload
  - **Windows**: Windows 10/11

### Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/yourusername/DS-Project.git
   cd DS-Project/MauiApp3
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Build the project**:
   ```bash
   dotnet build
   ```

### Running the Application

**For Android**:
```bash
dotnet build -f net10.0-android -c Debug
```

**For iOS** (macOS only):
```bash
dotnet build -f net10.0-ios -c Debug
```

**For macOS Catalyst** (macOS only):
```bash
dotnet build -f net10.0-maccatalyst -c Debug
```

**For Windows**:
```bash
dotnet build -f net10.0-windows10.0.19041.0 -c Debug
```

**Using Visual Studio**:
- Open `MauiApp3.slnx` in Visual Studio 2022
- Select your target platform from the toolbar
- Press **F5** to debug or **Ctrl+F5** to run

## 🎯 Features

- Cross-platform mobile development using a single codebase
- XAML-based UI with data binding and MVVM support
- Platform-specific customizations where needed
- Audio plugin integration for multimedia capabilities
- Responsive design for multiple screen sizes

## 🔧 Development

### Building for Production

**Android**:
```bash
dotnet publish -f net10.0-android -c Release
```

**iOS**:
```bash
dotnet publish -f net10.0-ios -c Release
```

**Windows**:
```bash
dotnet publish -f net10.0-windows10.0.19041.0 -c Release
```

### Code Style & Conventions

- Follow [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use nullable reference types (`#nullable enable`)
- Implicit usings are enabled for cleaner code
- XAML source generation is enabled for faster builds

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Maui.Controls | Latest | MAUI UI framework |
| Microsoft.Extensions.Logging.Debug | 10.0.0 | Debug logging |
| Plugin.Maui.Audio | 4.0.0 | Audio playback and recording |

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🐛 Bug Reports

Found a bug? Please open an issue on [GitHub Issues](../../issues) with:
- Clear description of the bug
- Steps to reproduce
- Expected vs. actual behavior
- Platform(s) affected
- Environment details

## 📖 Resources

- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [XAML Documentation](https://learn.microsoft.com/en-us/dotnet/maui/xaml/)
- [MVVM Toolkit](https://learn.microsoft.com/en-us/windows/communitytoolkit/mvvm/)
- [Maui Community Toolkit](https://github.com/CommunityToolkit/Maui)

## 👤 Author

Created by [Your Name]

---

**Happy coding! 🎉**
