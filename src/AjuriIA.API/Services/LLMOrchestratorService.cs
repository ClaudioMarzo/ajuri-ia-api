using System.Runtime.CompilerServices;
using AjuriIA.API.Models;

namespace AjuriIA.API.Services;

public class LLMOrchestratorService(IEnumerable<ILLMService> services)
{
    public string? LastUsedLlm { get; private set; }

    public async IAsyncEnumerable<string> StreamAsync(
        Profile profile,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var ordered = GetServicesInOrder(profile.Llm);

        foreach (var service in ordered)
        {
            IAsyncEnumerator<string>? enumerator = null;
            bool failedOnFirst = false;

            try
            {
                enumerator = service.StreamAsync(profile.SystemPrompt, userMessage, ct)
                                    .GetAsyncEnumerator(ct);
                var hasFirst = await enumerator.MoveNextAsync();
                if (!hasFirst)
                {
                    await enumerator.DisposeAsync();
                    continue;
                }
                LastUsedLlm = service.Name;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failedOnFirst = true;
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
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(continuationException).Throw();

            yield break;
        }

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
