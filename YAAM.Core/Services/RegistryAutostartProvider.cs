using Microsoft.Win32;
using YAAM.Core.Interfaces;
using YAAM.Core.Models;
using YAAM.Core.Utils.Native;

namespace YAAM.Core.Services;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы",
    Justification = "<Ожидание>")]
public class RegistryAutostartProvider : IAutostartProvider
{
    public AutostartType Type => AutostartType.Registry;

    private static readonly byte[] EnabledValue =
    [
        0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    private static readonly byte[] DisabledValue =
        [0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    public Task<IEnumerable<AutostartItem>> GetAutostartItemsAsync()
    {
        return Task.Run(() =>
        {
            var items = new List<AutostartItem>();
            items.AddRange(GetItemsFromHive(Registry.LocalMachine, "LocalMachine"));
            items.AddRange(GetItemsFromHive(Registry.CurrentUser, "CurrentUser"));
            return items.AsEnumerable();
        });
    }

    private static IEnumerable<AutostartItem> GetItemsFromHive(RegistryKey hive, string scopeName)
    {
        const string runPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string approvedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        var items = new List<AutostartItem>();

        using var runKey = hive.OpenSubKey(runPath);
        if (runKey == null) return items;

        using var approvedKey = hive.OpenSubKey(approvedPath);

        items.AddRange(from valueName in runKey.GetValueNames()
            let rawState = approvedKey?.GetValue(valueName) as byte[]
            let isEnabled = rawState == null || rawState.Length == 0 || rawState[0] % 2 == 0
            let commandLine = runKey.GetValue(valueName)?.ToString() ?? ""
            let parsedCommand = ParseCommandLine(commandLine)
            select new AutostartItem
            {
                Name = valueName,
                ExecutablePath = parsedCommand.ExecutablePath,
                Arguments = parsedCommand.Arguments,
                Type = AutostartType.Registry,
                IsEnabled = isEnabled,
                Location = $"{scopeName}\\{runPath}"
            });

        return items;
    }

    private static (string ExecutablePath, string? Arguments) ParseCommandLine(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return ("", null);
        }

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

    public Task EnableAutostartItem(AutostartItem item) => Task.Run(() => SetItemState(item, true));

    public Task DisableAutostartItem(AutostartItem item) => Task.Run(() => SetItemState(item, false));

    public Task CreateAutostartItem(AutostartItem item)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new ArgumentException("Item name cannot be empty.", nameof(item.Name));
            if (string.IsNullOrWhiteSpace(item.ExecutablePath))
                throw new ArgumentException("Executable path cannot be empty.", nameof(item.ExecutablePath));

            var (hive, runPath) = GetHiveAndPathFromLocation(item.Location);
            using var runKey = hive.OpenSubKey(runPath, writable: true)
                               ?? throw new InvalidOperationException(
                                   $"Registry key not found: {hive.Name}\\{runPath}");

            var command = BuildCommand(item);
            runKey.SetValue(item.Name, command, RegistryValueKind.String);
        });
    }

    public Task ModifyAutostartItem(AutostartItem originalItem, AutostartItem modifiedItem)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(modifiedItem.Name))
                throw new ArgumentException("Item name cannot be empty.", nameof(modifiedItem.Name));
            if (string.IsNullOrWhiteSpace(modifiedItem.ExecutablePath))
                throw new ArgumentException("Executable path cannot be empty.", nameof(modifiedItem.ExecutablePath));

            var (hive, runPath) = GetHiveAndPathFromLocation(originalItem.Location);
            using var runKey = hive.OpenSubKey(runPath, writable: true)
                               ?? throw new InvalidOperationException(
                                   $"Registry key not found: {hive.Name}\\{runPath}");

            if (originalItem.Name != modifiedItem.Name)
            {
                runKey.DeleteValue(originalItem.Name, false);
            }

            var command = BuildCommand(modifiedItem);
            runKey.SetValue(modifiedItem.Name, command, RegistryValueKind.String);
        });
    }

    public Task DeleteAutostartItem(AutostartItem item)
    {
        return Task.Run(() =>
        {
            var (hive, runPath) = GetHiveAndPathFromLocation(item.Location);
            const string approvedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

            using (var runKey = hive.OpenSubKey(runPath, writable: true))
            {
                runKey?.DeleteValue(item.Name, false);
            }

            using (var approvedKey = hive.OpenSubKey(approvedPath, writable: true))
            {
                approvedKey?.DeleteValue(item.Name, false);
            }
        });
    }

    private static void SetItemState(AutostartItem item, bool enabled)
    {
        try
        {
            var (hive, _) = GetHiveAndPathFromLocation(item.Location);
            const string approvedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

            using var key = hive.OpenSubKey(approvedPath, writable: true)
                            ?? throw new InvalidOperationException(
                                $"Registry key not found: {hive.Name}\\{approvedPath}");

            key.SetValue(item.Name, enabled ? EnabledValue : DisabledValue, RegistryValueKind.Binary);

            item.IsEnabled = enabled;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set item state. Item: {item}", ex);
        }
    }

    private static (RegistryKey hive, string path) GetHiveAndPathFromLocation(string location)
    {
        var parts = location.Split('\\');
        var hiveName = parts.FirstOrDefault();
        var path = string.Join("\\", parts.Skip(1));

        var hive = hiveName switch
        {
            "LocalMachine" => Registry.LocalMachine,
            "CurrentUser" => Registry.CurrentUser,
            _ => throw new ArgumentException(
                $"Invalid location format. Must start with 'LocalMachine' or 'CurrentUser'. Location: {location}")
        };

        return (hive, path);
    }

    private static string BuildCommand(AutostartItem item)
    {
        return $"\"{item.ExecutablePath}\" {item.Arguments}".Trim();
    }
}