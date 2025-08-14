using YAAM.Core.Models;

namespace YAAM.Core.Interfaces;

public interface IAutostartProvider
{
    AutostartType Type { get; }
    Task<IEnumerable<AutostartItem>> GetAutostartItemsAsync();
    Task EnableAutostartItem(AutostartItem item);
    Task DisableAutostartItem(AutostartItem item);
    Task CreateAutostartItem(AutostartItem item);
    Task ModifyAutostartItem(AutostartItem originalItem, AutostartItem modifiedItem);
    Task DeleteAutostartItem(AutostartItem item);
}