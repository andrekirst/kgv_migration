using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using KGV.Infrastructure.Patterns.AntiCorruption.LegacyModels;

namespace KGV.Infrastructure.Patterns.AntiCorruption
{
    /// <summary>
    /// Database context for legacy SQL Server system
    /// Provides raw data access with proper connection management
    /// </summary>
    public interface ILegacyDatabaseContext : IDisposable
    {
        Task<IDbConnection> GetConnectionAsync();
        Task<T> QuerySingleAsync<T>(string sql, object parameters = null);
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null);
        Task<int> ExecuteAsync(string sql, object parameters = null);
        Task<bool> TestConnectionAsync();
    }

    public class LegacyDatabaseContext : ILegacyDatabaseContext
    {
        private readonly string _connectionString;
        private readonly ILogger<LegacyDatabaseContext> _logger;
        private Microsoft.Data.SqlClient.SqlConnection _connection;

        public LegacyDatabaseContext(string connectionString, ILogger<LegacyDatabaseContext> logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        public async Task<IDbConnection> GetConnectionAsync()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await _connection.OpenAsync();
                _logger?.LogDebug("Opened connection to legacy database");
            }

            return _connection;
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object parameters = null)
        {
            var connection = await GetConnectionAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            
            if (parameters != null)
                AddParameters(command, parameters);

            _logger?.LogDebug("Executing query: {Sql}", sql);
            
            using var reader = await ((Microsoft.Data.SqlClient.SqlCommand)command).ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return MapFromDataReader<T>(reader);
            }

            return default(T);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null)
        {
            var connection = await GetConnectionAsync();
            var results = new List<T>();
            
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            
            if (parameters != null)
                AddParameters(command, parameters);

            _logger?.LogDebug("Executing query: {Sql}", sql);
            
            using var reader = await ((Microsoft.Data.SqlClient.SqlCommand)command).ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(MapFromDataReader<T>(reader));
            }

            return results;
        }

        public async Task<int> ExecuteAsync(string sql, object parameters = null)
        {
            var connection = await GetConnectionAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            
            if (parameters != null)
                AddParameters(command, parameters);

            _logger?.LogDebug("Executing command: {Sql}", sql);
            
            return await ((Microsoft.Data.SqlClient.SqlCommand)command).ExecuteNonQueryAsync();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var connection = await GetConnectionAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                
                var result = await ((Microsoft.Data.SqlClient.SqlCommand)command).ExecuteScalarAsync();
                return result?.ToString() == "1";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to test legacy database connection");
                return false;
            }
        }

        private void AddParameters(IDbCommand command, object parameters)
        {
            var properties = parameters.GetType().GetProperties();
            foreach (var property in properties)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{property.Name}";
                parameter.Value = property.GetValue(parameters) ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        private T MapFromDataReader<T>(IDataReader reader)
        {
            // Simple mapping - in production, consider using Dapper or AutoMapper
            var type = typeof(T);
            
            if (type == typeof(LegacyAntrag))
                return (T)(object)MapLegacyAntrag(reader);
            
            if (type == typeof(LegacyPerson))
                return (T)(object)MapLegacyPerson(reader);
            
            if (type == typeof(LegacyBezirk))
                return (T)(object)MapLegacyBezirk(reader);
            
            if (type == typeof(LegacyVerlauf))
                return (T)(object)MapLegacyVerlauf(reader);
            
            if (type == typeof(LegacyKatasterbezirk))
                return (T)(object)MapLegacyKatasterbezirk(reader);
            
            if (type == typeof(LegacyAktenzeichen))
                return (T)(object)MapLegacyAktenzeichen(reader);
            
            if (type == typeof(LegacyEingangsnummer))
                return (T)(object)MapLegacyEingangsnummer(reader);

            throw new NotSupportedException($"Mapping for type {type.Name} is not implemented");
        }

        private LegacyAntrag MapLegacyAntrag(IDataReader reader)
        {
            var sqlReader = (Microsoft.Data.SqlClient.SqlDataReader)reader;
            return new LegacyAntrag
            {
                an_ID = sqlReader.GetGuid("an_ID"),
                an_Aktenzeichen = sqlReader.IsDBNull("an_Aktenzeichen") ? null : sqlReader.GetString("an_Aktenzeichen"),
                an_WartelistenNr32 = sqlReader.IsDBNull("an_WartelistenNr32") ? null : sqlReader.GetString("an_WartelistenNr32"),
                an_WartelistenNr33 = sqlReader.IsDBNull("an_WartelistenNr33") ? null : sqlReader.GetString("an_WartelistenNr33"),
                an_Anrede = sqlReader.IsDBNull("an_Anrede") ? null : sqlReader.GetString("an_Anrede"),
                an_Titel = sqlReader.IsDBNull("an_Titel") ? null : sqlReader.GetString("an_Titel"),
                an_Vorname = sqlReader.IsDBNull("an_Vorname") ? null : sqlReader.GetString("an_Vorname"),
                an_Nachname = sqlReader.IsDBNull("an_Nachname") ? null : sqlReader.GetString("an_Nachname"),
                an_Anrede2 = sqlReader.IsDBNull("an_Anrede2") ? null : sqlReader.GetString("an_Anrede2"),
                an_Titel2 = sqlReader.IsDBNull("an_Titel2") ? null : sqlReader.GetString("an_Titel2"),
                an_Vorname2 = sqlReader.IsDBNull("an_Vorname2") ? null : sqlReader.GetString("an_Vorname2"),
                an_Nachname2 = sqlReader.IsDBNull("an_Nachname2") ? null : sqlReader.GetString("an_Nachname2"),
                an_Briefanrede = sqlReader.IsDBNull("an_Briefanrede") ? null : sqlReader.GetString("an_Briefanrede"),
                an_Strasse = sqlReader.IsDBNull("an_Strasse") ? null : sqlReader.GetString("an_Strasse"),
                an_PLZ = sqlReader.IsDBNull("an_PLZ") ? null : sqlReader.GetString("an_PLZ"),
                an_Ort = sqlReader.IsDBNull("an_Ort") ? null : sqlReader.GetString("an_Ort"),
                an_Telefon = sqlReader.IsDBNull("an_Telefon") ? null : sqlReader.GetString("an_Telefon"),
                an_MobilTelefon = sqlReader.IsDBNull("an_MobilTelefon") ? null : sqlReader.GetString("an_MobilTelefon"),
                an_GeschTelefon = sqlReader.IsDBNull("an_GeschTelefon") ? null : sqlReader.GetString("an_GeschTelefon"),
                an_Bewerbungsdatum = sqlReader.IsDBNull("an_Bewerbungsdatum") ? null : sqlReader.GetDateTime("an_Bewerbungsdatum"),
                an_Bestaetigungsdatum = sqlReader.IsDBNull("an_Bestaetigungsdatum") ? null : sqlReader.GetDateTime("an_Bestaetigungsdatum"),
                an_AktuellesAngebot = sqlReader.IsDBNull("an_AktuellesAngebot") ? null : sqlReader.GetDateTime("an_AktuellesAngebot"),
                an_Loeschdatum = sqlReader.IsDBNull("an_Loeschdatum") ? null : sqlReader.GetDateTime("an_Loeschdatum"),
                an_Wunsch = sqlReader.IsDBNull("an_Wunsch") ? null : sqlReader.GetString("an_Wunsch"),
                an_Vermerk = sqlReader.IsDBNull("an_Vermerk") ? null : sqlReader.GetString("an_Vermerk"),
                an_Aktiv = ParseNullableChar(reader, "an_Aktiv"),
                an_DeaktiviertAm = sqlReader.IsDBNull("an_DeaktiviertAm") ? null : sqlReader.GetDateTime("an_DeaktiviertAm"),
                an_Geburtstag = sqlReader.IsDBNull("an_Geburtstag") ? null : sqlReader.GetString("an_Geburtstag"),
                an_Geburtstag2 = sqlReader.IsDBNull("an_Geburtstag2") ? null : sqlReader.GetString("an_Geburtstag2"),
                an_MobilTelefon2 = sqlReader.IsDBNull("an_MobilTelefon2") ? null : sqlReader.GetString("an_MobilTelefon2"),
                an_EMail = sqlReader.IsDBNull("an_EMail") ? null : sqlReader.GetString("an_EMail")
            };
        }

        private LegacyPerson MapLegacyPerson(IDataReader reader)
        {
            var sqlReader = (Microsoft.Data.SqlClient.SqlDataReader)reader;
            return new LegacyPerson
            {
                Pers_ID = sqlReader.GetGuid("Pers_ID"),
                Pers_Anrede = sqlReader.IsDBNull("Pers_Anrede") ? null : sqlReader.GetString("Pers_Anrede"),
                Pers_Vorname = sqlReader.IsDBNull("Pers_Vorname") ? null : sqlReader.GetString("Pers_Vorname"),
                Pers_Nachname = sqlReader.IsDBNull("Pers_Nachname") ? null : sqlReader.GetString("Pers_Nachname"),
                Pers_Nummer = sqlReader.IsDBNull("Pers_Nummer") ? null : sqlReader.GetString("Pers_Nummer"),
                Pers_Organisationseinheit = sqlReader.IsDBNull("Pers_Organisationseinheit") ? null : sqlReader.GetString("Pers_Organisationseinheit"),
                Pers_Zimmer = sqlReader.IsDBNull("Pers_Zimmer") ? null : sqlReader.GetString("Pers_Zimmer"),
                Pers_Telefon = sqlReader.IsDBNull("Pers_Telefon") ? null : sqlReader.GetString("Pers_Telefon"),
                Pers_FAX = sqlReader.IsDBNull("Pers_FAX") ? null : sqlReader.GetString("Pers_FAX"),
                Pers_Email = sqlReader.IsDBNull("Pers_Email") ? null : sqlReader.GetString("Pers_Email"),
                Pers_Diktatzeichen = sqlReader.IsDBNull("Pers_Diktatzeichen") ? null : sqlReader.GetString("Pers_Diktatzeichen"),
                Pers_Unterschrift = sqlReader.IsDBNull("Pers_Unterschrift") ? null : sqlReader.GetString("Pers_Unterschrift"),
                Pers_Dienstbezeichnung = sqlReader.IsDBNull("Pers_Dienstbezeichnung") ? null : sqlReader.GetString("Pers_Dienstbezeichnung"),
                Pers_Grp_ID = sqlReader.IsDBNull("Pers_Grp_ID") ? null : sqlReader.GetGuid("Pers_Grp_ID"),
                Pers_istAdmin = ParseNullableChar(reader, "Pers_istAdmin"),
                Pers_darfAdministration = ParseNullableChar(reader, "Pers_darfAdministration"),
                Pers_darfLeistungsgruppen = ParseNullableChar(reader, "Pers_darfLeistungsgruppen"),
                Pers_darfPrioUndSLA = ParseNullableChar(reader, "Pers_darfPrioUndSLA"),
                Pers_darfKunden = ParseNullableChar(reader, "Pers_darfKunden"),
                Pers_Aktiv = ParseNullableChar(reader, "Pers_Aktiv")
            };
        }

        private LegacyBezirk MapLegacyBezirk(IDataReader reader)
        {
            var sqlReader = (Microsoft.Data.SqlClient.SqlDataReader)reader;
            return new LegacyBezirk
            {
                bez_ID = sqlReader.GetGuid("bez_ID"),
                bez_Name = sqlReader.IsDBNull("bez_Name") ? null : sqlReader.GetString("bez_Name")
            };
        }

        private LegacyVerlauf MapLegacyVerlauf(IDataReader reader)
        {
            var sqlReader = (Microsoft.Data.SqlClient.SqlDataReader)reader;
            return new LegacyVerlauf
            {
                verl_ID = sqlReader.GetGuid("verl_ID"),
                verl_An_ID = sqlReader.IsDBNull("verl_An_ID") ? null : sqlReader.GetGuid("verl_An_ID"),
                verl_Art = sqlReader.IsDBNull("verl_Art") ? null : sqlReader.GetString("verl_Art"),
                verl_Datum = sqlReader.IsDBNull("verl_Datum") ? null : sqlReader.GetDateTime("verl_Datum"),
                verl_Gemarkung = sqlReader.IsDBNull("verl_Gemarkung") ? null : sqlReader.GetString("verl_Gemarkung"),
                verl_Flur = sqlReader.IsDBNull("verl_Flur") ? null : sqlReader.GetString("verl_Flur"),
                verl_Parzelle = sqlReader.IsDBNull("verl_Parzelle") ? null : sqlReader.GetString("verl_Parzelle"),
                verl_Groesse = sqlReader.IsDBNull("verl_Groesse") ? null : sqlReader.GetString("verl_Groesse"),
                verl_Sachbearbeiter = sqlReader.IsDBNull("verl_Sachbearbeiter") ? null : sqlReader.GetString("verl_Sachbearbeiter"),
                verl_Hinweis = sqlReader.IsDBNull("verl_Hinweis") ? null : sqlReader.GetString("verl_Hinweis"),
                verl_Kommentar = sqlReader.IsDBNull("verl_Kommentar") ? null : sqlReader.GetString("verl_Kommentar")
            };
        }

        private LegacyKatasterbezirk MapLegacyKatasterbezirk(IDataReader reader)
        {
            var sqlReader = (Microsoft.Data.SqlClient.SqlDataReader)reader;
            return new LegacyKatasterbezirk
            {
                kat_ID = sqlReader.GetGuid("kat_ID"),
                kat_bez_ID = sqlReader.IsDBNull("kat_bez_ID") ? null : sqlReader.GetGuid("kat_bez_ID"),
                kat_Katasterbezirk = sqlReader.IsDBNull("kat_Katasterbezirk") ? null : sqlReader.GetString("kat_Katasterbezirk"),
                kat_KatasterbezirkName = sqlReader.IsDBNull("kat_KatasterbezirkName") ? null : sqlReader.GetString("kat_KatasterbezirkName")
            };
        }

        private LegacyAktenzeichen MapLegacyAktenzeichen(IDataReader reader)
        {
            var sqlReader = (Microsoft.Data.SqlClient.SqlDataReader)reader;
            return new LegacyAktenzeichen
            {
                az_ID = sqlReader.GetGuid("az_ID"),
                az_Bezirk = sqlReader.IsDBNull("az_Bezirk") ? null : sqlReader.GetString("az_Bezirk"),
                az_Nummer = sqlReader.IsDBNull("az_Nummer") ? null : (int?)sqlReader.GetInt32("az_Nummer"),
                az_Jahr = sqlReader.IsDBNull("az_Jahr") ? null : (int?)sqlReader.GetInt32("az_Jahr")
            };
        }

        private LegacyEingangsnummer MapLegacyEingangsnummer(IDataReader reader)
        {
            var sqlReader = (Microsoft.Data.SqlClient.SqlDataReader)reader;
            return new LegacyEingangsnummer
            {
                enr_ID = sqlReader.GetGuid("enr_ID"),
                enr_Bezirk = sqlReader.IsDBNull("enr_Bezirk") ? null : sqlReader.GetString("enr_Bezirk"),
                enr_Nummer = sqlReader.IsDBNull("enr_Nummer") ? null : (int?)sqlReader.GetInt32("enr_Nummer"),
                enr_Jahr = sqlReader.IsDBNull("enr_Jahr") ? null : (int?)sqlReader.GetInt32("enr_Jahr")
            };
        }

        private int? ParseNullableInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            // If it's already an int, return it
            if (value is int intValue)
                return intValue;

            // Try to parse string values
            if (value is string stringValue)
            {
                if (string.IsNullOrWhiteSpace(stringValue))
                    return null;

                if (int.TryParse(stringValue.Trim(), out int parsedValue))
                    return parsedValue;

                _logger?.LogWarning("Failed to parse string value '{Value}' as integer", stringValue);
                return null;
            }

            // Try to convert other numeric types
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to convert value '{Value}' of type '{Type}' to integer", value, value.GetType().Name);
                return null;
            }
        }

        private char? ParseNullableChar(IDataReader reader, string fieldName)
        {
            var sqlReader = (Microsoft.Data.SqlClient.SqlDataReader)reader;
            // Explicitly ensure we're using the string overload methods
            if (sqlReader.IsDBNull(fieldName))
                return null;

            var stringValue = sqlReader.GetString(fieldName);
            if (string.IsNullOrEmpty(stringValue))
                return null;

            return stringValue[0];
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _logger?.LogDebug("Disposed legacy database connection");
        }
    }
}