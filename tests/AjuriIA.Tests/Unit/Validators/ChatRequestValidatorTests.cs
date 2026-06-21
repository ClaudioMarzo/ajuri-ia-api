using AjuriIA.API.Models;
using AjuriIA.API.Services;
using AjuriIA.API.Validators;
using FluentAssertions;

namespace AjuriIA.Tests.Unit.Validators;

public class ChatRequestValidatorTests
{
    private readonly ChatRequestValidator _sut = new(new ProfileService());

    [Fact(DisplayName = "Given empty profileId, When validate, Then returns validation error")]
    public async Task Given_EmptyProfileId_When_Validate_Should_ReturnValidationError()
    {
        // Given
        var request = new ChatRequest { ProfileId = "", Message = "mensagem válida com mais de 3 chars" };

        // When
        var result = await _sut.ValidateAsync(request);

        // Then
        result.IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "Given nonexistent profileId, When validate, Then returns validation error")]
    public async Task Given_NonexistentProfileId_When_Validate_Should_ReturnValidationError()
    {
        // Given
        var request = new ChatRequest { ProfileId = "perfil-que-nao-existe", Message = "mensagem válida" };

        // When
        var result = await _sut.ValidateAsync(request);

        // Then
        result.IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "Given message with 2 characters, When validate, Then returns validation error")]
    public async Task Given_MessageWithTwoChars_When_Validate_Should_ReturnValidationError()
    {
        // Given
        var request = new ChatRequest { ProfileId = "professor", Message = "ab" };

        // When
        var result = await _sut.ValidateAsync(request);

        // Then
        result.IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "Given message with 2001 characters, When validate, Then returns validation error")]
    public async Task Given_MessageWith2001Chars_When_Validate_Should_ReturnValidationError()
    {
        // Given
        var request = new ChatRequest { ProfileId = "professor", Message = new string('a', 2001) };

        // When
        var result = await _sut.ValidateAsync(request);

        // Then
        result.IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "Given valid request, When validate, Then returns no errors")]
    public async Task Given_ValidRequest_When_Validate_Should_ReturnNoErrors()
    {
        // Given
        var request = new ChatRequest { ProfileId = "professor", Message = "Crie uma aula sobre guaraná para o 5º ano" };

        // When
        var result = await _sut.ValidateAsync(request);

        // Then
        result.IsValid.Should().BeTrue();
    }
}
