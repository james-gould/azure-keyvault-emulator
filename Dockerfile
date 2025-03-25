# Use official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the HTTPS certificates
RUN mkdir -p /certs
COPY local-certs/emulator.pfx /certs/
COPY local-certs/emulator.crt /certs/

# Install CA certificates
RUN apt-get update && apt-get install -y ca-certificates \
    && update-ca-certificates \
    && apt-get clean

# Extract private key from PFX and install the certs
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
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=https://+:4997
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/certs/emulator.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=emulator

# Copy published .NET application
COPY --from=build /out ./

RUN mkdir -p /certs

# Copy again because it's a different step, can probably clean this up
COPY --from=build certs/ /certs/

# Copy certificates to trust store
COPY --from=build --chmod=644 /certs/emulator.crt /usr/local/share/ca-certificates/emulator.crt
COPY --from=build --chmod=644 /certs/ /certs/

# Set correct permissions for certificates
RUN chmod 644 -R /certs/emulator.crt 

# Get cert in trust store installed correctly
RUN update-ca-certificates

# Expose 4997 so host can reach it
EXPOSE 4997

ENTRYPOINT ["dotnet", "AzureKeyVaultEmulator.dll"]