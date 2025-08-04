using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Enums;
using MediatR;

namespace KGV.Application.Features.Antraege.Commands;

/// <summary>
/// Command to update an existing Antrag
/// </summary>
public class UpdateAntragCommand : IRequest<Result<AntragDto>>
{
    /// <summary>
    /// Antrag ID to update
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Primary applicant salutation
    /// </summary>
    public Anrede? Anrede { get; set; }

    /// <summary>
    /// Primary applicant title
    /// </summary>
    public string? Titel { get; set; }

    /// <summary>
    /// Primary applicant first name
    /// </summary>
    public string? Vorname { get; set; }

    /// <summary>
    /// Primary applicant last name
    /// </summary>
    public string? Nachname { get; set; }

    /// <summary>
    /// Secondary applicant salutation
    /// </summary>
    public Anrede? Anrede2 { get; set; }

    /// <summary>
    /// Secondary applicant title
    /// </summary>
    public string? Titel2 { get; set; }

    /// <summary>
    /// Secondary applicant first name
    /// </summary>
    public string? Vorname2 { get; set; }

    /// <summary>
    /// Secondary applicant last name
    /// </summary>
    public string? Nachname2 { get; set; }

    /// <summary>
    /// Letter salutation
    /// </summary>
    public string? Briefanrede { get; set; }

    /// <summary>
    /// Street address
    /// </summary>
    public string? Strasse { get; set; }

    /// <summary>
    /// Postal code
    /// </summary>
    public string? PLZ { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? Ort { get; set; }

    /// <summary>
    /// Primary phone number
    /// </summary>
    public string? Telefon { get; set; }

    /// <summary>
    /// Mobile phone number
    /// </summary>
    public string? MobilTelefon { get; set; }

    /// <summary>
    /// Business phone number
    /// </summary>
    public string? GeschTelefon { get; set; }

    /// <summary>
    /// Secondary mobile phone number
    /// </summary>
    public string? MobilTelefon2 { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public string? EMail { get; set; }

    /// <summary>
    /// Applicant's wishes/preferences
    /// </summary>
    public string? Wunsch { get; set; }

    /// <summary>
    /// Primary applicant birthday
    /// </summary>
    public string? Geburtstag { get; set; }

    /// <summary>
    /// Secondary applicant birthday
    /// </summary>
    public string? Geburtstag2 { get; set; }

    /// <summary>
    /// Notes/remarks
    /// </summary>
    public string? Vermerk { get; set; }

    /// <summary>
    /// Waiting list number for district 32
    /// </summary>
    public string? WartelistenNr32 { get; set; }

    /// <summary>
    /// Waiting list number for district 33
    /// </summary>
    public string? WartelistenNr33 { get; set; }
}