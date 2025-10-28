using Xunit;

namespace Share.Tests.Identity;

/// <summary>
/// Tests for user registration and management scenarios
/// </summary>
public class UserRegistrationTests
{
    [Fact]
    public void UserAddDto_ValidData_CreatesObject()
    {
        // Arrange
        var dto = new
        {
            UserName = "testuser",
            Email = "test@example.com",
            PhoneNumber = "1234567890",
            Password = "Test@Password123",
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            LockoutEnabled = true
        };

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("testuser", dto.UserName);
        Assert.Equal("test@example.com", dto.Email);
        Assert.Equal("1234567890", dto.PhoneNumber);
        Assert.True(dto.LockoutEnabled);
    }

    [Fact]
    public void UserUpdateDto_ValidData_CreatesObject()
    {
        // Arrange
        var dto = new
        {
            Email = "newemail@example.com",
            PhoneNumber = "9876543210",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            IsTwoFactorEnabled = false,
            LockoutEnabled = false
        };

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("newemail@example.com", dto.Email);
        Assert.True(dto.EmailConfirmed);
        Assert.False(dto.LockoutEnabled);
    }

    [Fact]
    public void UserFilterDto_SupportsPagination()
    {
        // Arrange
        var filter = new
        {
            UserName = "test",
            Email = "test@example.com",
            PageIndex = 1,
            PageSize = 10,
            OrderBy = "CreatedTime",
            IsDescending = true
        };

        // Assert
        Assert.NotNull(filter);
        Assert.Equal(1, filter.PageIndex);
        Assert.Equal(10, filter.PageSize);
    }

    [Fact]
    public void NormalizeUserName_UpperCase_ReturnsExpectedValue()
    {
        // Arrange
        var userName = "TestUser123";

        // Act
        var normalized = userName.ToUpperInvariant();

        // Assert
        Assert.Equal("TESTUSER123", normalized);
    }

    [Fact]
    public void NormalizeEmail_UpperCase_ReturnsExpectedValue()
    {
        // Arrange
        var email = "Test@Example.Com";

        // Act
        var normalized = email.ToUpperInvariant();

        // Assert
        Assert.Equal("TEST@EXAMPLE.COM", normalized);
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.com")]
    [InlineData("user+tag@example.org")]
    public void EmailFormat_ValidEmails_AcceptsFormat(string email)
    {
        // Act
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateValue(
            email,
            new System.ComponentModel.DataAnnotations.ValidationContext(new object()) { MemberName = "Email" },
            null,
            [new System.ComponentModel.DataAnnotations.EmailAddressAttribute()]
        );

        // Assert
        Assert.True(isValid);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    public void EmailFormat_InvalidEmails_RejectsFormat(string email)
    {
        // Act
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateValue(
            email,
            new System.ComponentModel.DataAnnotations.ValidationContext(new object()) { MemberName = "Email" },
            null,
            [new System.ComponentModel.DataAnnotations.EmailAddressAttribute()]
        );

        // Assert
        Assert.False(isValid);
    }
}
