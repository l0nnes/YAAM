using YAAM.Core.Interfaces;
using YAAM.Core.Models;

namespace YAAM.Core;

public class AutostartManager(IEnumerable<IAutostartProvider> providers)
{
    public async Task<List<AutostartItem>> GetAllItemsAsync()
    {
        var allItems = new List<AutostartItem>();
        var allTasks = providers.Select(p => p.GetAutostartItemsAsync());
        var results = await Task.WhenAll(allTasks);

        foreach (var result in results)
        {
            allItems.AddRange(result);
        }
        return allItems;
    }

    public async Task ToggleEnableStateAsync(AutostartItem item)
    {
        var provider = FindProvider(item.Type);
        if (item.IsEnabled)
        {
            await provider.DisableAutostartItem(item);
        }
        else
        {
            await provider.EnableAutostartItem(item);
        }
    }

    public Task CreateItemAsync(AutostartItem newItem)
    {
        var provider = FindProvider(newItem.Type);
        return provider.CreateAutostartItem(newItem);
    }

    public Task ModifyItemAsync(AutostartItem originalItem, AutostartItem modifiedItem)
    {
        var provider = FindProvider(originalItem.Type);
        return provider.ModifyAutostartItem(originalItem, modifiedItem);
    }

    public Task DeleteItemAsync(AutostartItem item)
    {
        var provider = FindProvider(item.Type);
        return provider.DeleteAutostartItem(item);
    }

    private IAutostartProvider FindProvider(AutostartType type)
    {
        var provider = providers.FirstOrDefault(p => p.Type == type);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider for type {type} not found.");
        }
        return provider;
    }
}