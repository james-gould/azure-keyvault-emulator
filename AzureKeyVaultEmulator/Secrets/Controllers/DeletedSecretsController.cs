using AzureKeyVaultEmulator.Secrets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Secrets.Controllers
{
    [ApiController]
    [Route("deletedsecrets")]
    [Authorize]
    public class DeletedSecretsController : Controller
    {
        private IKeyVaultSecretService _keyVaultSecretService;

        public DeletedSecretsController(IKeyVaultSecretService keyVaultSecretService)
        {
                _keyVaultSecretService = keyVaultSecretService;
        }


    }
}
