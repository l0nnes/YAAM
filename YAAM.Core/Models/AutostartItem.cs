namespace YAAM.Core.Models;

public class AutostartItem
{
    public required string Name { get; set; }
    public required string ExecutablePath { get; set; }
    public string? Arguments { get; set; }
    public required AutostartType Type { get; set; }
    public required string Location { get; set; }
    public bool IsEnabled { get; set; }
}