using System.Net;
using System.Text;
using System.Text.Json;

namespace LlmProxy.Tests.Fixtures;

/// <summary>
/// Вспомогательный класс для создания моков HTTP-ответов
/// </summary>
public static class MockHttpMessageHandler
{
    public static HttpMessageHandler CreateForJsonResponse<T>(T response, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandlerImpl
        {
            ResponseContent = new StringContent(
                JsonSerializer.Serialize(response),
                Encoding.UTF8,
                "application/json"
            ),
            StatusCode = statusCode
        };
        return handler;
    }

    public static HttpMessageHandler CreateForSseResponse(IEnumerable<string> sseLines, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var content = string.Join("\n\n", sseLines);
        var handler = new MockHttpMessageHandlerImpl
        {
            ResponseContent = new StringContent(content, Encoding.UTF8, "text/event-stream"),
            StatusCode = statusCode
        };
        return handler;
    }

    public static HttpMessageHandler CreateForError(HttpStatusCode statusCode, string? errorMessage = null)
    {
        var handler = new MockHttpMessageHandlerImpl
        {
            ResponseContent = errorMessage != null 
                ? new StringContent(JsonSerializer.Serialize(new { error = errorMessage }), Encoding.UTF8, "application/json")
                : new StringContent("", Encoding.UTF8, "text/plain"),
            StatusCode = statusCode
        };
        return handler;
    }
}

internal class MockHttpMessageHandlerImpl : HttpMessageHandler
{
    public HttpContent? ResponseContent { get; set; }
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return new HttpResponseMessage
        {
            StatusCode = StatusCode,
            Content = ResponseContent,
            RequestMessage = request
        };
    }
}