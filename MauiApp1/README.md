# MauiApp1 - IT Support Framework Application

A comprehensive .NET MAUI cross-platform application for IT device management, network monitoring, and database administration with enterprise-level security features.

![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-8.0-blue)
![C#](https://img.shields.io/badge/C%23-12.0-green)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2019+-red)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Android%20%7C%20iOS%20%7C%20macOS-lightgrey)

## 📋 Table of Contents

1. [Overview](#overview)
2. [Features](#features)
   - [Device Management](#device-management)
   - [Database Integration](#database-integration)
   - [Security Features](#security-features)
   - [UI/UX Features](#uiux-features)
3. [Architecture](#architecture)
   - [Project Structure](#project-structure)
   - [Design Patterns](#design-patterns)
   - [Data Models](#data-models)
4. [Database Schema](#database-schema)
   - [Devices Table](#devices-table)
   - [Audit Table](#audit-table)
5. [Getting Started](#getting-started)
   - [Prerequisites](#prerequisites)
   - [Installation](#installation)
   - [Configuration](#configuration)
6. [Usage Guide](#usage-guide)
   - [Database Administration](#database-administration)
   - [Device Management](#device-management-usage)
   - [Network Operations](#network-operations)
   - [Filtering and Search](#filtering-and-search)
7. [Security Implementation](#security-implementation)
   - [SQL Injection Prevention](#sql-injection-prevention)
   - [Input Sanitization](#input-sanitization)
   - [Data Validation](#data-validation)
8. [API Documentation](#api-documentation)
   - [Services](#services)
   - [ViewModels](#viewmodels)
   - [Interfaces](#interfaces)
9. [Development](#development)
   - [Building the Project](#building-the-project)
   - [Testing](#testing)
   - [Contributing](#contributing)
10. [Troubleshooting](#troubleshooting)
11. [Performance Considerations](#performance-considerations)
12. [Future Enhancements](#future-enhancements)
13. [License](#license)

---

## Overview

MauiApp1 is an enterprise-grade IT Support Framework application built with .NET MAUI, designed for comprehensive device management, network monitoring, and database administration. The application provides a unified interface for managing printers, cameras, network devices, and other IT equipment with robust security features and real-time data synchronization.

### Key Highlights

- **Enterprise Security**: Advanced SQL injection prevention and input sanitization
- **Real-Time Data**: Automatic synchronization with SQL Server databases
- **Modular Architecture**: Clean separation of concerns with MVVM pattern
- **Extensible Design**: Easy to add new device types and features

---

## Features

### Device Management

- **Multi-Type Support**: Printers, Cameras, NetOp Devices, and Other equipment
- **Comprehensive Data**: 25+ fields including technical specifications, dates, and notes
- **CRUD Operations**: Create, Read, Update, Delete with transaction safety
- **Auto-Loading**: Automatic data refresh when navigating between pages
- **Real-Time Updates**: Immediate UI updates after database operations

### Database Integration

- **Dual-Table Architecture**: Synchronized ChaMPS and DeviceInfo tables
- **Foreign Key Management**: Proper referential integrity with cascade operations
- **Transaction Safety**: ACID compliance with automatic rollback on failures
- **Parameterized Queries**: Protection against SQL injection
- **Connection Management**: Secure credential storage and connection pooling

### Security Features

- **Input Sanitization**: Multi-layer validation with regex patterns
- **SQL Injection Prevention**: Comprehensive protection against attack vectors
- **Type Validation**: Field-specific format checking (IP addresses, URLs, etc.)
- **Length Limits**: Buffer overflow protection with configurable maximums
- **Error Handling**: Graceful failure with detailed logging

### UI/UX Features

- **Modern Design**: Clean, intuitive interface with dark/light theme support
- **Responsive Layout**: Adaptive UI for different screen sizes
- **Real-Time Feedback**: Loading indicators and status messages
- **Comprehensive Forms**: All database fields accessible through organized sections
- **Advanced Filtering**: Multi-criteria search with real-time results

---

## Architecture

### Project Structure

```
MauiApp1/
├── Controls/                 # Custom UI controls
│   ├── AppHeader.xaml       # Application header component
│   ├── BackButton.xaml      # Navigation back button
│   └── SevenSegmentClock.xaml # Digital clock display
├── Converters/              # Value converters for data binding
│   ├── BoolToColorConverter.cs
│   ├── DigitalClockConverter.cs
│   └── SevenSegmentConverter.cs
├── Extensions/              # Service collection extensions
│   └── ServiceCollectionExtensions.cs
├── Interfaces/              # Service contracts
│   ├── IAdminService.cs
│   ├── IDatabaseService.cs
│   ├── ILoadableViewModel.cs
│   └── INavigationService.cs
├── Models/                  # Data models
│   ├── Device.cs           # Main device model
│   ├── Camera.cs           # Camera-specific model
│   ├── Printer.cs          # Printer-specific model
│   └── DatabaseTestResult.cs
├── Services/                # Business logic services
│   ├── DatabaseService.cs  # Database operations
│   ├── InputSanitizer.cs   # Security and validation
│   ├── NavigationService.cs
│   └── SettingsService.cs
├── ViewModels/              # MVVM view models
│   ├── BaseViewModel.cs    # Common functionality
│   ├── DatabaseAdminViewModel.cs
│   ├── CamerasViewModel.cs
│   └── PrintersViewModel.cs
├── Views/                   # XAML pages
│   ├── DatabaseAdminPage.xaml
│   ├── CamerasPage.xaml
│   └── PrintersPage.xaml
└── Scripts/                 # Automation scripts
    ├── Network/
    ├── System/
    └── Utilities/
```

### Design Patterns

- **MVVM (Model-View-ViewModel)**: Clean separation of UI and business logic
- **Dependency Injection**: Loose coupling with service registration
- **Repository Pattern**: Abstracted data access through interfaces
- **Command Pattern**: UI actions handled through ICommand implementations
- **Observer Pattern**: Property change notifications with INotifyPropertyChanged

### Data Models

#### Device Model
```csharp
public class Device : IFilterable
{
    // Primary identification
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    
    // Location hierarchy
    public string Area { get; set; }
    public string Zone { get; set; }
    public string Line { get; set; }
    public string Column { get; set; }
    public string Level { get; set; }
    public string Pitch { get; set; }
    
    // Technical specifications
    public string? SerialNumber { get; set; }
    public string? AssetTag { get; set; }
    public string? MacAddress { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    
    // Network configuration
    public string? IpAddress { get; set; }
    public string Hostname { get; set; }
    public int Priority { get; set; }
    
    // Lifecycle management
    public DateTime? PurchaseDate { get; set; }
    public DateTime? ServiceDate { get; set; }
    public DateTime? WarrantyDate { get; set; }
    
    // Documentation
    public string? AdditionalNotes { get; set; }
    public string? WebLink { get; set; }
    public string? WebLinkName { get; set; }
    
    // Status
    public bool IsActive { get; set; }
}
```

---

## Database Schema

### ChaMPS Table

Primary table for device management with core operational data.

| Column | Type | Description |
|--------|------|-------------|
| DeviceID | int (PK, Identity) | Unique device identifier |
| Area | varchar(50) | Facility area designation |
| Line | varchar(50) | Production line identifier |
| Pitch | varchar(50) | Position within line |
| IPAddress | varchar(15) | Network IP address |
| Active | bit | Device operational status |
| Priority | int | Operational priority (1-10) |
| HostName | varchar(100) | Network hostname |

### DeviceInfo Table

Extended device information with detailed specifications and metadata.

| Column | Type | Description |
|--------|------|-------------|
| DeviceID | int (FK) | References ChaMPS.DeviceID |
| SerialNumber | varchar(50) | Manufacturer serial number |
| AssetTag | varchar(50) | Company asset identifier |
| MACAddress | varchar(17) | Network MAC address |
| Manufacturer | varchar(100) | Device manufacturer |
| Model | varchar(100) | Device model number |
| EquipmentGroup | varchar(50) | Device classification |
| PurchaseDate | datetime | Purchase/acquisition date |
| ServiceDate | datetime | Last service date |
| WarrantyDate | datetime | Warranty expiration |
| AdditionalNotes | text | Free-form notes |
| WebLink | varchar(500) | Related documentation URL |
| WebLinkName | varchar(100) | Link display name |
| DeviceType | varchar(50) | Type classification |
| Area | varchar(50) | Redundant area data |
| Zone | varchar(50) | Zone within area |
| Line | varchar(50) | Redundant line data |
| Pillar | varchar(50) | Column designation |
| Floor | varchar(50) | Level designation |
| Pitch | varchar(50) | Redundant pitch data |

### Relationships

```sql
ALTER TABLE DeviceInfo 
ADD CONSTRAINT FK_DeviceInfo_ChaMPS 
FOREIGN KEY (DeviceID) REFERENCES ChaMPS(DeviceID)
ON DELETE CASCADE
```

**Key Features:**
- **One-to-Zero-or-One**: Each ChaMPS record can have optional DeviceInfo
- **Cascade Deletion**: Removing ChaMPS record automatically removes DeviceInfo
- **Data Redundancy**: Critical fields (Area, Line, Pitch) stored in both tables
- **COALESCE Logic**: Queries prioritize DeviceInfo data with ChaMPS fallback

---

## Getting Started

### Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** with MAUI workload
- **SQL Server 2019** or later (Express edition supported)
- **Windows 11** for development (other platforms for deployment)

### Installation

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd MauiApp1
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Install MAUI Workload** (if not already installed)
   ```bash
   dotnet workload install maui
   ```

4. **Build the Project**
   ```bash
   dotnet build
   ```

### Configuration

1. **Database Setup**
   - Create SQL Server database
   - Run schema creation scripts
   - Configure connection credentials

2. **Application Settings**
   ```json
   {
     "DatabaseSettings": {
       "DefaultServer": "localhost",
       "DefaultDatabase": "ITSupportDB",
       "ConnectionTimeout": 30
     },
     "SecuritySettings": {
       "MaxStringLength": 255,
       "MaxNotesLength": 2000,
       "EnableInputSanitization": true
     }
   }
   ```

3. **First Run**
   - Launch application
   - Configure database connection
   - Test connectivity
   - Import initial data (optional)

---

## Usage Guide

### Database Administration

The Database Administration page provides comprehensive device management capabilities:

#### Adding Devices

1. **Navigate** to Database Admin page
2. **Select Device Type**: Printer, Camera, NetOp, or Other
3. **Fill Required Fields**:
   - Name (required)
   - Location hierarchy (Area, Zone, Line, etc.)
   - Technical specifications
4. **Optional Fields**:
   - Network configuration (IP, Hostname)
   - Dates (Purchase, Service, Warranty)
   - Documentation (Notes, Web links)
5. **Submit** - Device is added to both database tables

#### Viewing Devices

- **Auto-Loading**: Device lists populate automatically on page load
- **Categorized Display**: Separate sections for each device type
- **Real-Time Updates**: Lists refresh after add/edit/delete operations

#### Device Management

- **Edit**: Click device to modify details
- **Delete**: Use trash icon with confirmation dialog
- **Filter**: Use search/filter controls for large datasets

### Device Management Usage

#### Current Devices Section
- **Printers**: Network and local printing devices
- **Cameras**: Security and monitoring cameras  
- **NetOp Devices**: PCs, servers, network equipment
- **Other Devices**: Miscellaneous IT equipment

#### Data Synchronization
- **Automatic Loading**: All sections populate on page navigation
- **Transaction Safety**: All operations wrapped in database transactions
- **Error Handling**: User-friendly messages for failures
- **Logging**: Comprehensive operation tracking

### Network Operations

#### Connectivity Testing
- **Ping Tests**: Check device accessibility
- **Port Scanning**: Verify service availability
- **DNS Resolution**: Validate hostname configuration

#### Device Discovery
- **Network Scanning**: Discover devices on network segments
- **Service Detection**: Identify running services
- **Inventory Updates**: Auto-update device information

### Filtering and Search

#### Available Filters
- **Device Type**: Printer, Camera, NetOp, Other
- **Location**: Area, Zone, Line, Column, Level
- **Status**: Active/Inactive devices
- **Manufacturer**: Equipment vendor
- **Date Ranges**: Purchase, Service, Warranty dates

#### Search Capabilities
- **Text Search**: Name, hostname, notes
- **Advanced Filters**: Multiple criteria combination
- **Real-Time Results**: Instant result updates
- **Export Options**: Filtered result export

---

## Security Implementation

### SQL Injection Prevention

The application implements **defense-in-depth** security with multiple protection layers:

#### Parameterized Queries
```csharp
// ✅ SECURE: All queries use parameters
const string query = @"
    INSERT INTO dbo.devices (Area, Line, Pitch, IPAddress, Active, Priority, HostName)
    VALUES (@Area, @Line, @Pitch, @IPAddress, @Active, @Priority, @HostName)";

command.Parameters.AddWithValue("@Area", device.Area);
```

#### Input Sanitization
```csharp
// Multi-layer validation
InputSanitizer.SanitizeDevice(device);
var sanitizedDeviceType = InputSanitizer.SanitizeDeviceType(deviceType);
```

#### Attack Vector Protection
- **Classic Injection**: `'; DROP TABLE devices; --`
- **Union Attacks**: `' UNION SELECT * FROM users --`
- **Boolean Blind**: `' OR 1=1 --`
- **Time-Based**: `'; WAITFOR DELAY '00:00:05' --`
- **Second-Order**: Stored malicious data execution

### Input Sanitization

#### Field-Specific Validation
```csharp
public static class InputSanitizer
{
    // IP Address validation
    public static string? SanitizeIPAddress(string? ipAddress)
    {
        if (!ValidIPAddressPattern.IsMatch(sanitized))
            throw new ArgumentException($"Invalid IP address format: {ipAddress}");
        return sanitized;
    }
    
    // Hostname validation  
    public static string SanitizeHostname(string? hostname)
    {
        if (!ValidHostnamePattern.IsMatch(sanitized))
            throw new ArgumentException($"Invalid hostname format: {hostname}");
        return sanitized;
    }
    
    // URL validation
    public static string? SanitizeUrl(string? url)
    {
        if (!ValidUrlPattern.IsMatch(sanitized))
            throw new ArgumentException($"Invalid URL format: {url}");
        return sanitized;
    }
}
```

#### Security Features
- **Length Limits**: Prevent buffer overflow attacks
- **Pattern Matching**: Regex validation for all field types
- **Keyword Detection**: Block dangerous SQL commands
- **Type Safety**: Enforce proper data types
- **Null Handling**: Graceful null value processing

### Data Validation

#### Multi-Layer Approach
1. **UI Layer**: Real-time input validation
2. **ViewModel Layer**: Business rule enforcement
3. **Service Layer**: Security and sanitization
4. **Database Layer**: Constraint enforcement

#### Validation Rules
- **Required Fields**: Name, device type
- **Format Validation**: IP addresses, URLs, hostnames
- **Range Validation**: Priority (1-10), dates
- **Length Limits**: All text fields
- **Pattern Matching**: Alphanumeric, special characters

---

## API Documentation

### Services

#### DatabaseService
Handles all database operations with security and transaction management.

```csharp
public interface IDatabaseService
{
    Task<List<Device>> GetDevicesAsync(string? deviceType = null);
    Task<bool> AddDeviceAsync(Device device, string deviceType = "Other");
    Task<bool> UpdateDeviceAsync(Device device, string deviceType = "Other");
    Task<bool> DeleteDeviceAsync(int deviceId);
    Task<DatabaseTestResult> TestConnectionAsync(string server, string database, bool useWindowsAuth, string? username = null, string? password = null);
}
```

**Key Features:**
- Automatic input sanitization
- Transaction safety
- Foreign key management
- Comprehensive error handling

#### InputSanitizer
Provides security validation and input sanitization.

```csharp
public static class InputSanitizer
{
    public static string SanitizeString(string? input, int maxLength = 255);
    public static string SanitizeDeviceType(string? deviceType);
    public static string? SanitizeIPAddress(string? ipAddress);
    public static string SanitizeHostname(string? hostname);
    public static string SanitizeAlphanumeric(string? input);
    public static void SanitizeDevice(Device device);
}
```

#### NavigationService
Manages application navigation with type safety.

```csharp
public interface INavigationService
{
    Task NavigateToAsync(string route);
    Task NavigateToAsync<T>(string route, T parameter);
    Task GoBackAsync();
}
```

### ViewModels

#### DatabaseAdminViewModel
Main ViewModel for database administration with comprehensive device management.

**Key Properties:**
- Device form fields (Name, Area, Zone, etc.)
- Observable collections for device lists
- Commands for CRUD operations
- Validation and error handling

**Key Commands:**
- `AddDeviceCommand`: Add new device with validation
- `LoadAllDevicesCommand`: Refresh device lists
- `DeletePrinterCommand`: Remove printer device
- `DeleteCameraCommand`: Remove camera device
- `DeleteNetopDeviceCommand`: Remove NetOp device
- `DeleteOtherDeviceCommand`: Remove other device

#### BaseViewModel
Common functionality for all ViewModels.

```csharp
public abstract class BaseViewModel : ObservableObject
{
    public bool IsBusy { get; set; }
    public string Title { get; set; }
    protected async Task ExecuteSafelyAsync(Func<Task> operation, string operationName);
}
```

### Interfaces

#### ILoadableViewModel
Standardizes automatic page data loading.

```csharp
public interface ILoadableViewModel : IViewModel
{
    IAsyncRelayCommand LoadDataCommand { get; }
}
```

#### IFilterable
Enables advanced filtering capabilities for data models.

```csharp
public interface IFilterable
{
    string GetFilterValue(string filterProperty);
    bool MatchesFilter(string filterProperty, string filterValue);
}
```

---

## Development

### Building the Project

#### Development Build
```bash
# Debug build for development
dotnet build --configuration Debug

# Run on Windows
dotnet run --framework net8.0-windows10.0.19041.0
```

#### Release Build  
```bash
# Optimized release build
dotnet build --configuration Release

# Publish for deployment
dotnet publish --configuration Release --runtime win-x64 --self-contained
```

#### Platform-Specific Builds
```bash
# Android
dotnet build --framework net8.0-android

# iOS (requires Mac)
dotnet build --framework net8.0-ios

# macOS (requires Mac)  
dotnet build --framework net8.0-maccatalyst
```

### Testing

#### Unit Tests
```bash
# Run all unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

#### Integration Tests
```bash
# Database integration tests
dotnet test --filter "Category=Integration"

# Security tests
dotnet test --filter "Category=Security"
```

#### Security Testing
- **SQL Injection Tests**: Automated injection attempt detection
- **Input Validation Tests**: Boundary condition testing
- **Authentication Tests**: Credential handling verification

### Contributing

#### Development Workflow
1. **Fork Repository**: Create personal fork
2. **Create Branch**: Feature/bug fix branch
3. **Implement Changes**: Follow coding standards
4. **Add Tests**: Unit and integration tests
5. **Update Documentation**: README and code comments
6. **Submit PR**: Pull request with description

#### Coding Standards
- **C# Conventions**: Microsoft coding guidelines
- **XAML Standards**: Consistent naming and structure
- **Documentation**: XML comments for public APIs
- **Security**: Input validation for all user inputs

#### Pull Request Process
1. **Code Review**: Peer review required
2. **Testing**: All tests must pass
3. **Security Review**: Security implications assessed
4. **Documentation**: Updates to README if needed

---

## Troubleshooting

### Common Issues

#### Database Connection Problems
```
Error: "Cannot connect to SQL Server"
```
**Solutions:**
- Verify SQL Server is running
- Check connection string format
- Confirm network connectivity
- Validate credentials
- Test with SQL Server Management Studio

#### Build Errors
```
Error: "MAUI workload not found"
```
**Solutions:**
```bash
# Install MAUI workload
dotnet workload install maui

# Update workloads
dotnet workload update

# Repair workloads
dotnet workload repair
```

#### Performance Issues
```
Issue: Slow device loading
```
**Solutions:**
- Check database query performance
- Verify network latency
- Review connection pool settings
- Consider data pagination

### Debugging

#### Enable Detailed Logging
```csharp
// In MauiProgram.cs
#if DEBUG
builder.Services.AddLogging(configure =>
{
    configure.SetMinimumLevel(LogLevel.Debug);
    configure.AddDebug();
});
#endif
```

#### Database Query Analysis
```sql
-- Enable SQL Server query statistics
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Run your query here
SELECT * FROM ChaMPS c
LEFT JOIN DeviceInfo di ON c.DeviceID = di.DeviceID;
```

#### Performance Profiling
- Use **dotTrace** or **PerfView** for performance analysis
- Monitor memory usage with diagnostic tools
- Profile database queries with SQL Server Profiler

---

## Performance Considerations

### Database Optimization

#### Query Performance
- **Indexes**: Ensure proper indexing on DeviceID, DeviceType, Area, Zone
- **Query Plans**: Review execution plans for complex queries
- **Pagination**: Implement for large datasets
- **Caching**: Consider result caching for frequently accessed data

#### Connection Management
```csharp
// Connection pooling configuration
services.AddSingleton<IDbConnectionFactory>(provider =>
    new SqlConnectionFactory(connectionString, new SqlConnectionPoolSettings
    {
        MaxPoolSize = 100,
        MinPoolSize = 5,
        ConnectionTimeout = 30
    }));
```

### Application Performance

#### Memory Management
- **Dispose Pattern**: Proper resource cleanup
- **Weak References**: For event subscriptions
- **Observable Collections**: Efficient list updates
- **Image Optimization**: Compressed assets

#### UI Responsiveness  
- **Async Operations**: All database calls asynchronous
- **Background Tasks**: Heavy operations on background threads
- **Progress Indicators**: User feedback during operations
- **Virtualization**: For large lists

### Scalability Considerations

#### Data Volume
- **Pagination**: Limit records per page
- **Filtering**: Server-side filtering for large datasets
- **Archiving**: Historical data management
- **Partitioning**: Database table partitioning

#### Concurrent Users
- **Connection Pooling**: Efficient connection reuse
- **Transaction Isolation**: Proper isolation levels
- **Optimistic Concurrency**: Version-based conflict resolution
- **Load Balancing**: Multiple application instances

---

## Future Enhancements

### Planned Features

#### v2.0 - Advanced Analytics
- **Usage Reports**: Device utilization analytics
- **Predictive Maintenance**: AI-powered maintenance scheduling
- **Cost Analysis**: TCO calculations and budget planning
- **Performance Dashboards**: Real-time KPI monitoring

#### v2.1 - Mobile Enhancements
- **Offline Support**: Local database synchronization
- **Barcode Scanning**: Asset tag scanning for mobile devices
- **Photo Capture**: Device documentation with camera integration
- **GPS Integration**: Location-based device management

#### v2.2 - Integration Capabilities
- **API Endpoints**: RESTful API for third-party integration
- **LDAP Integration**: Active Directory user authentication
- **SNMP Monitoring**: Network device status monitoring
- **Asset Import**: Bulk import from Excel/CSV files

### Technology Roadmap

#### .NET Upgrades
- **.NET 9**: Upgrade to latest framework version
- **C# 13**: Leverage new language features
- **MAUI Updates**: Latest MAUI toolkit features

#### Database Enhancements
- **Entity Framework**: ORM implementation for complex queries
- **Database Migrations**: Automated schema versioning
- **Backup Integration**: Automated backup and restore
- **Replication**: Multi-site database synchronization

#### Security Improvements
- **OAuth 2.0**: Modern authentication protocols
- **Encryption**: Data-at-rest encryption
- **Audit Logging**: Comprehensive audit trails
- **Role-Based Access**: Granular permission system

---

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

### MIT License Summary

```
Copyright (c) 2025 MauiApp1 Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
```

---

## Support

### Getting Help

- **Documentation**: This README and inline code comments
- **Issues**: GitHub Issues for bug reports and feature requests
- **Discussions**: GitHub Discussions for questions and ideas
- **Wiki**: Additional documentation and tutorials

### Contact Information

- **Project Maintainer**: [Your Name]
- **Email**: [your.email@example.com]
- **GitHub**: [https://github.com/yourusername/MauiApp1]

---

*Last Updated: August 7, 2025*

---

**Navigation**: [Top](#mauiapp1---it-support-framework-application) | [Table of Contents](#-table-of-contents)

