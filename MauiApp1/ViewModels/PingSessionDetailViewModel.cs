using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MauiApp1.Models;

namespace MauiApp1.ViewModels;

[QueryProperty(nameof(SessionId), "sessionId")]
public partial class PingSessionDetailViewModel : BaseViewModel
{
    private readonly MultiPingViewModel _multiPingViewModel;

    [ObservableProperty]
    private string _sessionId = string.Empty;

    [ObservableProperty]
    private PingSession? _session;

    [ObservableProperty]
    private bool _hasSession = false;

    public PingSessionDetailViewModel(
        ILogger<PingSessionDetailViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService,
        MultiPingViewModel multiPingViewModel)
        : base(logger, navigationService, dialogService)
    {
        Title = "Ping Session Details";
        _multiPingViewModel = multiPingViewModel;
    }

    partial void OnSessionIdChanged(string value)
    {
        LoadSession(value);
    }

    private void LoadSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            Session = null;
            HasSession = false;
            return;
        }

        // Find the session in the MultiPingViewModel
        var session = _multiPingViewModel.PingSessions.FirstOrDefault(s => s.Id == sessionId);
        
        if (session != null)
        {
            Session = session;
            HasSession = true;
            Title = $"Ping Details - {session.Target}";
            
            _logger.LogInformation("Loaded ping session details for {Target} (ID: {SessionId})", 
                session.Target, sessionId);
        }
        else
        {
            Session = null;
            HasSession = false;
            Title = "Ping Session Details";
            
            _logger.LogWarning("Could not find ping session with ID: {SessionId}", sessionId);
        }
    }

    [RelayCommand]
    private async Task StartSession()
    {
        if (Session == null) return;

        try
        {
            _multiPingViewModel.StartSessionCommand.Execute(Session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start ping session from detail view");
            await _dialogService.ShowAlertAsync("Error", $"Failed to start session: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StopSession()
    {
        if (Session == null) return;

        try
        {
            _multiPingViewModel.StopSessionCommand.Execute(Session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop ping session from detail view");
            await _dialogService.ShowAlertAsync("Error", $"Failed to stop session: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RemoveSession()
    {
        if (Session == null) return;

        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Remove Session", 
                $"Are you sure you want to remove the ping session for {Session.Target}?",
                "Remove", 
                "Cancel");

            if (confirmed)
            {
                _multiPingViewModel.RemoveSessionCommand.Execute(Session);
                
                // Navigate back to dashboard
                await _navigationService.GoBackAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove ping session from detail view");
            await _dialogService.ShowAlertAsync("Error", $"Failed to remove session: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        try
        {
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate back from ping session detail view");
            await _dialogService.ShowAlertAsync("Error", $"Failed to go back: {ex.Message}");
        }
    }
}
