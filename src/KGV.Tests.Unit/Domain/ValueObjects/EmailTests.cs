using FluentAssertions;
using KGV.Domain.ValueObjects;
using KGV.Tests.Unit.Shared;
using Xunit;

namespace KGV.Tests.Unit.Domain.ValueObjects;

/// <summary>
/// Unit Tests für das Email Value Object.
/// Testet Validierung, Gleichheit und Verhalten der E-Mail-Implementierung.
/// </summary>
public class EmailTests : TestBase
{
    [Theory]
    [InlineData("max.mustermann@beispiel.de")]
    [InlineData("test@test.com")]
    [InlineData("user123@domain-name.org")]
    [InlineData("firstname.lastname@company.co.uk")]
    [InlineData("test.email+tag@example.com")]
    public void Email_Should_Accept_Valid_Email_Addresses(string validEmail)
    {
        // Act
        var email = new Email(validEmail);

        // Assert
        email.Should().NotBeNull("weil eine gültige E-Mail akzeptiert werden sollte");
        email.Value.Should().Be(validEmail, "weil die E-Mail-Adresse korrekt gespeichert werden sollte");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("invalid-email")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user..double.dot@domain.com")]
    [InlineData("user@domain..com")]
    public void Email_Should_Reject_Invalid_Email_Addresses(string invalidEmail)
    {
        // Act & Assert
        var act = () => new Email(invalidEmail);

        act.Should().Throw<ArgumentException>("weil ungültige E-Mail-Adressen abgelehnt werden sollten")
            .WithMessage("*email*", "weil die Fehlermeldung 'email' erwähnen sollte");
    }

    [Fact]
    public void Email_Should_Support_German_Domain_Names()
    {
        // Arrange
        var germanEmails = new[]
        {
            "user@münchen.de",
            "test@straße.org",
            "admin@büro.com"
        };

        // Act & Assert
        foreach (var germanEmail in germanEmails)
        {
            var act = () => new Email(germanEmail);
            act.Should().NotThrow($"weil deutsche Domains wie '{germanEmail}' unterstützt werden sollten");
        }
    }

    [Fact]
    public void Email_Should_Be_Case_Insensitive()
    {
        // Arrange
        var email1 = new Email("Max.Mustermann@Beispiel.DE");
        var email2 = new Email("max.mustermann@beispiel.de");

        // Act & Assert
        email1.Should().Be(email2, "weil E-Mail-Adressen case-insensitive sein sollten");
        email1.GetHashCode().Should().Be(email2.GetHashCode(), "weil Hash-Codes für case-insensitive E-Mails gleich sein sollten");
    }

    [Fact]
    public void Email_Should_Implement_Value_Object_Equality()
    {
        // Arrange
        var email1 = new Email("test@example.com");
        var email2 = new Email("test@example.com");
        var email3 = new Email("different@example.com");

        // Act & Assert
        email1.Should().Be(email2, "weil gleiche E-Mail-Adressen als gleich gelten sollten");
        email1.Should().NotBe(email3, "weil verschiedene E-Mail-Adressen als ungleich gelten sollten");
        
        // Operator-Tests
        (email1 == email2).Should().BeTrue("weil der == Operator für gleiche E-Mails true zurückgeben sollte");
        (email1 != email3).Should().BeTrue("weil der != Operator für verschiedene E-Mails true zurückgeben sollte");
    }

    [Fact]
    public void Email_Should_Return_Null_For_Null_Comparison()
    {
        // Arrange
        var email = new Email("test@example.com");

        // Act & Assert
        email.Should().NotBe(null, "weil eine gültige E-Mail nicht null sein sollte");
        email.Equals(null).Should().BeFalse("weil Equals(null) false zurückgeben sollte");
    }

    [Fact]
    public void Email_ToString_Should_Return_Email_Value()
    {
        // Arrange
        var emailAddress = "max.mustermann@beispiel.de";
        var email = new Email(emailAddress);

        // Act
        var stringRepresentation = email.ToString();

        // Assert
        stringRepresentation.Should().Be(emailAddress, "weil ToString() die E-Mail-Adresse zurückgeben sollte");
    }

    [Fact]
    public void Email_Should_Extract_Domain_Correctly()
    {
        // Arrange
        var email = new Email("user@example.com");

        // Act
        var domain = email.Domain;

        // Assert
        domain.Should().Be("example.com", "weil die Domain korrekt extrahiert werden sollte");
    }

    [Fact]
    public void Email_Should_Extract_LocalPart_Correctly()
    {
        // Arrange
        var email = new Email("max.mustermann@beispiel.de");

        // Act
        var localPart = email.LocalPart;

        // Assert
        localPart.Should().Be("max.mustermann", "weil der lokale Teil korrekt extrahiert werden sollte");
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("user@gmail.com", false)]
    [InlineData("test@company.org", false)]
    public void Email_IsFromDomain_Should_Check_Domain_Correctly(string emailAddress, bool expectedResult)
    {
        // Arrange
        var email = new Email(emailAddress);

        // Act
        var isFromDomain = email.IsFromDomain("example.com");

        // Assert
        isFromDomain.Should().Be(expectedResult, 
            $"weil die Domain-Prüfung für '{emailAddress}' das erwartete Ergebnis liefern sollte");
    }

    [Fact]
    public void Email_Should_Handle_International_Characters()
    {
        // Arrange
        var internationalEmails = new[]
        {
            "пользователь@пример.рф", // Kyrillisch
            "用户@例子.中国", // Chinesisch
            "müller@straße.de" // Deutsche Umlaute
        };

        // Act & Assert
        foreach (var email in internationalEmails)
        {
            var act = () => new Email(email);
            act.Should().NotThrow($"weil internationale E-Mail-Adressen wie '{email}' unterstützt werden sollten");
        }
    }

    [Theory]
    [GermanAutoData]
    public void Email_Should_Work_With_AutoFixture_Generated_Domains(string localPart)
    {
        // Arrange
        localPart = string.IsNullOrWhiteSpace(localPart) ? "test" : localPart.Replace("@", "").Replace(" ", "");
        var emailAddress = $"{localPart}@beispiel.de";

        // Act
        var email = new Email(emailAddress);

        // Assert
        email.Should().NotBeNull("weil eine gültige E-Mail erstellt werden sollte");
        email.Value.Should().Be(emailAddress, "weil die E-Mail korrekt gespeichert werden sollte");
        email.Domain.Should().Be("beispiel.de", "weil die Domain korrekt sein sollte");
    }

    [Fact]
    public void Email_Should_Implement_IComparable_For_Sorting()
    {
        // Arrange
        var emails = new[]
        {
            new Email("z@example.com"),
            new Email("a@example.com"),
            new Email("m@example.com")
        };

        // Act
        var sortedEmails = emails.OrderBy(e => e).ToArray();

        // Assert
        sortedEmails[0].Value.Should().Be("a@example.com", "weil die E-Mails alphabetisch sortiert werden sollten");
        sortedEmails[1].Value.Should().Be("m@example.com");
        sortedEmails[2].Value.Should().Be("z@example.com");
    }
}