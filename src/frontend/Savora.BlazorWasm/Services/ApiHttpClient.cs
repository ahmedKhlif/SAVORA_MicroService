using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.JSInterop;
using Savora.Shared.DTOs.Common;

namespace Savora.BlazorWasm.Services;

public class ApiHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly IConfiguration _configuration;
    private readonly IJSRuntime _jsRuntime;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiHttpClient(HttpClient httpClient, ILocalStorageService localStorage, IConfiguration configuration, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _configuration = configuration;
        _jsRuntime = jsRuntime;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string service, string endpoint)
    {
        try
        {
            var baseUrl = GetServiceUrl(service);
            var fullUrl = $"{baseUrl}{endpoint}";
            
            await _jsRuntime.InvokeVoidAsync("console.log", $"[ApiHttpClient] GET {fullUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
            await SetAuthHeader(request);
            
            var response = await _httpClient.SendAsync(request);
            return await HandleResponse<T>(response, fullUrl);
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] Exception:", new { 
                service, 
                endpoint, 
                message = ex.Message,
                stackTrace = ex.StackTrace
            });
            return ApiResponse<T>.FailureResponse($"Request failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string service, string endpoint, object? data = null)
    {
        try
        {
            var baseUrl = GetServiceUrl(service);
            var fullUrl = $"{baseUrl}{endpoint}";
            
            await _jsRuntime.InvokeVoidAsync("console.log", $"[ApiHttpClient] POST {fullUrl}", data);
            
            var request = new HttpRequestMessage(HttpMethod.Post, fullUrl)
            {
                Content = data != null ? JsonContent.Create(data, options: _jsonOptions) : null
            };
            await SetAuthHeader(request);
            
            var response = await _httpClient.SendAsync(request);
            return await HandleResponse<T>(response, fullUrl);
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] Exception:", new { 
                service, 
                endpoint, 
                message = ex.Message 
            });
            return ApiResponse<T>.FailureResponse($"Request failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> PutAsync(string service, string endpoint, object? data = null)
    {
        try
        {
            var baseUrl = GetServiceUrl(service);
            var fullUrl = $"{baseUrl}{endpoint}";
            
            await _jsRuntime.InvokeVoidAsync("console.log", $"[ApiHttpClient] PUT {fullUrl}", data);
            
            var request = new HttpRequestMessage(HttpMethod.Put, fullUrl)
            {
                Content = data != null ? JsonContent.Create(data, options: _jsonOptions) : null
            };
            await SetAuthHeader(request);
            
            var response = await _httpClient.SendAsync(request);
            return await HandleResponse(response, fullUrl);
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] Exception:", new { 
                service, 
                endpoint, 
                message = ex.Message 
            });
            return ApiResponse.FailureResponse($"Request failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string service, string endpoint, object? data = null)
    {
        try
        {
            var baseUrl = GetServiceUrl(service);
            var fullUrl = $"{baseUrl}{endpoint}";
            
            await _jsRuntime.InvokeVoidAsync("console.log", $"[ApiHttpClient] PUT {fullUrl}", data);
            
            var request = new HttpRequestMessage(HttpMethod.Put, fullUrl)
            {
                Content = data != null ? JsonContent.Create(data, options: _jsonOptions) : null
            };
            await SetAuthHeader(request);
            
            var response = await _httpClient.SendAsync(request);
            return await HandleResponse<T>(response, fullUrl);
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] Exception:", new { 
                service, 
                endpoint, 
                message = ex.Message 
            });
            return ApiResponse<T>.FailureResponse($"Request failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> DeleteAsync(string service, string endpoint)
    {
        try
        {
            var baseUrl = GetServiceUrl(service);
            var fullUrl = $"{baseUrl}{endpoint}";
            
            await _jsRuntime.InvokeVoidAsync("console.log", $"[ApiHttpClient] DELETE {fullUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Delete, fullUrl);
            await SetAuthHeader(request);
            
            var response = await _httpClient.SendAsync(request);
            return await HandleResponse(response, fullUrl);
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] Exception:", new { 
                service, 
                endpoint, 
                message = ex.Message 
            });
            return ApiResponse.FailureResponse($"Request failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<T>> DeleteAsync<T>(string service, string endpoint)
    {
        try
        {
            var baseUrl = GetServiceUrl(service);
            var fullUrl = $"{baseUrl}{endpoint}";
            
            await _jsRuntime.InvokeVoidAsync("console.log", $"[ApiHttpClient] DELETE {fullUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Delete, fullUrl);
            await SetAuthHeader(request);
            
            var response = await _httpClient.SendAsync(request);
            return await HandleResponse<T>(response, fullUrl);
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] Exception:", new { 
                service, 
                endpoint, 
                message = ex.Message 
            });
            return ApiResponse<T>.FailureResponse($"Request failed: {ex.Message}");
        }
    }

    public async Task<byte[]?> GetBytesAsync(string service, string endpoint)
    {
        try
        {
            var baseUrl = GetServiceUrl(service);
            var fullUrl = $"{baseUrl}{endpoint}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
            await SetAuthHeader(request);
            
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiResponse<T>> PostMultipartAsync<T>(string service, string endpoint, HttpContent content)
    {
        try
        {
            var baseUrl = GetServiceUrl(service);
            var fullUrl = $"{baseUrl}{endpoint}";
            
            await _jsRuntime.InvokeVoidAsync("console.log", $"[ApiHttpClient] POST (multipart) {fullUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Post, fullUrl)
            {
                Content = content
            };
            await SetAuthHeader(request);
            
            var response = await _httpClient.SendAsync(request);
            return await HandleResponse<T>(response, fullUrl);
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] Exception:", new { 
                service, 
                endpoint, 
                message = ex.Message,
                stackTrace = ex.StackTrace
            });
            return ApiResponse<T>.FailureResponse($"Error: {ex.Message}");
        }
    }

    private async Task SetAuthHeader(HttpRequestMessage request)
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrEmpty(token))
        {
            // Ensure token doesn't already have "Bearer " prefix
            var cleanToken = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) 
                ? token.Substring(7) 
                : token;
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cleanToken);
            
            await _jsRuntime.InvokeVoidAsync("console.log", "[ApiHttpClient] Auth header set", new {
                hasToken = !string.IsNullOrEmpty(cleanToken),
                tokenLength = cleanToken.Length,
                tokenPreview = cleanToken.Length > 20 ? cleanToken.Substring(0, 20) + "..." : cleanToken
            });
        }
        else
        {
            await _jsRuntime.InvokeVoidAsync("console.warn", "[ApiHttpClient] No auth token found in localStorage");
        }
    }

    private string GetServiceUrl(string service)
    {
        var useGateway = _configuration.GetValue<bool>("ApiSettings:UseGateway", false);
        if (useGateway)
        {
            var gatewayUrl = _configuration["ApiSettings:GatewayUrl"] ?? "http://localhost:5010";
            return gatewayUrl;
        }

        return service.ToLower() switch
        {
            "auth" => _configuration["ApiSettings:AuthServiceUrl"] ?? "http://localhost:5001",
            "articles" => _configuration["ApiSettings:ArticlesServiceUrl"] ?? "http://localhost:5002",
            "reclamations" => _configuration["ApiSettings:ReclamationsServiceUrl"] ?? "http://localhost:5003",
            "interventions" => _configuration["ApiSettings:InterventionsServiceUrl"] ?? "http://localhost:5004",
            _ => throw new ArgumentException($"Unknown service: {service}")
        };
    }

    private async Task<ApiResponse<T>> HandleResponse<T>(HttpResponseMessage response, string? url = null)
    {
        var content = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        
        // Log response details
        await _jsRuntime.InvokeVoidAsync("console.log", "[ApiHttpClient] Response:", new {
            url = url ?? "unknown",
            statusCode = (int)response.StatusCode,
            statusText = response.StatusCode.ToString(),
            contentType = contentType,
            contentLength = content.Length,
            isSuccess = response.IsSuccessStatusCode,
            contentPreview = content.Length > 200 ? content.Substring(0, 200) + "..." : content
        });
        
        if (string.IsNullOrEmpty(content))
        {
            if (response.IsSuccessStatusCode)
            {
                return ApiResponse<T>.SuccessResponse(default!);
            }
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] Empty response with status:", response.StatusCode);
            return ApiResponse<T>.FailureResponse($"Request failed with status {response.StatusCode}");
        }

        // Check if response is HTML (error page) instead of JSON
        if (content.TrimStart().StartsWith("<") || !contentType.Contains("json"))
        {
            var errorDetails = new {
                url = url ?? "unknown",
                statusCode = (int)response.StatusCode,
                statusText = response.StatusCode.ToString(),
                contentType = contentType,
                contentPreview = content.Length > 0 ? content.Substring(0, Math.Min(500, content.Length)) : "Empty content",
                method = response.RequestMessage?.Method?.ToString() ?? "Unknown",
                requestUrl = response.RequestMessage?.RequestUri?.ToString() ?? "Unknown"
            };
            
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] ❌ Error Response:", errorDetails);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return ApiResponse<T>.FailureResponse("Non autorisé. Veuillez vous connecter.");
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return ApiResponse<T>.FailureResponse($"Ressource non trouvée: {url ?? "unknown"}");
            }
            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                return ApiResponse<T>.FailureResponse($"Méthode HTTP non autorisée pour {url ?? "unknown"}. Vérifiez que l'endpoint supporte la méthode {response.RequestMessage?.Method}.");
            }
            return ApiResponse<T>.FailureResponse($"Erreur serveur ({response.StatusCode}): {url ?? "unknown"}");
        }

        try
        {
            var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
            if (result != null)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "[ApiHttpClient] ✅ Success:", new {
                    success = result.Success,
                    message = result.Message
                });
            }
            return result ?? ApiResponse<T>.FailureResponse("Failed to deserialize response");
        }
        catch (JsonException ex)
        {
            var errorDetails = new {
                url = url ?? "unknown",
                statusCode = (int)response.StatusCode,
                statusText = response.StatusCode.ToString(),
                error = ex.Message,
                contentPreview = content.Length > 0 ? content.Substring(0, Math.Min(500, content.Length)) : "Empty content",
                method = response.RequestMessage?.Method?.ToString() ?? "Unknown"
            };
            
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] ❌ JSON Parse Error:", errorDetails);
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                    return ApiResponse<T>.SuccessResponse(data!);
                }
                catch
                {
                    return ApiResponse<T>.FailureResponse("Failed to parse response");
                }
            }
            
            // Try to extract error message from JSON if possible
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
                var errorMessage = errorResponse?.Message ?? $"Erreur {response.StatusCode}";
                
                await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] Error details:", new {
                    url = url ?? "unknown",
                    statusCode = (int)response.StatusCode,
                    message = errorMessage,
                    fullContent = content
                });
                
                return ApiResponse<T>.FailureResponse(errorMessage);
            }
            catch
            {
                var errorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.MethodNotAllowed => $"Méthode HTTP non autorisée pour {url ?? "unknown"}",
                    System.Net.HttpStatusCode.NotFound => $"Ressource non trouvée: {url ?? "unknown"}",
                    System.Net.HttpStatusCode.BadRequest => $"Requête invalide: {url ?? "unknown"}",
                    _ => $"Erreur {response.StatusCode}: {url ?? "unknown"}"
                };
                
                await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] Error:", new {
                    url = url ?? "unknown",
                    statusCode = (int)response.StatusCode,
                    message = errorMessage,
                    rawContent = content
                });
                
                return ApiResponse<T>.FailureResponse(errorMessage);
            }
        }
    }

    private async Task<ApiResponse> HandleResponse(HttpResponseMessage response, string? url = null)
    {
        var content = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        
        // Log response details
        await _jsRuntime.InvokeVoidAsync("console.log", "[ApiHttpClient] Response:", new {
            url = url ?? "unknown",
            statusCode = (int)response.StatusCode,
            statusText = response.StatusCode.ToString(),
            contentType = contentType,
            contentLength = content.Length,
            isSuccess = response.IsSuccessStatusCode
        });
        
        if (string.IsNullOrEmpty(content))
        {
            if (response.IsSuccessStatusCode)
            {
                return ApiResponse.SuccessResponse();
            }
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] Empty response with status:", response.StatusCode);
            return ApiResponse.FailureResponse($"Request failed with status {response.StatusCode}");
        }

        // Check if response is HTML (error page) instead of JSON
        if (content.TrimStart().StartsWith("<") || !contentType.Contains("json"))
        {
            await _jsRuntime.InvokeVoidAsync("console.error", "[ApiHttpClient] ❌ HTML Response (not JSON):", new {
                url = url ?? "unknown",
                statusCode = (int)response.StatusCode,
                contentType = contentType,
                contentPreview = content.Substring(0, Math.Min(500, content.Length))
            });
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return ApiResponse.FailureResponse("Non autorisé. Veuillez vous connecter.");
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return ApiResponse.FailureResponse("Ressource non trouvée.");
            }
            return ApiResponse.FailureResponse($"Erreur serveur: {response.StatusCode}");
        }

        try
        {
            var result = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            return result ?? ApiResponse.FailureResponse("Failed to deserialize response");
        }
        catch (JsonException)
        {
            if (response.IsSuccessStatusCode)
            {
                return ApiResponse.SuccessResponse();
            }
            // Try to extract error message from JSON if possible
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
                return ApiResponse.FailureResponse(errorResponse?.Message ?? $"Erreur: {response.StatusCode}");
            }
            catch
            {
                return ApiResponse.FailureResponse($"Erreur: {response.StatusCode}");
            }
        }
    }
}

