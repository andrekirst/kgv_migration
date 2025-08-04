using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
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
        private SqlConnection _connection;

        public LegacyDatabaseContext(string connectionString, ILogger<LegacyDatabaseContext> logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        public async Task<IDbConnection> GetConnectionAsync()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = new SqlConnection(_connectionString);
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
            
            using var reader = await ((SqlCommand)command).ExecuteReaderAsync();
            
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
            
            using var reader = await ((SqlCommand)command).ExecuteReaderAsync();
            
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
            
            return await ((SqlCommand)command).ExecuteNonQueryAsync();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var connection = await GetConnectionAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                
                var result = await ((SqlCommand)command).ExecuteScalarAsync();
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
            return new LegacyAntrag
            {
                an_ID = reader.GetGuid("an_ID"),
                an_Aktenzeichen = reader.IsDBNull("an_Aktenzeichen") ? null : reader.GetString("an_Aktenzeichen"),
                an_WartelistenNr32 = reader.IsDBNull("an_WartelistenNr32") ? null : reader.GetString("an_WartelistenNr32"),
                an_WartelistenNr33 = reader.IsDBNull("an_WartelistenNr33") ? null : reader.GetString("an_WartelistenNr33"),
                an_Anrede = reader.IsDBNull("an_Anrede") ? null : reader.GetString("an_Anrede"),
                an_Titel = reader.IsDBNull("an_Titel") ? null : reader.GetString("an_Titel"),
                an_Vorname = reader.IsDBNull("an_Vorname") ? null : reader.GetString("an_Vorname"),
                an_Nachname = reader.IsDBNull("an_Nachname") ? null : reader.GetString("an_Nachname"),
                an_Anrede2 = reader.IsDBNull("an_Anrede2") ? null : reader.GetString("an_Anrede2"),
                an_Titel2 = reader.IsDBNull("an_Titel2") ? null : reader.GetString("an_Titel2"),
                an_Vorname2 = reader.IsDBNull("an_Vorname2") ? null : reader.GetString("an_Vorname2"),
                an_Nachname2 = reader.IsDBNull("an_Nachname2") ? null : reader.GetString("an_Nachname2"),
                an_Briefanrede = reader.IsDBNull("an_Briefanrede") ? null : reader.GetString("an_Briefanrede"),
                an_Strasse = reader.IsDBNull("an_Strasse") ? null : reader.GetString("an_Strasse"),
                an_PLZ = reader.IsDBNull("an_PLZ") ? null : reader.GetString("an_PLZ"),
                an_Ort = reader.IsDBNull("an_Ort") ? null : reader.GetString("an_Ort"),
                an_Telefon = reader.IsDBNull("an_Telefon") ? null : reader.GetString("an_Telefon"),
                an_MobilTelefon = reader.IsDBNull("an_MobilTelefon") ? null : reader.GetString("an_MobilTelefon"),
                an_GeschTelefon = reader.IsDBNull("an_GeschTelefon") ? null : reader.GetString("an_GeschTelefon"),
                an_Bewerbungsdatum = reader.IsDBNull("an_Bewerbungsdatum") ? null : reader.GetDateTime("an_Bewerbungsdatum"),
                an_Bestaetigungsdatum = reader.IsDBNull("an_Bestaetigungsdatum") ? null : reader.GetDateTime("an_Bestaetigungsdatum"),
                an_AktuellesAngebot = reader.IsDBNull("an_AktuellesAngebot") ? null : reader.GetDateTime("an_AktuellesAngebot"),
                an_Loeschdatum = reader.IsDBNull("an_Loeschdatum") ? null : reader.GetDateTime("an_Loeschdatum"),
                an_Wunsch = reader.IsDBNull("an_Wunsch") ? null : reader.GetString("an_Wunsch"),
                an_Vermerk = reader.IsDBNull("an_Vermerk") ? null : reader.GetString("an_Vermerk"),
                an_Aktiv = reader.IsDBNull("an_Aktiv") ? null : reader.GetString("an_Aktiv")[0],
                an_DeaktiviertAm = reader.IsDBNull("an_DeaktiviertAm") ? null : reader.GetDateTime("an_DeaktiviertAm"),
                an_Geburtstag = reader.IsDBNull("an_Geburtstag") ? null : reader.GetString("an_Geburtstag"),
                an_Geburtstag2 = reader.IsDBNull("an_Geburtstag2") ? null : reader.GetString("an_Geburtstag2"),
                an_MobilTelefon2 = reader.IsDBNull("an_MobilTelefon2") ? null : reader.GetString("an_MobilTelefon2"),
                an_EMail = reader.IsDBNull("an_EMail") ? null : reader.GetString("an_EMail")
            };
        }

        private LegacyPerson MapLegacyPerson(IDataReader reader)
        {
            return new LegacyPerson
            {
                Pers_ID = reader.GetGuid("Pers_ID"),
                Pers_Anrede = reader.IsDBNull("Pers_Anrede") ? null : reader.GetString("Pers_Anrede"),
                Pers_Vorname = reader.IsDBNull("Pers_Vorname") ? null : reader.GetString("Pers_Vorname"),
                Pers_Nachname = reader.IsDBNull("Pers_Nachname") ? null : reader.GetString("Pers_Nachname"),
                Pers_Nummer = reader.IsDBNull("Pers_Nummer") ? null : reader.GetString("Pers_Nummer"),
                Pers_Organisationseinheit = reader.IsDBNull("Pers_Organisationseinheit") ? null : reader.GetString("Pers_Organisationseinheit"),
                Pers_Zimmer = reader.IsDBNull("Pers_Zimmer") ? null : reader.GetString("Pers_Zimmer"),
                Pers_Telefon = reader.IsDBNull("Pers_Telefon") ? null : reader.GetString("Pers_Telefon"),
                Pers_FAX = reader.IsDBNull("Pers_FAX") ? null : reader.GetString("Pers_FAX"),
                Pers_Email = reader.IsDBNull("Pers_Email") ? null : reader.GetString("Pers_Email"),
                Pers_Diktatzeichen = reader.IsDBNull("Pers_Diktatzeichen") ? null : reader.GetString("Pers_Diktatzeichen"),
                Pers_Unterschrift = reader.IsDBNull("Pers_Unterschrift") ? null : reader.GetString("Pers_Unterschrift"),
                Pers_Dienstbezeichnung = reader.IsDBNull("Pers_Dienstbezeichnung") ? null : reader.GetString("Pers_Dienstbezeichnung"),
                Pers_Grp_ID = reader.IsDBNull("Pers_Grp_ID") ? null : reader.GetGuid("Pers_Grp_ID"),
                Pers_istAdmin = reader.IsDBNull("Pers_istAdmin") ? null : reader.GetString("Pers_istAdmin")[0],
                Pers_darfAdministration = reader.IsDBNull("Pers_darfAdministration") ? null : reader.GetString("Pers_darfAdministration")[0],
                Pers_darfLeistungsgruppen = reader.IsDBNull("Pers_darfLeistungsgruppen") ? null : reader.GetString("Pers_darfLeistungsgruppen")[0],
                Pers_darfPrioUndSLA = reader.IsDBNull("Pers_darfPrioUndSLA") ? null : reader.GetString("Pers_darfPrioUndSLA")[0],
                Pers_darfKunden = reader.IsDBNull("Pers_darfKunden") ? null : reader.GetString("Pers_darfKunden")[0],
                Pers_Aktiv = reader.IsDBNull("Pers_Aktiv") ? null : reader.GetString("Pers_Aktiv")[0]
            };
        }

        private LegacyBezirk MapLegacyBezirk(IDataReader reader)
        {
            return new LegacyBezirk
            {
                bez_ID = reader.GetGuid("bez_ID"),
                bez_Name = reader.IsDBNull("bez_Name") ? null : reader.GetString("bez_Name")
            };
        }

        private LegacyVerlauf MapLegacyVerlauf(IDataReader reader)
        {
            return new LegacyVerlauf
            {
                verl_ID = reader.GetGuid("verl_ID"),
                verl_An_ID = reader.IsDBNull("verl_An_ID") ? null : reader.GetGuid("verl_An_ID"),
                verl_Art = reader.IsDBNull("verl_Art") ? null : reader.GetString("verl_Art"),
                verl_Datum = reader.IsDBNull("verl_Datum") ? null : reader.GetDateTime("verl_Datum"),
                verl_Gemarkung = reader.IsDBNull("verl_Gemarkung") ? null : reader.GetString("verl_Gemarkung"),
                verl_Flur = reader.IsDBNull("verl_Flur") ? null : reader.GetString("verl_Flur"),
                verl_Parzelle = reader.IsDBNull("verl_Parzelle") ? null : reader.GetString("verl_Parzelle"),
                verl_Groesse = reader.IsDBNull("verl_Groesse") ? null : reader.GetString("verl_Groesse"),
                verl_Sachbearbeiter = reader.IsDBNull("verl_Sachbearbeiter") ? null : reader.GetString("verl_Sachbearbeiter"),
                verl_Hinweis = reader.IsDBNull("verl_Hinweis") ? null : reader.GetString("verl_Hinweis"),
                verl_Kommentar = reader.IsDBNull("verl_Kommentar") ? null : reader.GetString("verl_Kommentar")
            };
        }

        private LegacyKatasterbezirk MapLegacyKatasterbezirk(IDataReader reader)
        {
            return new LegacyKatasterbezirk
            {
                kat_ID = reader.GetGuid("kat_ID"),
                kat_bez_ID = reader.IsDBNull("kat_bez_ID") ? null : reader.GetGuid("kat_bez_ID"),
                kat_Katasterbezirk = reader.IsDBNull("kat_Katasterbezirk") ? null : reader.GetString("kat_Katasterbezirk"),
                kat_KatasterbezirkName = reader.IsDBNull("kat_KatasterbezirkName") ? null : reader.GetString("kat_KatasterbezirkName")
            };
        }

        private LegacyAktenzeichen MapLegacyAktenzeichen(IDataReader reader)
        {
            return new LegacyAktenzeichen
            {
                az_ID = reader.GetGuid("az_ID"),
                az_Bezirk = reader.IsDBNull("az_Bezirk") ? null : reader.GetString("az_Bezirk"),
                az_Nummer = reader.IsDBNull("az_Nummer") ? null : reader.GetInt32("az_Nummer"),
                az_Jahr = reader.IsDBNull("az_Jahr") ? null : reader.GetInt32("az_Jahr")
            };
        }

        private LegacyEingangsnummer MapLegacyEingangsnummer(IDataReader reader)
        {
            return new LegacyEingangsnummer
            {
                enr_ID = reader.GetGuid("enr_ID"),
                enr_Bezirk = reader.IsDBNull("enr_Bezirk") ? null : reader.GetString("enr_Bezirk"),
                enr_Nummer = reader.IsDBNull("enr_Nummer") ? null : reader.GetInt32("enr_Nummer"),
                enr_Jahr = reader.IsDBNull("enr_Jahr") ? null : reader.GetInt32("enr_Jahr")
            };
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _logger?.LogDebug("Disposed legacy database connection");
        }
    }
}