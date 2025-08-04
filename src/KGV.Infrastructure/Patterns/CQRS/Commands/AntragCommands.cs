using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using KGV.Infrastructure.Patterns.AntiCorruption.ModernModels;

namespace KGV.Infrastructure.Patterns.CQRS.Commands
{
    /// <summary>
    /// Commands for Application (Antrag) entity operations
    /// Following CQRS pattern for write operations
    /// </summary>

    #region Create Application Commands

    public class CreateApplicationCommand : BaseCommand<OperationResult<Guid>>
    {
        [Required]
        public string FileReference { get; set; }
        
        public string WaitingListNumber32 { get; set; }
        public string WaitingListNumber33 { get; set; }
        
        [Required]
        public ContactDto PrimaryContact { get; set; }
        
        public ContactDto SecondaryContact { get; set; }
        
        [Required]
        public AddressDto Address { get; set; }
        
        public ApplicationDatesDto Dates { get; set; }
        
        public string Preferences { get; set; }
        public string Notes { get; set; }
    }

    public class CreateApplicationCommandHandler : ICommandHandler<CreateApplicationCommand, OperationResult<Guid>>
    {
        private readonly IApplicationRepository _repository;
        private readonly ILogger<CreateApplicationCommandHandler> _logger;

        public CreateApplicationCommandHandler(
            IApplicationRepository repository,
            ILogger<CreateApplicationCommandHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OperationResult<Guid>> HandleAsync(
            CreateApplicationCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating new application with file reference {FileReference}",
                    command.FileReference);

                // Check if file reference already exists
                var existingApplication = await _repository.GetByFileReferenceAsync(command.FileReference);
                if (existingApplication != null)
                {
                    return OperationResult<Guid>.Failure(
                        $"Application with file reference {command.FileReference} already exists",
                        "DUPLICATE_FILE_REFERENCE");
                }

                // Create new application
                var application = new Application
                {
                    Id = Guid.NewGuid(),
                    FileReference = command.FileReference,
                    WaitingListNumber32 = command.WaitingListNumber32,
                    WaitingListNumber33 = command.WaitingListNumber33,
                    PrimaryContact = MapContact(command.PrimaryContact, ContactType.Primary),
                    SecondaryContact = command.SecondaryContact != null 
                        ? MapContact(command.SecondaryContact, ContactType.Secondary) 
                        : null,
                    Address = MapAddress(command.Address),
                    Dates = MapDates(command.Dates),
                    Status = ApplicationStatus.Draft,
                    Preferences = command.Preferences,
                    Notes = command.Notes,
                    CreatedAt = DateTime.UtcNow,
                    History = new List<ApplicationHistory>()
                };

                await _repository.AddAsync(application);

                _logger.LogInformation("Application {ApplicationId} created successfully", application.Id);

                return OperationResult<Guid>.Success(application.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create application with file reference {FileReference}",
                    command.FileReference);
                
                return OperationResult<Guid>.Failure(
                    $"Failed to create application: {ex.Message}",
                    "CREATE_APPLICATION_ERROR");
            }
        }

        private Contact MapContact(ContactDto dto, ContactType type)
        {
            return new Contact
            {
                Type = type,
                Salutation = dto.Salutation,
                Title = dto.Title,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DateOfBirth = dto.DateOfBirth,
                ContactDetails = new ContactDetails
                {
                    Phone = dto.Phone,
                    Mobile = dto.Mobile,
                    BusinessPhone = dto.BusinessPhone,
                    Email = dto.Email,
                    FormalSalutation = dto.FormalSalutation
                }
            };
        }

        private Address MapAddress(AddressDto dto)
        {
            return new Address
            {
                Street = dto.Street,
                PostalCode = dto.PostalCode,
                City = dto.City
            };
        }

