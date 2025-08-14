using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using YAAM.Core.Interfaces;
using YAAM.Core.Models;
using YAAM.Core.Utils;
using YAAM.Core.Utils.Native;

namespace YAAM.Core.Services;

[SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы")]
public class ServiceAutostartProvider : IAutostartProvider
{
    public AutostartType Type => AutostartType.ThirdPartyService;

    public Task<IEnumerable<AutostartItem>> GetAutostartItemsAsync()
    {
        return Task.Run(() =>
        {
            var items = new List<AutostartItem>();
            using var servicesKey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Services");
            if (servicesKey == null) return Enumerable.Empty<AutostartItem>();

            foreach (var service in ServiceController.GetServices())
            {
                try
                {
                    using var serviceKey = servicesKey.OpenSubKey(service.ServiceName);
                    if (serviceKey == null) continue;

                    var startMode = (int?)serviceKey.GetValue("Start") ?? 3; // 3 = Manual
                    var imagePath = serviceKey.GetValue("ImagePath")?.ToString() ?? string.Empty;

                    if (startMode != 2 && startMode != 4) continue; // 2 = Auto, 4 = Disabled

                    var parsedCommand = ParseCommandLine(imagePath);
                    if (SignatureChecker.IsMicrosoftSigned(parsedCommand.ExecutablePath)) continue;

                    items.Add(new AutostartItem
                    {
                        Name = service.DisplayName,
                        ExecutablePath = parsedCommand.ExecutablePath,
                        Arguments = parsedCommand.Arguments,
                        Type = AutostartType.ThirdPartyService,
                        IsEnabled = startMode == 2,
                        Location = service.ServiceName
                    });
                }
                catch (Exception)
                {
                    /* Ignore services we can't access */
                }
            }

            return items;
        });
    }

    public Task CreateAutostartItem(AutostartItem item)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(item.Location))
                throw new ArgumentException("Service system name (Location) cannot be empty.", nameof(item.Location));
            if (string.IsNullOrWhiteSpace(item.ExecutablePath))
                throw new ArgumentException("Executable path cannot be empty.", nameof(item.ExecutablePath));

            ServiceManager.CreateService(item);

            if (!item.IsEnabled)
            {
                SetServiceStartValue(item, false);
            }
        });
    }

    public Task ModifyAutostartItem(AutostartItem originalItem, AutostartItem modifiedItem)
    {
        return Task.Run(() =>
        {
            if (originalItem.Location != modifiedItem.Location)
            {
                DeleteAutostartItem(originalItem).Wait();
                CreateAutostartItem(modifiedItem).Wait();
            }
            else
            {
                ServiceManager.ModifyService(modifiedItem);
            }
        });
    }

    public Task DeleteAutostartItem(AutostartItem item) => Task.Run(() => ServiceManager.DeleteService(item.Location));

    public Task EnableAutostartItem(AutostartItem item) => Task.Run(() => SetServiceStartValue(item, true));

    public Task DisableAutostartItem(AutostartItem item) => Task.Run(() => SetServiceStartValue(item, false));

    private static (string ExecutablePath, string? Arguments) ParseCommandLine(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return (string.Empty, null);

        var path = CommandLineHelper.GetFileName(commandLine);
        string? args = null;

        var pathInCmd = commandLine.Contains($"\"{path}\"") ? $"\"{path}\"" : path;
        var pathEndIndex = commandLine.IndexOf(pathInCmd, StringComparison.OrdinalIgnoreCase) + pathInCmd.Length;

        if (pathEndIndex < commandLine.Length)
        {
            args = commandLine[pathEndIndex..].Trim();
        }

        return (path, args);
    }

    private static void SetServiceStartValue(AutostartItem autostartItem, bool enabled)
    {
        var keyPath = @$"SYSTEM\CurrentControlSet\Services\{autostartItem.Location}";
        using var key = Registry.LocalMachine.OpenSubKey(keyPath, true);
        if (key == null)
        {
            throw new InvalidOperationException($"Service registry key not found for '{autostartItem.Location}'");
        }

        key.SetValue("Start", enabled ? 2 : 4, RegistryValueKind.DWord);
    }
}