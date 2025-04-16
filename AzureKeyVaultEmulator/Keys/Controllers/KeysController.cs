using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Shared.Models.Keys.RequestModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// https://learn.microsoft.com/en-us/rest/api/keyvault/keys/operation-groups
namespace AzureKeyVaultEmulator.Keys.Controllers
{
    [ApiController]
    [Route("keys")]
    [Authorize]
    public class KeysController(IKeyService keyService, ITokenService tokenService) : ControllerBase
    {
        [HttpPost("{name}/create")]
        public IActionResult CreateKey(
            [FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromBody] CreateKeyModel requestBody)
        {
            var createdKey = keyService.CreateKey(name, requestBody);

            return Ok(createdKey);
        }

        [HttpGet("{name}/{version}")]
        public IActionResult GetKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion)
        {
            var keyResult = keyService.GetKey(name, version);

            if (keyResult == null)
                return NotFound();

            return Ok(keyResult);
        }

        [HttpGet("{name}")]
        public IActionResult GetKey(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var keyResult = keyService.GetKey(name);

            if (keyResult == null)
                return NotFound();

            return Ok(keyResult);
        }

        // Azure.Security.KeyVault.Keys v4.7.0 doesn't provide {name}/{version}
        // But the REST API spec expects it. One of the two is out of date:
        // https://learn.microsoft.com/en-us/rest/api/keyvault/keys/update-key/update-key

        [HttpPatch("{name}/{version}")]
        public IActionResult UpdateKeyWithVersion(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] UpdateKeyRequest request)
        {
            var result = keyService.UpdateKey(name, version, request.Attributes, request.Tags);

            return Ok(result);
        }

        
        [HttpPatch("{name}")]
        public IActionResult UpdateKeyWithoutVersion(
            [FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromBody] UpdateKeyRequest request)
        {
            var result = keyService.UpdateKey(name, "", request.Attributes, request.Tags);

            return Ok(result);
        }

        [HttpDelete("{name}")]
        public IActionResult DeleteKey(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.DeleteKey(name);

            return Ok(result);
        }

        [HttpGet]
        public IActionResult GetKeys(
            [ApiVersion] string apiVersion,
            [FromQuery] int maxResults = 25,
            [SkipToken] string token = "")
        {
            int skipCount = 0;

            if (!string.IsNullOrEmpty(token))
                skipCount = tokenService.DecodeSkipToken(token);

            var result = keyService.GetKeys(maxResults, skipCount);

            return Ok(result);
        }

        [HttpGet("{name}/versions")]
        public IActionResult GetKeyVersions(
            [FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromQuery] int maxResults = 25,
            [SkipToken] string skipToken = "")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            int skipCount = 0;

            if(!string.IsNullOrEmpty(skipToken))
                skipCount = tokenService.DecodeSkipToken(skipToken);

            var result = keyService.GetKeyVersions(name, maxResults, skipCount);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/encrypt")]
        public IActionResult Encrypt(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] KeyOperationParameters keyOperationParameters)
        {
            var result = keyService.Encrypt(name, version, keyOperationParameters);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/decrypt")]
        public IActionResult Decrypt(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] KeyOperationParameters keyOperationParameters)
        {
            var result = keyService.Decrypt(name, version, keyOperationParameters);

            return Ok(result);
        }

        [HttpPost("{name}/backup")]
        public IActionResult BackupKey(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.BackupKey(name);

            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("restore")]
        public IActionResult RestoreKey(
            [FromBody] ValueModel<string> backedUpKey,
            [ApiVersion]string apiVersion)
        {
            var result = keyService.RestoreKey(backedUpKey.Value);

            return Ok(result);
        }

        [HttpGet("{name}/rotationpolicy")]
        public IActionResult GetKeyRotationPolicy(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.GetKeyRotationPolicy(name);

            return Ok(result);
        }

        [HttpPut("{name}/rotationpolicy")]
        public IActionResult UpdateKeyRotationPolicy(
            [FromRoute] string name,
            [FromBody] KeyRotationPolicy policy,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.UpdateKeyRotationPolicy(name, policy.Attributes, policy.LifetimeActions);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/release")]
        public IActionResult ReleaseKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] ReleaseKeyRequest vm)
        {
            var result = keyService.ReleaseKey(name, version);

            return Ok(result);
        }

        [HttpPut("{name}")]
        public IActionResult ImportKey(
            [FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromBody] ImportKeyRequest req)
        {
            var result = keyService.ImportKey(name, req.Key, req.KeyAttributes, req.Tags);

            return Ok(result);
        }

        [HttpPost("{name}/sign")]
        public IActionResult SignWithKey(
            [FromRoute] string name,
            [FromBody] SignKeyRequest model,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.SignWithKey(name, string.Empty, model.SigningAlgorithm, model.Value);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/sign")]
        public IActionResult SignWithKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromBody] SignKeyRequest model,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.SignWithKey(name, version, model.SigningAlgorithm, model.Value);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/verify")]
        public IActionResult VerifyHash(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromBody] VerifyHashRequest req)
        {
            var result = keyService.VerifyDigest(name, version, req.Digest, req.Value);

            return Ok(result);
        }

        [HttpPost("{name}/verify")]
        public IActionResult VerifyHash(
            [FromRoute] string name,
            [FromBody] VerifyHashRequest req)
        {
            var result = keyService.VerifyDigest(name, string.Empty, req.Digest, req.Value);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/wrapkey")]
        public IActionResult WrapKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromBody] KeyOperationParameters para,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.WrapKey(name, version, para);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/unwrapkey")]
        public IActionResult UnwrapKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromBody] KeyOperationParameters para,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.UnwrapKey(name, version, para);

            return Ok(result);
        }
    }
}
