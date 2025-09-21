# Azure Key Vault Emulator - Entity Framework Migration Script
# This script contains the commands used to create and manage database migrations

# Prerequisites:
# 1. Install .NET 9.0 SDK
# 2. Install EF Core tools: dotnet tool install --global dotnet-ef

# Navigate to the main application project (where DbContext is configured via DI)
Set-Location "src/AzureKeyVaultEmulator"

# Create the initial migration (command used to create 20250921172203_InitialCreate.cs)
# Note: This command was actually run from the main application project, not the shared project
# because the VaultContext is properly configured there via dependency injection
dotnet ef migrations add InitialCreate --context VaultContext

# Other useful migration commands:

# List all migrations
# dotnet ef migrations list --context VaultContext

# Remove the last migration (if needed)
# dotnet ef migrations remove --context VaultContext

# Create a new migration after model changes
# dotnet ef migrations add YourMigrationName --context VaultContext

# Apply migrations to database (done automatically in Program.cs)
# dotnet ef database update --context VaultContext

# Generate SQL script for migrations
# dotnet ef migrations script --context VaultContext