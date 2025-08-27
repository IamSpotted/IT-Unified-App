# Role-Based Access Control (RBAC) Implementation Guide

## Overview
Your MAUI app now has a complete Role-Based Access Control system integrated with Windows Active Directory. Users' AD group memberships determine their app permissions and menu visibility.

**🔒 Security Enhancement**: AD group names are hard-coded in the application code for enhanced security, preventing information disclosure about your organization's AD structure.

## Role System

### User Roles (UserRole enum)
- **ReadOnly**: Basic viewing access
- **Standard**: Standard user operations  
- **DatabaseAdmin**: Database administration capabilities
- **SystemAdmin**: Full system administration access

### Permissions (Permission flags)
- **ViewData**: Can view application data
- **ModifyData**: Can modify application data
- **ManageDatabase**: Can perform database operations
- **ManageSystem**: Can perform system administration
- **AccessReports**: Can access reporting features
- **ManageUsers**: Can manage user accounts

## Active Directory Integration

### Service: `WindowsAuthorizationService`
- **Location**: `Services/WindowsAuthorizationService.cs`
- **Features**:
  - Automatic AD group membership detection
  - Role mapping based on group membership
  - 30-minute caching for performance
  - Fallback to ReadOnly role if AD unavailable

### Current AD Group Mapping (HARD-CODED FOR SECURITY)
```csharp
// Hard-coded in WindowsAuthorizationService.cs - customize for your environment
private readonly Dictionary<string, UserRole> _groupToRoleMapping = new()
{
    { "ITSF-App-ReadOnly", UserRole.ReadOnly },
    { "ITSF-App-Standard", UserRole.Standard },
    { "ITSF-App-DatabaseAdmin", UserRole.DatabaseAdmin },
    { "ITSF-App-SystemAdmin", UserRole.SystemAdmin }
};
```
**🔒 Security Enhancement**: Group names are hard-coded to prevent information disclosure

## Configuration Required

### 1. Domain Configuration (Settings Page)
Configure your domain connection in the app:
1. Go to **Settings** page
2. Scroll to **Authorization Settings** section  
3. Click **Configure Domain Settings** to expand options
4. Enter your domain information:
   - **Domain Name**: Your Active Directory domain (e.g., `COMPANY.LOCAL`)
5. Click **Save Domain Configuration** to store settings permanently
6. Use **Test Domain Connection** to verify connectivity

### 2. Active Directory Groups (Hard-Coded)  
**🔒 Security Note**: AD group names are now hard-coded in the application for security.

Work with your AD team to create these groups (or provide your existing group names to update the code):
- **Default Groups**: `ITSF-App-ReadOnly`, `ITSF-App-Standard`, `ITSF-App-DatabaseAdmin`, `ITSF-App-SystemAdmin`
- **Customization**: Update the group names in `WindowsAuthorizationService.cs` to match your environment
- **Example**: `CONTOSO-ITSupport-ReadOnly`, `CONTOSO-ITSupport-Standard`, etc.

### 3. Code Customization (One-Time Setup)
Update the hard-coded groups in `Services/WindowsAuthorizationService.cs`:
```csharp
private readonly Dictionary<string, UserRole> _groupToRoleMapping = new()
{
    { "YOUR-COMPANY-ReadOnly", UserRole.ReadOnly },
    { "YOUR-COMPANY-Standard", UserRole.Standard },
    { "YOUR-COMPANY-DatabaseAdmin", UserRole.DatabaseAdmin },
    { "YOUR-COMPANY-SystemAdmin", UserRole.SystemAdmin }
};
```

### 4. Permanent Storage
- Domain configuration is **permanently stored** in app settings
- No need to re-enter domain information
- Settings persist across app restarts and updates
- Only the "domain availability check" is cached for 5 minutes (for performance)

### 2. Assign Users to Groups
Add users to the appropriate AD groups based on their required access levels.

## Settings Page Configuration

### Domain Settings Section
The Settings page includes an **Authorization Settings** section with:

