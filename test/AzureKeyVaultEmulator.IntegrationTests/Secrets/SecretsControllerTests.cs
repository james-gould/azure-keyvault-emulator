using System.Text.Json;
using Azure;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Secrets
{
    public class SecretsControllerTests(SecretsTestingFixture fixture) : IClassFixture<SecretsTestingFixture>
    {
        [Fact]
        public async Task GetSecretReturnsCorrectValueTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;
            var secretValue = fixture.FreshlyGeneratedGuid;

            var createdSecret = await fixture.CreateSecretAsync(secretName, secretValue);

            var fromEmulator = await client.GetSecretAsync(secretName);

            Assert.NotNull(fromEmulator);

            Assert.Equal(secretValue, createdSecret.Value);
        }

        [Fact]
        public async Task SetSecretCreatesSecretInMemoryTest()
        {
            var client = await fixture.GetClientAsync();

            var name = fixture.FreshlyGeneratedGuid;
            var value = fixture.FreshlyGeneratedGuid;

            var createdSecret = await fixture.CreateSecretAsync(name, value);

            Assert.NotNull(createdSecret.Value);

            var secretFromKv = await client.GetSecretAsync(name, createdSecret.Properties.Version);

            Assert.Equal(createdSecret.Value, secretFromKv.Value.Value);
            Assert.Equal(createdSecret.Properties.Version, secretFromKv.Value.Properties.Version);
        }

        [Fact]
        public async Task SecretWillHaveAttributesSet()
        {
            var secretName = fixture.FreshlyGeneratedGuid;

            var secretValue = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            Assert.NotNull(secret);
            Assert.NotNull(secret.Properties.RecoveryLevel);
            Assert.NotNull(secret.Properties.RecoverableDays);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SecretCanBeEmptyOrWhitespaceString(string secretValue)
        {
            var secretName = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);
            Assert.NotNull(secret);

            var client = await fixture.GetClientAsync();
            var retrievedSecret = await client.GetSecretAsync(secretName);
            Assert.NotNull(retrievedSecret);
            Assert.Equal(secretValue, retrievedSecret.Value.Value);
        }

        [Fact]
        public async Task GetSecretAfterDeletingProvidesKeyVaultErrorTest()
        {
            var client = await fixture.GetClientAsync();

            var deletedName = fixture.FreshlyGeneratedGuid;
            var deletedValue = fixture.FreshlyGeneratedGuid;

            var createdSecret = await fixture.CreateSecretAsync(deletedName, deletedValue);

            Assert.NotNull(createdSecret.Value);

            var deletedSecret = await client.StartDeleteSecretAsync(deletedName);

            Assert.NotNull(deletedSecret.Value);
            Assert.Equal(deletedName, deletedSecret.Value.Name);

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(deletedName));

        }

        [Fact]
        public async Task BackupSecretAsyncReturnsEncodedNameTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            await fixture.CreateSecretAsync(secretName, fixture.FreshlyGeneratedGuid);

            var backup = await client.BackupSecretAsync(secretName);

            Assert.NotEmpty(backup.Value);
        }

        [Fact]
        public async Task GetSecretVersionsPagesAllSecretsByNameTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            var executionCount = await RequestSetup
                .CreateMultiple(26, 30, i => client.SetSecretAsync(secretName, $"{i}value"));

            var properties = client.GetPropertiesOfSecretVersionsAsync(secretName);

            var versions = new List<string>();

            await foreach (var version in properties)
            {
                Assert.NotEqual(string.Empty, version.Version);
                versions.Add(version.Version);
            }

            Assert.Equal(executionCount, versions.Count);
        }

        [Fact]
        public async Task GetSecretsPagesAllSecretsCreatedTest()
        {
            var client = await fixture.GetClientAsync();

            var firstSecretName = fixture.FreshlyGeneratedGuid;
            var firstSecretValue = fixture.FreshlyGeneratedGuid;
            var firstSecretSecondValue = fixture.FreshlyGeneratedGuid;

            await fixture.CreateSecretAsync(firstSecretName, firstSecretValue);

            await Task.Delay(1000);

            await fixture.CreateSecretAsync(firstSecretName, firstSecretSecondValue);

            var secondSecretName = fixture.FreshlyGeneratedGuid;
            var secondSecretValue = fixture.FreshlyGeneratedGuid;
            var secondSecretSecondValue = fixture.FreshlyGeneratedGuid;

            await fixture.CreateSecretAsync(secondSecretName, secondSecretValue);

            await Task.Delay(1000);

            await fixture.CreateSecretAsync(secondSecretName, secondSecretSecondValue);

            var testSecrets = new List<SecretProperties?>();

            var secrets = client.GetPropertiesOfSecretsAsync();

            await foreach (var secret in secrets)
                if(
                    secret.Name.Equals(firstSecretName, StringComparison.InvariantCultureIgnoreCase) ||
                    secret.Name.Equals(secondSecretName, StringComparison.InvariantCultureIgnoreCase)
                  )
                        testSecrets.Add(secret);

            Assert.Equal(2, testSecrets.Count);
        }

        [Fact]
         public async Task SecretListOnlyContainsBaseIdentifierUri()
         {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretName, secretName);

            var listResponse = await client.GetPropertiesOfSecretsAsync().ToListAsync();

            var secretFromList = listResponse.Single(x => x.Id.ToString().Contains(secretName));

            var baseIdentifier = $"{secret.Properties.VaultUri}secrets/{secret.Name}";
            Assert.Equal(baseIdentifier, secretFromList.Id.ToString());
        }

        [Fact]
        public async Task SecretVersionListContainsFullIdentifierUri()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            var firstVersion = await fixture.CreateSecretAsync(secretName, secretName);
            await Task.Delay(1000);
            var secondVersion =  await fixture.CreateSecretAsync(secretName, secretName);

            var listResponse = await client.GetPropertiesOfSecretVersionsAsync(secretName).ToListAsync();

            var firstVersionFromList = listResponse.Single(x => x.Id.ToString().Contains(secretName) && x.CreatedOn == firstVersion.Properties.CreatedOn);
            var secondVersionFromList = listResponse.Single(x => x.Id.ToString().Contains(secretName) && x.CreatedOn == secondVersion.Properties.CreatedOn);

            Assert.Contains(firstVersion.Properties.Version, firstVersionFromList.Id.ToString());
            Assert.Contains(secondVersion.Properties.Version, secondVersionFromList.Id.ToString());
        }

        [Fact]
        public async Task SecretListOnlyContainsOneSecretForMultipleVersions()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            await fixture.CreateSecretAsync(secretName, secretName);
            await Task.Delay(1000);
            await fixture.CreateSecretAsync(secretName, secretName);

            var listResponse = await client.GetPropertiesOfSecretsAsync().ToListAsync();
            Assert.Single(listResponse, x => x.Id.ToString().Contains(secretName));
        }

        [Fact]
        public async Task SecretListContainsAttributesForLatestVersion()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            await RequestSetup.CreateMultiple(1, 5, _ => CreateDelayedSecret(secretName, secretName));

            var latestVersion = await fixture.CreateSecretAsync(secretName, secretName);

            var listResponse = await client.GetPropertiesOfSecretsAsync().ToListAsync();

            var secretFromList = listResponse.Single(x => x.Id.ToString().Contains(secretName) && x.CreatedOn == latestVersion.Properties.CreatedOn);

            Assert.Equal(latestVersion.Properties.NotBefore, secretFromList.NotBefore);
            Assert.Equal(latestVersion.Properties.ExpiresOn, secretFromList.ExpiresOn);
            Assert.Equal(latestVersion.Properties.UpdatedOn, secretFromList.UpdatedOn);

            async Task<KeyVaultSecret> CreateDelayedSecret(string secretName, string secretValue)
            {
                var key = await client.SetSecretAsync(secretName, secretValue);

                await Task.Delay(1000);

                return key;
            }
        }

        [Fact]
        public async Task RestoreSecretTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;
            var secretValue = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            var backup = await client.BackupSecretAsync(secretName);

            Assert.NotNull(backup);

            var restored = await client.RestoreSecretBackupAsync(backup.Value);

            Assert.Equal(secret.Name, restored.Value.Name);
            Assert.Equal(secret.Id, restored.Value.Id);
        }

        [Fact]
        public async Task UpdateSecretAppliesChangesTest()
        {
            var client = await fixture.GetClientAsync();

            var secret = await fixture.CreateSecretAsync(fixture.FreshlyGeneratedGuid, fixture.FreshlyGeneratedGuid);

            Assert.True(string.IsNullOrEmpty(secret.Properties.ContentType));

            var newContentType = "application/json";

            secret.Properties.ContentType = newContentType;

            var updated = await client.UpdateSecretPropertiesAsync(secret.Properties);

            Assert.Equal(newContentType, updated.Value.ContentType);
        }

        [Fact]
        public async Task DeleteSecretBehavesCorrectlyTest()
        {
            var client = await fixture.GetClientAsync();

            var secret = await fixture.CreateSecretAsync(fixture.FreshlyGeneratedGuid, fixture.FreshlyGeneratedGuid);

            var deletedOperation = await client.StartDeleteSecretAsync(secret.Name);

            Assert.Equal(deletedOperation.Value.Value, secret.Value);

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secret.Name));
        }

        [Fact]
        public async Task DeletedKeyWillBeMissedInAllKeys()
        {
            var client = await fixture.GetClientAsync();

            var secretNameToDelete = fixture.FreshlyGeneratedGuid;
            var secretNamesToKeep = fixture.FreshlyGeneratedGuid;

            var secretValue = fixture.FreshlyGeneratedGuid;

            async Task<KeyVaultSecret> CreateDelayedSecret(string name, string value)
            {
                var secret = await fixture.CreateSecretAsync(name, value);

                await Task.Delay(1000);

                return secret;
            }

            var createdCount = await RequestSetup.CreateMultiple(1, 3, x => CreateDelayedSecret(secretNamesToKeep, secretValue));

            var secretToDelete = await fixture.CreateSecretAsync(secretNameToDelete, secretValue);

            Assert.NotNull(secretToDelete);
            Assert.Equal(secretNameToDelete, secretToDelete.Name);

            var deletedSecretOp = await client.StartDeleteSecretAsync(secretNameToDelete);

            await deletedSecretOp.WaitForCompletionAsync();

            var inDeletedStore = await client.GetDeletedSecretAsync(secretNameToDelete);

            Assert.NotNull(inDeletedStore.Value);
            Assert.Equal(secretNameToDelete, inDeletedStore.Value.Name);

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secretNameToDelete));

            var allUndeletedSecrets = client.GetPropertiesOfSecretsAsync();

            var existingCount = 0;

            await foreach (var existingSecret in allUndeletedSecrets)
            {
                Assert.NotEqual(secretNameToDelete, existingSecret.Name);

                if(existingSecret.Name.Equals(secretNamesToKeep, StringComparison.InvariantCultureIgnoreCase))
                    existingCount++;
            }

            Assert.Equal(createdCount, existingCount);
        }

        [Fact]
        public async Task CreatingUpdatingAndDeletePurgingSecretWillFlowCorrectly()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;
            var secretValue = fixture.FreshlyGeneratedGuid;

            var createdSecret = await fixture.CreateSecretAsync(secretName, secretValue);

            Assert.Equal(createdSecret.Name, secretName);
            Assert.Equal(createdSecret.Value, secretValue);

            var contentType = "application/text";

            createdSecret.Properties.ContentType = contentType;

            await client.UpdateSecretPropertiesAsync(createdSecret.Properties);

            var updatedSecretResponse = await client.GetSecretAsync(secretName);

            var updatedSecret = updatedSecretResponse.Value;

            Assert.Equal(contentType, updatedSecret.Properties.ContentType);

            var deleteOperation = await client.StartDeleteSecretAsync(secretName);

            await deleteOperation.WaitForCompletionAsync();

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secretName));

            var deletedSecretResponse = await client.GetDeletedSecretAsync(secretName);

            var deletedSecret = deletedSecretResponse.Value;

            Assert.Equal(deletedSecret.Name, secretName);
            Assert.Equal(deletedSecret.Value, secretValue);

            await client.PurgeDeletedSecretAsync(secretName);

            await Assert.RequestFailsAsync(() => client.GetDeletedSecretAsync(secretName));
            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secretName));
        }

        [Fact]
        public async Task CreatedAndOverridingSecretWillReturnUpdatedValue()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;
            var initialValue = fixture.FreshlyGeneratedGuid;
            var overrideValue = fixture.FreshlyGeneratedGuid;

            var initialSecret = await fixture.CreateSecretAsync(secretName, initialValue);

            Assert.Equal(secretName, initialSecret.Name);
            Assert.Equal(initialValue, initialSecret.Value);

            // Ensure underpinning unix time is 100% different
            await Task.Delay(1000);

            var overrideSecret = await fixture.CreateSecretAsync(secretName, overrideValue);

            Assert.Equal(secretName, overrideSecret.Name);
            Assert.Equal(overrideValue, overrideSecret.Value);

            var fromStoreResponse = await client.GetSecretAsync(secretName);

            var fromStore = fromStoreResponse.Value;

            Assert.Equal(secretName, fromStore.Name);
            Assert.Equal(overrideValue, fromStore.Value);
        }

        [Fact]
        public async Task DeletingSecretWithoutVersionWillDeleteAllByName()
        {
            var client = await fixture.GetClientAsync();

            var secretName = "deletedSecretMultiple";
            var firstValue = "deletedFirst";
            var secondValue = "deletedSecond";

            var initialSecret = await fixture.CreateSecretAsync(secretName, firstValue);

            var firstResponse = await client.GetSecretAsync(secretName);

            Assert.Equal(secretName, firstResponse.Value.Name);
            Assert.Equal(firstValue, firstResponse.Value.Value);

            // Force timestamps to be different, race condition in fast compute environments...
            await Task.Delay(1000);

            var secondSecret = await fixture.CreateSecretAsync(secretName, secondValue);

            var secondResponse = await client.GetSecretAsync(secretName);

            Assert.Equal(secretName, secondResponse.Value.Name);
            Assert.Equal(secondValue, secondResponse.Value.Value);

            var deleteOperation = await client.StartDeleteSecretAsync(secretName);

            await deleteOperation.WaitForCompletionAsync();

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secretName));
        }

        [Fact]
        public async Task DeletedSecretWontHaveAnyVersion()
        {
            var client = await fixture.GetClientAsync();

            var secretNameToDelete = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretNameToDelete, "val1");

            Assert.NotNull(secret);
            Assert.Equal(secretNameToDelete, secret.Name);

            var deletedkeyOp = await client.StartDeleteSecretAsync(secretNameToDelete);

            await deletedkeyOp.WaitForCompletionAsync();

            await foreach (var keyVersion in client.GetPropertiesOfSecretVersionsAsync(secretNameToDelete))
            {
                Assert.Fail("No key version should be available for a deleted secret");
            }
        }

        [Fact]
        public async Task RecreatingDeletedSecretReturnConflict()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretName, "val1");

            Assert.NotNull(secret);
            Assert.Equal(secretName, secret.Name);

            var deletedkeyOp = await client.StartDeleteSecretAsync(secretName);

            await deletedkeyOp.WaitForCompletionAsync();

            var exception = await Assert.ThrowsAsync<RequestFailedException>(() => client.SetSecretAsync(secretName, "val2"));

            Assert.Equal((int)HttpStatusCode.Conflict, exception.Status);
        }

        [Fact]
        public async Task GettingCasedSecretIgnoresCaseVariance()
        {
            var client = await fixture.GetClientAsync();

            var secretName = "cAsE-sEnSiTiVe";

            var secret = await fixture.CreateSecretAsync(secretName, "vAlUe");

            Assert.NotNull(secret);
            Assert.Equal(secret.Name, secretName);

            var caseInsensitiveSecretName = "case-sensitive";

            var secretFromStore = await client.GetSecretAsync(caseInsensitiveSecretName);

            Assert.NotNull(secretFromStore.Value);
            Assert.Equal(secret.Value, secretFromStore.Value.Value);
        }

        [Fact]
        public async Task DisablingSecretVersionBlocksReadsUntilReEnabled()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;
            var secretValue = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            secret.Properties.Enabled = false;

            var disabled = await client.UpdateSecretPropertiesAsync(secret.Properties);

            Assert.False(disabled.Value.Enabled);
            Assert.Equal(secret.Id, disabled.Value.Id);
            Assert.Equal(secret.Properties.Version, disabled.Value.Version);
            Assert.Equal(secret.Properties.CreatedOn, disabled.Value.CreatedOn);

            var forbidden = await Assert.ThrowsAsync<RequestFailedException>(() => client.GetSecretAsync(secretName, secret.Properties.Version));
            Assert.Equal((int)HttpStatusCode.Forbidden, forbidden.Status);
            Assert.Equal("Forbidden", forbidden.ErrorCode);

            var versions = await client.GetPropertiesOfSecretVersionsAsync(secretName).ToListAsync();
            Assert.False(Assert.Single(versions).Enabled);

            secret.Properties.Enabled = true;

            await client.UpdateSecretPropertiesAsync(secret.Properties);

            var reEnabled = await client.GetSecretAsync(secretName, secret.Properties.Version);

            Assert.True(reEnabled.Value.Properties.Enabled);
            Assert.Equal(secretValue, reEnabled.Value.Value);
            Assert.Equal(secret.Id, reEnabled.Value.Id);
        }

        [Fact]
        public async Task DisabledSecretReadReturnsSecretDisabledInnerError()
        {
            var client = await fixture.GetClientAsync();

            var secret = await fixture.CreateSecretAsync(fixture.FreshlyGeneratedGuid, fixture.FreshlyGeneratedGuid);

            secret.Properties.Enabled = false;
            await client.UpdateSecretPropertiesAsync(secret.Properties);

            using var response = await SendAsync(HttpMethod.Get, $"secrets/{secret.Name}/{secret.Properties.Version}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var error = body.RootElement.GetProperty("error");

            Assert.Equal("Forbidden", error.GetProperty("code").GetString());
            Assert.Equal("SecretDisabled", error.GetProperty("innererror").GetProperty("code").GetString());
        }

        [Theory]
        [InlineData("""{"contentType":"text/plain"}""")]
        [InlineData("""{"attributes":{}}""")]
        [InlineData("""{"attributes":{"enabled":null}}""")]
        public async Task UpdateSecretWithoutEnabledKeepsStoredFlag(string patchBody)
        {
            var client = await fixture.GetClientAsync();

            var secret = await fixture.CreateSecretAsync(fixture.FreshlyGeneratedGuid, fixture.FreshlyGeneratedGuid);

            secret.Properties.Enabled = false;
            await client.UpdateSecretPropertiesAsync(secret.Properties);

            using var response = await SendAsync(HttpMethod.Patch, $"secrets/{secret.Name}/{secret.Properties.Version}", patchBody);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var versions = await client.GetPropertiesOfSecretVersionsAsync(secret.Name).ToListAsync();
            Assert.False(Assert.Single(versions).Enabled);
        }

        [Fact]
        public async Task DisablingOlderVersionLeavesLatestVersionReadable()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            var older = await fixture.CreateSecretAsync(secretName, fixture.FreshlyGeneratedGuid);
            await Task.Delay(1000);
            var latest = await fixture.CreateSecretAsync(secretName, fixture.FreshlyGeneratedGuid);

            older.Properties.Enabled = false;
            await client.UpdateSecretPropertiesAsync(older.Properties);

            var fromStore = await client.GetSecretAsync(secretName);
            Assert.Equal(latest.Properties.Version, fromStore.Value.Properties.Version);
            Assert.Equal(latest.Value, fromStore.Value.Value);
            Assert.True(fromStore.Value.Properties.Enabled);

            var forbidden = await Assert.ThrowsAsync<RequestFailedException>(() => client.GetSecretAsync(secretName, older.Properties.Version));
            Assert.Equal((int)HttpStatusCode.Forbidden, forbidden.Status);
        }

        [Fact]
        public async Task DisabledLatestVersionBlocksNameOnlyReadWithoutFallback()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            var older = await fixture.CreateSecretAsync(secretName, fixture.FreshlyGeneratedGuid);
            await Task.Delay(1000);
            var latest = await fixture.CreateSecretAsync(secretName, fixture.FreshlyGeneratedGuid);

            latest.Properties.Enabled = false;
            await client.UpdateSecretPropertiesAsync(latest.Properties);

            var forbidden = await Assert.ThrowsAsync<RequestFailedException>(() => client.GetSecretAsync(secretName));
            Assert.Equal((int)HttpStatusCode.Forbidden, forbidden.Status);

            var olderFromStore = await client.GetSecretAsync(secretName, older.Properties.Version);
            Assert.Equal(older.Value, olderFromStore.Value.Value);

            var listed = await client.GetPropertiesOfSecretsAsync().ToListAsync();
            var entry = Assert.Single(listed, x => x.Name == secretName);
            Assert.Equal(latest.Properties.CreatedOn, entry.CreatedOn);
            Assert.False(entry.Enabled);
        }

        [Fact]
        public async Task UpdatingOlderVersionDoesNotPromoteItToLatest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            var older = await fixture.CreateSecretAsync(secretName, fixture.FreshlyGeneratedGuid);
            await Task.Delay(1000);
            var latest = await fixture.CreateSecretAsync(secretName, fixture.FreshlyGeneratedGuid);

            // Ensure the update timestamp lands after the latest version's creation time.
            await Task.Delay(1000);

            older.Properties.ContentType = "text/plain";
            await client.UpdateSecretPropertiesAsync(older.Properties);

            var fromStore = await client.GetSecretAsync(secretName);
            Assert.Equal(latest.Properties.Version, fromStore.Value.Properties.Version);

            var listed = await client.GetPropertiesOfSecretsAsync().ToListAsync();
            var entry = Assert.Single(listed, x => x.Name == secretName);
            Assert.Equal(latest.Properties.CreatedOn, entry.CreatedOn);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? json = null)
        {
            var http = await fixture.CreateHttpClient();

            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new("Bearer", await fixture.GetBearerTokenAsync());

            if (json is not null)
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await http.SendAsync(request);
        }
    }
}
