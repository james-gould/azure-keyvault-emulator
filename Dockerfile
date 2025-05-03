# Use official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and restore dependencies
COPY AzureKeyVaultEmulator.sln ./
COPY AzureKeyVaultEmulator/*.csproj AzureKeyVaultEmulator/
COPY AzureKeyVaultEmulator.Shared/*.csproj AzureKeyVaultEmulator.Shared/
RUN dotnet restore AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj

# Copy everything and build
COPY . ./
WORKDIR /app/AzureKeyVaultEmulator
RUN dotnet publish -c Release -o /out

# Use runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install ca-certificates and openssl to handle .crt/.pfx
RUN apt-get update && \
    apt-get install -y ca-certificates openssl && \
    update-ca-certificates

# Sets the ENV VARS for ASP.NET Core to expect these certificates
ENV ASPNETCORE_URLS=https://+:4997
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/certs/emulator.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=emulator

# Installs the crt to the trust store
# Or fails and prevents the container from spinning up.
RUN bash -c '[ -f /certs/emulator.crt ] && cp /certs/emulator.crt /usr/local/share/ca-certificates/emulator.crt && update-ca-certificates'

# Copy published .NET application
COPY --from=build /out ./

# Expose 4997 so host can reach it
EXPOSE 4997

ENTRYPOINT ["dotnet", "AzureKeyVaultEmulator.dll"]