- **Current User Info**: Shows logged-in user and current role
- **Domain Configuration**: 
  - Toggle to show/hide domain settings
  - **🔒 Security**: Only SystemAdmin users can edit domain configuration
  - Domain name input field (read-only for non-SystemAdmin)
  - Save and test domain connection buttons (only visible to SystemAdmin)
- **🔒 Security Enhancement**: AD group names are now hidden from the UI for security
- **Developer Mode**: Hidden testing features (tap version 7 times to activate)

### Security Model
- **ReadOnly/Standard/DatabaseAdmin**: Can view domain settings but cannot modify them
- **SystemAdmin**: Full access to edit, save, and test domain configuration
- **Group Configuration**: Removed from UI - groups are hard-coded in application code
- **Visual Indicators**: Clear messages when access is restricted
- **Command Protection**: Save/Test commands check permissions before executing

### Configuration Steps
1. **Ensure SystemAdmin Access**: Only users with SystemAdmin role can modify domain settings
2. **Open Settings**: Navigate to Settings page in your app
3. **Expand Domain Settings**: Click "Configure Domain Settings" 
4. **Enter Domain Info** (SystemAdmin only): 
   ```
   Domain Name: YOURCOMPANY.LOCAL
   ```
5. **Save Configuration**: Click "Save Domain Configuration" (SystemAdmin only)
6. **Test Connection**: Click "Test Domain Connection" to verify (SystemAdmin only)
7. **Immediate Effect**: Changes take effect immediately (no restart needed)

**🔒 Security Note**: AD group names are hard-coded in the application and not configurable through the UI. This prevents information disclosure about your AD structure.

**Note**: Non-SystemAdmin users can view domain settings for reference but cannot modify them.

## Developer Testing Features

### Developer Mode Activation
1. Go to **Settings** page
2. Tap the **app version number 7 times** quickly
3. Developer options will appear

### Developer Mode Features
- **Role Simulation**: Test different roles without changing AD groups
- **Permission Testing**: View current permissions for selected role
- **User Info Display**: See current user and role information
- **AD Group Override**: Temporarily override AD-determined role

## Menu Integration

### AppShell Integration
The app shell now shows/hides menu items based on user roles:

```xml
<!-- Database Admin menu only visible to DatabaseAdmin and SystemAdmin -->
<FlyoutItem Title="Database Admin" 
            IsVisible="{Binding ShowDatabaseAdmin}"
            Icon="database.png">
    <ShellContent Route="databaseadmin" 
                  ContentTemplate="{DataTemplate local:DatabaseAdminPage}" />
</FlyoutItem>
```

### User Information Display
The flyout header shows:
- Current user (Domain\Username)
- Assigned role
- Developer mode status (if active)

## Usage in ViewModels

### Checking Permissions
```csharp
public class SomeViewModel : BaseViewModel
{
    private readonly IAuthorizationService _authService;

    public SomeViewModel(IAuthorizationService authService)
    {
        _authService = authService;
    }

    public async Task<bool> CanUserModifyData()
    {
        return await _authService.HasPermissionAsync(Permission.ModifyData);
    }

    public async Task<UserRole> GetCurrentUserRole()
    {
        return await _authService.GetUserRoleAsync();
    }

    public async Task<bool> CanConnectToDatabase(string serverName)
    {
        return await _authService.CanConnectToDatabaseAsync(serverName);
    }
}
```

### Database Connection Example
```csharp
// Check if user can connect to a specific database server
bool canConnect = await _authService.CanConnectToDatabaseAsync("SQL-SERVER-01");
if (!canConnect)
{
    // In non-domain environment, suggest localhost
    bool isNonDomain = await _authService.IsNonDomainEnvironmentAsync();
    if (isNonDomain)
    {
        await DisplayAlert("Database Access", 
            "In non-domain environment, only localhost database connections are allowed. Try 'localhost' or '127.0.0.1'.", 
            "OK");
    }
    else
    {
        await DisplayAlert("Access Denied", 
            "You don't have permission to connect to this database server.", 
            "OK");
    }
}
```

### Conditional UI Elements
```xml
<!-- Only show button if user has permission -->
<Button Text="Delete" 
        IsVisible="{Binding CanDeleteData}"
        Command="{Binding DeleteCommand}" />
```

