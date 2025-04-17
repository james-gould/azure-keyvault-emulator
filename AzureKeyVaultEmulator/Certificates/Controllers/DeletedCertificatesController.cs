using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Certificates.Controllers;
public class DeletedCertificatesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
