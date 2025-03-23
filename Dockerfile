# Use official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and restore dependencies
COPY AzureKeyVaultEmulator.sln ./
COPY AzureKeyVaultEmulator/*.csproj AzureKeyVaultEmulator/
RUN dotnet restore AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj

# Copy everything and build
COPY . ./
WORKDIR /app/AzureKeyVaultEmulator
RUN dotnet publish -c Release -o /out

# Use runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

# Set the entry point (adjust based on your application)
ENTRYPOINT ["dotnet", "AzureKeyVaultEmulator.dll"]
