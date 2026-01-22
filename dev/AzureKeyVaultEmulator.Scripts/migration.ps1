# Azure Key Vault Emulator - Entity Framework Migration Script
# This script contains the commands used to create and manage database migrations

# Prerequisites:
# 1. Install .NET 9.0 SDK
# 2. Install EF Core tools: dotnet tool install --global dotnet-ef

# Create a new migration with a dynamically generated name
$migrationName = "Migration_" + [System.Guid]::NewGuid().ToString("N").Substring(0, 8)
# Use explicit .csproj paths so `dotnet ef` can locate SDK-style projects reliably.
# Adjust the paths below if your solution layout or project names change.
dotnet ef migrations add $migrationName --project src/AzureKeyVaultEmulator.Shared/AzureKeyVaultEmulator.Shared.csproj --startup-project src/AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj --context VaultContext