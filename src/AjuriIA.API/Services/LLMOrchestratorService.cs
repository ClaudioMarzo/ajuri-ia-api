using System.Diagnostics;
using System.Runtime.CompilerServices;
using AjuriIA.API.Models;
using Microsoft.Extensions.Logging;

namespace AjuriIA.API.Services;

public class LLMOrchestratorService(
    IEnumerable<ILLMService> services,
    ILogger<LLMOrchestratorService> logger)
{
    public string? LastUsedLlm { get; private set; }

    public async IAsyncEnumerable<string> StreamAsync(
        Profile profile,
        string userMessage,
        string? preferredLlm = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var primary = string.IsNullOrWhiteSpace(preferredLlm) ? profile.Llm : preferredLlm;
        var ordered = GetServicesInOrder(primary);
        bool hadFailure = false;

        foreach (var service in ordered)
        {
            if (hadFailure)
                logger.LogInformation("[LLM] Fallback para {LlmName}", service.Name);

            logger.LogInformation(
                "[LLM] Tentando {LlmName} para perfil {ProfileId}",
                service.Name, profile.Id);

            IAsyncEnumerator<string>? enumerator = null;
            bool failedOnFirst = false;

            try
            {
                enumerator = service.StreamAsync(profile.SystemPrompt, userMessage, ct)
                                    .GetAsyncEnumerator(ct);

                var sw = Stopwatch.StartNew();
                var hasFirst = await enumerator.MoveNextAsync();
                sw.Stop();

                if (!hasFirst)
                {
                    await enumerator.DisposeAsync();
                    continue;
                }

                LastUsedLlm = service.Name;
                logger.LogInformation(
                    "[LLM] Primeiro chunk em {ElapsedMs}ms — {LlmName} respondeu",
                    sw.ElapsedMilliseconds, service.Name);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var httpStatus = ex is HttpRequestException httpEx
                    ? (int?)httpEx.StatusCode
                    : null;

                logger.LogWarning(
                    "[LLM] {LlmName} falhou — {ExceptionType}: {Message} HttpStatus={HttpStatus}",
                    service.Name,
                    ex.GetType().Name,
                    ex.Message,
                    httpStatus.HasValue ? httpStatus.Value.ToString() : "n/a");

                failedOnFirst = true;
                hadFailure = true;

                if (enumerator is not null)
                    await enumerator.DisposeAsync();
            }

            if (failedOnFirst) continue;

            yield return enumerator!.Current;

            Exception? continuationException = null;
            try
            {
                bool hasNext;
                while (true)
                {
                    try
                    {
                        hasNext = await enumerator!.MoveNextAsync();
                    }
                    catch (OperationCanceledException ex)
                    {
                        continuationException = ex;
                        break;
                    }
                    if (!hasNext) break;
                    yield return enumerator!.Current;
                }
            }
            finally
            {
                await enumerator!.DisposeAsync();
            }

            if (continuationException is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo
                    .Capture(continuationException).Throw();

            yield break;
        }

        logger.LogError(
            "[LLM] Todos os LLMs falharam para {ProfileId} — tentados: {LlmNames}",
            profile.Id,
            string.Join(", ", GetServicesInOrder(primary).Select(s => s.Name)));

        throw new AllLLMsUnavailableException(
            "Serviço de IA temporariamente indisponível. Tente novamente em instantes.");
    }

    private IEnumerable<ILLMService> GetServicesInOrder(string primaryLlmName)
    {
        var primary = services.FirstOrDefault(s =>
            s.Name.Equals(primaryLlmName, StringComparison.OrdinalIgnoreCase));
        var rest = services.Where(s =>
            !s.Name.Equals(primaryLlmName, StringComparison.OrdinalIgnoreCase));
        return primary is not null ? rest.Prepend(primary) : services;
    }
}
