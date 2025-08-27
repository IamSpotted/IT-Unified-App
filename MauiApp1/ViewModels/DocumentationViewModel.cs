using MauiApp1.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MauiApp1.ViewModels;

public partial class DocumentationViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _selectedDocumentTitle = "Welcome to IT Support Framework";

    [ObservableProperty]
    private string _selectedDocumentContent = "";

    [ObservableProperty]
    private bool _isDocumentSelected = false;

    public List<DocumentationItem> DocumentationItems { get; }

    public DocumentationViewModel(
        ILogger<DocumentationViewModel> logger,
        INavigationService navigationService,
        IDialogService dialogService) 
        : base(logger, navigationService, dialogService)
    {
        Title = "Documentation";

        // Initialize documentation items
        DocumentationItems = new List<DocumentationItem>
        {
            new DocumentationItem
            {
                Title = "📖 Application Overview",
                Category = "General",
                Icon = "📖",
                Description = "Complete overview of the IT Support Framework application",
                ContentType = "overview"
            },
            new DocumentationItem
            {
                Title = "🔐 RBAC Setup Guide",
                Category = "Security",
                Icon = "🔐",
                Description = "Role-Based Access Control setup and configuration",
                ContentType = "rbac"
            },
            new DocumentationItem
            {
                Title = "🛡️ Security Implementation",
                Category = "Security", 
                Icon = "🛡️",
                Description = "Comprehensive security features and implementation details",
                ContentType = "security"
            },
            new DocumentationItem
            {
                Title = "🏢 Non-Domain Database Setup",
                Category = "Configuration",
                Icon = "🏢",
                Description = "Database configuration for non-domain environments",
                ContentType = "nondomain"
            },
            new DocumentationItem
            {
                Title = "⚙️ Settings Configuration",
                Category = "Configuration",
                Icon = "⚙️", 
                Description = "Application settings and configuration options",
                ContentType = "settings"
            },
            new DocumentationItem
            {
                Title = "🔒 AD Groups Security",
                Category = "Security",
                Icon = "🔒",
                Description = "Active Directory groups security enhancement details",
                ContentType = "adgroups"
            },
            new DocumentationItem
            {
                Title = "🛡️ SQL Injection Protection",
                Category = "Security",
                Icon = "🛡️",
                Description = "SQL injection prevention and input validation",
                ContentType = "sqlprotection"
            },
            new DocumentationItem
            {
                Title = "📝 Security Enhancement Summary",
                Category = "Security",
                Icon = "📝",
                Description = "Summary of all security enhancements and changes",
                ContentType = "securitysummary"
            }
        };

        // Load default content
        LoadWelcomeContent();
    }

    [RelayCommand]
    private async Task LoadDocument(DocumentationItem item)
    {
        if (item == null) return;

        await ExecuteSafelyAsync(async () =>
        {
            SelectedDocumentTitle = item.Title;
            IsDocumentSelected = true;

            try
            {
                SelectedDocumentContent = item.ContentType switch
                {
                    "overview" => await LoadApplicationOverview(),
                    "rbac" => await LoadFileContent("Documentation/RBAC_Setup_Guide.md"),
                    "security" => await LoadFileContent("Documentation/Domain_Security_Implementation.md"),
                    "nondomain" => await LoadFileContent("Documentation/NonDomain_Database_Example.md"),
                    "settings" => await LoadFileContent("Documentation/Settings_UI_Example.md"),
                    "adgroups" => await LoadFileContent("Documentation/AD_Groups_Security_Enhancement.md"),
                    "sqlprotection" => await LoadFileContent("Documentation/SQL_Injection_Protection.md"),
                    "securitysummary" => await LoadFileContent("Documentation/Security_Enhancement_Summary.md"),
                    _ => "# Document Not Found\n\nThe requested documentation could not be loaded."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load documentation content for {ContentType}", item.ContentType);
                SelectedDocumentContent = $"# Error Loading Document\n\nFailed to load {item.Title}.\n\nError: {ex.Message}";
            }
        }, "Load Documentation");
    }

    [RelayCommand]
    private void BackToDocumentList()
    {
        IsDocumentSelected = false;
        SelectedDocumentTitle = "Welcome to IT Support Framework";
        LoadWelcomeContent();
    }

    [RelayCommand]
    private async Task SearchDocumentation()
    {
        await _dialogService.ShowAlertAsync("Search", 
            "Documentation search functionality will be implemented in a future update.");
    }

    private void LoadWelcomeContent()
    {
        SelectedDocumentContent = @"# 🖥️ IT Support Framework Documentation

Welcome to the comprehensive documentation for the IT Support Framework application!

## 📚 What's Available

This documentation center provides you with complete information about:

### 🔐 Security & Access Control
- **RBAC Setup Guide** - Configure role-based access control
- **Security Implementation** - Comprehensive security features
- **AD Groups Security** - Active Directory integration details
- **SQL Injection Protection** - Database security measures

### ⚙️ Configuration & Setup  
- **Non-Domain Database Setup** - Configuration for standalone environments
- **Settings Configuration** - Application settings and preferences

### 📖 General Information
- **Application Overview** - Complete feature overview and technical details
- **Security Enhancement Summary** - Summary of all security improvements

## 🎯 Getting Started

1. **New Users**: Start with the **Application Overview** to understand the system
2. **Administrators**: Review the **RBAC Setup Guide** for access control configuration  
3. **Security Teams**: Check the **Security Implementation** documentation
4. **Troubleshooting**: Refer to specific configuration guides based on your environment

## 💡 Tips for Using This Documentation

- 📱 **Always Available**: This documentation is built into the app - no internet required
- 🔍 **Searchable**: Use the search function to quickly find specific topics
- 📋 **Copy-Friendly**: You can select and copy text from any documentation page
- 🔄 **Always Current**: Documentation is updated with each app release

Select any documentation item from the list to get started!

---
*Last Updated: 20 August 2025 | Version: 0.6.0*";
    }

    private async Task<string> LoadApplicationOverview()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "APPLICATION_OVERVIEW.md");
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path);
            }
            else
            {
                // Fallback to hardcoded overview if file not found
                return GetHardcodedOverview();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load APPLICATION_OVERVIEW.md");
            return GetHardcodedOverview();
        }
    }

    private async Task<string> LoadFileContent(string relativePath)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, relativePath);
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path);
            }
            else
            {
                return $"# File Not Found\n\nThe documentation file `{relativePath}` could not be found.\n\nThis may indicate the file was not included in the application deployment.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load documentation file {Path}", relativePath);
            return $"# Error Loading File\n\nFailed to load `{relativePath}`.\n\nError: {ex.Message}";
        }
    }

    private string GetHardcodedOverview()
    {
        return @"# IT Support Framework - Application Overview

## 🏢 Executive Summary

**IT Support Framework** is a comprehensive enterprise-grade desktop application built with .NET MAUI 8.0 that revolutionizes IT device management and network operations.

## 🎯 Core Features

### Device Management
- **NetOps**: Network device management and remote PC connections
- **Printers**: Printer discovery and management
- **Cameras**: IP camera management and monitoring
- **Scripts**: Automation and maintenance scripts

### Security & Database
- **Role-Based Access Control** with Active Directory integration
- **Enterprise Database Integration** with SQL Server
- **Advanced Security Features** including SQL injection prevention
- **Comprehensive Audit Logging**

### Technical Excellence
- **.NET MAUI 8.0** cross-platform framework
- **MVVM Architecture** with dependency injection
- **Enterprise Security** with multi-layer protection
- **Real-Time Synchronization** with transaction safety

For complete details, please refer to the APPLICATION_OVERVIEW.md file in the application directory.";
    }
}

public class DocumentationItem
{
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Description { get; set; } = "";
    public string ContentType { get; set; } = "";
}
