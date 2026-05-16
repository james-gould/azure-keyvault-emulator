# External Secrets Operator (ESO) integration — investigation

> Tracking issue: [#316](https://github.com/james-gould/azure-keyvault-emulator/issues/316)
> Prerequisite PR (merged): [#446](https://github.com/james-gould/azure-keyvault-emulator/pull/446) — *Permit `DefaultAzureCredential` against the emulator*
>
> Status: **Feasible.** The auth surface added in #446 is sufficient for the token-acquisition half of the ESO + Azure Workload Identity flow. The remaining work is k8s integration plumbing (sample/MVP, TLS trust, env wiring) plus validating one Go-SDK challenge knob.

---

## 1. What ESO + Azure Workload Identity actually does

The [External Secrets Operator](https://github.com/external-secrets/external-secrets) `azurekv` provider supports four `authType`s. The interesting one here is `WorkloadIdentity`, which mirrors the [Azure Workload Identity](https://azure.github.io/azure-workload-identity/docs/) project.

Concretely, at runtime the ESO controller pod (or the per-`SecretStore` `serviceAccount`) ends up doing:

1. A k8s-projected ServiceAccount JWT is mounted at `AZURE_FEDERATED_TOKEN_FILE` (the workload-identity webhook injects `AZURE_AUTHORITY_HOST`, `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_FEDERATED_TOKEN_FILE` env vars).
2. The Azure Go SDK's `azidentity.WorkloadIdentityCredential` posts that JWT to the AAD token endpoint:
   ```
   POST {AZURE_AUTHORITY_HOST}/{tenant}/oauth2/v2.0/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=client_credentials
   client_id={AZURE_CLIENT_ID}
   scope=https://vault.azure.net/.default
   client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer
   client_assertion={contents of AZURE_FEDERATED_TOKEN_FILE}
   ```
3. AAD returns an `access_token`; the SDK attaches it as `Authorization: Bearer …` on Key Vault REST calls.
4. The Key Vault REST call may trigger a `WWW-Authenticate` challenge; the SDK re-derives the authority/scope from the challenge and retries (this is what `DisableChallengeResourceVerification` relaxes on the client side).

So, to host ESO locally against the emulator, the emulator needs to (a) accept that token POST, (b) accept the resulting bearer token on AKV REST calls, and (c) emit a usable challenge.

## 2. What PR #446 added

PR #446 (already merged on `master`) stood up an Entra-shaped OAuth2/OIDC surface inside the emulator. The endpoints live in [`OAuthController`](../src/AzureKeyVaultEmulator/Emulator/Controllers/OAuthController.cs):

| Endpoint | Behaviour |
| --- | --- |
| `GET /common/discovery/instance` | MSAL instance discovery, points back at the emulator. |
| `GET /{tenantId}/v2.0/.well-known/openid-configuration` | OIDC metadata document, `token_endpoint = {emulator}/{tenant}/oauth2/v2.0/token`. |
| `POST /{tenantId}/oauth2/v2.0/token` | **Unconditionally** issues a `Bearer` token. Form parameters (`grant_type`, `client_id`, `client_secret`, `client_assertion`, `client_assertion_type`, `scope`, …) are not parsed or validated. |
| `GET /{tenantId}/discovery/v2.0/keys` | Empty JWKS stub. |

[`AuthenticationSetup.AddConfiguredAuthentication`](../src/AzureKeyVaultEmulator/ApiConfiguration/AuthenticationSetup.cs) keeps `ValidateIssuer/Audience/Lifetime/IssuerSigningKey` all `false` and installs a no-op `SignatureValidator`, so any token minted by `OAuthController.IssueToken` round-trips through the JwtBearer pipeline. The `OnChallenge` event re-writes `WWW-Authenticate` to:

```
Bearer authorization="{scheme}://{host}/{tenantId}",
       scope="https://vault.azure.net/.default",
       resource="https://vault.azure.net"
```

where `tenantId` is read from `AZURE_TENANT_ID` (host-machine fallthrough) or falls back to the fixed emulator GUID.

## 3. Why this is enough for ESO's auth flow

Mapping the ESO/WorkloadIdentity steps from §1 against the surface in §2:

| ESO/SDK step | Emulator support |
| --- | --- |
| POST to `{AZURE_AUTHORITY_HOST}/{tenant}/oauth2/v2.0/token` with `grant_type=client_credentials` + `client_assertion_type=…:jwt-bearer` + `client_assertion=<SA JWT>` | ✅ — `IssueToken` doesn't read the body; **any** POST to that route returns `{ token_type: "Bearer", expires_in: 3600, access_token: <jwt> }`. The Kubernetes service-account JWT does not need to be cryptographically validatable against AAD — it doesn't even need to be a real SA token for emulator purposes. |
| Re-use the access token on `https://{vaultUri}/secrets/...` | ✅ — JwtBearer accepts anything `OAuthController` issued (validation is disabled and the signature validator is a no-op). |
| AKV challenge → re-derive authority/scope | ✅ — the rewritten `WWW-Authenticate` advertises the emulator itself as the authority. |
| AKV challenge → resource-match check (`vault.azure.net` ↔ request host) | ⚠️ — The .NET SDK exposes `*ClientOptions.DisableChallengeResourceVerification`. The Go AKV SDK used by ESO has the same knob (`azkeys/azsecrets ClientOptions.DisableChallengeResourceVerification`). ESO would need to pass it through — see §5. |

Crucially, **#446 already removed the blocker** that made the workload-identity path conceptually unreachable: there was previously no AAD-shaped token endpoint the SDK could even POST to. There is now, and because the implementation deliberately ignores the request body, every credential type that ends up at the token endpoint (`ClientSecretCredential`, `ClientAssertionCredential`, `WorkloadIdentityCredential`, `ManagedIdentityCredential` once it falls through to IMDS, …) collapses to the same successful path.

## 4. Known gaps / open items

These are the items not yet covered by #446 — none are blockers, all are integration work.

1. **No MVP / sample**. Issue #316 explicitly asks for one. Proposed shape in §5.
2. **TLS trust inside the cluster**. The emulator ships a self-signed cert; ESO pods need either:
   - The cert added to `caBundle`/`caProvider` on the ESO `SecretStore`, or
   - The cert mounted into the controller's CA trust store, or
   - A reverse-proxy in front of the emulator terminating TLS with a cluster-trusted cert.
3. **`AZURE_AUTHORITY_HOST` shape**. `WorkloadIdentityCredential` builds `${AZURE_AUTHORITY_HOST}/${tenant}/oauth2/v2.0/token`. A value like `https://kv-emulator.kv-emulator.svc.cluster.local:4997/` (no trailing path) is what we want; we should document this explicitly. The emulator already accepts `AZURE_TENANT_ID` from the host, so the tenant segment is consistent with the challenge.
4. **Go SDK challenge-resource verification**. ESO's azurekv provider currently constructs the AKV client with default `ClientOptions`. To support a non-`*.vault.azure.net` URL it needs `ClientOptions.DisableChallengeResourceVerification = true`. This is an *ESO-side* change, not an emulator change; it parallels the `DisableChallengeResourceVerification` story we already document for the .NET SDK in the main README.
   - Worth filing upstream at `external-secrets/external-secrets` — *"Allow disabling challenge resource verification for `azurekv` provider when targeting an emulator"* — referencing this doc.
5. **`vaultUrl` schema validation**. The `azurekv` provider's `vaultUrl` field accepts any URL; smoke-test against `https://kv-emulator.<ns>.svc.cluster.local:4997` to confirm no implicit `*.vault.azure.net` regex.
6. **Federated identity setup is a no-op against the emulator**. Because the emulator never inspects `client_assertion`, there is no AAD app registration, federated credential, or trust relationship to configure. The workload-identity webhook just needs to inject *some* projected SA token at `AZURE_FEDERATED_TOKEN_FILE` (the default behaviour given the right SA annotations).

## 5. Proposed MVP

A minimal end-to-end sample that lives under `samples/external-secrets/` (new directory) with:

```
samples/external-secrets/
├── README.md                       # walkthrough (kind / k3d)
├── 00-kind-cluster.yaml            # optional: cluster config exposing :4997
├── 10-emulator.yaml                # Deployment + Service + Secret (cert) for the emulator
├── 20-workload-identity.yaml       # SA annotated with azure.workload.identity/client-id
├── 30-secretstore.yaml             # ESO SecretStore (authType: WorkloadIdentity, vaultUrl: https://kv-emulator.<ns>.svc.cluster.local:4997)
├── 40-externalsecret.yaml          # ExternalSecret referencing a secret pre-seeded into the emulator
└── seed-secrets.sh                 # uses az CLI / curl to preload a secret into the emulator
```

Validation steps (manual, once the YAML is in place):

1. `kind create cluster --config 00-kind-cluster.yaml`
2. Install Azure Workload Identity webhook (`helm install workload-identity-webhook …`) and ESO (`helm install external-secrets …`).
3. `kubectl apply -f samples/external-secrets/`
4. `./seed-secrets.sh` to write `demo-secret` into the emulator.
5. `kubectl get externalsecret demo-secret` should reach `SecretSynced`; `kubectl get secret demo-secret -o yaml` should contain the value.

The smoke test exercises exactly the auth path #446 unblocked plus the AKV REST surface the emulator already implements. **No emulator code changes are expected.** If smoke-test step 4 fails on challenge-resource verification, that confirms gap §4.4 above and motivates the upstream ESO change.

## 6. Recommended follow-ups

1. **Open a follow-up issue** ("MVP: External Secrets sample") owned by this repo, to land `samples/external-secrets/` and the matching README.
2. **Open an upstream ESO issue** ("Expose `DisableChallengeResourceVerification` for azurekv against emulators") referencing this writeup. This is the most likely real-world blocker once the sample is wired up.
3. **No emulator API rework is required** for ESO's stated needs. The OAuth surface introduced in #446 is sufficient; we should resist the temptation to start parsing `client_assertion` form values, because the emulator's value proposition is *exactly* that it accepts every credential type without configuration.

## 7. TL;DR

> *"With the recent changes in #446 this may now be possible…"* — confirmed yes. The emulator now hosts the AAD-shaped token endpoint that `WorkloadIdentityCredential` requires, and because that endpoint and the JwtBearer pipeline both intentionally skip validation, ESO's workload-identity auth flow will transparently succeed against the emulator. What's left is a sample/MVP and a small upstream ESO knob for challenge-resource verification, neither of which require changes to the emulator itself.