## Security Features

### Automatic Fallback
- **Domain Environment**: Normal AD group-based role assignment
- **Non-Domain Environment**: Automatically grants SystemAdmin role with localhost-only database restrictions
- If user not in any AD group: Assigned ReadOnly role
- If service fails: Graceful degradation with minimal permissions

### Database Connection Security
- **Domain Users**: Database access based on role permissions (DatabaseAdmin/SystemAdmin)
- **Non-Domain Users**: Full app access but database connections restricted to localhost only
- **Localhost Patterns Allowed**: `localhost`, `127.0.0.1`, `::1`, `(local)`, `.`, machine name
- **Port Support**: Localhost connections with ports (e.g., `localhost:1433`) are allowed

### Caching
- AD queries cached for 30 minutes
- Improves performance for repeated permission checks
- Cache invalidated when developer mode changes roles

### Logging
The service logs:
- Role assignments and changes
- AD query results
- Permission checks (in debug mode)
- Developer mode activation/deactivation

## Testing Scenarios

### 1. Domain Environment User
1. Ensure user is in appropriate AD group
2. Launch app on domain-joined machine
3. Verify correct role assignment in Settings
4. Check menu visibility matches role
5. Test database connections based on permissions

### 2. Non-Domain Environment (Developer/Standalone)
1. Launch app on non-domain machine (or disconnect from domain)
2. Verify SystemAdmin role assignment
3. Confirm full app functionality
4. Test database connections - should only allow localhost
5. Try connecting to remote database - should be blocked

### 3. Developer Testing
1. Activate developer mode (7 taps on version)
2. Select different role from dropdown
3. Verify menu changes immediately
4. Test permission checks in various pages
5. Test database connection restrictions in different roles

## Files Modified/Added

### New Files
- `Models/UserRole.cs` - Role and permission definitions
- `Services/IAuthorizationService.cs` - Authorization interface
- `Services/WindowsAuthorizationService.cs` - AD integration service
- `ViewModels/AppShellViewModel.cs` - Menu visibility control

### Modified Files
- `ViewModels/SettingsViewModel.cs` - Added developer mode
- `AppShell.xaml` - Added conditional menus and user info
- `AppShell.xaml.cs` - Dependency injection support
- `App.xaml.cs` - DI integration for AppShell
- `MauiProgram.cs` - Service registration
- `MauiApp1.csproj` - Added AD and caching packages

## Next Steps

1. **Configure AD Groups**: Create the required AD groups in your domain
2. **Update Group Names**: Modify `WindowsAuthorizationService.cs` with your actual AD group names
3. **Test with Real Users**: Assign users to groups and test access
4. **Add Role Checks**: Implement permission checks in your ViewModels and Pages
5. **Customize Permissions**: Adjust the Permission flags and RolePermissions mapping as needed

## Troubleshooting

### User Shows as ReadOnly Despite AD Group
- Check AD group names match exactly (case-sensitive)
- Verify user is actually in the group
- Check if app can connect to domain controller
- Look at debug logs for AD query results

### Non-Domain Environment Not Detected
- Verify machine is actually not domain-joined
- Check if domain controller is reachable (might cause false domain detection)
- Look for "Non-domain environment detected" in logs
- Clear authorization cache and restart app

### Database Connection Blocked
- **Domain Environment**: Check user has DatabaseAdmin or SystemAdmin role
- **Non-Domain Environment**: Ensure using localhost, 127.0.0.1, (local), or machine name
- **Ports Allowed**: `localhost:1433`, `127.0.0.1:1433`, etc.
- **Blocked Examples**: Remote servers like `SQL-SERVER-01`, `192.168.1.100`

### Developer Mode Not Activating
- Ensure you're tapping the version number (not other text)
- Tap quickly (within 2-3 seconds)
- Check Settings page is using enhanced SettingsViewModel

### Menu Items Not Updating
- Verify AppShell is using dependency injection
- Check binding context is set to AppShellViewModel
- Ensure permission properties are implemented correctly

The RBAC system now provides secure non-domain fallback with localhost-only database restrictions!
