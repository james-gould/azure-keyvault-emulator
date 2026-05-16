using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Emulator.Controllers
{
    /// <summary>
    /// Minimal Entra-compatible OAuth2/OIDC endpoints so <c>DefaultAzureCredential</c> can acquire
    /// a token from the emulator without a real Entra tenant. Tokens are unconditionally issued; the
    /// JwtBearer pipeline does not validate them.
    /// </summary>
    [Route("")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class OAuthController(ITokenService tokenService) : Controller
    {
        /// <summary>
        /// MSAL Instance Discovery endpoint. Returns metadata that points back at the emulator itself.
        /// </summary>
        [HttpGet("common/discovery/instance")]
        public IActionResult InstanceDiscovery([FromQuery(Name = "authorization_endpoint")] string? authorizationEndpoint)
        {
            var authority = $"{Request.Scheme}://{Request.Host}";

            var tenantId = ExtractTenantFromAuthorizationEndpoint(authorizationEndpoint)
                ?? AuthConstants.EmulatorTenantId;

            return Ok(new
            {
                tenant_discovery_endpoint = $"{authority}/{tenantId}/v2.0/.well-known/openid-configuration",
                api_version = "1.1",
                metadata = new[]
                {
                    new
                    {
                        preferred_network = Request.Host.ToString(),
                        preferred_cache = Request.Host.ToString(),
                        aliases = new[] { Request.Host.ToString() }
                    }
                }
            });
        }

        /// <summary>
        /// OpenID Connect discovery document for a given tenant.
        /// </summary>
        [HttpGet("{tenantId}/v2.0/.well-known/openid-configuration")]
        [HttpGet("{tenantId}/.well-known/openid-configuration")]
        public IActionResult OpenIdConfiguration([FromRoute] string tenantId)
        {
            var authority = $"{Request.Scheme}://{Request.Host}";

            return Ok(new
            {
                issuer = $"{authority}/{tenantId}/v2.0",
                authorization_endpoint = $"{authority}/{tenantId}/oauth2/v2.0/authorize",
                token_endpoint = $"{authority}/{tenantId}/oauth2/v2.0/token",
                device_authorization_endpoint = $"{authority}/{tenantId}/oauth2/v2.0/devicecode",
                jwks_uri = $"{authority}/{tenantId}/discovery/v2.0/keys",
                response_modes_supported = new[] { "query", "fragment", "form_post" },
                response_types_supported = new[] { "code", "id_token", "code id_token", "token" },
                scopes_supported = new[] { "openid", "profile", "email", "offline_access" },
                subject_types_supported = new[] { "pairwise" },
                token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic" },
                tenant_region_scope = "EMU",
                cloud_instance_name = Request.Host.ToString(),
                cloud_graph_host_name = Request.Host.ToString(),
                msgraph_host = Request.Host.ToString()
            });
        }

        /// <summary>
        /// OAuth2 v2.0 token endpoint. Accepts any credentials and unconditionally issues a JWT.
        /// </summary>
        [HttpPost("{tenantId}/oauth2/v2.0/token")]
        [HttpPost("{tenantId}/oauth2/token")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult IssueToken([FromRoute] string tenantId)
        {
            var jwt = tokenService.CreateBearerToken();

            return Ok(new
            {
                token_type = "Bearer",
                expires_in = 3600,
                ext_expires_in = 3600,
                access_token = jwt
            });
        }

        /// <summary>
        /// JWKS endpoint stub. Returned empty as the JwtBearer pipeline does not validate signatures.
        /// </summary>
        [HttpGet("{tenantId}/discovery/v2.0/keys")]
        public IActionResult JsonWebKeySet([FromRoute] string tenantId)
        {
            return Ok(new { keys = Array.Empty<object>() });
        }

        private static string? ExtractTenantFromAuthorizationEndpoint(string? authorizationEndpoint)
        {
            if (string.IsNullOrWhiteSpace(authorizationEndpoint))
                return null;

            if (!Uri.TryCreate(authorizationEndpoint, UriKind.Absolute, out var parsed))
                return null;

            // /<tenantId>/oauth2/v2.0/authorize
            var segments = parsed.AbsolutePath.Trim('/').Split('/');

            if (segments.Length == 0 || string.IsNullOrWhiteSpace(segments[0]))
                return null;

            return segments[0];
        }
    }
}
