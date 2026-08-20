// PoC: prove the azure-keyvault-emulator (post-PR-#446) accepts the same
// authentication path External Secrets Operator's azurekv provider uses,
// namely azidentity.WorkloadIdentityCredential -> azsecrets.Client.
//
// Mirrors what ESO does in
// external-secrets/pkg/provider/azure/keyvault: it constructs a
// WorkloadIdentityCredential with the ClientID/TenantID/TokenFilePath the
// Azure Workload Identity webhook injects, then hands that credential to
// the Azure Go SDK's keyvault clients.
package main

import (
	"context"
	"crypto/tls"
	"crypto/x509"
	"encoding/base64"
	"fmt"
	"net/http"
	"os"
	"time"

	"github.com/Azure/azure-sdk-for-go/sdk/azcore"
	"github.com/Azure/azure-sdk-for-go/sdk/azcore/cloud"
	"github.com/Azure/azure-sdk-for-go/sdk/azcore/policy"
	"github.com/Azure/azure-sdk-for-go/sdk/azidentity"
	"github.com/Azure/azure-sdk-for-go/sdk/security/keyvault/azsecrets"
)

const (
	emulatorURL = "https://localhost:4997"
	tenantID    = "a0c2a3f5-e1b3-4d6a-9c41-2cdd1f2c7e0f"
	clientID    = "a0c2a3f5-e1b3-4d6a-9c41-2cdd1f2c7e0f"
)

func must(err error, msg string) {
	if err != nil {
		fmt.Fprintf(os.Stderr, "[FAIL] %s: %v\n", msg, err)
		os.Exit(1)
	}
}

func b64url(s string) string { return base64.RawURLEncoding.EncodeToString([]byte(s)) }

// writeFakeSAToken writes a Kubernetes-service-account-shaped JWT to a temp
// file. The emulator does not validate it; the workload-identity flow just
// needs *some* file at AZURE_FEDERATED_TOKEN_FILE.
func writeFakeSAToken() string {
	header := b64url(`{"alg":"RS256","kid":"poc","typ":"JWT"}`)
	payload := b64url(fmt.Sprintf(
		`{"iss":"https://kubernetes.default.svc","sub":"system:serviceaccount:eso:eso-controller","aud":"api://AzureADTokenExchange","exp":%d}`,
		time.Now().Add(time.Hour).Unix()))
	sig := b64url("not-a-real-signature")
	f, err := os.CreateTemp("", "sa-token-*.jwt")
	must(err, "create temp")
	_, _ = f.WriteString(header + "." + payload + "." + sig)
	_ = f.Close()
	return f.Name()
}

// trustingTransport returns a policy.Transporter that trusts the emulator's
// self-signed dev cert.
type httpTransport struct{ c *http.Client }

func (t httpTransport) Do(r *http.Request) (*http.Response, error) { return t.c.Do(r) }

func trustingTransport() policy.Transporter {
	pool, _ := x509.SystemCertPool()
	if pool == nil {
		pool = x509.NewCertPool()
	}
	pem, err := os.ReadFile("/tmp/certs/emulator.crt")
	must(err, "read emulator.crt")
	if !pool.AppendCertsFromPEM(pem) {
		fmt.Fprintln(os.Stderr, "[FAIL] could not append cert")
		os.Exit(1)
	}
	return httpTransport{&http.Client{Transport: &http.Transport{TLSClientConfig: &tls.Config{RootCAs: pool}}}}
}

func main() {
	tokenFile := writeFakeSAToken()
	fmt.Println("[step 1] wrote fake k8s SA JWT to", tokenFile)

	transport := trustingTransport()

	// Cloud config pointing the credential at the emulator instead of Entra.
	// ESO sets AZURE_AUTHORITY_HOST via the workload-identity webhook; using
	// an explicit cloud.Configuration here is equivalent and avoids polluting
	// the process env.
	emuCloud := cloud.Configuration{
		ActiveDirectoryAuthorityHost: emulatorURL + "/",
		Services:                     map[cloud.ServiceName]cloud.ServiceConfiguration{},
	}

	// === Exact credential ESO's azurekv provider uses for WorkloadIdentity ===
	cred, err := azidentity.NewWorkloadIdentityCredential(&azidentity.WorkloadIdentityCredentialOptions{
		ClientID:      clientID,
		TenantID:      tenantID,
		TokenFilePath: tokenFile,
		ClientOptions: azcore.ClientOptions{
			Transport: transport,
			Cloud:     emuCloud,
		},
		DisableInstanceDiscovery: true,
	})
	must(err, "construct WorkloadIdentityCredential")
	fmt.Println("[step 2] constructed azidentity.WorkloadIdentityCredential")

	// Sanity-check: pull a token directly (this is the POST to
	//   {emulator}/{tenant}/oauth2/v2.0/token
	// carrying grant_type=client_credentials + client_assertion_type=...:jwt-bearer
	// + client_assertion=<SA JWT>).
	tok, err := cred.GetToken(context.Background(), policy.TokenRequestOptions{
		Scopes: []string{"https://vault.azure.net/.default"},
	})
	must(err, "WorkloadIdentityCredential.GetToken")
	fmt.Printf("[step 3] got bearer token (head=%s...), expires %s\n",
		tok.Token[:24], tok.ExpiresOn.UTC().Format(time.RFC3339))

	// === Real AKV SDK call using that credential ===
	client, err := azsecrets.NewClient(emulatorURL, cred, &azsecrets.ClientOptions{
		ClientOptions: azcore.ClientOptions{
			Transport: transport,
			Cloud:     emuCloud,
		},
		DisableChallengeResourceVerification: true,
	})
	must(err, "construct azsecrets.Client")
	fmt.Println("[step 4] constructed azsecrets.Client (DisableChallengeResourceVerification=true)")

	ctx := context.Background()
	name := "eso-poc-secret"
	val := "hello-from-external-secrets-flow"

	_, err = client.SetSecret(ctx, name, azsecrets.SetSecretParameters{Value: &val}, nil)
	must(err, "SetSecret")
	fmt.Println("[step 5] SetSecret OK")

	got, err := client.GetSecret(ctx, name, "", nil)
	must(err, "GetSecret")
	if got.Value == nil || *got.Value != val {
		fmt.Fprintf(os.Stderr, "[FAIL] value mismatch: %v\n", got.Value)
		os.Exit(1)
	}
	fmt.Printf("[step 6] GetSecret OK: value=%q\n", *got.Value)

	fmt.Println()
	fmt.Println("=========================================================")
	fmt.Println(" PoC PASSED")
	fmt.Println(" WorkloadIdentityCredential -> azsecrets round-trip")
	fmt.Println(" succeeded against the emulator. This is the exact code")
	fmt.Println(" path External Secrets Operator's azurekv provider uses.")
	fmt.Println("=========================================================")
}
