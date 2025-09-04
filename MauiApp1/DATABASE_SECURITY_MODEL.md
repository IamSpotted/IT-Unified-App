# Database Connection Security Model

## 🔐 Overview

The application implements a multi-layered security model to control which database servers users can connect to, based on their environment and role.

## 🛡️ Security Layers

### Layer 1: Role-Based Access Control
**Who can configure database connections?**
- Only users with `DatabaseAdmin` or `SystemAdmin` roles
- Users with lower roles (`ReadOnly`, `Standard`) cannot access database configuration

### Layer 2: Environment-Based Restrictions
**Domain vs Non-Domain Environment Detection:**

#### **Non-Domain Environment** 
- Users are **not** connected to Active Directory
- **Restriction**: Can only connect to localhost database servers
- **Allowed servers**: 
  - `localhost`
  - `127.0.0.1`
  - `::1` (IPv6 localhost)
  - `(local)`
  - `.` (local SQL Server shorthand)
  - `[MachineName]` (current computer name)
  - Localhost with ports: `localhost:1433`, `127.0.0.1:1433`
  - Local named instances: `localhost\\SQLEXPRESS`, `(local)\\MYINSTANCE`

#### **Domain Environment**
- Users are connected to Active Directory with proper domain configuration
- **Default**: Can connect to any database server (if they have DatabaseAdmin role)
- **Optional**: Can be restricted to approved server whitelist (see Layer 3)

### Layer 3: Optional Server Whitelist (Domain Only)
**Configurable approved database servers:**
- Set `EnforceApprovedServersOnly = true` in authorization config
- Add approved servers to `ApprovedDatabaseServers` list
- When enabled, domain users can **only** connect to whitelisted servers

## 🔧 Configuration Examples

### Example 1: Default Domain Security (Permissive)
```json
{
  "DomainName": "COMPANY.LOCAL",
  "EnforceApprovedServersOnly": false,
  "ApprovedDatabaseServers": []
}
```
**Result**: Domain users with DatabaseAdmin role can connect to any server

### Example 2: Restricted Domain Security (Recommended)
```json
{
  "DomainName": "COMPANY.LOCAL", 
  "EnforceApprovedServersOnly": true,
  "ApprovedDatabaseServers": [
    "PROD-SQL-01.COMPANY.LOCAL",
    "DEV-SQL-01.COMPANY.LOCAL",
    "localhost"
  ]
}
```
**Result**: Domain users can only connect to the three approved servers

### Example 3: Non-Domain Environment (Automatic)
```json
{
  "DomainName": "WORKGROUP_OR_INVALID_DOMAIN"
}
```
**Result**: All users automatically restricted to localhost connections only

## 🚫 What Gets Blocked

### Non-Domain Environment Examples:
- ❌ `REMOTE-SERVER.COMPANY.COM`
- ❌ `192.168.1.100`
- ❌ `SQL-PROD-01`
- ✅ `localhost`
- ✅ `127.0.0.1`
- ✅ `(local)\\SQLEXPRESS`

### Domain Environment with Whitelist Examples:
- ❌ `UNKNOWN-SQL-SERVER` (not in approved list)
- ❌ `192.168.1.200` (not in approved list)
- ✅ `PROD-SQL-01.COMPANY.LOCAL` (in approved list)
- ✅ `localhost` (in approved list)

## 🔍 Security Verification

### Check Current User's Database Access:
```csharp
bool canConnect = await authorizationService.CanConnectToDatabaseAsync("SOME-SERVER");
```

### Verify Environment Detection:
```csharp
bool isNonDomain = await authorizationService.IsNonDomainEnvironmentAsync();
```

### View Authorization Details:
- Navigate to **Settings** page
- Check **User Information** section
- View current role and permissions

## 📋 Security Recommendations

### For Production Deployment:
1. **Set up proper AD domain** connection
2. **Create AD security groups** for role assignment
3. **Enable server whitelist** (`EnforceApprovedServersOnly = true`)
4. **Add only approved database servers** to the whitelist
5. **Remove testing mode** functionality entirely

### For Development/Testing:
1. **Use localhost connections** for non-domain testing
2. **Use testing mode** to bypass restrictions temporarily
3. **Test with actual AD groups** before production deployment

## 🚨 Security Warnings

### Bypass Scenarios:
- **Testing Mode Enabled**: All security checks are bypassed
- **Developer Mode**: Can simulate any role (debug builds only)
- **Missing Domain Config**: Falls back to non-domain restrictions

### Audit Trail:
- All database connection attempts are logged
- Blocked connections generate warning logs
- User roles and permissions are logged for audit

## ⚙️ Advanced Configuration

### Programmatic Server Whitelist Management:
```csharp
var config = await authorizationService.GetConfigurationAsync();
config.EnforceApprovedServersOnly = true;
config.ApprovedDatabaseServers.Add("NEW-SQL-SERVER.COMPANY.LOCAL");
await authorizationService.SaveConfigurationAsync(config);
```

### Dynamic Server Approval:
- Server whitelist can be updated without app restart
- Changes take effect immediately
- Configuration is cached for performance (30-minute default)
