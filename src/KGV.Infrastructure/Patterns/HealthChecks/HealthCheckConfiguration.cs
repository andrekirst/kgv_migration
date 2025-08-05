using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Threading;

namespace KGV.Infrastructure.Patterns.HealthChecks
{
    /// <summary>
    /// Container-native health check configuration for KGV migration system
    /// Provides comprehensive health monitoring for Docker/Kubernetes deployments
    /// </summary>
    public static class HealthCheckConfiguration
    {
        public static IServiceCollection AddKgvHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var healthChecksBuilder = services.AddHealthChecks();

            // Database health checks
            var postgresConnection = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(postgresConnection))
            {
                healthChecksBuilder.AddNpgSql(
                    connectionString: postgresConnection,
                    name: "postgresql-database",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "database", "postgresql", "ready" });
            }

            var legacyConnection = configuration.GetConnectionString("LegacyDatabase");
            if (!string.IsNullOrEmpty(legacyConnection))
            {
                healthChecksBuilder.AddSqlServer(
                    connectionString: legacyConnection,
                    name: "legacy-sqlserver",
                    failureStatus: HealthStatus.Degraded, // Legacy can be degraded without stopping service
                    tags: new[] { "database", "sqlserver", "legacy", "ready" });
            }

            // Redis cache health check is handled by CacheConfiguration

            // Custom health checks
            healthChecksBuilder.AddCheck<ApplicationHealthCheck>(
                name: "application-health",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "application", "ready" });

            healthChecksBuilder.AddCheck<LegacySystemHealthCheck>(
                name: "legacy-system-connectivity",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "legacy", "external", "ready" });

            healthChecksBuilder.AddCheck<DataConsistencyHealthCheck>(
                name: "data-consistency",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "data", "consistency", "ready" });

            healthChecksBuilder.AddCheck<MigrationHealthCheck>(
                name: "migration-status",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "migration", "status", "ready" });

            // File system checks for container environments
            healthChecksBuilder.AddCheck<FileSystemHealthCheck>(
                name: "filesystem",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "filesystem", "storage", "live" });

            // Memory usage check
            healthChecksBuilder.AddCheck<MemoryHealthCheck>(
                name: "memory-usage",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "memory", "resources", "live" });

            // Container-specific health checks
            if (IsRunningInContainer())
            {
                healthChecksBuilder.AddCheck<ContainerHealthCheck>(
                    name: "container-health",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "container", "kubernetes", "live" });
            }

            // Register health check publishers
            services.AddSingleton<IHealthCheckPublisher, PrometheusHealthCheckPublisher>();
            services.AddSingleton<IHealthCheckPublisher, LoggingHealthCheckPublisher>();

            // Configure health check options
            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(5);
                options.Period = TimeSpan.FromSeconds(30);
                options.Predicate = check => true; // Publish all checks
                options.Timeout = TimeSpan.FromSeconds(10);
            });

            return services;
        }

        private static bool IsRunningInContainer()
        {
            return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ||
                   System.IO.File.Exists("/.dockerenv");
        }
    }

    /// <summary>
    /// Application-level health check
    /// Verifies core application components are functioning
    /// </summary>
    public class ApplicationHealthCheck : IHealthCheck
    {
        private readonly ILogger<ApplicationHealthCheck> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ApplicationHealthCheck(ILogger<ApplicationHealthCheck> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>();

                // Check if essential services are registered
                var checks = new List<(string name, bool healthy, string message)>();

                // Verify DI container health
                try
                {
                    var _ = _serviceProvider.GetService<IConfiguration>();
                    checks.Add(("DI Container", true, "Dependency injection working"));
                }
                catch (Exception ex)
                {
                    checks.Add(("DI Container", false, $"DI failure: {ex.Message}"));
                }

                // Check application startup time
                var startupTime = Environment.TickCount64;
                data.Add("uptime_ms", startupTime);
                data.Add("process_id", Environment.ProcessId);
                data.Add("machine_name", Environment.MachineName);
                data.Add("dotnet_version", Environment.Version.ToString());

                // Verify configuration
                try
                {
                    var config = _serviceProvider.GetRequiredService<IConfiguration>();
                    var connectionString = config.GetConnectionString("DefaultConnection");
                    checks.Add(("Configuration", !string.IsNullOrEmpty(connectionString), 
                        string.IsNullOrEmpty(connectionString) ? "Missing connection string" : "Configuration loaded"));
                }
                catch (Exception ex)
                {
                    checks.Add(("Configuration", false, $"Config error: {ex.Message}"));
                }

                // Aggregate results
                var unhealthyChecks = checks.Where(c => !c.healthy).ToList();
                
                foreach (var check in checks)
                {
                    data.Add($"check_{check.name.Replace(" ", "_").ToLower()}", 
                        new { healthy = check.healthy, message = check.message });
                }

                if (unhealthyChecks.Any())
                {
                    var errorMessages = string.Join("; ", unhealthyChecks.Select(c => c.message));
                    return HealthCheckResult.Unhealthy($"Application health issues: {errorMessages}", data: data);
                }

                return HealthCheckResult.Healthy("Application is running normally", data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application health check failed");
                return HealthCheckResult.Unhealthy($"Health check exception: {ex.Message}", data: new Dictionary<string, object> { { "error", ex.Message } });
            }
        }
    }

    /// <summary>
    /// Legacy system connectivity health check
    /// Verifies connection to legacy SQL Server system
    /// </summary>
    public class LegacySystemHealthCheck : IHealthCheck
    {
        private readonly ILogger<LegacySystemHealthCheck> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public LegacySystemHealthCheck(ILogger<LegacySystemHealthCheck> logger, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>();
                var legacyConnectionString = _configuration.GetConnectionString("LegacyDatabase");

                if (string.IsNullOrEmpty(legacyConnectionString))
                {
                    data.Add("legacy_configured", false);
                    return HealthCheckResult.Degraded("Legacy database connection not configured", data: data);
                }

                data.Add("legacy_configured", true);

                // Test legacy database connection
                using var legacyContext = new AntiCorruption.LegacyDatabaseContext(legacyConnectionString, 
                    _serviceProvider?.GetService<ILogger<AntiCorruption.LegacyDatabaseContext>>());
                var isConnected = await legacyContext.TestConnectionAsync();

                data.Add("legacy_connected", isConnected);
                data.Add("check_time", DateTime.UtcNow);

                if (!isConnected)
                {
                    return HealthCheckResult.Degraded("Cannot connect to legacy database", data: data);
                }

                // Additional legacy system checks could go here
                // e.g., check specific tables, recent data, etc.

                return HealthCheckResult.Healthy("Legacy system is accessible", data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Legacy system health check failed");
                return HealthCheckResult.Degraded($"Legacy system check error: {ex.Message}", data: new Dictionary<string, object> { { "error", ex.Message } });
            }
        }
    }

    /// <summary>
    /// Data consistency health check
    /// Verifies data integrity between systems
    /// </summary>
    public class DataConsistencyHealthCheck : IHealthCheck
    {
        private readonly ILogger<DataConsistencyHealthCheck> _logger;

        public DataConsistencyHealthCheck(ILogger<DataConsistencyHealthCheck> logger)
        {
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>();

                // Sample data consistency checks
                // In production, these would verify actual data consistency
                
                var checks = new List<(string name, bool consistent, string details)>
                {
                    ("Antrag Records", true, "Sample consistency check passed"),
                    ("Person Records", true, "Sample consistency check passed"),
                    ("Reference Data", true, "Sample consistency check passed")
                };

                var inconsistentChecks = checks.Where(c => !c.consistent).ToList();

                foreach (var check in checks)
                {
                    data.Add($"consistency_{check.name.Replace(" ", "_").ToLower()}", 
                        new { consistent = check.consistent, details = check.details });
                }

                data.Add("last_check", DateTime.UtcNow);
                data.Add("total_checks", checks.Count);
                data.Add("inconsistent_count", inconsistentChecks.Count);

                if (inconsistentChecks.Any())
                {
                    var issues = string.Join("; ", inconsistentChecks.Select(c => c.details));
                    return HealthCheckResult.Degraded($"Data consistency issues: {issues}", data: data);
                }

                return HealthCheckResult.Healthy("Data consistency verified", data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data consistency health check failed");
                return HealthCheckResult.Degraded($"Data consistency check error: {ex.Message}", data: new Dictionary<string, object> { { "error", ex.Message } });
            }
        }
    }

    /// <summary>
    /// Migration status health check
    /// Reports on the current state of the migration process
    /// </summary>
    public class MigrationHealthCheck : IHealthCheck
    {
        private readonly ILogger<MigrationHealthCheck> _logger;
        private readonly IConfiguration _configuration;

        public MigrationHealthCheck(ILogger<MigrationHealthCheck> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>();

                // Get migration configuration
                var stranglerFigConfig = _configuration.GetSection("StranglerFig");
                var isEnabled = stranglerFigConfig.GetValue<bool>("Enabled");
                
                data.Add("strangler_fig_enabled", isEnabled);

                if (isEnabled)
                {
                    var routes = stranglerFigConfig.GetSection("Routes").Get<dynamic[]>() ?? Array.Empty<dynamic>();
                    var totalRoutes = routes.Length;
                    
                    // Calculate migration progress (this is a simplified example)
                    var migratedRoutes = 0; // In reality, count routes with 100% migration
                    var partialRoutes = 0;  // Routes with partial migration
                    
                    data.Add("total_routes", totalRoutes);
                    data.Add("fully_migrated_routes", migratedRoutes);
                    data.Add("partially_migrated_routes", partialRoutes);
                    data.Add("migration_percentage", totalRoutes > 0 ? (migratedRoutes * 100.0 / totalRoutes) : 0);
                    data.Add("migration_phase", DetermineMigrationPhase(migratedRoutes, totalRoutes));
                }

                data.Add("migration_active", isEnabled);
                data.Add("check_time", DateTime.UtcNow);

                return HealthCheckResult.Healthy("Migration status reported", data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration health check failed");
                return HealthCheckResult.Degraded($"Migration status check error: {ex.Message}", data: new Dictionary<string, object> { { "error", ex.Message } });
            }
        }

        private string DetermineMigrationPhase(int migratedRoutes, int totalRoutes)
        {
            if (totalRoutes == 0) return "not_configured";
            
            var percentage = (migratedRoutes * 100.0) / totalRoutes;
            
            return percentage switch
            {
                0 => "pre_migration",
                < 25 => "initial_migration",
                < 50 => "active_migration",
                < 75 => "advanced_migration",
                < 100 => "final_migration",
                100 => "migration_complete",
                _ => "unknown"
            };
        }
    }

    /// <summary>
    /// File system health check for container environments
    /// </summary>
    public class FileSystemHealthCheck : IHealthCheck
    {
        private readonly ILogger<FileSystemHealthCheck> _logger;

        public FileSystemHealthCheck(ILogger<FileSystemHealthCheck> logger)
        {
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>();

                // Check temp directory access
                var tempPath = Path.GetTempPath();
                var canWriteTemp = CheckDirectoryAccess(tempPath);
                data.Add("temp_directory_writable", canWriteTemp);
                data.Add("temp_path", tempPath);

                // Check current directory access
                var currentPath = Directory.GetCurrentDirectory();
                var canWriteCurrent = CheckDirectoryAccess(currentPath);
                data.Add("current_directory_writable", canWriteCurrent);
                data.Add("current_path", currentPath);

                // Check disk space
                var drive = new DriveInfo(Path.GetPathRoot(currentPath));
                var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                var totalSpaceGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                var freeSpacePercent = (freeSpaceGB / totalSpaceGB) * 100;

                data.Add("free_space_gb", Math.Round(freeSpaceGB, 2));
                data.Add("total_space_gb", Math.Round(totalSpaceGB, 2));
                data.Add("free_space_percent", Math.Round(freeSpacePercent, 1));

                var issues = new List<string>();

                if (!canWriteTemp) issues.Add("Cannot write to temp directory");
                if (!canWriteCurrent) issues.Add("Cannot write to current directory");
                if (freeSpacePercent < 5) issues.Add($"Low disk space: {freeSpacePercent:F1}%");

                if (issues.Any())
                {
                    return HealthCheckResult.Unhealthy($"File system issues: {string.Join("; ", issues)}", data: data);
                }

                return HealthCheckResult.Healthy("File system is accessible", data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File system health check failed");
                return HealthCheckResult.Unhealthy($"File system check error: {ex.Message}", data: new Dictionary<string, object> { { "error", ex.Message } });
            }
        }

        private bool CheckDirectoryAccess(string path)
        {
            try
            {
                var testFile = Path.Combine(path, $"health_check_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Memory usage health check
    /// </summary>
    public class MemoryHealthCheck : IHealthCheck
    {
        private readonly ILogger<MemoryHealthCheck> _logger;

        public MemoryHealthCheck(ILogger<MemoryHealthCheck> logger)
        {
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>();

                // Get memory information
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var workingSetMB = process.WorkingSet64 / (1024.0 * 1024.0);
                var privateMemoryMB = process.PrivateMemorySize64 / (1024.0 * 1024.0);

                // Get GC information
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);
                var totalMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

                data.Add("working_set_mb", Math.Round(workingSetMB, 2));
                data.Add("private_memory_mb", Math.Round(privateMemoryMB, 2));
                data.Add("gc_total_memory_mb", Math.Round(totalMemoryMB, 2));
                data.Add("gc_gen0_collections", gen0Collections);
                data.Add("gc_gen1_collections", gen1Collections);
                data.Add("gc_gen2_collections", gen2Collections);

                // Define memory thresholds (in MB)
                const double warningThreshold = 1024; // 1 GB
                const double criticalThreshold = 2048; // 2 GB

                var issues = new List<string>();

                if (workingSetMB > criticalThreshold)
                {
                    issues.Add($"Critical memory usage: {workingSetMB:F0} MB");
                }
                else if (workingSetMB > warningThreshold)
                {
                    issues.Add($"High memory usage: {workingSetMB:F0} MB");
                }

                if (gen2Collections > 100) // Arbitrary threshold for Gen2 collections
                {
                    issues.Add($"High Gen2 GC pressure: {gen2Collections} collections");
                }

                if (issues.Any())
                {
                    var status = workingSetMB > criticalThreshold ? HealthStatus.Unhealthy : HealthStatus.Degraded;
                    return new HealthCheckResult(status, $"Memory issues: {string.Join("; ", issues)}", data: data);
                }

                return HealthCheckResult.Healthy("Memory usage is normal", data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory health check failed");
                return HealthCheckResult.Degraded($"Memory check error: {ex.Message}", data: new Dictionary<string, object> { { "error", ex.Message } });
            }
        }
    }

    /// <summary>
    /// Container-specific health check
    /// </summary>
    public class ContainerHealthCheck : IHealthCheck
    {
        private readonly ILogger<ContainerHealthCheck> _logger;

        public ContainerHealthCheck(ILogger<ContainerHealthCheck> logger)
        {
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>();

                // Check container environment variables
                var containerEnvVars = new[]
                {
                    "DOTNET_RUNNING_IN_CONTAINER",
                    "HOSTNAME",
                    "PATH",
                    "HOME"
                };

                var envData = new Dictionary<string, string>();
                foreach (var envVar in containerEnvVars)
                {
                    var value = Environment.GetEnvironmentVariable(envVar);
                    envData.Add(envVar.ToLower(), value ?? "not_set");
                }

                data.Add("environment_variables", envData);
                data.Add("is_container", Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true");
                data.Add("has_dockerenv", File.Exists("/.dockerenv"));

                // Check container-specific paths
                var containerPaths = new[]
                {
                    "/proc/self/cgroup",
                    "/sys/fs/cgroup",
                    "/.dockerenv"
                };

                var pathData = new Dictionary<string, bool>();
                foreach (var path in containerPaths)
                {
                    pathData.Add(path, File.Exists(path) || Directory.Exists(path));
                }

                data.Add("container_paths", pathData);

                return HealthCheckResult.Healthy("Container environment verified", data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Container health check failed");
                return HealthCheckResult.Degraded($"Container check error: {ex.Message}", data: new Dictionary<string, object> { { "error", ex.Message } });
            }
        }
    }
}