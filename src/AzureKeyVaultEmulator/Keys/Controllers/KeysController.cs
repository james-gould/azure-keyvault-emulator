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
        public async Task<IActionResult> CreateKey(
            [FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromBody] CreateKeyModel requestBody)
        {
            var createdKey = await keyService.CreateKeyAsync(name, requestBody);

            return Ok(createdKey);
        }

        [HttpGet("{name}/{version}")]
        public async Task<IActionResult> GetKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion)
        {
            var keyResult = await keyService.GetKeyAsync(name, version);

            if (keyResult == null)
                return NotFound();

            return Ok(keyResult);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetKey(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var keyResult = await keyService.GetKeyAsync(name);

            if (keyResult == null)
                return NotFound();

            return Ok(keyResult);
        }

        // Azure.Security.KeyVault.Keys v4.7.0 doesn't provide {name}/{version}
        // But the REST API spec expects it. One of the two is out of date:
        // https://learn.microsoft.com/en-us/rest/api/keyvault/keys/update-key/update-key

        [HttpPatch("{name}/{version}")]
        public async Task<IActionResult> UpdateKeyWithVersion(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] UpdateKeyRequest request)
        {
            var result = await keyService.UpdateKeyAsync(name, version, request.Attributes, request.Tags);

            return Ok(result);
        }

        [HttpPatch("{name}")]
        public async Task<IActionResult> UpdateKeyWithoutVersion(
            [FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromBody] UpdateKeyRequest request)
        {
            var result = await keyService.UpdateKeyAsync(name, "", request.Attributes, request.Tags);

            return Ok(result);
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteKey(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var result = await keyService.DeleteKeyAsync(name);

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
        public async Task<IActionResult> Encrypt(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] KeyOperationParameters keyOperationParameters)
        {
            var result = await keyService.EncryptAsync(name, version, keyOperationParameters);

            return Ok(result);
        }

        [HttpPost("{name}/encrypt")]
        public async Task<IActionResult> Encrypt(
           [FromRoute] string name,
           [ApiVersion] string apiVersion,
           [FromBody] KeyOperationParameters keyOperationParameters)
        {
            var result = await keyService.EncryptAsync(name, string.Empty, keyOperationParameters);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/decrypt")]
        public async Task<IActionResult> Decrypt(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] KeyOperationParameters keyOperationParameters)
        {
            var result = await keyService.DecryptAsync(name, version, keyOperationParameters);

            return Ok(result);
        }

        [HttpPost("{name}/decrypt")]
        public async Task<IActionResult> Decrypt(
           [FromRoute] string name,
           [ApiVersion] string apiVersion,
           [FromBody] KeyOperationParameters keyOperationParameters)
        {
            var result = await keyService.DecryptAsync(name, string.Empty, keyOperationParameters);

            return Ok(result);
        }

        [HttpPost("{name}/backup")]
        public async Task<IActionResult> BackupKey(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var result = await keyService.BackupKeyAsync(name);

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
        public async Task<IActionResult> UpdateKeyRotationPolicy(
            [FromRoute] string name,
            [FromBody] KeyRotationPolicy policy,
            [ApiVersion] string apiVersion)
        {
            var result = await keyService.UpdateKeyRotationPolicyAsync(name, policy.Attributes, policy.LifetimeActions);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/release")]
        public async Task<IActionResult> ReleaseKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] ReleaseKeyRequest vm)
        {
            var result = await keyService.ReleaseKeyAsync(name, version);

            return Ok(result);
        }

        [HttpPut("{name}")]
        public async Task<IActionResult> ImportKey(
            [FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromBody] ImportKeyRequest req)
        {
            var result = await keyService.ImportKeyAsync(name, req.Key, req.KeyAttributes, req.Tags);

            return Ok(result);
        }

        [HttpPost("{name}/sign")]
        public async Task<IActionResult> SignWithKey(
            [FromRoute] string name,
            [FromBody] SignKeyRequest model,
            [ApiVersion] string apiVersion)
        {
            var result = await keyService.SignWithKeyAsync(name, string.Empty, model.SigningAlgorithm, model.Value);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/sign")]
        public async Task<IActionResult> SignWithKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromBody] SignKeyRequest model,
            [ApiVersion] string apiVersion)
        {
            var result = await keyService.SignWithKeyAsync(name, version, model.SigningAlgorithm, model.Value);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/verify")]
        public async Task<IActionResult> VerifyHash(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromBody] VerifyHashRequest req)
        {
            var result = await keyService.VerifyDigestAsync(name, version, req.Digest, req.Value);

            return Ok(result);
        }

        [HttpPost("{name}/verify")]
        public async Task<IActionResult> VerifyHash(
            [FromRoute] string name,
            [FromBody] VerifyHashRequest req)
        {
            var result = await keyService.VerifyDigestAsync(name, string.Empty, req.Digest, req.Value);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/wrapkey")]
        public async Task<IActionResult> WrapKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromBody] KeyOperationParameters para,
            [ApiVersion] string apiVersion)
        {
            var result = await keyService.WrapKeyAsync(name, version, para);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/unwrapkey")]
        public async Task<IActionResult> UnwrapKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromBody] KeyOperationParameters para,
            [ApiVersion] string apiVersion)
        {
            var result = await keyService.UnwrapKeyAsync(name, version, para);

            return Ok(result);
        }
    }
}
