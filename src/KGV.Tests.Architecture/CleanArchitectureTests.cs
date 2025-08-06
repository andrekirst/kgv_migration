using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace KGV.Tests.Architecture;

/// <summary>
/// Architecture Tests für Clean Architecture Regeln.
/// Stellt sicher, dass die Abhängigkeitsregeln der Clean Architecture eingehalten werden.
/// </summary>
public class CleanArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(KGV.Domain.Common.BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(KGV.Application.Common.Models.Result).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(KGV.Infrastructure.Data.KgvDbContext).Assembly;
    private static readonly Assembly ApiAssembly = typeof(KGV.API.Program).Assembly;

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Application()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("KGV.Application")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil die Domain-Schicht keine Abhängigkeiten zur Application-Schicht haben sollte. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Infrastructure()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("KGV.Infrastructure")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil die Domain-Schicht keine Abhängigkeiten zur Infrastructure-Schicht haben sollte. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_API()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("KGV.API")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil die Domain-Schicht keine Abhängigkeiten zur API-Schicht haben sollte. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Application_Should_Not_Have_Dependency_On_Infrastructure()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn("KGV.Infrastructure")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil die Application-Schicht keine direkten Abhängigkeiten zur Infrastructure-Schicht haben sollte. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Application_Should_Not_Have_Dependency_On_API()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn("KGV.API")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil die Application-Schicht keine Abhängigkeiten zur API-Schicht haben sollte. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Controllers_Should_Have_Controller_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("KGV.API.Controllers")
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil alle Controller-Klassen das Suffix 'Controller' haben sollten. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Entities_Should_Be_In_Domain_Entities_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(KGV.Domain.Common.BaseEntity))
            .Should()
            .ResideInNamespace("KGV.Domain.Entities")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil alle Entitäten im Domain.Entities Namespace sein sollten. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Value_Objects_Should_Be_In_Domain_ValueObjects_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(KGV.Domain.Common.ValueObject))
            .Should()
            .ResideInNamespace("KGV.Domain.ValueObjects")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil alle Value Objects im Domain.ValueObjects Namespace sein sollten. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Command_Handlers_Should_Have_Handler_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceMatching("KGV.Application.Features.*.Commands.*")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Handler")
            .Or()
            .HaveNameEndingWith("Command")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil Command Handler das Suffix 'Handler' oder 'Command' haben sollten. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Query_Handlers_Should_Have_Handler_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceMatching("KGV.Application.Features.*.Queries.*")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Handler")
            .Or()
            .HaveNameEndingWith("Query")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil Query Handler das Suffix 'Handler' oder 'Query' haben sollten. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Validators_Should_Have_Validator_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceMatching("*.Validators")
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil alle Validator-Klassen das Suffix 'Validator' haben sollten. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void DTOs_Should_Have_Dto_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("KGV.Application.DTOs")
            .Should()
            .HaveNameEndingWith("Dto")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil alle DTO-Klassen das Suffix 'Dto' haben sollten. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Repositories_Should_Have_Repository_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespaceMatching("*.Repositories.*")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil alle Repository-Klassen das Suffix 'Repository' haben sollten. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Domain_Should_Not_Reference_External_Libraries_Except_System()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Newtonsoft.Json",
                "AutoMapper",
                "MediatR")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil die Domain-Schicht nur System-Bibliotheken referenzieren sollte. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Infrastructure_Should_Implement_Application_Interfaces()
    {
        // Arrange
        var applicationInterfaces = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .And()
            .ResideInNamespaceMatching("KGV.Application.Common.Interfaces")
            .GetTypes()
            .ToList();

        // Act & Assert
        foreach (var interfaceType in applicationInterfaces)
        {
            var implementationExists = Types.InAssembly(InfrastructureAssembly)
                .That()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .GetTypes()
                .Any(t => interfaceType.IsAssignableFrom(t));

            implementationExists.Should().BeTrue(
                $"weil das Interface {interfaceType.Name} in der Infrastructure-Schicht implementiert werden sollte");
        }
    }

    [Fact]
    public void Application_Handlers_Should_Be_Internal_Or_Public()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .BePublic()
            .Or()
            .BeInternal()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil Handler public oder internal sein sollten für Dependency Injection. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Domain_Entities_Should_Not_Have_Public_Setters_For_Id()
    {
        // Arrange
        var entities = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(KGV.Domain.Common.BaseEntity))
            .GetTypes();

        // Act & Assert
        foreach (var entity in entities)
        {
            var idProperty = entity.GetProperty("Id");
            if (idProperty?.SetMethod != null)
            {
                idProperty.SetMethod.IsPublic.Should().BeFalse(
                    $"weil die Entity {entity.Name} keine public Id-Setter haben sollte für Domain-Kapselung");
            }
        }
    }

    [Fact]
    public void Controllers_Should_Not_Have_Business_Logic()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .Should()
            .NotHaveDependencyOnAny(
                "KGV.Domain.Entities",
                "KGV.Infrastructure.Repositories")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "weil Controller keine direkten Abhängigkeiten zu Domain Entities oder Repositories haben sollten. " +
            "Sie sollten nur Application Services verwenden. " +
            $"Verletzende Typen: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void German_Naming_Conventions_Should_Be_Followed_In_Domain()
    {
        // Arrange
        var germanTerms = new[] { "Antrag", "Bezirk", "Parzelle", "Verlauf" };

        // Act
        var domainTypes = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("KGV.Domain.Entities")
            .GetTypes();

        // Assert
        domainTypes.Should().NotBeEmpty("weil Domain-Entitäten existieren sollten");
        
        var hasGermanNames = domainTypes.Any(t => 
            germanTerms.Any(term => t.Name.Contains(term)));

        hasGermanNames.Should().BeTrue(
            "weil die Domain-Entitäten deutsche Fachbegriffe verwenden sollten für bessere Verständlichkeit");
    }
}