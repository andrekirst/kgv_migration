using AutoMapper;
using KGV.Application.DTOs;
using KGV.Domain.Entities;
using KGV.Domain.Enums;

namespace KGV.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for mapping between domain entities and DTOs
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Antrag, AntragDto>()
            .ForMember(dest => dest.Strasse, opt => opt.MapFrom(src => src.Adresse != null ? src.Adresse.Strasse : null))
            .ForMember(dest => dest.PLZ, opt => opt.MapFrom(src => src.Adresse != null ? src.Adresse.PLZ : null))
            .ForMember(dest => dest.Ort, opt => opt.MapFrom(src => src.Adresse != null ? src.Adresse.Ort : null))
            .ForMember(dest => dest.Telefon, opt => opt.MapFrom(src => src.Telefon != null ? src.Telefon.Value : null))
            .ForMember(dest => dest.MobilTelefon, opt => opt.MapFrom(src => src.MobilTelefon != null ? src.MobilTelefon.Value : null))
            .ForMember(dest => dest.GeschTelefon, opt => opt.MapFrom(src => src.GeschTelefon != null ? src.GeschTelefon.Value : null))
            .ForMember(dest => dest.MobilTelefon2, opt => opt.MapFrom(src => src.MobilTelefon2 != null ? src.MobilTelefon2.Value : null))
            .ForMember(dest => dest.EMail, opt => opt.MapFrom(src => src.EMail != null ? src.EMail.Value : null))
            .ForMember(dest => dest.StatusBeschreibung, opt => opt.MapFrom(src => GetStatusBeschreibung(src.Status)))
            .ForMember(dest => dest.VollName, opt => opt.MapFrom(src => src.GetFullName()))
            .ForMember(dest => dest.VollName2, opt => opt.MapFrom(src => src.GetSecondaryFullName()))
            .ForMember(dest => dest.VollAdresse, opt => opt.MapFrom(src => src.Adresse != null ? src.Adresse.GetFullAddress() : null))
            .ForMember(dest => dest.AktenzeichenValue, opt => opt.MapFrom(src => src.AktenzeichenValue))
            .ForMember(dest => dest.Verlauf, opt => opt.MapFrom(src => src.Verlauf));

        CreateMap<Antrag, AntragListDto>()
            .ForMember(dest => dest.Ort, opt => opt.MapFrom(src => src.Adresse != null ? src.Adresse.Ort : null))
            .ForMember(dest => dest.StatusBeschreibung, opt => opt.MapFrom(src => GetStatusBeschreibung(src.Status)))
            .ForMember(dest => dest.VollName, opt => opt.MapFrom(src => src.GetFullName()))
            .ForMember(dest => dest.Aktenzeichen, opt => opt.MapFrom(src => src.AktenzeichenValue));

        CreateMap<Verlauf, VerlaufDto>()
            .ForMember(dest => dest.ArtBeschreibung, opt => opt.MapFrom(src => GetVerlaufArtBeschreibung(src.Art)))
            .ForMember(dest => dest.ParzellInfo, opt => opt.MapFrom(src => src.GetPlotInfoString()))
            .ForMember(dest => dest.Zusammenfassung, opt => opt.MapFrom(src => src.GetSummary()));

        CreateMap<Bezirk, BezirkDto>()
            .ForMember(dest => dest.AnzeigeName, opt => opt.MapFrom(src => src.GetDisplayName()))
            .ForMember(dest => dest.AnzahlAntraege, opt => opt.Ignore()) // Will be populated separately
            .ForMember(dest => dest.Katasterbezirke, opt => opt.MapFrom(src => src.Katasterbezirke));

        CreateMap<Bezirk, BezirkListDto>()
            .ForMember(dest => dest.AnzeigeName, opt => opt.MapFrom(src => src.GetDisplayName()))
            .ForMember(dest => dest.AnzahlKatasterbezirke, opt => opt.MapFrom(src => src.Katasterbezirke.Count))
            .ForMember(dest => dest.AnzahlAntraege, opt => opt.Ignore()); // Will be populated separately

        CreateMap<Katasterbezirk, KatasterbezirkDto>()
            .ForMember(dest => dest.BezirkName, opt => opt.MapFrom(src => src.Bezirk != null ? src.Bezirk.Name : null))
            .ForMember(dest => dest.VollAnzeigeName, opt => opt.MapFrom(src => src.GetFullDisplayName()));

        CreateMap<Person, PersonDto>()
            .ForMember(dest => dest.Telefon, opt => opt.MapFrom(src => src.Telefon != null ? src.Telefon.Value : null))
            .ForMember(dest => dest.FAX, opt => opt.MapFrom(src => src.FAX != null ? src.FAX.Value : null))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email != null ? src.Email.Value : null))
            .ForMember(dest => dest.VollName, opt => opt.MapFrom(src => src.GetFullName()))
            .ForMember(dest => dest.AnzeigeName, opt => opt.MapFrom(src => src.GetDisplayName()));

        CreateMap<AktenzeichenEntity, AktenzeichenDto>()
            .ForMember(dest => dest.FormattedValue, opt => opt.MapFrom(src => src.GetFormattedValue()));

        CreateMap<Eingangsnummer, EingangsnummerDto>()
            .ForMember(dest => dest.FormattedValue, opt => opt.MapFrom(src => src.GetFormattedValue()));
    }

    private static string GetStatusBeschreibung(AntragStatus status)
    {
        return status switch
        {
            AntragStatus.NeuEingegangen => "Neu eingegangen",
            AntragStatus.InBearbeitung => "In Bearbeitung",
            AntragStatus.Warteschlange => "Warteschlange",
            AntragStatus.AngebotGemacht => "Angebot gemacht",
            AntragStatus.AngebotAngenommen => "Angebot angenommen",
            AntragStatus.AngebotAbgelehnt => "Angebot abgelehnt",
            AntragStatus.Abgeschlossen => "Abgeschlossen",
            AntragStatus.Abgebrochen => "Abgebrochen",
            AntragStatus.Deaktiviert => "Deaktiviert",
            _ => status.ToString()
        };
    }

    private static string GetVerlaufArtBeschreibung(VerlaufArt art)
    {
        return art switch
        {
            VerlaufArt.AntragEingegangen => "Antrag eingegangen",
            VerlaufArt.BestaetigungVersendet => "Bestätigung versendet",
            VerlaufArt.AngebotGemacht => "Angebot gemacht",
            VerlaufArt.AngebotAngenommen => "Angebot angenommen",
            VerlaufArt.AngebotAbgelehnt => "Angebot abgelehnt",
            VerlaufArt.Besichtigung => "Besichtigung",
            VerlaufArt.VertragErstellt => "Vertrag erstellt",
            VerlaufArt.Abgeschlossen => "Abgeschlossen",
            VerlaufArt.Notiz => "Notiz",
            _ => art.ToString()
        };
    }
}

