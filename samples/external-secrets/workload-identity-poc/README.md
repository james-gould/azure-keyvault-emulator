# External Secrets Operator — workload-identity PoC

This sample is the proof-of-concept that closes the investigation in
[`docs/EXTERNAL_SECRETS_INVESTIGATION.md`](../../../docs/EXTERNAL_SECRETS_INVESTIGATION.md).

It runs the exact authentication path the [External Secrets Operator
(ESO)](https://github.com/external-secrets/external-secrets) `azurekv` provider
uses when `authType: WorkloadIdentity` is selected — namely
`azidentity.WorkloadIdentityCredential` from the Azure Go SDK, pointed at the
emulator's OAuth2 surface (introduced in
[#446](https://github.com/james-gould/azure-keyvault-emulator/pull/446)), used
to drive `azsecrets.Client` against the emulator's Key Vault REST API.

If this PoC succeeds, the auth half of ESO's flow works against the emulator.
The only ESO-specific concern that remains is on the ESO side
(`DisableChallengeResourceVerification` for the Go AKV SDK) — see the
investigation doc.

## What it does

1. Writes a Kubernetes-service-account-shaped JWT to a temp file. This stands
   in for `AZURE_FEDERATED_TOKEN_FILE`, which the Azure Workload Identity
   webhook would otherwise mount into ESO's pod. The emulator does not
   validate this token; any well-formed JWT works.
2. Constructs `azidentity.WorkloadIdentityCredential` with
   `ClientID` / `TenantID` / `TokenFilePath` and a `cloud.Configuration`
   whose `ActiveDirectoryAuthorityHost` is the emulator URL.
3. Acquires a bearer token (this is the
   `POST {emulator}/{tenant}/oauth2/v2.0/token` with
   `grant_type=client_credentials` +
   `client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer` +
   `client_assertion=<SA JWT>` that ESO performs).
4. Constructs `azsecrets.Client` with
   `DisableChallengeResourceVerification = true` (required because the
   emulator's host is not `*.vault.azure.net`).
5. Round-trips `SetSecret` + `GetSecret` to prove the bearer token is
   accepted on the AKV REST API.

## Running it

Prerequisites: Docker, Go ≥ 1.24, .NET 10 SDK (only for generating dev
certificates — see [`docs/setup.sh`](../../../docs/setup.sh) for the same
flow).

```bash
# 1. Dev certs
dotnet dev-certs https --clean
mkdir -p /tmp/certs && cd /tmp/certs
dotnet dev-certs https -ep ./emulator.crt --format PEM -p emulator -q
dotnet dev-certs https -ep ./emulator.pfx -p emulator -q

# 2. Emulator (either pull jamesgoulddev/azure-keyvault-emulator:latest or
#    build from this repo's Dockerfile)
docker run -d --name akv-poc -p 4997:4997 -v /tmp/certs:/certs:ro \
  jamesgoulddev/azure-keyvault-emulator:latest

# 3. PoC
cd samples/external-secrets/workload-identity-poc
go run .
```

Expected tail of output:

```
[step 5] SetSecret OK
[step 6] GetSecret OK: value="hello-from-external-secrets-flow"

=========================================================
 PoC PASSED
 WorkloadIdentityCredential -> azsecrets round-trip
 succeeded against the emulator. This is the exact code
 path External Secrets Operator's azurekv provider uses.
=========================================================
```

## Mapping back to a real ESO deployment

| In ESO + Workload Identity on k8s        | In this PoC                                |
| ---------------------------------------- | ------------------------------------------ |
| `AZURE_FEDERATED_TOKEN_FILE` projected   | `writeFakeSAToken()` writes a temp JWT     |
| `AZURE_CLIENT_ID` (SA annotation)        | `clientID` constant                        |
| `AZURE_TENANT_ID` (SA annotation)        | `tenantID` constant                        |
| `AZURE_AUTHORITY_HOST` (webhook env)     | `cloud.Configuration.ActiveDirectoryAuthorityHost` |
| `SecretStore.spec.provider.azurekv.vaultUrl` | `emulatorURL`                          |
| `ExternalSecret` refresh → `GetSecret`   | `client.GetSecret(...)`                    |

The PoC is intentionally a single Go file so anyone wanting to wire up a real
in-cluster sample (kind/k3d + the workload-identity webhook + ESO charts +
`SecretStore`/`ExternalSecret` manifests) can use the same env-var values and
expect the same behaviour from the emulator side.
