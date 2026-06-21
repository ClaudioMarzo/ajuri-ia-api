using System.Text.Json;
using AjuriIA.API.Models;

namespace AjuriIA.API.Services;

public class ProfileService
{
    private readonly IReadOnlyList<Profile> _profiles;

    public ProfileService()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Config", "profiles.json");
        var json = File.ReadAllText(path);
        _profiles = JsonSerializer.Deserialize<List<Profile>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? [];
    }

    public Profile? GetById(string id) =>
        _profiles.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Profile> GetAll() => _profiles;
}
