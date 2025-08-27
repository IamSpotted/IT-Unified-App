using CommunityToolkit.Mvvm.Input;

namespace MauiApp1.Interfaces;

/// <summary>
/// Interface for ViewModels that support automatic data loading
/// </summary>
public interface ILoadableViewModel : IViewModel
{
    /// <summary>
    /// Command to load data for the ViewModel
    /// </summary>
    IAsyncRelayCommand LoadDataCommand { get; }
}
