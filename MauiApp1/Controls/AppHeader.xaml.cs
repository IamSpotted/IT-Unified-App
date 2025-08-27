using Microsoft.Maui.Controls;
using System;
using MauiApp1.Interfaces;

namespace MauiApp1.Controls
{
    public partial class AppHeader : ContentView
    {
        private readonly IAdminService? _adminService;

        public AppHeader()
        {
            InitializeComponent();
            
            // Try to get the AdminService from DI
            try
            {
                _adminService = IPlatformApplication.Current?.Services?.GetService<IAdminService>();
            }
            catch
            {
                _adminService = null;
            }
            
            Loaded += OnAppHeaderLoaded;
        }

        private void OnAppHeaderLoaded(object? sender, EventArgs e)
        {
            // Auto-detect page name from the parent page
            DetectPageTitle();
            
            // Update admin status display
            UpdateAdminStatus();
        }

        private void UpdateAdminStatus()
        {
            if (_adminService != null)
            {
                AdminIconLabel.Text = _adminService.AdminStatusIcon;
                AdminTextLabel.Text = _adminService.AdminStatusText;
                
                // Update text color based on admin status
                if (_adminService.IsRunningAsAdmin)
                {
                    AdminTextLabel.TextColor = Colors.Green; // Green for admin
                }
                else
                {
                    AdminTextLabel.TextColor = Colors.Orange; // Orange for user
                }
            }
        }

        private void DetectPageTitle()
        {
            try
            {
                // Walk up the visual tree to find the ContentPage
                Element current = this.Parent;
                while (current != null && !(current is ContentPage))
                {
                    current = current.Parent;
                }

                if (current is ContentPage page)
                {
                    string pageName = GetPageDisplayName(page.GetType().Name);
                    PageTitleLabel.Text = pageName;
                }
            }
            catch
            {
                // Fallback if detection fails
                PageTitleLabel.Text = "ITSF Network Tools";
            }
        }

        private string GetPageDisplayName(string pageTypeName)
        {
            // Remove "Page" suffix and convert to display name
            return pageTypeName switch
            {
                "CamerasPage" => "Cameras",
                "PrintersPage" => "Printers", 
                "NetworkingPage" => "Networking",
                "NetopsPage" => "NetOps",
                "ScriptsPage" => "Scripts",
                "SettingsPage" => "Settings",
                _ => "ITSF Network Tools"
            };
        }

        private void OnThemeToggleTapped(object sender, EventArgs e)
        {
            try
            {
                // Toggle between light and dark theme
                if (Application.Current?.UserAppTheme == AppTheme.Light)
                {
                    Application.Current.UserAppTheme = AppTheme.Dark;
                    ThemeToggle.Text = "☀️";
                }
                else
                {
                    Application.Current!.UserAppTheme = AppTheme.Light;
                    ThemeToggle.Text = "🌙";
                }
            }
            catch (Exception ex)
            {
                // Handle any theme switching errors silently
                System.Diagnostics.Debug.WriteLine($"Theme toggle error: {ex.Message}");
            }
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();
            if (Parent != null)
            {
                // Detect page title when parent is set
                DetectPageTitle();
                
                // Update admin status when parent is set
                UpdateAdminStatus();
            }
        }
    }
}
