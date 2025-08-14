using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using YAAM.Core.Interfaces;
using YAAM.Core.Models;
using YAAM.Core.Services;
using YAAM.Core.ViewModels;

namespace AutostartManager.Tester
{

    class Program
    {
        private static MainViewModel _viewModel;
        private static List<AutostartItem> _itemsToDisplay;

        static async Task Main(string[] args)
        {
            var providers = new List<IAutostartProvider>
            {
                new RegistryAutostartProvider(),
                new ServiceAutostartProvider(),
                new ScheduledTaskAutostartProvider()
            };
            _viewModel = new MainViewModel(providers);

            Console.WriteLine("YAAM - Yet Another Autostart Manager (Full Control Tester)");
            Console.WriteLine("---------------------------------------------------------");
            Console.WriteLine("NOTE: This application requires administrator privileges for full functionality.");

            while (true)
            {
                await RefreshAndDisplayItems();

                Console.WriteLine("\nChoose an action:");
                Console.WriteLine(" (C)reate  (M)odify  (D)elete  (T)oggle Enable/Disable");
                Console.WriteLine(" (R)efresh list, or (Q)uit.");
                Console.Write("> ");
                var choice = Console.ReadLine()?.ToUpper();

                var actionTask = choice switch
                {
                    "C" => HandleCreate(),
                    "M" => HandleModify(),
                    "D" => HandleDelete(),
                    "T" => HandleToggle(),
                    "R" => Task.CompletedTask, 
                    "Q" => null, 
                    _ => Task.Run(() => Log("Invalid choice. Please try again.", ConsoleColor.Red))
                };

                if (actionTask == null) break; 
                
                await actionTask;

                if (choice != "R") await Task.Delay(1000);
            }

            Console.WriteLine("\nApplication exited.");
        }
        
