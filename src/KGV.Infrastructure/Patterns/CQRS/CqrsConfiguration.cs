using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using KGV.Infrastructure.Patterns.CQRS.Commands;
using KGV.Infrastructure.Patterns.CQRS.Queries;
using KGV.Infrastructure.Patterns.CQRS.Decorators;

namespace KGV.Infrastructure.Patterns.CQRS
{
    /// <summary>
    /// Configuration for CQRS pattern implementation
    /// Provides clean separation between command and query responsibilities
    /// </summary>
    public static class CqrsConfiguration
    {
        public static IServiceCollection AddCqrsPattern(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register core CQRS services
            services.AddScoped<ICqrsMediator, CqrsMediator>();
            services.AddSingleton<ICqrsMetrics, CqrsMetrics>();

            // Register command handlers
            RegisterCommandHandlers(services);

            // Register query handlers
            RegisterQueryHandlers(services);

            // Register validators
            RegisterValidators(services);

            // Register decorators for cross-cutting concerns
            RegisterDecorators(services, configuration);

            return services;
        }

        private static void RegisterCommandHandlers(IServiceCollection services)
        {
            var assembly = typeof(CqrsConfiguration).Assembly;
            
            // Find all command handler types
            var commandHandlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i => 
                    i.IsGenericType && 
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                     i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))))
                .ToList();

            foreach (var handlerType in commandHandlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && 
                               (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                                i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)))
                    .ToList();

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, handlerType);
                }
            }
        }

        private static void RegisterQueryHandlers(IServiceCollection services)
        {
            var assembly = typeof(CqrsConfiguration).Assembly;
            
            // Find all query handler types
            var queryHandlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i => 
                    i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                .ToList();

            foreach (var handlerType in queryHandlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && 
                               i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                    .ToList();

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, handlerType);
                }
            }
        }

        private static void RegisterValidators(IServiceCollection services)
        {
            var assembly = typeof(CqrsConfiguration).Assembly;

            // Register command validators
            var commandValidatorTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i => 
                    i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(ICommandValidator<>)))
                .ToList();

            foreach (var validatorType in commandValidatorTypes)
            {
                var interfaces = validatorType.GetInterfaces()
                    .Where(i => i.IsGenericType && 
                               i.GetGenericTypeDefinition() == typeof(ICommandValidator<>))
                    .ToList();

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, validatorType);
                }
            }

            // Register query validators
            var queryValidatorTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i => 
                    i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(IQueryValidator<>)))
                .ToList();

            foreach (var validatorType in queryValidatorTypes)
            {
                var interfaces = validatorType.GetInterfaces()
                    .Where(i => i.IsGenericType && 
                               i.GetGenericTypeDefinition() == typeof(IQueryValidator<>))
                    .ToList();

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, validatorType);
                }
            }
        }

        private static void RegisterDecorators(IServiceCollection services, IConfiguration configuration)
        {
            // Register caching decorator for queries
            if (configuration.GetValue<bool>("CQRS:EnableQueryCaching", true))
            {
                services.Decorate(typeof(IQueryHandler<,>), typeof(CachingQueryHandlerDecorator<,>));
            }

            // Register logging decorator
            if (configuration.GetValue<bool>("CQRS:EnableLogging", true))
            {
                services.Decorate(typeof(ICommandHandler<>), typeof(LoggingCommandHandlerDecorator<>));
                services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingCommandHandlerDecorator<,>));
                services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingQueryHandlerDecorator<,>));
            }

            // Register retry decorator for commands
            if (configuration.GetValue<bool>("CQRS:EnableRetry", false))
            {
                services.Decorate(typeof(ICommandHandler<>), typeof(RetryCommandHandlerDecorator<>));
                services.Decorate(typeof(ICommandHandler<,>), typeof(RetryCommandHandlerDecorator<,>));
            }

            // Register transaction decorator for commands
            if (configuration.GetValue<bool>("CQRS:EnableTransactions", true))
            {
                services.Decorate(typeof(ICommandHandler<>), typeof(TransactionCommandHandlerDecorator<>));
                services.Decorate(typeof(ICommandHandler<,>), typeof(TransactionCommandHandlerDecorator<,>));
            }
        }
    }

    /// <summary>
    /// Example command validator for CreateApplicationCommand
    /// </summary>
    public class CreateApplicationCommandValidator : ICommandValidator<CreateApplicationCommand>
    {
        public async Task<ValidationResult> ValidateAsync(CreateApplicationCommand command)
        {
            var errors = new System.Collections.Generic.List<string>();

            if (string.IsNullOrWhiteSpace(command.FileReference))
                errors.Add("File reference is required");

            if (command.PrimaryContact == null)
                errors.Add("Primary contact is required");
            else
            {
                if (string.IsNullOrWhiteSpace(command.PrimaryContact.FirstName))
                    errors.Add("Primary contact first name is required");
                
                if (string.IsNullOrWhiteSpace(command.PrimaryContact.LastName))
                    errors.Add("Primary contact last name is required");
            }

            if (command.Address == null)
                errors.Add("Address is required");
            else
            {
                if (string.IsNullOrWhiteSpace(command.Address.Street))
                    errors.Add("Street is required");
                
                if (string.IsNullOrWhiteSpace(command.Address.PostalCode))
                    errors.Add("Postal code is required");
                
                if (string.IsNullOrWhiteSpace(command.Address.City))
                    errors.Add("City is required");
            }

            // Email validation if provided
            if (!string.IsNullOrWhiteSpace(command.PrimaryContact?.Email))
            {
                if (!IsValidEmail(command.PrimaryContact.Email))
                    errors.Add("Invalid email format for primary contact");
            }

            if (errors.Any())
                return ValidationResult.Failure(errors.ToArray());

            return ValidationResult.Success();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

