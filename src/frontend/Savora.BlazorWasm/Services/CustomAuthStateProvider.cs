using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Savora.BlazorWasm.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            
            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(_anonymous);
            }

            var claims = ParseClaimsFromJwt(token);
            
            // Check if token is expired
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim != null && long.TryParse(expClaim.Value, out var expValue))
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(expValue).DateTime;
                if (expDate < DateTime.UtcNow)
                {
                    await _localStorage.RemoveItemAsync("authToken");
                    await _localStorage.RemoveItemAsync("refreshToken");
                    await _localStorage.RemoveItemAsync("user");
                    return new AuthenticationState(_anonymous);
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            
            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public void NotifyUserAuthentication(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        
        var authState = Task.FromResult(new AuthenticationState(user));
        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLogout()
    {
        var authState = Task.FromResult(new AuthenticationState(_anonymous));
        NotifyAuthenticationStateChanged(authState);
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            claims.AddRange(token.Claims);
            
            // Ensure role claim is properly mapped
            var roleClaim = token.Claims.FirstOrDefault(c => 
                c.Type == ClaimTypes.Role || 
                c.Type == "role" || 
                c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
                
            if (roleClaim != null && !claims.Any(c => c.Type == ClaimTypes.Role))
            {
                claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
            }
        }
        catch
        {
            // Invalid token
        }
        
        return claims;
    }
}