        private static async Task HandleCreate()
        {
            Log("--- Create New Autostart Item ---", ConsoleColor.Yellow);
            Console.WriteLine("Choose type: (1) Registry  (2) Scheduled Task  (3) Service");
            var typeChoice = Console.ReadLine();

            AutostartItem newItem;
            try
            {
                newItem = typeChoice switch
                {
                    "1" => PromptForItemDetails(AutostartType.Registry),
                    "2" => PromptForItemDetails(AutostartType.ScheduledTask),
                    "3" => PromptForItemDetails(AutostartType.ThirdPartyService),
                    _ => throw new InvalidOperationException("Invalid type selected.")
                };
            }
            catch (Exception ex)
            {
                Log(ex.Message, ConsoleColor.Red);
                return;
            }

            try
            {
                await _viewModel.CreateItemCommand.ExecuteAsync(newItem);
                Log($"Successfully created item '{newItem.Name}'!", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                Log($"ERROR creating item: {ex.InnerException?.Message ?? ex.Message}", ConsoleColor.Red);
            }
        }

        private static async Task HandleModify()
        {
            Log("--- Modify Existing Item ---", ConsoleColor.Yellow);
            var originalItem = SelectItem("Enter the number of the item to modify:");
            if (originalItem == null) return;
            
            Log($"Modifying '{originalItem.Name}'. Press Enter to keep current value.", ConsoleColor.Cyan);

            var modifiedItem = new AutostartItem
            {
                Name = originalItem.Name,
                ExecutablePath = originalItem.ExecutablePath,
                Arguments = originalItem.Arguments,
                Location = originalItem.Location, 
                Type = originalItem.Type,
                IsEnabled = originalItem.IsEnabled
            };

            if (originalItem.Type is AutostartType.ThirdPartyService or AutostartType.ScheduledTask)
            {
                modifiedItem.Location = Prompt($"System Name/ID (Key)", originalItem.Location);
            }
            modifiedItem.Name = Prompt($"Display Name", originalItem.Name);
            modifiedItem.ExecutablePath = Prompt("Executable Path", originalItem.ExecutablePath);
            modifiedItem.Arguments = Prompt("Arguments", originalItem.Arguments);
            
            try
            {
                await _viewModel.ModifyItemCommand.ExecuteAsync(Tuple.Create(originalItem, modifiedItem));
                Log($"Successfully modified item '{modifiedItem.Name}'!", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                Log($"ERROR modifying item: {ex.InnerException?.Message ?? ex.Message}", ConsoleColor.Red);
            }
        }

        private static async Task HandleDelete()
        {
            Log("--- Delete Existing Item ---", ConsoleColor.Yellow);
            var itemToDelete = SelectItem("Enter the number of the item to delete:");
            if (itemToDelete == null) return;

            Log($"Are you sure you want to PERMANENTLY delete '{itemToDelete.Name}'? (y/n)", ConsoleColor.Red);
            if (Console.ReadLine()?.ToLower() != "y")
            {
                Log("Delete operation cancelled.", ConsoleColor.White);
                return;
            }
            
            try
            {
                await _viewModel.DeleteItemCommand.ExecuteAsync(itemToDelete);
                Log($"Successfully deleted item '{itemToDelete.Name}'!", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                Log($"ERROR deleting item: {ex.InnerException?.Message ?? ex.Message}", ConsoleColor.Red);
            }
        }

        private static async Task HandleToggle()
        {
            Log("--- Toggle Item State ---", ConsoleColor.Yellow);
            var itemToToggle = SelectItem("Enter the number of the item to toggle:");
            if (itemToToggle == null) return;
            
            try
            {
                await _viewModel.ToggleEnableStateCommand.ExecuteAsync(itemToToggle);
                Log($"Successfully changed state for '{itemToToggle.Name}'!", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                Log($"ERROR toggling item state: {ex.InnerException?.Message ?? ex.Message}", ConsoleColor.Red);
            }
        }

        private static async Task RefreshAndDisplayItems()
        {
            Log("Loading autostart items...", ConsoleColor.White);
            await _viewModel.LoadItemsCommand.ExecuteAsync(null);

            _itemsToDisplay = _viewModel.Items
                .OrderBy(i => i.Type)
                .ThenBy(i => i.Name)
                .ToList();

            if (!_itemsToDisplay.Any())
            {
                Console.WriteLine("No autostart items found.");
                return;
            }
            
            Console.WriteLine("\n--- Current Autostart Items ---");
            int index = 1;
            foreach (var item in _itemsToDisplay)
            {
                var status = item.IsEnabled ? "Enabled" : "Disabled";
                var color = item.IsEnabled ? ConsoleColor.Green : ConsoleColor.Red;

                Console.Write($"{index,3}. ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{item.Type,-17}]");
                Console.ResetColor();
                Console.Write($" {item.Name}");

                Console.ForegroundColor = color;
                Console.WriteLine($" ({status})");
                Console.ResetColor();

                Console.WriteLine($"\tPath: {item.ExecutablePath}");
                if (!string.IsNullOrEmpty(item.Arguments))
                    Console.WriteLine($"\tArgs: {item.Arguments}");
                Console.WriteLine($"\tLocation: {item.Location}");
                index++;
            }
            Console.WriteLine("--------------------------\n");
        }
        
        private static AutostartItem SelectItem(string prompt)
        {
            Console.WriteLine(prompt);
            Console.Write("> ");
            if (int.TryParse(Console.ReadLine(), out int itemIndex) && itemIndex > 0 && itemIndex <= _itemsToDisplay.Count)
            {
                return _itemsToDisplay[itemIndex - 1];
            }
            Log("Invalid selection.", ConsoleColor.Red);
            return null;
        }

        private static AutostartItem PromptForItemDetails(AutostartType type)
        {
            var item = new AutostartItem
            {
                Type = type,
                Name = null,
                ExecutablePath = null,
                Location = null
            };
            
            if (type == AutostartType.Registry)
            {
                Console.WriteLine("Create in: (1) CurrentUser (recommended)  (2) LocalMachine");
                var hiveChoice = Console.ReadLine();
                item.Location = hiveChoice == "2" 
                    ? @"LocalMachine\Software\Microsoft\Windows\CurrentVersion\Run"
                    : @"CurrentUser\Software\Microsoft\Windows\CurrentVersion\Run";
            }
            
            item.Name = Prompt("Entry/Display Name");
            
            if (type is AutostartType.ThirdPartyService or AutostartType.ScheduledTask)
            {
                item.Location = Prompt("System Name / ID (must be unique)");
            }

            item.ExecutablePath = Prompt("Executable Path (e.g., C:\\Path\\To\\App.exe)");
            item.Arguments = Prompt("Arguments (optional)");
            
            var enabled = Prompt("Enable by default? (y/n)", "y");
            item.IsEnabled = enabled.ToLower() == "y";

            if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.ExecutablePath))
                throw new ArgumentException("Name and Executable Path cannot be empty.");
            
            if (string.IsNullOrWhiteSpace(item.Location)) item.Location = item.Name;
            
            return item;
        }

        private static string Prompt(string message, string defaultValue = null)
        {
            if (defaultValue != null)
            {
                Console.Write($"{message} [{defaultValue}]: ");
            }
            else
            {
                Console.Write($"{message}: ");
            }
            var input = Console.ReadLine();
            return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
        }
        
        private static void Log(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

    }
}