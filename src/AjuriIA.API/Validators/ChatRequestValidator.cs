using AjuriIA.API.Models;
using AjuriIA.API.Services;
using FluentValidation;

namespace AjuriIA.API.Validators;

public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator(ProfileService profileService, GeminiOptions geminiOptions)
    {
        RuleFor(x => x.ProfileId)
            .NotEmpty().WithMessage("O campo profileId é obrigatório.")
            .Must(id => profileService.GetById(id) is not null)
            .WithMessage(x => $"Perfil '{x.ProfileId}' não encontrado.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("O campo message é obrigatório.")
            .MinimumLength(3).WithMessage("A mensagem deve ter no mínimo 3 caracteres.")
            .MaximumLength(2000).WithMessage("A mensagem deve ter no máximo 2000 caracteres.");

        // model é opcional; se informado, precisa estar na allowlist do servidor.
        RuleFor(x => x.Model)
            .Must(m => string.IsNullOrWhiteSpace(m) || geminiOptions.IsAllowed(m))
            .WithMessage(x => $"Modelo '{x.Model}' não suportado. Consulte GET /api/models.");
    }
}
