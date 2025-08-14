using CommunityToolkit.Mvvm.ComponentModel;

namespace YAAM.Core.Models;

public partial class AutostartItem : ObservableObject
{
    public required string Name { get; set; }
    public required string ExecutablePath { get; set; }
    public string? Arguments { get; set; }
    public required AutostartType Type { get; set; }
    public required string Location { get; set; }

    [ObservableProperty]
    private bool _isEnabled;
}