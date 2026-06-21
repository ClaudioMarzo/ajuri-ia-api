using AjuriIA.API.Services;
using FluentAssertions;

namespace AjuriIA.Tests.Unit.Services;

public class ProfileServiceTests
{
    private readonly ProfileService _sut = new();

    [Fact(DisplayName = "Given valid profileId, When GetById, Then returns profile with systemPrompt")]
    public void Given_ValidProfileId_When_GetById_Should_ReturnProfileWithSystemPrompt()
    {
        // Given
        const string profileId = "professor";

        // When
        var result = _sut.GetById(profileId);

        // Then
        result!.SystemPrompt.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Given valid profileId, When GetById, Then returns profile with correct id")]
    public void Given_ValidProfileId_When_GetById_Should_ReturnProfileWithCorrectId()
    {
        // Given
        const string profileId = "produtor";

        // When
        var result = _sut.GetById(profileId);

        // Then
        result!.Id.Should().Be("produtor");
    }

    [Fact(DisplayName = "Given nonexistent profileId, When GetById, Then returns null")]
    public void Given_NonexistentProfileId_When_GetById_Should_ReturnNull()
    {
        // Given
        const string profileId = "perfil-invalido";

        // When
        var result = _sut.GetById(profileId);

        // Then
        result.Should().BeNull();
    }

    [Fact(DisplayName = "Given profiles loaded, When GetAll, Then returns exactly 6 profiles")]
    public void Given_ProfilesLoaded_When_GetAll_Should_ReturnSixProfiles()
    {
        // When
        var result = _sut.GetAll();

        // Then
        result.Should().HaveCount(6);
    }

    [Fact(DisplayName = "Given profiles loaded, When GetAll, Then all profiles have non-empty llm field")]
    public void Given_ProfilesLoaded_When_GetAll_Should_HaveNonEmptyLlmField()
    {
        // When
        var result = _sut.GetAll();

        // Then
        result.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Llm));
    }
}
