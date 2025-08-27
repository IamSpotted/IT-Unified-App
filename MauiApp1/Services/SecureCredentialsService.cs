namespace MauiApp1.Services;

/// <summary>
/// Service for securely storing and retrieving SolarWinds credentials
/// </summary>
public class SecureCredentialsService : ITransientService
{
    private readonly ILogger<SecureCredentialsService> _logger;
    
    // Keys for secure storage
    private const string SOLARWINDS_HOSTNAME_KEY = "solarwinds_hostname";
    private const string SOLARWINDS_USERNAME_KEY = "solarwinds_username";
    private const string SOLARWINDS_PASSWORD_KEY = "solarwinds_password";
    private const string SOLARWINDS_PORT_KEY = "solarwinds_port";
    
    // Database credential keys
    private const string DATABASE_SERVER_KEY = "database_server";
    private const string DATABASE_NAME_KEY = "database_name";
    private const string DATABASE_USERNAME_KEY = "database_username";
    private const string DATABASE_PASSWORD_KEY = "database_password";
    private const string DATABASE_AUTH_TYPE_KEY = "database_auth_type";
    private const string DATABASE_TIMEOUT_KEY = "database_timeout";

    public SecureCredentialsService(ILogger<SecureCredentialsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Stores SolarWinds credentials securely
    /// </summary>
    public async Task SaveSolarWindsCredentialsAsync(string hostname, string username, string password, int port = 17778)
    {
        try
        {
            await SecureStorage.SetAsync(SOLARWINDS_HOSTNAME_KEY, hostname);
            await SecureStorage.SetAsync(SOLARWINDS_USERNAME_KEY, username);
            await SecureStorage.SetAsync(SOLARWINDS_PASSWORD_KEY, password);
            await SecureStorage.SetAsync(SOLARWINDS_PORT_KEY, port.ToString());
            
            _logger.LogInformation("SolarWinds credentials saved securely");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save SolarWinds credentials");
            throw;
        }
    }

    /// <summary>
    /// Retrieves SolarWinds credentials from secure storage
    /// </summary>
    public async Task<SolarWindsCredentials?> GetSolarWindsCredentialsAsync()
    {
        try
        {
            var hostname = await SecureStorage.GetAsync(SOLARWINDS_HOSTNAME_KEY);
            var username = await SecureStorage.GetAsync(SOLARWINDS_USERNAME_KEY);
            var password = await SecureStorage.GetAsync(SOLARWINDS_PASSWORD_KEY);
            var portString = await SecureStorage.GetAsync(SOLARWINDS_PORT_KEY);

            if (string.IsNullOrEmpty(hostname) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            var port = int.TryParse(portString, out var parsedPort) ? parsedPort : 17778;

            return new SolarWindsCredentials
            {
                Hostname = hostname,
                Username = username,
                Password = password,
                Port = port
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve SolarWinds credentials");
            return null;
        }
    }

    /// <summary>
    /// Checks if SolarWinds credentials are configured
    /// </summary>
    public async Task<bool> HasSolarWindsCredentialsAsync()
    {
        try
        {
            var hostname = await SecureStorage.GetAsync(SOLARWINDS_HOSTNAME_KEY);
            var username = await SecureStorage.GetAsync(SOLARWINDS_USERNAME_KEY);
            var password = await SecureStorage.GetAsync(SOLARWINDS_PASSWORD_KEY);

            return !string.IsNullOrEmpty(hostname) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check SolarWinds credentials");
            return false;
        }
    }

    /// <summary>
    /// Clears all stored SolarWinds credentials
    /// </summary>
    public Task ClearSolarWindsCredentialsAsync()
    {
        try
        {
            SecureStorage.Remove(SOLARWINDS_HOSTNAME_KEY);
            SecureStorage.Remove(SOLARWINDS_USERNAME_KEY);
            SecureStorage.Remove(SOLARWINDS_PASSWORD_KEY);
            SecureStorage.Remove(SOLARWINDS_PORT_KEY);
            
            _logger.LogInformation("SolarWinds credentials cleared");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear SolarWinds credentials");
            throw;
        }
    }

    /// <summary>
    /// Stores database credentials securely
    /// </summary>
    public async Task SaveDatabaseCredentialsAsync(string server, string database, bool useWindowsAuth, 
        string? username = null, string? password = null, int connectionTimeout = 30)
    {
        try
        {
            await SecureStorage.SetAsync(DATABASE_SERVER_KEY, server);
            await SecureStorage.SetAsync(DATABASE_NAME_KEY, database);
            await SecureStorage.SetAsync(DATABASE_AUTH_TYPE_KEY, useWindowsAuth.ToString());
            await SecureStorage.SetAsync(DATABASE_TIMEOUT_KEY, connectionTimeout.ToString());
            
            if (!useWindowsAuth)
            {
                await SecureStorage.SetAsync(DATABASE_USERNAME_KEY, username ?? string.Empty);
                await SecureStorage.SetAsync(DATABASE_PASSWORD_KEY, password ?? string.Empty);
            }
            else
            {
                // Clear SQL auth credentials when using Windows auth
                SecureStorage.Remove(DATABASE_USERNAME_KEY);
                SecureStorage.Remove(DATABASE_PASSWORD_KEY);
            }
            
            _logger.LogInformation("Database credentials saved securely");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save database credentials");
            throw;
        }
    }

    /// <summary>
    /// Retrieves database credentials from secure storage
    /// </summary>
    public async Task<DatabaseCredentials?> GetDatabaseCredentialsAsync()
    {
        try
        {
            var server = await SecureStorage.GetAsync(DATABASE_SERVER_KEY);
            var database = await SecureStorage.GetAsync(DATABASE_NAME_KEY);
            var authTypeString = await SecureStorage.GetAsync(DATABASE_AUTH_TYPE_KEY);
            var timeoutString = await SecureStorage.GetAsync(DATABASE_TIMEOUT_KEY);

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database))
            {
                return null;
            }

            var useWindowsAuth = bool.TryParse(authTypeString, out var parsedAuth) ? parsedAuth : true;
            var timeout = int.TryParse(timeoutString, out var parsedTimeout) ? parsedTimeout : 30;

            var credentials = new DatabaseCredentials
            {
                Server = server,
                Database = database,
                UseWindowsAuthentication = useWindowsAuth,
                ConnectionTimeout = timeout
            };

            if (!useWindowsAuth)
            {
                credentials.Username = await SecureStorage.GetAsync(DATABASE_USERNAME_KEY);
                credentials.Password = await SecureStorage.GetAsync(DATABASE_PASSWORD_KEY);
            }

            return credentials;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve database credentials");
            return null;
        }
    }

    /// <summary>
    /// Checks if database credentials are configured
    /// </summary>
    public async Task<bool> HasDatabaseCredentialsAsync()
    {
        try
        {
            var server = await SecureStorage.GetAsync(DATABASE_SERVER_KEY);
            var database = await SecureStorage.GetAsync(DATABASE_NAME_KEY);
            var authTypeString = await SecureStorage.GetAsync(DATABASE_AUTH_TYPE_KEY);

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database))
            {
                return false;
            }

            var useWindowsAuth = bool.TryParse(authTypeString, out var parsedAuth) ? parsedAuth : true;
            
            if (useWindowsAuth)
            {
                return true; // Windows auth doesn't need username/password
            }
            else
            {
                var username = await SecureStorage.GetAsync(DATABASE_USERNAME_KEY);
                var password = await SecureStorage.GetAsync(DATABASE_PASSWORD_KEY);
                return !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check database credentials");
            return false;
        }
    }

    /// <summary>
    /// Clears all stored database credentials
    /// </summary>
    public Task ClearDatabaseCredentialsAsync()
    {
        try
        {
            SecureStorage.Remove(DATABASE_SERVER_KEY);
            SecureStorage.Remove(DATABASE_NAME_KEY);
            SecureStorage.Remove(DATABASE_USERNAME_KEY);
            SecureStorage.Remove(DATABASE_PASSWORD_KEY);
            SecureStorage.Remove(DATABASE_AUTH_TYPE_KEY);
            SecureStorage.Remove(DATABASE_TIMEOUT_KEY);
            
            _logger.LogInformation("Database credentials cleared");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear database credentials");
            throw;
        }
    }
}

/// <summary>
/// Model for SolarWinds credentials
/// </summary>
public class SolarWindsCredentials
{
    public string Hostname { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Port { get; set; } = 17778;
}

/// <summary>
/// Model for SQL Database credentials
/// </summary>
public class DatabaseCredentials
{
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public bool UseWindowsAuthentication { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int ConnectionTimeout { get; set; } = 30;
}
