using System.Text;
using System.Text.Json;
using DigitalMe.Web.Models;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace DigitalMe.Web.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetTokenAsync();
    Task<string?> GetUserNameAsync();
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string email, string password, string confirmPassword);
    Task LogoutAsync();
    Task<bool> IsTokenValidAsync();
    event Action<bool>? AuthStateChanged;
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? UserName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthService : IAuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<AuthService> _logger;
    private readonly HttpClient _httpClient;
    private readonly DigitalMeConfiguration _configuration;

    public event Action<bool>? AuthStateChanged;

    public AuthService(IJSRuntime jsRuntime, ILogger<AuthService> logger, HttpClient httpClient, IOptions<DigitalMeConfiguration> configuration)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration.Value;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            return await IsTokenValidAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return false;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
        {
            _logger.LogDebug("Cannot access localStorage during prerendering");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving auth token");
            return null;
        }
    }

    public async Task<string?> GetUserNameAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "userName");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
        {
            _logger.LogDebug("Cannot access localStorage during prerendering");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving username");
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Clear localStorage
            try 
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authExpiry");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userName");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                _logger.LogDebug("Cannot access localStorage during prerendering");
            }

            // Clear HttpClient authorization header
            _httpClient.DefaultRequestHeaders.Authorization = null;

            _logger.LogInformation("User logged out successfully");

            // Notify subscribers
            AuthStateChanged?.Invoke(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    public async Task<bool> IsTokenValidAsync()
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            // Check expiry time
            string? expiryString = null;
            try 
            {
                expiryString = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authExpiry");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                _logger.LogDebug("Cannot access localStorage during prerendering");
            }
            
            if (string.IsNullOrEmpty(expiryString))
                return false;

            if (DateTime.TryParse(expiryString, out var expiry))
            {
                if (DateTime.Now >= expiry.AddMinutes(-5)) // 5-minute buffer
                {
                    _logger.LogInformation("Token expired, logging out");
                    await LogoutAsync();
                    return false;
                }
            }

            // Validate token with server (optional - can be expensive)
            return await ValidateTokenWithServerAsync(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            if (!_configuration.Features.UseRealAuthentication)
            {
                // Demo mode - accept demo credentials
                if (email == "demo@digitalme.ai" && password == "Ivan2024!")
                {
                    var demoToken = "demo-jwt-token-" + DateTime.UtcNow.Ticks;
                    var demoExpiry = DateTime.UtcNow.AddHours(8);
                    
                    await SetAuthenticationAsync(demoToken, "Demo User", demoExpiry);
                    
                    return new AuthResult
                    {
                        Success = true,
                        Token = demoToken,
                        UserName = "Demo User",
                        ExpiresAt = demoExpiry
                    };
                }
                else
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Неверные учетные данные. Используйте: demo@digitalme.ai / Ivan2024!"
                    };
                }
            }

            // Real authentication mode
            var loginRequest = new LoginRequest
            {
                Email = email,
                Password = password
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_configuration.ApiBaseUrl}/api/auth/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse != null)
                {
                    await SetAuthenticationAsync(loginResponse.Token, loginResponse.UserName, loginResponse.ExpiresAt);

                    return new AuthResult
                    {
                        Success = true,
                        Token = loginResponse.Token,
                        UserName = loginResponse.UserName,
                        ExpiresAt = loginResponse.ExpiresAt
                    };
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Login failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);

            return new AuthResult
            {
                Success = false,
                ErrorMessage = response.StatusCode == System.Net.HttpStatusCode.Unauthorized 
                    ? "Неверные учетные данные" 
                    : "Ошибка входа. Попробуйте позже."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during login");
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Ошибка сети. Проверьте подключение к интернету."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Произошла ошибка при входе. Попробуйте позже."
            };
        }
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string confirmPassword)
    {
        try
        {
            if (password != confirmPassword)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Пароли не совпадают"
                };
            }

            if (!_configuration.Features.UseRealAuthentication)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Регистрация недоступна в демо-режиме. Используйте: demo@digitalme.ai / Ivan2024!"
                };
            }

            var registerRequest = new RegisterRequest
            {
                Email = email,
                Password = password,
                ConfirmPassword = confirmPassword
            };

            var json = JsonSerializer.Serialize(registerRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_configuration.ApiBaseUrl}/api/auth/register", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var registerResponse = JsonSerializer.Deserialize<LoginResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (registerResponse != null)
                {
                    await SetAuthenticationAsync(registerResponse.Token, registerResponse.UserName, registerResponse.ExpiresAt);

                    return new AuthResult
                    {
                        Success = true,
                        Token = registerResponse.Token,
                        UserName = registerResponse.UserName,
                        ExpiresAt = registerResponse.ExpiresAt
                    };
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Registration failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);

            return new AuthResult
            {
                Success = false,
                ErrorMessage = response.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? "Проблема с данными регистрации. Проверьте email и пароль."
                    : "Ошибка регистрации. Попробуйте позже."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during registration");
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Ошибка сети. Проверьте подключение к интернету."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Произошла ошибка при регистрации. Попробуйте позже."
            };
        }
    }

    private async Task<bool> ValidateTokenWithServerAsync(string token)
    {
        try
        {
            if (!_configuration.Features.UseRealAuthentication)
            {
                // Demo mode - always return true for demo tokens
                return token.StartsWith("demo-jwt-token-");
            }

            // Add token to request headers
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"{_configuration.ApiBaseUrl}/api/auth/validate");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Token validation successful");
                return true;
            }
            else
            {
                _logger.LogWarning("Token validation failed with status: {StatusCode}", response.StatusCode);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await LogoutAsync();
                }
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during token validation");
            // Don't logout on network errors - could be temporary
            return true; // Assume valid to avoid unnecessary logouts
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token with server");
            return false;
        }
    }

    public async Task SetAuthenticationAsync(string token, string userName, DateTime expiresAt)
    {
        try
        {
            // Store in localStorage
            try 
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", token);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userName", userName);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authExpiry", expiresAt.ToString("O"));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                _logger.LogDebug("Cannot access localStorage during prerendering, will set on client side");
            }

            // Set HttpClient authorization header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            _logger.LogInformation("Authentication set for user: {UserName}", userName);

            // Notify subscribers
            AuthStateChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting authentication");
        }
    }
}

public static class AuthServiceExtensions
{
    public static IServiceCollection AddAuthService(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}