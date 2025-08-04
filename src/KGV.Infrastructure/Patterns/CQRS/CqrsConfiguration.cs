using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using KGV.Infrastructure.Patterns.CQRS.Commands;
using KGV.Infrastructure.Patterns.CQRS.Queries;

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

// Note: Decorator implementations would typically be in separate files
// They are included here as placeholders to show the pattern

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension method for decorator pattern registration
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Decorate<TInterface, TDecorator>(this IServiceCollection services)
            where TInterface : class
            where TDecorator : class, TInterface
        {
            return services.Decorate(typeof(TInterface), typeof(TDecorator));
        }

        public static IServiceCollection Decorate(this IServiceCollection services, Type interfaceType, Type decoratorType)
        {
            var wrappedDescriptor = services.FirstOrDefault(s => s.ServiceType == interfaceType);

            if (wrappedDescriptor == null)
                return services;

            var objectFactory = ActivatorUtilities.CreateFactory(decoratorType, new[] { interfaceType });

            services.Replace(ServiceDescriptor.Describe(
                interfaceType,
                s => (object)objectFactory(s, new[] { s.CreateInstance(wrappedDescriptor) }),
                wrappedDescriptor.Lifetime)
            );

            return services;
        }

        private static object CreateInstance(this IServiceProvider services, ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance;

            if (descriptor.ImplementationFactory != null)
                return descriptor.ImplementationFactory(services);

            return ActivatorUtilities.GetServiceOrCreateInstance(services, descriptor.ImplementationType);
        }
    }
}

namespace KGV.Infrastructure.Patterns.CQRS.Decorators
{
    /// <summary>
    /// Placeholder decorator implementations
    /// In a real implementation, these would be in separate files with full implementation
    /// </summary>
    
    public class CachingQueryHandlerDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly IQueryHandler<TQuery, TResult> _inner;

        public CachingQueryHandlerDecorator(IQueryHandler<TQuery, TResult> inner)
        {
            _inner = inner;
        }

        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            // TODO: Implement caching logic
            return await _inner.HandleAsync(query, cancellationToken);
        }
    }

    public class LoggingCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> _inner;

        public LoggingCommandHandlerDecorator(ICommandHandler<TCommand> inner)
        {
            _inner = inner;
        }

        public async Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            // TODO: Implement logging logic
            await _inner.HandleAsync(command, cancellationToken);
        }
    }

    public class LoggingCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        private readonly ICommandHandler<TCommand, TResult> _inner;

        public LoggingCommandHandlerDecorator(ICommandHandler<TCommand, TResult> inner)
        {
            _inner = inner;
        }

        public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            // TODO: Implement logging logic
            return await _inner.HandleAsync(command, cancellationToken);
        }
    }

    public class LoggingQueryHandlerDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly IQueryHandler<TQuery, TResult> _inner;

        public LoggingQueryHandlerDecorator(IQueryHandler<TQuery, TResult> inner)
        {
            _inner = inner;
        }

        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            // TODO: Implement logging logic
            return await _inner.HandleAsync(query, cancellationToken);
        }
    }

    public class RetryCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> _inner;

        public RetryCommandHandlerDecorator(ICommandHandler<TCommand> inner)
        {
            _inner = inner;
        }

        public async Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            // TODO: Implement retry logic
            await _inner.HandleAsync(command, cancellationToken);
        }
    }

    public class RetryCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        private readonly ICommandHandler<TCommand, TResult> _inner;

        public RetryCommandHandlerDecorator(ICommandHandler<TCommand, TResult> inner)
        {
            _inner = inner;
        }

        public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            // TODO: Implement retry logic
            return await _inner.HandleAsync(command, cancellationToken);
        }
    }

    public class TransactionCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> _inner;

        public TransactionCommandHandlerDecorator(ICommandHandler<TCommand> inner)
        {
            _inner = inner;
        }

        public async Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            // TODO: Implement transaction logic
            await _inner.HandleAsync(command, cancellationToken);
        }
    }

    public class TransactionCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        private readonly ICommandHandler<TCommand, TResult> _inner;

        public TransactionCommandHandlerDecorator(ICommandHandler<TCommand, TResult> inner)
        {
            _inner = inner;
        }

        public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            // TODO: Implement transaction logic
            return await _inner.HandleAsync(command, cancellationToken);
        }
    }
}