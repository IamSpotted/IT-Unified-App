# Testing Mode Instructions

## ⚠️ IMPORTANT: Remove Before Production Deployment

This document explains how to temporarily disable Active Directory (AD) security checks for testing purposes.

## 🎯 Purpose

When you need to test the application with multiple users but cannot yet create the required AD security groups, you can enable "Testing Mode" to grant SystemAdmin access to all users temporarily.

## 🔓 How to Enable Testing Mode

### Method 1: Settings Page UI (Easiest)
1. Launch the application
2. Navigate to **Settings** page
3. Scroll down to the **"⚠️ Testing Mode"** section (yellow/orange frame)
4. Click **"🔓 Enable Testing Mode"** button
5. Confirm the dialog that appears
6. **All users now have SystemAdmin access** regardless of AD group membership

### Method 2: Programmatic (Advanced)
```csharp
// In any service that has access to IAuthorizationService
_authorizationService.EnableTestingMode(true);
```

## 🔒 How to Disable Testing Mode

### Settings Page UI
1. Navigate to **Settings** page
2. In the **"⚠️ Testing Mode"** section
3. Click **"🔒 Disable Testing Mode"** button
4. Normal AD-based access control is restored

### Programmatic
```csharp
_authorizationService.EnableTestingMode(false);
```

## 🛡️ What Testing Mode Does

When **enabled**:
- ✅ All users get **SystemAdmin** role and permissions
- ✅ Full access to all pages and features
- ✅ Database Admin page visible to everyone
- ✅ All settings sections accessible
- ⚠️ **AD security checks are bypassed entirely**

When **disabled**:
- 🔄 Normal AD group-based access control resumes
- 🔄 Users get roles based on their AD group memberships
- 🔄 Page access restricted according to assigned roles

## 📋 Testing Scenarios

### Test All Roles
1. Enable Testing Mode
2. Test SystemAdmin functionality with multiple users
3. Disable Testing Mode
4. Use Developer Mode to simulate specific roles:
   - ReadOnly
   - Standard
   - DatabaseAdmin
   - SystemAdmin

### Multi-User Testing
1. Enable Testing Mode
2. Have multiple users log in simultaneously
3. Verify all features work correctly
4. Test concurrent database operations
5. Disable Testing Mode when complete

## 🚨 Security Warning

**NEVER deploy to production with Testing Mode enabled!**

### Before Production Deployment:
1. **Remove Testing Mode code** entirely
2. **Delete this documentation file**
3. **Set up proper AD security groups**
4. **Test with actual AD group memberships**

### Files to Clean Up:
- `WindowsAuthorizationService.cs` - Remove `_testingModeEnabled` and `EnableTestingMode()`
- `IAuthorizationService.cs` - Remove `EnableTestingMode()` method
- `SettingsViewModel.cs` - Remove `EnableTestingModeCommand` and `DisableTestingModeCommand`
- `SettingsPage.xaml` - Remove the Testing Mode UI section
- `TESTING_MODE_INSTRUCTIONS.md` - Delete this file

## 🔍 Verification

### Check if Testing Mode is Active
- Look for **yellow/orange warning** section in Settings
- Check application logs for: `"TESTING MODE: Granting SystemAdmin access"`
- Verify all users can access Database Admin page

### Confirm Normal Operation
- Testing Mode section should not be visible (after cleanup)
- Users should have different access levels based on AD groups
- Application logs show normal role assignments

## 📝 Notes

- Testing Mode setting is **not persistent** - it resets when the app restarts
- Works in both DEBUG and RELEASE builds
- Generates warning logs when enabled for security audit trail
- Can be toggled on/off without app restart
