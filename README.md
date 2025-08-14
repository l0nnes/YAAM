# YAAM - Yet Another Autostart Manager 🚀

![Build and Release](https://github.com/l0nnes/YAAM/actions/workflows/release.yml/badge.svg)

Welcome to **YAAM**! A powerful and flexible .NET library designed to simplify the management of Windows autostart applications. Whether you're building a system utility, a security tool, or just need programmatic control over what runs at startup, YAAM provides a unified and straightforward API to handle it all. 🛡️

---

## ✨ Features

- **Unified Interface**: Manage autostart entries from different sources with a single, consistent API.
- **Multiple Providers**: Out-of-the-box support for common autostart locations:
  - 📂 Windows Registry (`HKEY_CURRENT_USER` and `HKEY_LOCAL_MACHINE`)
  - ⚙️ Windows Services (focusing on third-party services)
  - 🗓️ Windows Task Scheduler
- **Comprehensive Control**: Enumerate, create, modify, delete, enable, and disable autostart items.
- **Smart Filtering**: Automatically filters out core Microsoft services, allowing you to focus on what matters.
- **Detailed Information**: Retrieves executable paths, arguments, and status for each autostart entry.

---

## 🛠️ Tech Stack & Dependencies

YAAM is built with modern and reliable technologies:

- **.NET 8**: The core library is written in C# and targets the latest version of .NET for maximum performance and security.

This project relies on several great open-source libraries:

- **[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)**: Provides modern, fast, and modular MVVM components. Used for the underlying architecture of the library. (MIT License)
- **[TaskScheduler](https://github.com/dahall/TaskScheduler)**: A wrapper for the Windows Task Scheduler, providing easy access to manage scheduled tasks. (MIT License)
- **[System.Management.Automation](https://www.nuget.org/packages/System.Management.Automation)**: The engine for PowerShell, used for advanced system queries and operations. (Microsoft Public License)
- **[System.ServiceProcess.ServiceController](https://www.nuget.org/packages/System.ServiceProcess.ServiceController)**: Provides classes to manage Windows services. (MIT License)

For automation and CI/CD, this project uses:

- **[GitHub Actions](https://github.com/features/actions)**: To automatically build the library and create releases. 🤖
- **[Semantic Release](https://github.com/semantic-release/semantic-release)**: For automated version management and `CHANGELOG` generation. 📦

---

## 📦 Getting Started

### Installation

The easiest way to use YAAM.Core is to grab the latest `YAAM.Core.dll` from our [**GitHub Releases**](https://github.com/l0nnes/YAAM/releases) page.

Once downloaded, simply add a reference to the DLL in your .NET project.

### Basic Usage

Using YAAM is simple. Here’s how you can get a list of all autostart items from all available providers:

```csharp
using YAAM.Core.Interfaces;
using YAAM.Core.Models;
using YAAM.Core.Services;

public class AutostartManager
{
    private readonly List<IAutostartProvider> _providers;

    public AutostartManager()
    {
        // Initialize all available providers
        _providers = new List<IAutostartProvider>
        {
            new RegistryAutostartProvider(),
            new ServiceAutostartProvider(),
            new TaskSchedulerAutostartProvider() // Make sure to add this!
        };
    }

    public async Task<List<AutostartItem>> GetAllAutostartItemsAsync()
    {
        var allItems = new List<AutostartItem>();
        foreach (var provider in _providers)
        {
            var items = await provider.GetAutostartItemsAsync();
            allItems.AddRange(items);
        }
        return allItems;
    }
}

// Example of how to use it:
var manager = new AutostartManager();
var startupItems = await manager.GetAllAutostartItemsAsync();

foreach (var item in startupItems)
{
    Console.WriteLine($"▶️ {item.Name} ({item.Type})");
    Console.WriteLine($"  Path: {item.ExecutablePath}");
    Console.WriteLine($"  Enabled: {item.IsEnabled}");
    Console.WriteLine("--------------------------");
}
```

---

## 🏗️ Building from Source

If you want to build the project yourself, follow these steps:

1.  **Clone the repository:**
    ```sh
    git clone https://github.com/l0nnes/YAAM.git
    cd YAAM
    ```

2.  **Restore dependencies and build the project:**
    ```sh
    dotnet build YAAM.Core/YAAM.Core.csproj --configuration Release
    ```

    The output DLL will be located in `YAAM.Core/bin/Release/net8.0/`.

---

## 🤝 Contributing

Contributions are welcome! If you have ideas for new features, bug fixes, or improvements, feel free to open an issue or submit a pull request. 💖

---

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
