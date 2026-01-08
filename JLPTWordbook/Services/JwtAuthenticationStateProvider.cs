using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace JLPTWordbook.Services;

public class JwtAuthenticationStateProvider(IHttpContextAccessor accessor, WordbookDatabaseService wdb) : AuthenticationStateProvider
{
    private ClaimsPrincipal? m_CurrentUser;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (m_CurrentUser == null)
        {
            var httpContext = accessor.HttpContext;
            if (httpContext != null)
            {
                var jwtToken = httpContext.Request.Cookies["id_token"];
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.ReadJwtToken(jwtToken);
                    var claims = token.Claims.ToList();
                    var identity = new ClaimsIdentity(claims, "JwtAuthType");
                    var principal = new ClaimsPrincipal(identity);
                    m_CurrentUser = principal;

                    var sub = Sub;
                    if (!string.IsNullOrEmpty(sub))
                    {
                        await wdb.LoginAsync(sub, Name);
                    }
                }
            }

            m_CurrentUser ??= new ClaimsPrincipal(new ClaimsIdentity());
        }

        m_CurrentUser ??= new ClaimsPrincipal();
        return new AuthenticationState(m_CurrentUser);
    }

    public bool IsAuthorized => m_CurrentUser?.Identity?.IsAuthenticated == true;

    public string? Sub => m_CurrentUser?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
    public string? Name => m_CurrentUser?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;
    public string? Email => m_CurrentUser?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
    public string? Picture => m_CurrentUser?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Picture)?.Value;
}
