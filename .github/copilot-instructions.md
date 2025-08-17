# Azure Key Vault Emulator

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

The Azure Key Vault Emulator is a .NET 9.0 ASP.NET Core Web API that mimics the full Azure Key Vault API, supporting secrets, keys, and certificates. It runs locally using Docker or directly with the .NET CLI.

## Working Effectively

### Prerequisites and Setup
- Install .NET 9.0 SDK: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 9.0.102`
- Add to PATH: `export PATH="$HOME/.dotnet:$PATH"`
- Verify installation: `dotnet --version`
- Install Docker for container-based testing and deployment

### Build and Restore Process
- **CRITICAL**: Set appropriate timeouts for all operations. NEVER CANCEL builds or long-running commands.
- Restore dependencies: `dotnet restore AzureKeyVaultEmulator.sln` -- takes 2-3 minutes. NEVER CANCEL. Set timeout to 300+ seconds.
- Build solution: `dotnet build AzureKeyVaultEmulator.sln --configuration Release` -- takes 25-30 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
- Build main project only: `dotnet build src/AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj --configuration Release` -- takes 2-3 seconds.

### SSL Certificate Setup (Required)
The emulator requires SSL certificates to function correctly. Choose one method:

#### Method 1: Using dotnet dev-certs (Recommended for Development)
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
mkdir -p /tmp/certs && cd /tmp/certs
dotnet dev-certs https -ep ./emulator.crt -p emulator -q
dotnet dev-certs https -ep ./emulator.pfx -p emulator -q
```

#### Method 2: Automated Setup Script
```bash
# Linux/macOS: Requires sudo for certificate installation
sudo bash <(curl -fsSL https://raw.githubusercontent.com/james-gould/azure-keyvault-emulator/refs/heads/master/docs/setup.sh)

# Windows: Use Git Bash
bash <(curl -fsSL https://raw.githubusercontent.com/james-gould/azure-keyvault-emulator/refs/heads/master/docs/setup.sh)
```

### Running the Emulator

#### Direct .NET Execution
```bash
cd src/AzureKeyVaultEmulator
ASPNETCORE_URLS=https://+:4997 \
ASPNETCORE_Kestrel__Certificates__Default__Path=/tmp/certs/emulator.pfx \
ASPNETCORE_Kestrel__Certificates__Default__Password=emulator \
dotnet run --configuration Release
```

#### Docker Container (Recommended)
```bash
# Using pre-built image
docker run -d -p 4997:4997 -v /tmp/certs:/certs -e Persist=true jamesgoulddev/azure-keyvault-emulator:latest

# Check status
curl -k -s -w "%{http_code}" https://localhost:4997/keys?api-version=7.0
```

### Testing Process
- **CRITICAL**: Tests can take 15-45 seconds. NEVER CANCEL. Set timeout to 600+ seconds for safety.
- Run TestContainers tests: `dotnet test ./test/AzureKeyVaultEmulator.TestContainers.Tests/ --configuration Release --logger trx` -- takes 15-20 seconds.
- Run integration tests: `dotnet test ./test/AzureKeyVaultEmulator.IntegrationTests/ --configuration Release --logger trx` -- takes 40-50 seconds.
- **NOTE**: Some tests may fail in CI environments due to orchestration/container issues. This is expected in sandboxed environments.

## Validation Scenarios

### After Making Changes, Always:
1. **Build validation**: Run `dotnet build AzureKeyVaultEmulator.sln --configuration Release` and ensure it succeeds.
2. **Application startup test**: 
   - Set up SSL certificates using Method 1 above
   - Start the emulator using Direct .NET Execution method
   - Verify it starts on port 4997 and shows "Application started" message
   - Test basic API: `curl -k -s -w "%{http_code}" https://localhost:4997/keys?api-version=7.0` should return 401 (authentication required)
3. **Docker functionality test**:
   - Pull and run the pre-built image: `docker run -d -p 4997:4997 -v /tmp/certs:/certs jamesgoulddev/azure-keyvault-emulator:latest`
   - Verify container starts and API responds
4. **Code quality validation**: The project uses strict compiler settings with TreatWarningsAsErrors=true and EnforceCodeStyleInBuild=true.

### Manual End-to-End Scenario Testing
When testing functionality changes, exercise these scenarios:
1. **Secret Management**: Create, retrieve, update, and delete secrets using Azure SDK clients
2. **Key Operations**: Generate, import, and use cryptographic keys
3. **Certificate Lifecycle**: Create certificates, manage policies, and handle operations
4. **Persistence Testing**: Verify data persists between restarts when using `-e Persist=true`

## Key Architecture Details

### Project Structure
```
src/
├── AzureKeyVaultEmulator/           # Main Web API application
├── AzureKeyVaultEmulator.Shared/    # Common utilities and models  
├── AzureKeyVaultEmulator.Client/    # Client library for easy integration
├── AzureKeyVaultEmulator.Aspire.Hosting/  # .NET Aspire integration
└── TestContainers/dotnet/          # TestContainers module

test/
├── AzureKeyVaultEmulator.IntegrationTests/      # API integration tests
├── AzureKeyVaultEmulator.TestContainers.Tests/  # Container-based tests  
└── AzureKeyVaultEmulator.Wiremock.IntegrationTests/  # Mock service tests
```

### Important Configuration
- **Port**: Fixed at 4997 (not configurable)
- **SSL**: Always required, certificates must be trusted
- **Persistence**: Optional SQLite database with `-e Persist=true`
- **Authentication**: Emulated - no real Azure AD integration required

### Common Development Tasks
- **Main application**: `src/AzureKeyVaultEmulator/` contains controllers and API logic
- **Business logic**: `src/AzureKeyVaultEmulator.Shared/` contains core functionality
- **Client integration**: `src/AzureKeyVaultEmulator.Client/` for Azure SDK integration helpers
- **Testing**: Focus on `test/AzureKeyVaultEmulator.IntegrationTests/` for API behavior validation

## Build Timing Reference
- **dotnet restore**: 2-3 minutes (140-180 seconds) 
- **dotnet build (full solution)**: 25-30 seconds
- **dotnet build (single project)**: 2-5 seconds
- **Integration tests**: 40-50 seconds
- **TestContainer tests**: 15-20 seconds
- **Application startup**: 2-5 seconds after certificate setup

Always use these timing expectations when setting timeout values for automated processes.

## Troubleshooting
- **Certificate errors**: Ensure SSL certificates are generated and trusted. Use `dotnet dev-certs https --trust`.
- **Port conflicts**: Emulator always uses port 4997. Stop existing processes using this port.
- **Docker build issues**: Use pre-built image `jamesgoulddev/azure-keyvault-emulator:latest` instead of local building.
- **Test failures**: Some tests may fail in constrained CI environments due to orchestration timeouts. Focus on build success and manual validation.
- **Authentication issues**: API returns 401 without authentication - this is expected emulator behavior.