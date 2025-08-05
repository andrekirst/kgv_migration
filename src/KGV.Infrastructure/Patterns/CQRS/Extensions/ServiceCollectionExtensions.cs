using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for decorator pattern registration
    /// Provides fluent API for registering decorators in the DI container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Decorates a service registration with a decorator implementation
        /// </summary>
        /// <typeparam name="TInterface">The service interface type</typeparam>
        /// <typeparam name="TDecorator">The decorator implementation type</typeparam>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for fluent chaining</returns>
        public static IServiceCollection Decorate<TInterface, TDecorator>(this IServiceCollection services)
            where TInterface : class
            where TDecorator : class, TInterface
        {
            return services.Decorate(typeof(TInterface), typeof(TDecorator));
        }

        /// <summary>
        /// Decorates a service registration with a decorator implementation
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="interfaceType">The service interface type</param>
        /// <param name="decoratorType">The decorator implementation type</param>
        /// <returns>The service collection for fluent chaining</returns>
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

        /// <summary>
        /// Creates an instance from a service descriptor
        /// </summary>
        /// <param name="services">The service provider</param>
        /// <param name="descriptor">The service descriptor</param>
        /// <returns>The created instance</returns>
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