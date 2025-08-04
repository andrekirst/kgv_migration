using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KGV.Infrastructure.Patterns.AntiCorruption.ModernModels
{
    /// <summary>
    /// Modern domain models following DDD principles and clean architecture
    /// These models represent the desired state after migration
    /// </summary>

    public enum ApplicationStatus
    {
        Draft,
        Submitted,
        UnderReview,
        Approved,
        Rejected,
        Cancelled,
        Active,
        Archived
    }

    public enum ContactType
    {
        Primary,
        Secondary
    }

    public class Application
    {
        public Guid Id { get; set; }
        public string FileReference { get; set; }
        public string WaitingListNumber32 { get; set; }
        public string WaitingListNumber33 { get; set; }
        
        public Contact PrimaryContact { get; set; }
        public Contact SecondaryContact { get; set; }
        
        public Address Address { get; set; }
        public ApplicationDates Dates { get; set; }
        public ApplicationStatus Status { get; set; }
        
        public string Preferences { get; set; }
        public string Notes { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        
        public List<ApplicationHistory> History { get; set; } = new List<ApplicationHistory>();
    }

    public class Contact
    {
        public ContactType Type { get; set; }
        public string Salutation { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        
        public ContactDetails ContactDetails { get; set; }
    }

    public class ContactDetails
    {
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string BusinessPhone { get; set; }
        public string Email { get; set; }
        public string FormalSalutation { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string FullAddress => $"{Street}, {PostalCode} {City}".Trim();
    }

    public class ApplicationDates
    {
        public DateTime? ApplicationDate { get; set; }
        public DateTime? ConfirmationDate { get; set; }
        public DateTime? CurrentOfferDate { get; set; }
        public DateTime? DeletionDate { get; set; }
    }

    public class Person
    {
        public Guid Id { get; set; }
        public string EmployeeNumber { get; set; }
        public Contact Contact { get; set; }
        public OrganizationalInfo OrganizationalInfo { get; set; }
        public ContactDetails ContactDetails { get; set; }
        public SystemPermissions Permissions { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class OrganizationalInfo
    {
        public string OrganizationalUnit { get; set; }
        public string Room { get; set; }
        public string JobTitle { get; set; }
        public string DictationMark { get; set; }
        public string Signature { get; set; }
        public Guid? GroupId { get; set; }
    }

    public class SystemPermissions
    {
        public bool IsAdmin { get; set; }
        public bool CanAdministrate { get; set; }
        public bool CanManageServiceGroups { get; set; }
        public bool CanManagePriorityAndSLA { get; set; }
        public bool CanManageCustomers { get; set; }
    }

    public class District
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public List<CadastralDistrict> CadastralDistricts { get; set; } = new List<CadastralDistrict>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CadastralDistrict
    {
        public Guid Id { get; set; }
        public Guid DistrictId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public District District { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ApplicationHistory
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public PlotInfo PlotInfo { get; set; }
        public string CaseWorker { get; set; }
        public string Note { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PlotInfo
    {
        public string Gemarkung { get; set; }
        public string Flur { get; set; }
        public string Parzelle { get; set; }
        public string Size { get; set; }
        public string FullDescription => $"{Gemarkung}, Flur {Flur}, Parzelle {Parzelle} ({Size})".Trim();
    }

    public class FileReference
    {
        public Guid Id { get; set; }
        public string District { get; set; }
        public int Number { get; set; }
        public int Year { get; set; }
        public string FullReference => $"{District}-{Number:D4}/{Year}";
        public DateTime CreatedAt { get; set; }
    }

    public class EntryNumber
    {
        public Guid Id { get; set; }
        public string District { get; set; }
        public int Number { get; set; }
        public int Year { get; set; }
        public string FullNumber => $"{District}-{Number:D4}/{Year}";
        public DateTime CreatedAt { get; set; }
    }
}