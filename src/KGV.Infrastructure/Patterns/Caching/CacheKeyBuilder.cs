using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KGV.Infrastructure.Patterns.Caching
{
    /// <summary>
    /// Cache key builder implementation for consistent key generation
    /// Provides standardized cache key patterns for KGV domain entities
    /// </summary>
    public class CacheKeyBuilder : ICacheKeyBuilder
    {
        private readonly string _keyPrefix;
        private readonly string _applicationName;

        public CacheKeyBuilder(string keyPrefix = "kgv", string applicationName = "migration")
        {
            _keyPrefix = keyPrefix?.ToLower() ?? "kgv";
            _applicationName = applicationName?.ToLower() ?? "migration";
        }

        public string BuildKey<T>(string identifier, params object[] parameters)
        {
            return BuildKey(typeof(T).Name, identifier, parameters);
        }

        public string BuildKey(string entityType, string identifier, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));

            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));

            var keyBuilder = new StringBuilder();
            
            // Base pattern: {prefix}:{app}:{entityType}:{identifier}
            keyBuilder.Append(_keyPrefix)
                     .Append(':')
                     .Append(_applicationName)
                     .Append(':')
                     .Append(entityType.ToLower())
                     .Append(':')
                     .Append(SanitizeIdentifier(identifier));

            // Add parameters if provided
            if (parameters != null && parameters.Length > 0)
            {
                var paramString = string.Join(":", parameters.Select(p => SanitizeParameter(p)));
                if (!string.IsNullOrEmpty(paramString))
                {
                    keyBuilder.Append(':').Append(paramString);
                }
            }

            var key = keyBuilder.ToString();
            
            // Ensure key length is reasonable for Redis
            if (key.Length > 250) // Redis recommended max key length
            {
                return CreateHashedKey(key);
            }

            return key;
        }

        public string BuildPatternKey(string entityType, string pattern = "*")
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));

            return $"{_keyPrefix}:{_applicationName}:{entityType.ToLower()}:{pattern ?? "*"}";
        }

        public string BuildUserKey(string userId, string entityType, string identifier)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            return BuildKey(entityType, identifier, "user", SanitizeIdentifier(userId));
        }

        public string BuildListKey(string entityType, params object[] filters)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));

            var keyBuilder = new StringBuilder();
            
            keyBuilder.Append(_keyPrefix)
                     .Append(':')
                     .Append(_applicationName)
                     .Append(':')
                     .Append(entityType.ToLower())
                     .Append(":list");

            if (filters != null && filters.Length > 0)
            {
                var filterString = string.Join(":", filters.Select(f => SanitizeParameter(f)));
                if (!string.IsNullOrEmpty(filterString))
                {
                    keyBuilder.Append(':').Append(filterString);
                }
            }

            var key = keyBuilder.ToString();
            
            if (key.Length > 250)
            {
                return CreateHashedKey(key);
            }

            return key;
        }

        public string BuildStatsKey(string entityType, string statsType)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));

            if (string.IsNullOrWhiteSpace(statsType))
                throw new ArgumentException("Stats type cannot be null or empty", nameof(statsType));

            return $"{_keyPrefix}:{_applicationName}:{entityType.ToLower()}:stats:{statsType.ToLower()}";
        }

        private string SanitizeIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return string.Empty;

            // Remove or replace characters that could cause issues in Redis keys
            return identifier.Replace(" ", "_")
                           .Replace(":", "_")
                           .Replace("*", "_")
                           .Replace("?", "_")
                           .Replace("[", "_")
                           .Replace("]", "_")
                           .Replace("{", "_")
                           .Replace("}", "_")
                           .ToLower();
        }

        private string SanitizeParameter(object parameter)
        {
            if (parameter == null)
                return "null";

            var paramString = parameter switch
            {
                DateTime dateTime => dateTime.ToString("yyyyMMddHHmmss"),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("yyyyMMddHHmmss"),
                Guid guid => guid.ToString("N"),
                bool boolean => boolean.ToString().ToLower(),
                _ => parameter.ToString()
            };

            return SanitizeIdentifier(paramString);
        }

        private string CreateHashedKey(string originalKey)
        {
            // Create a shorter key using hash but keep the prefix for readability
            var prefixParts = originalKey.Split(':').Take(3).ToArray(); // Keep prefix:app:entityType
            var prefix = string.Join(":", prefixParts);
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(originalKey));
            var hash = Convert.ToBase64String(hashBytes)
                            .Replace("+", "-")
                            .Replace("/", "_")
                            .Replace("=", "")
                            .Substring(0, 16); // Take first 16 characters

            return $"{prefix}:hash:{hash}";
        }
    }

    /// <summary>
    /// KGV-specific cache key patterns and constants
    /// </summary>
    public static class KgvCacheKeys
    {
        // Entity type constants
        public const string APPLICATION = "application";
        public const string PERSON = "person";
        public const string DISTRICT = "district";
        public const string HISTORY = "history";
        public const string STATISTICS = "statistics";

        // Cache patterns
        public static class Patterns
        {
            public const string ALL_APPLICATIONS = "kgv:migration:application:*";
            public const string ALL_PERSONS = "kgv:migration:person:*";
            public const string ALL_DISTRICTS = "kgv:migration:district:*";
            public const string ALL_STATISTICS = "kgv:migration:*:stats:*";
            public const string USER_CACHE = "kgv:migration:*:*:user:*";
        }

        // Statistics types
        public static class Statistics
        {
            public const string DAILY = "daily";
            public const string MONTHLY = "monthly";
            public const string YEARLY = "yearly";
            public const string STATUS_COUNTS = "status_counts";
            public const string PERFORMANCE = "performance";
        }

        // List keys
        public static class Lists
        {
            public const string APPLICATIONS_BY_STATUS = "applications_by_status";
            public const string APPLICATIONS_BY_DISTRICT = "applications_by_district";
            public const string RECENT_APPLICATIONS = "recent_applications";
            public const string PENDING_APPLICATIONS = "pending_applications";
        }
    }
}