        private ApplicationDates MapDates(ApplicationDatesDto dto)
        {
            if (dto == null) return new ApplicationDates();
            
            return new ApplicationDates
            {
                ApplicationDate = dto.ApplicationDate,
                ConfirmationDate = dto.ConfirmationDate,
                CurrentOfferDate = dto.CurrentOfferDate,
                DeletionDate = dto.DeletionDate
            };
        }
    }

    #endregion

    #region Update Application Commands

    public class UpdateApplicationCommand : BaseCommand<OperationResult>
    {
        [Required]
        public Guid ApplicationId { get; set; }
        
        public string WaitingListNumber32 { get; set; }
        public string WaitingListNumber33 { get; set; }
        
        public ContactDto PrimaryContact { get; set; }
        public ContactDto SecondaryContact { get; set; }
        public AddressDto Address { get; set; }
        public ApplicationDatesDto Dates { get; set; }
        
        public string Preferences { get; set; }
        public string Notes { get; set; }
    }

    public class UpdateApplicationCommandHandler : ICommandHandler<UpdateApplicationCommand, OperationResult>
    {
        private readonly IApplicationRepository _repository;
        private readonly ILogger<UpdateApplicationCommandHandler> _logger;

        public UpdateApplicationCommandHandler(
            IApplicationRepository repository,
            ILogger<UpdateApplicationCommandHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OperationResult> HandleAsync(
            UpdateApplicationCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Updating application {ApplicationId}", command.ApplicationId);

                var application = await _repository.GetByIdAsync(command.ApplicationId);
                if (application == null)
                {
                    return OperationResult.Failure(
                        $"Application {command.ApplicationId} not found",
                        "APPLICATION_NOT_FOUND");
                }

                // Update fields
                if (command.WaitingListNumber32 != null)
                    application.WaitingListNumber32 = command.WaitingListNumber32;
                
                if (command.WaitingListNumber33 != null)
                    application.WaitingListNumber33 = command.WaitingListNumber33;

                if (command.PrimaryContact != null)
                {
                    application.PrimaryContact = MapContact(command.PrimaryContact, ContactType.Primary);
                }

                if (command.SecondaryContact != null)
                {
                    application.SecondaryContact = MapContact(command.SecondaryContact, ContactType.Secondary);
                }

                if (command.Address != null)
                {
                    application.Address = MapAddress(command.Address);
                }

                if (command.Dates != null)
                {
                    application.Dates = MapDates(command.Dates);
                }

                if (command.Preferences != null)
                    application.Preferences = command.Preferences;

                if (command.Notes != null)
                    application.Notes = command.Notes;

                application.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(application);

                _logger.LogInformation("Application {ApplicationId} updated successfully", command.ApplicationId);

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update application {ApplicationId}", command.ApplicationId);
                
                return OperationResult.Failure(
                    $"Failed to update application: {ex.Message}",
                    "UPDATE_APPLICATION_ERROR");
            }
        }

        private Contact MapContact(ContactDto dto, ContactType type)
        {
            return new Contact
            {
                Type = type,
                Salutation = dto.Salutation,
                Title = dto.Title,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DateOfBirth = dto.DateOfBirth,
                ContactDetails = new ContactDetails
                {
                    Phone = dto.Phone,
                    Mobile = dto.Mobile,
                    BusinessPhone = dto.BusinessPhone,
                    Email = dto.Email,
                    FormalSalutation = dto.FormalSalutation
                }
            };
        }

        private Address MapAddress(AddressDto dto)
        {
            return new Address
            {
                Street = dto.Street,
                PostalCode = dto.PostalCode,
                City = dto.City
            };
        }

        private ApplicationDates MapDates(ApplicationDatesDto dto)
        {
            return new ApplicationDates
            {
                ApplicationDate = dto.ApplicationDate,
                ConfirmationDate = dto.ConfirmationDate,
                CurrentOfferDate = dto.CurrentOfferDate,
                DeletionDate = dto.DeletionDate
            };
        }
    }

    #endregion

    #region Change Application Status Commands

    public class ChangeApplicationStatusCommand : BaseCommand<OperationResult>
    {
        [Required]
        public Guid ApplicationId { get; set; }
        
        [Required]
        public ApplicationStatus NewStatus { get; set; }
        
        public string Reason { get; set; }
    }

    public class ChangeApplicationStatusCommandHandler : ICommandHandler<ChangeApplicationStatusCommand, OperationResult>
    {
        private readonly IApplicationRepository _repository;
        private readonly ILogger<ChangeApplicationStatusCommandHandler> _logger;

        public ChangeApplicationStatusCommandHandler(
            IApplicationRepository repository,
            ILogger<ChangeApplicationStatusCommandHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OperationResult> HandleAsync(
            ChangeApplicationStatusCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Changing status of application {ApplicationId} to {NewStatus}",
                    command.ApplicationId, command.NewStatus);

                var application = await _repository.GetByIdAsync(command.ApplicationId);
                if (application == null)
                {
                    return OperationResult.Failure(
                        $"Application {command.ApplicationId} not found",
                        "APPLICATION_NOT_FOUND");
                }

                var oldStatus = application.Status;

                // Validate status transition
                if (!IsValidStatusTransition(oldStatus, command.NewStatus))
                {
                    return OperationResult.Failure(
                        $"Invalid status transition from {oldStatus} to {command.NewStatus}",
                        "INVALID_STATUS_TRANSITION");
                }

                application.Status = command.NewStatus;
                application.UpdatedAt = DateTime.UtcNow;

                // Handle status-specific logic
                switch (command.NewStatus)
                {
                    case ApplicationStatus.Archived:
                        application.DeactivatedAt = DateTime.UtcNow;
                        break;
                    case ApplicationStatus.Approved:
                        if (application.Dates?.ConfirmationDate == null)
                        {
                            application.Dates = application.Dates ?? new ApplicationDates();
                            application.Dates.ConfirmationDate = DateTime.UtcNow;
                        }
                        break;
                }

                // Add history entry
                var historyEntry = new ApplicationHistory
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = application.Id,
                    Type = "STATUS_CHANGE",
                    Date = DateTime.UtcNow,
                    CaseWorker = command.UserId ?? "System",
                    Note = $"Status changed from {oldStatus} to {command.NewStatus}",
                    Comment = command.Reason,
                    CreatedAt = DateTime.UtcNow
                };

                application.History.Add(historyEntry);

                await _repository.UpdateAsync(application);

                _logger.LogInformation("Application {ApplicationId} status changed from {OldStatus} to {NewStatus}",
                    command.ApplicationId, oldStatus, command.NewStatus);

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change status of application {ApplicationId}",
                    command.ApplicationId);
                
                return OperationResult.Failure(
                    $"Failed to change application status: {ex.Message}",
                    "CHANGE_STATUS_ERROR");
            }
        }

        private bool IsValidStatusTransition(ApplicationStatus from, ApplicationStatus to)
        {
            // Define valid transitions based on business rules
            return (from, to) switch
            {
                (ApplicationStatus.Draft, ApplicationStatus.Submitted) => true,
                (ApplicationStatus.Submitted, ApplicationStatus.UnderReview) => true,
                (ApplicationStatus.UnderReview, ApplicationStatus.Approved) => true,
                (ApplicationStatus.UnderReview, ApplicationStatus.Rejected) => true,
                (ApplicationStatus.Approved, ApplicationStatus.Active) => true,
                (_, ApplicationStatus.Cancelled) => true, // Can cancel from any status
                (ApplicationStatus.Active, ApplicationStatus.Archived) => true,
                (ApplicationStatus.Rejected, ApplicationStatus.Archived) => true,
                (ApplicationStatus.Cancelled, ApplicationStatus.Archived) => true,
                _ => false
            };
        }
    }

    #endregion

    #region Delete Application Command

    public class DeleteApplicationCommand : BaseCommand<OperationResult>
    {
        [Required]
        public Guid ApplicationId { get; set; }
        
        public string Reason { get; set; }
        public bool HardDelete { get; set; } = false; // Soft delete by default
    }

    public class DeleteApplicationCommandHandler : ICommandHandler<DeleteApplicationCommand, OperationResult>
    {
        private readonly IApplicationRepository _repository;
        private readonly ILogger<DeleteApplicationCommandHandler> _logger;

        public DeleteApplicationCommandHandler(
            IApplicationRepository repository,
            ILogger<DeleteApplicationCommandHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OperationResult> HandleAsync(
            DeleteApplicationCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting application {ApplicationId} (Hard: {HardDelete})",
                    command.ApplicationId, command.HardDelete);

                var application = await _repository.GetByIdAsync(command.ApplicationId);
                if (application == null)
                {
                    return OperationResult.Failure(
                        $"Application {command.ApplicationId} not found",
                        "APPLICATION_NOT_FOUND");
                }

                if (command.HardDelete)
                {
                    await _repository.DeleteAsync(command.ApplicationId);
                }
                else
                {
                    // Soft delete - change status to cancelled and set deletion date
                    application.Status = ApplicationStatus.Cancelled;
                    application.Dates = application.Dates ?? new ApplicationDates();
                    application.Dates.DeletionDate = DateTime.UtcNow;
                    application.UpdatedAt = DateTime.UtcNow;

                    // Add history entry
                    var historyEntry = new ApplicationHistory
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Type = "DELETION",
                        Date = DateTime.UtcNow,
                        CaseWorker = command.UserId ?? "System",
                        Note = "Application deleted (soft delete)",
                        Comment = command.Reason,
                        CreatedAt = DateTime.UtcNow
                    };

                    application.History.Add(historyEntry);

                    await _repository.UpdateAsync(application);
                }

                _logger.LogInformation("Application {ApplicationId} deleted successfully", command.ApplicationId);

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete application {ApplicationId}", command.ApplicationId);
                
                return OperationResult.Failure(
                    $"Failed to delete application: {ex.Message}",
                    "DELETE_APPLICATION_ERROR");
            }
        }
    }

    #endregion

    #region Data Transfer Objects

    public class ContactDto
    {
        public string Salutation { get; set; }
        public string Title { get; set; }
        
        [Required]
        public string FirstName { get; set; }
        
        [Required]
        public string LastName { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string BusinessPhone { get; set; }
        public string Email { get; set; }
        public string FormalSalutation { get; set; }
    }

    public class AddressDto
    {
        [Required]
        public string Street { get; set; }
        
        [Required]
        public string PostalCode { get; set; }
        
        [Required]
        public string City { get; set; }
    }

    public class ApplicationDatesDto
    {
        public DateTime? ApplicationDate { get; set; }
        public DateTime? ConfirmationDate { get; set; }
        public DateTime? CurrentOfferDate { get; set; }
        public DateTime? DeletionDate { get; set; }
    }

    #endregion

    #region Repository Interface (placeholder)

    public interface IApplicationRepository
    {
        Task<Application> GetByIdAsync(Guid id);
        Task<Application> GetByFileReferenceAsync(string fileReference);
        Task AddAsync(Application application);
        Task UpdateAsync(Application application);
        Task DeleteAsync(Guid id);
    }

    #endregion
}