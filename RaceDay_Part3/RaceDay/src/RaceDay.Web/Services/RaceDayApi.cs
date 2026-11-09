using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult
{
    public bool Success { get; init; }
    public HttpStatusCode? StatusCode { get; init; }

    /// <summary>A friendly, user-safe message (never a raw exception or stack trace).</summary>
    public string? Error { get; init; }

    /// <summary>Per-field validation messages returned by the API (field name -> messages).</summary>
    public Dictionary<string, string[]> FieldErrors { get; init; } = new();

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}

public interface IRaceDayApi
{
    Task<ApiResult<T>> GetAsync<T>(string url);
    Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null);
    Task<ApiResult> SendAsync(HttpMethod method, string url, object? body = null);
    Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file);
}

/// <summary>Thin typed wrapper over the RaceDay API. Adds the session's JWT and turns failures into friendly messages.</summary>
public class RaceDayApi : IRaceDayApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<RaceDayApi> _logger;

    public RaceDayApi(HttpClient http, IHttpContextAccessor accessor, ILogger<RaceDayApi> logger)
    {
        _http = http;
        _accessor = accessor;
        _logger = logger;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) =>
        ExecuteAsync<T>(() => new HttpRequestMessage(HttpMethod.Get, url));

    public Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null) =>
        ExecuteAsync<T>(() => BuildJsonRequest(method, url, body));

    public async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body = null) =>
        await ExecuteAsync<object>(() => BuildJsonRequest(method, url, body));

    public async Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        // Buffer the upload so the request can be built safely.
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        var bytes = buffer.ToArray();

        return await ExecuteAsync<T>(() =>
        {
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", Path.GetFileName(file.FileName));
            return new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        });
    }

    private static HttpRequestMessage BuildJsonRequest(HttpMethod method, string url, object? body)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        return request;
    }

    private async Task<ApiResult<T>> ExecuteAsync<T>(Func<HttpRequestMessage> buildRequest)
    {
        var session = _accessor.HttpContext?.Session;
        var token = session?.GetString(SessionKeys.Token);

        try
        {
            using var request = buildRequest();
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                T? data = default;
                if (!string.IsNullOrWhiteSpace(text))
                    data = JsonSerializer.Deserialize<T>(text, Json);
                return new ApiResult<T> { Success = true, StatusCode = response.StatusCode, Data = data };
            }

            // The token was rejected (expired or invalid): drop the session so the user is asked to log in again.
            if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
            {
                session?.Clear();
                return Failure<T>(response.StatusCode, "Your session has expired. Please log in again.");
            }

            return ParseFailure<T>(response.StatusCode, text);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Could not reach the RaceDay API");
            return Failure<T>(null, "We couldn't reach the RaceDay service right now. Please try again in a moment.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Unexpected response from the RaceDay API");
            return Failure<T>(null, "We received an unexpected response from the RaceDay service.");
        }
    }

    private static ApiResult<T> Failure<T>(HttpStatusCode? status, string message) =>
        new() { Success = false, StatusCode = status, Error = message };

    private static ApiResult<T> ParseFailure<T>(HttpStatusCode status, string body)
    {
        string? title = null, detail = null;
        var fieldErrors = new Dictionary<string, string[]>();

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String) title = t.GetString();
                if (doc.RootElement.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String) detail = d.GetString();
                if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in errors.EnumerateObject())
                    {
                        var messages = field.Value.ValueKind == JsonValueKind.Array
                            ? field.Value.EnumerateArray().Select(m => m.GetString() ?? string.Empty).Where(m => m.Length > 0).ToArray()
                            : Array.Empty<string>();
                        if (messages.Length > 0) fieldErrors[field.Name] = messages;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Body was not JSON - fall through to the generic messages below.
        }

        var message = status switch
        {
            HttpStatusCode.BadRequest => fieldErrors.Count > 0
                ? "Please correct the highlighted fields and try again."
                : "Some of the information you entered isn't valid. Please check it and try again.",
            HttpStatusCode.Unauthorized => title ?? "Please log in to continue.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            HttpStatusCode.Conflict => detail ?? "That action conflicts with the current state of the data.",
            HttpStatusCode.ServiceUnavailable => detail ?? "That feature is temporarily unavailable.",
            _ when (int)status >= 500 => "Something went wrong on our side. Please try again later.",
            _ => detail ?? title ?? "Something went wrong. Please try again."
        };

        return new ApiResult<T>
        {
            Success = false,
            StatusCode = status,
            Error = message,
            FieldErrors = fieldErrors
        };
    }
}
