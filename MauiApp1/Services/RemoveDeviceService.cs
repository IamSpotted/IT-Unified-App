using System.Data.SqlClient;
using MauiApp1.Interfaces;
using MauiApp1.Models;
using Microsoft.Extensions.Logging;

namespace MauiApp1.Services
{
    public class RemoveDeviceService : IRemoveDeviceService
    {
        private readonly ILogger<RemoveDeviceService> _logger;
        private readonly SecureCredentialsService _credentialsService;

        public RemoveDeviceService(ILogger<RemoveDeviceService> logger, SecureCredentialsService credentialsService)
        {
            _logger = logger;
            _credentialsService = credentialsService;
        }

        public async Task<bool> RemoveDeviceAsync(string Hostname)
        {
            try
            {
                var credentials = await _credentialsService.GetDatabaseCredentialsAsync();
                if (credentials == null)
                {
                    _logger.LogError("No database credentials available for RemoveDevice");
                    return false;
                }

                var connectionString = new SqlConnectionStringBuilder
                {
                    DataSource = credentials.Server,
                    InitialCatalog = credentials.Database,
                    ConnectTimeout = 30,
                    TrustServerCertificate = true,
                    Encrypt = false,
                    IntegratedSecurity = credentials.UseWindowsAuthentication
                };

                if (!credentials.UseWindowsAuthentication)
                {
                    connectionString.UserID = credentials.Username ?? string.Empty;
                    connectionString.Password = credentials.Password ?? string.Empty;
                }

                using var connection = new SqlConnection(connectionString.ConnectionString);
                await connection.OpenAsync();

                var query = SQLQueryService.RemoveDeviceByHostnameQuery;
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@hostname", Hostname);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Removed {Hostname} from database", Hostname);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing {Hostname}", Hostname);
                return false;
            }
        }
    }
}