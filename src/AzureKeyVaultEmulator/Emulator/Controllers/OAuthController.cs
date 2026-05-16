using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Emulator.Controllers
{
    /// <summary>
    /// Exposes a minimal subset of the Microsoft Entra (Azure AD) v2.0 OAuth/OIDC surface area so that
    /// the official Azure SDK for .NET — and in particular <c>DefaultAzureCredential</c> via
    /// <c>EnvironmentCredential</c>/<c>ClientSecretCredential</c> — can acquire an access token from the
    /// emulator itself instead of having to round-trip a real Entra tenant.
    /// <para>
    /// The emulator does not validate inbound tokens (the JwtBearer pipeline is configured to accept
    /// any signature/issuer), so any token issued here is sufficient to satisfy the Key Vault SDK's
    /// challenge-based authentication policy.
    /// </para>
    /// </summary>
    [Route("")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class OAuthController(ITokenService token) : Controller
    {
        /// <summary>
        /// MSAL Instance Discovery endpoint. Always exposed at <c>/common/discovery/instance</c>.
        /// <para>
        /// MSAL (used internally by <c>DefaultAzureCredential</c>) calls this endpoint to determine
        /// whether an authority host is trusted and to discover the OpenID configuration endpoint for
        /// a specific tenant. We return metadata that points back at the emulator itself.
        /// </para>
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
        /// OpenID Connect discovery document for a given tenant. Returns the endpoints (issuer, token,
        /// authorization, jwks) that MSAL needs to acquire a token.
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
        /// OAuth2 v2.0 token endpoint. Accepts any credentials and unconditionally issues a JWT bearer
        /// token signed with the emulator's signing key. The token is suitable for use against the
        /// emulator's Key Vault API surface.
        /// </summary>
        [HttpPost("{tenantId}/oauth2/v2.0/token")]
        [HttpPost("{tenantId}/oauth2/token")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult IssueToken([FromRoute] string tenantId)
        {
            // Intentionally do not validate the inbound form. The emulator's purpose is to let any
            // caller acquire a token; the protected resource (Key Vault API) likewise does not validate.
            var jwt = token.CreateBearerToken();

            return Ok(new
            {
                token_type = "Bearer",
                expires_in = 3600,
                ext_expires_in = 3600,
                access_token = jwt
            });
        }

        /// <summary>
        /// JWKS endpoint stub. Returned empty since the emulator's JwtBearer pipeline does not validate
        /// signatures, but MSAL may probe this URL when validating issuer metadata.
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
            return segments.Length > 0 ? segments[0] : null;
        }
    }
}
