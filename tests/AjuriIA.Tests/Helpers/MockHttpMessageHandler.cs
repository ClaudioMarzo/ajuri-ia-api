namespace AjuriIA.Tests.Helpers;

public class MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) => handler(request);
}
