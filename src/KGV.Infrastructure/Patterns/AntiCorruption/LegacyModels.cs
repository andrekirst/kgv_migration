using System;
using System.ComponentModel.DataAnnotations;

namespace KGV.Infrastructure.Patterns.AntiCorruption.LegacyModels
{
    /// <summary>
    /// Legacy data models representing the original SQL Server schema
    /// These models match exactly with the legacy database structure
    /// </summary>

    public class LegacyAntrag
    {
        public Guid an_ID { get; set; }
        public string an_Aktenzeichen { get; set; }
        public string an_WartelistenNr32 { get; set; }
        public string an_WartelistenNr33 { get; set; }
        public string an_Anrede { get; set; }
        public string an_Titel { get; set; }
        public string an_Vorname { get; set; }
        public string an_Nachname { get; set; }
        public string an_Anrede2 { get; set; }
        public string an_Titel2 { get; set; }
        public string an_Vorname2 { get; set; }
        public string an_Nachname2 { get; set; }
        public string an_Briefanrede { get; set; }
        public string an_Strasse { get; set; }
        public string an_PLZ { get; set; }
        public string an_Ort { get; set; }
        public string an_Telefon { get; set; }
        public string an_MobilTelefon { get; set; }
        public string an_GeschTelefon { get; set; }
        public DateTime? an_Bewerbungsdatum { get; set; }
        public DateTime? an_Bestaetigungsdatum { get; set; }
        public DateTime? an_AktuellesAngebot { get; set; }
        public DateTime? an_Loeschdatum { get; set; }
        public string an_Wunsch { get; set; }
        public string an_Vermerk { get; set; }
        public char? an_Aktiv { get; set; }
        public DateTime? an_DeaktiviertAm { get; set; }
        public string an_Geburtstag { get; set; }
        public string an_Geburtstag2 { get; set; }
        public string an_MobilTelefon2 { get; set; }
        public string an_EMail { get; set; }
    }

    public class LegacyPerson
    {
        public Guid Pers_ID { get; set; }
        public string Pers_Anrede { get; set; }
        public string Pers_Vorname { get; set; }
        public string Pers_Nachname { get; set; }
        public string Pers_Nummer { get; set; }
        public string Pers_Organisationseinheit { get; set; }
        public string Pers_Zimmer { get; set; }
        public string Pers_Telefon { get; set; }
        public string Pers_FAX { get; set; }
        public string Pers_Email { get; set; }
        public string Pers_Diktatzeichen { get; set; }
        public string Pers_Unterschrift { get; set; }
        public string Pers_Dienstbezeichnung { get; set; }
        public Guid? Pers_Grp_ID { get; set; }
        public char? Pers_istAdmin { get; set; }
        public char? Pers_darfAdministration { get; set; }
        public char? Pers_darfLeistungsgruppen { get; set; }
        public char? Pers_darfPrioUndSLA { get; set; }
        public char? Pers_darfKunden { get; set; }
        public char? Pers_Aktiv { get; set; }
    }

    public class LegacyBezirk
    {
        public Guid bez_ID { get; set; }
        public string bez_Name { get; set; }
    }

    public class LegacyKatasterbezirk
    {
        public Guid kat_ID { get; set; }
        public Guid? kat_bez_ID { get; set; }
        public string kat_Katasterbezirk { get; set; }
        public string kat_KatasterbezirkName { get; set; }
    }

    public class LegacyVerlauf
    {
        public Guid verl_ID { get; set; }
        public Guid? verl_An_ID { get; set; }
        public string verl_Art { get; set; }
        public DateTime? verl_Datum { get; set; }
        public string verl_Gemarkung { get; set; }
        public string verl_Flur { get; set; }
        public string verl_Parzelle { get; set; }
        public string verl_Groesse { get; set; }
        public string verl_Sachbearbeiter { get; set; }
        public string verl_Hinweis { get; set; }
        public string verl_Kommentar { get; set; }
    }

    public class LegacyAktenzeichen
    {
        public Guid az_ID { get; set; }
        public string az_Bezirk { get; set; }
        public int? az_Nummer { get; set; }
        public int? az_Jahr { get; set; }
    }

    public class LegacyEingangsnummer
    {
        public Guid enr_ID { get; set; }
        public string enr_Bezirk { get; set; }
        public int? enr_Nummer { get; set; }
        public int? enr_Jahr { get; set; }
    }
}