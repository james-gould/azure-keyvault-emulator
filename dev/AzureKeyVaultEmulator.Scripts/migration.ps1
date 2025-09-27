# Azure Key Vault Emulator - Entity Framework Migration Script
# This script contains the commands used to create and manage database migrations

# Prerequisites:
# 1. Install .NET 9.0 SDK
# 2. Install EF Core tools: dotnet tool install --global dotnet-ef

# Navigate to the main application project (where DbContext is configured via DI)
Set-Location "src/AzureKeyVaultEmulator"

# Create a new migration with a dynamically generated name
$migrationName = "Migration_" + [System.Guid]::NewGuid().ToString("N").Substring(0, 8)
dotnet ef migrations add $migrationName --context VaultContext