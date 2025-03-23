# Use official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy and install the HTTPS certificates
RUN mkdir -p /certs
COPY local-certs/emulator.pfx /certs/

# # Install CA certificates
RUN apt-get update && apt-get install -y ca-certificates \
    && update-ca-certificates \
    && apt-get clean

# Import the certificate into the trusted store
RUN apt-get install -y openssl \
    && openssl pkcs12 -in /certs/emulator.pfx -nocerts -nodes -passin pass:emulator | openssl rsa -out /certs/emulator.key \
    && openssl pkcs12 -in /certs/emulator.pfx -clcerts -nokeys -passin pass:emulator | openssl x509 -out /certs/emulator.crt \
    && cp /certs/emulator.crt /usr/local/share/ca-certificates/emulator.crt \
    && update-ca-certificates

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
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /out ./

RUN mkdir -p /certs

COPY --from=build /certs/emulator.pfx /certs/

# # Exposes the port specified in the AzureKeyVaultEmulator.Hosting.Aspire.Extensions -> RunAsEmulator
ENV ASPNETCORE_URLS=https://emulator.vault.azure.net:4997
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/certs/emulator.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=emulator

EXPOSE 4997

# Set the entry point (adjust based on your application)
ENTRYPOINT ["dotnet", "AzureKeyVaultEmulator.dll"]
