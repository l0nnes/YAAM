using Microsoft.Win32.TaskScheduler;
using YAAM.Core.Interfaces;
using YAAM.Core.Models;
using Task = System.Threading.Tasks.Task;

namespace YAAM.Core.Services;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы",
    Justification = "<Ожидание>")]
public class ScheduledTaskAutostartProvider : IAutostartProvider
{
    public AutostartType Type => AutostartType.ScheduledTask;

    public Task<IEnumerable<AutostartItem>> GetAutostartItemsAsync()
    {
        return Task.Run<IEnumerable<AutostartItem>>(() =>
        {
            var items = new List<AutostartItem>();

            var taskService = TaskService.Instance;

            foreach (var task in taskService.AllTasks)
            {
                var isAutostart = task.Definition.Triggers.Any(t =>
                    t.TriggerType is TaskTriggerType.Logon or TaskTriggerType.Boot);

                if (!isAutostart) continue;

                if (task.Path.StartsWith(@"\Microsoft\")) continue;

                var path = "N/A";
                string? arguments = null;
                if (task.Definition.Actions.FirstOrDefault() is ExecAction execAction)
                {
                    path = execAction.Path;
                    arguments = execAction.Arguments;
                }

                items.Add(new AutostartItem
                {
                    Name = task.Name,
                    ExecutablePath = path,
                    Arguments = arguments,
                    Type = AutostartType.ScheduledTask,
                    IsEnabled = task.Definition.Settings.Enabled,
                    Location = task.Path
                });
            }

            return items;
        });
    }

    public Task EnableAutostartItem(AutostartItem item) => Task.Run(() => SetTaskStateAsync(item, true));

    public Task DisableAutostartItem(AutostartItem item) => Task.Run(() => SetTaskStateAsync(item, false));

    public Task CreateAutostartItem(AutostartItem item)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new ArgumentException("Task name cannot be empty.", nameof(item.Name));
            if (string.IsNullOrWhiteSpace(item.ExecutablePath))
                throw new ArgumentException("Executable path cannot be empty.", nameof(item.ExecutablePath));

            var taskService = TaskService.Instance;
            var td = taskService.NewTask();

            td.RegistrationInfo.Description = "Autostart task created by YAAM";
            td.Principal.LogonType = TaskLogonType.InteractiveToken;

            td.Triggers.Add(new LogonTrigger());

            td.Actions.Add(new ExecAction(item.ExecutablePath, item.Arguments));

            td.Settings.StopIfGoingOnBatteries = false;
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;

            taskService.RootFolder.RegisterTaskDefinition(item.Name, td);
        });
    }

    public Task ModifyAutostartItem(AutostartItem originalItem, AutostartItem modifiedItem)
    {
        return Task.Run(() =>
        {
            if (originalItem.Name != modifiedItem.Name)
            {
                DeleteAutostartItem(originalItem).Wait();
                CreateAutostartItem(modifiedItem).Wait();
                return;
            }

            using var ts = new TaskService();
            var task = ts.GetTask(originalItem.Location);
            if (task == null)
                throw new InvalidOperationException($"Task not found at path: {originalItem.Location}");

            var action = task.Definition.Actions.OfType<ExecAction>().FirstOrDefault();
            if (action == null)
                throw new InvalidOperationException("Task does not have an executable action to modify.");

            action.Path = modifiedItem.ExecutablePath;
            action.Arguments = modifiedItem.Arguments;

            task.RegisterChanges();
        });
    }

    public Task DeleteAutostartItem(AutostartItem item)
    {
        return Task.Run(() =>
        {
            var taskService = TaskService.Instance;
            var task = taskService.GetTask(item.Location);
            task?.Folder.DeleteTask(task.Name);
        });
    }

    private static void SetTaskStateAsync(AutostartItem autostartItem, bool enabled)
    {
        var taskService = TaskService.Instance;
        var task = taskService.GetTask(autostartItem.Location);
        if (task != null)
        {
            task.Enabled = enabled;

            autostartItem.IsEnabled = enabled;
        }
        else
        {
            throw new InvalidOperationException($"Task not found at path: {autostartItem.Location}");
        }
    }
}