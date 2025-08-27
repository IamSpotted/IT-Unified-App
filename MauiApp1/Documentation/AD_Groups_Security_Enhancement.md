# Active Directory Groups Security Enhancement

## Overview
The AD group names have been hard-coded in the application for enhanced security, removing them from user-configurable settings. This prevents exposure of the actual AD group structure to end users.

## Security Benefits

### 1. Information Hiding
- **Before**: AD group names were visible in the Settings page to all users
- **After**: AD group names are hidden and hard-coded in the service layer
- **Benefit**: Reduces information disclosure about your organization's AD structure

### 2. Tamper Protection
- **Before**: Users could potentially view or attempt to modify group mappings
- **After**: Only developers and system administrators with code access can see/modify groups
- **Benefit**: Prevents unauthorized attempts to manipulate role assignments

### 3. Simplified Configuration
- **Before**: Required entering and maintaining AD group names in the UI
- **After**: Only domain name configuration is needed
- **Benefit**: Reduced configuration complexity and fewer opportunities for errors

## Implementation Details

### Hard-Coded Groups Location
File: `Services/WindowsAuthorizationService.cs`

```csharp
// Hard-coded AD group names - to be provided by AD team
private readonly Dictionary<string, UserRole> _groupToRoleMapping = new()
{
    { "ITSF-App-ReadOnly", UserRole.ReadOnly },
    { "ITSF-App-Standard", UserRole.Standard },
    { "ITSF-App-DatabaseAdmin", UserRole.DatabaseAdmin },
    { "ITSF-App-SystemAdmin", UserRole.SystemAdmin }
};
```

### Customization for Your Environment
To customize for your AD environment:

1. **Work with your AD team** to get the exact group names
2. **Update the group names** in the `_groupToRoleMapping` dictionary
3. **Recompile and deploy** the application
4. **Users only need to configure** the domain name in Settings

### Example Customization
```csharp
// Example for CONTOSO domain
private readonly Dictionary<string, UserRole> _groupToRoleMapping = new()
{
    { "CONTOSO-ITSupport-ReadOnly", UserRole.ReadOnly },
    { "CONTOSO-ITSupport-Standard", UserRole.Standard },
    { "CONTOSO-ITSupport-DatabaseAdmin", UserRole.DatabaseAdmin },
    { "CONTOSO-ITSupport-SystemAdmin", UserRole.SystemAdmin }
};
```

## Settings Page Changes

### Removed Elements
- AD group name input fields
- Group configuration section
- Save/Test buttons for group configuration

### Remaining Elements
- Domain name configuration (still needed for AD connection)
- Current user and role display
- Developer mode features (debug builds only)

### User Experience
- **SystemAdmin users**: Can still configure domain name
- **All users**: Can view their current role and user information
- **Configuration**: Simplified to only essential domain connection settings

## Migration Notes

### For Existing Installations
1. **Existing domain configuration** will continue to work
2. **Group name settings** will be ignored (superseded by hard-coded values)
3. **No user action required** - the app will automatically use hard-coded groups

### For New Installations
1. **Only domain name** needs to be configured
2. **Group names** are pre-configured based on your customization
3. **Simplified setup** process for end users

## Verification Steps

### Testing the Security Enhancement
1. **Deploy the updated application**
2. **Log in as a non-SystemAdmin user**
3. **Navigate to Settings page**
4. **Verify**: Group configuration fields are not visible
5. **Verify**: Domain name can be viewed but not edited (unless SystemAdmin)

### Confirming Role Assignment
1. **Ensure users are in correct AD groups** (using the hard-coded names)
2. **Launch the application**
3. **Check Settings page** to see current user role
4. **Verify menu visibility** matches the assigned role

## Developer Notes

### Code Changes Made
1. **WindowsAuthorizationService.cs**: Added hard-coded group mapping
2. **WindowsAuthorizationService.cs**: Updated role determination logic
3. **IAuthorizationService.cs**: Removed group properties from AuthorizationConfig
4. **SettingsViewModel.cs**: Removed group name properties and related logic

### Future Maintenance
- **Update groups**: Modify the `_groupToRoleMapping` dictionary
- **New roles**: Add to both UserRole enum and group mapping
- **Group renames**: Update the dictionary keys to match new AD group names

## Security Considerations

### What This Protects Against
- ✅ Information disclosure of AD group structure
- ✅ Unauthorized configuration attempts
- ✅ User confusion about group names
- ✅ Accidental misconfiguration

### What This Doesn't Protect Against
- ❌ Authorized users being added to wrong AD groups (AD admin responsibility)
- ❌ Compromise of the application binary (general security concern)
- ❌ Man-in-the-middle attacks on AD queries (use secure network)

## Deployment Checklist

### Before Deployment
- [ ] Verify group names match your AD environment
- [ ] Test role assignment with actual AD users
- [ ] Confirm database connection restrictions work correctly
- [ ] Validate non-domain environment fallback

### After Deployment
- [ ] Verify existing users can still log in
- [ ] Confirm role assignments are correct
- [ ] Test Settings page shows simplified configuration
- [ ] Validate SystemAdmin users can configure domain

The enhanced security model provides better protection while maintaining all functional capabilities!
