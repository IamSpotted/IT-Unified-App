using MauiApp1.Models;

namespace MauiApp1.Interfaces
{
    /// <summary>
    /// Service interface for handling device editing operations including form population and state management
    /// </summary>
    public interface IEditDeviceService : ISingletonService
    {
        /// <summary>
        /// Populates the form fields with device data and sets edit mode
        /// </summary>
        /// <param name="device">The device to edit</param>
        /// <param name="viewModel">The view model to populate</param>
        /// <returns>Task representing the operation</returns>
        Task PopulateFormForEditAsync(Models.Device device, object viewModel);
        
        /// <summary>
        /// Clears the edit form and resets edit mode
        /// </summary>
        /// <param name="viewModel">The view model to clear</param>
        /// <returns>Task representing the operation</returns>
        Task ClearEditFormAsync(object viewModel);
        
        /// <summary>
        /// Sets the edit mode state and UI indicators
        /// </summary>
        /// <param name="viewModel">The view model to update</param>
        /// <param name="isEditMode">Whether to enable or disable edit mode</param>
        /// <param name="selectedDevice">The device being edited (null to clear)</param>
        /// <returns>Task representing the operation</returns>
        Task SetEditModeAsync(object viewModel, bool isEditMode, Models.Device? selectedDevice = null);
    }
}
