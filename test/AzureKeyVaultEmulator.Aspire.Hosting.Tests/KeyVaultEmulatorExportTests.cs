using System.Reflection;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AzureKeyVaultEmulator.Aspire.Hosting.Exceptions;
using Xunit;

namespace AzureKeyVaultEmulator.Aspire.Hosting.Tests;

public class KeyVaultEmulatorExportTests
{
    [Fact]
    public void OmittedOptionsPreserveDefaults()
    {
        var exported = JsonSerializer.Deserialize<KeyVaultEmulatorExportOptions>("{}")!.ToOptions();
        var defaults = new KeyVaultEmulatorOptions();

        Assert.Equal(JsonSerializer.Serialize(defaults), JsonSerializer.Serialize(exported));
    }

    [Fact]
    public void OptionsDeserializeFromCamelCaseJson()
    {
        var exported = JsonSerializer.Deserialize<KeyVaultEmulatorExportOptions>(
            """
            {
                "lifetime": "Persistent",
                "port": 4997,
                "persist": true,
                "localCertificatePath": "certificates",
                "loadCertificatesIntoTrustStore": false,
                "shouldGenerateCertificates": false,
                "useDotnetDevCerts": true
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            })!.ToOptions();

        Assert.Equal(ContainerLifetime.Persistent, exported.Lifetime);
        Assert.Equal(4997, exported.Port);
        Assert.True(exported.Persist);
        Assert.Equal("certificates", exported.LocalCertificatePath);
        Assert.False(exported.LoadCertificatesIntoTrustStore);
        Assert.False(exported.ShouldGenerateCertificates);
        Assert.True(exported.UseDotnetDevCerts);
    }

    [Theory]
    [InlineData("AddAzureKeyVaultEmulatorForExport", "addAzureKeyVaultEmulator")]
    [InlineData("RunAsEmulatorForExport", "runAsEmulator")]
    public void MethodsHaveStableExportIds(string methodName, string id)
    {
        var method = typeof(KeyVaultEmulatorExtensions).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Equal(id, method.GetCustomAttribute<AspireExportAttribute>()!.Id);
        Assert.NotNull(typeof(KeyVaultEmulatorExportOptions).GetCustomAttribute<AspireDtoAttribute>());
        Assert.Equal(7, typeof(KeyVaultEmulatorExportOptions).GetProperties().Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothExportsKeepPersistenceValidation(bool redirect)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { DisableDashboard = true });
        var options = new KeyVaultEmulatorExportOptions { Persist = true };

        var exception = Assert.Throws<KeyVaultEmulatorException>(() =>
        {
            if (redirect)
                builder.AddAzureKeyVault("vault").RunAsEmulatorForExport(options);
            else
                builder.AddAzureKeyVaultEmulatorForExport("vault", options);
        });

        Assert.Contains("static Port", exception.Message);
    }

    [Fact]
    public void ExportsKeepCertificateValidation()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { DisableDashboard = true });

        Assert.Throws<KeyVaultEmulatorException>(() => builder.AddAzureKeyVaultEmulatorForExport(
            "vault", new KeyVaultEmulatorExportOptions { ShouldGenerateCertificates = false }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OmittedOptionsKeepConfigurationBinding(bool redirect)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { DisableDashboard = true });
        var section = redirect ? "emulator" : "vault";
        builder.Configuration[$"{section}:Persist"] = "true";

        var exception = Assert.Throws<KeyVaultEmulatorException>(() =>
        {
            if (redirect)
                builder.AddAzureKeyVault("vault").RunAsEmulatorForExport(configSectionName: section);
            else
                builder.AddAzureKeyVaultEmulatorForExport("vault");
        });

        Assert.Contains("static Port", exception.Message);
    }

    [Fact]
    public void ExportsPreserveAzureResourcesWhenPublishing()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--publisher", "manifest"],
            DisableDashboard = true
        });
        Assert.True(builder.ExecutionContext.IsPublishMode);

        var direct = builder.AddAzureKeyVaultEmulatorForExport("direct");
        var redirected = builder.AddAzureKeyVault("redirected");
        Assert.Same(redirected, redirected.RunAsEmulatorForExport());
        Assert.DoesNotContain(direct.Resource.Annotations, annotation => annotation is ContainerImageAnnotation);
        Assert.DoesNotContain(redirected.Resource.Annotations, annotation => annotation is ContainerImageAnnotation);
    }
}