/// <summary>
/// Additional DTOs for completeness
/// </summary>
public class PersonDto
{
    public Guid Id { get; set; }
    public string? Anrede { get; set; }
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string? Nummer { get; set; }
    public string? Organisationseinheit { get; set; }
    public string? Zimmer { get; set; }
    public string? Telefon { get; set; }
    public string? FAX { get; set; }
    public string? Email { get; set; }
    public string? Diktatzeichen { get; set; }
    public string? Unterschrift { get; set; }
    public string? Dienstbezeichnung { get; set; }
    public Guid? GruppeId { get; set; }
    public bool IstAdmin { get; set; }
    public bool DarfAdministration { get; set; }
    public bool DarfLeistungsgruppen { get; set; }
    public bool DarfPrioUndSLA { get; set; }
    public bool DarfKunden { get; set; }
    public bool Aktiv { get; set; }
    public string? Username { get; set; }
    public DateTime? LastLogin { get; set; }
    public string VollName { get; set; } = string.Empty;
    public string AnzeigeName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class AktenzeichenDto
{
    public Guid Id { get; set; }
    public string Bezirk { get; set; } = string.Empty;
    public int Nummer { get; set; }
    public int Jahr { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string FormattedValue { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class EingangsnummerDto
{
    public Guid Id { get; set; }
    public string Bezirk { get; set; } = string.Empty;
    public int Nummer { get; set; }
    public int Jahr { get; set; }
    public DateTime Eingangsdatum { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string FormattedValue { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}