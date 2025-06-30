using Azure.Security.KeyVault.Certificates;
using Microsoft.AspNetCore.Mvc;

namespace WebApiWithEmulator.DebugHelper.Controllers;
public class CertificateController(CertificateClient client) : Controller
{
    [HttpPatch("updateCertificate")]
    public async Task<IActionResult> UpdateCertificate()
    {
        var name = Guid.NewGuid().ToString();

        var operation = await client.StartCreateCertificateAsync(name, CertificatePolicy.Default);

        await operation.WaitForCompletionAsync();

        var cert = await client.GetCertificateAsync(name);

        var props = new CertificateProperties(name)
        {
            Enabled = false,
        };

        var response = await client.UpdateCertificatePropertiesAsync(props);

        return Ok(response.Value);
    }
}
