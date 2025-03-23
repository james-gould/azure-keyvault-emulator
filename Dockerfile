# Use official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy and install the HTTPS certificates
RUN mkdir -p /certs
COPY local-certs/emulator.pfx /certs/
COPY local-certs/emulator.crt /certs/

# Install CA certificates
RUN apt-get update && apt-get install -y ca-certificates \
    && update-ca-certificates \
    && apt-get clean

# Extract private key from PFX
RUN apt-get install -y openssl \
    && openssl pkcs12 -in /certs/emulator.pfx -nocerts -nodes -passin pass:emulator | openssl rsa -out /certs/emulator.key \
    && openssl pkcs12 -in /certs/emulator.pfx -clcerts -nokeys -passin pass:emulator | openssl x509 -out /certs/emulator.crt \
    && cp /certs/emulator.crt /usr/local/share/ca-certificates/emulator.crt \
    && update-ca-certificates

RUN apt-get update && apt-get install -y curl

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

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/certs/emulator.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=emulator

# Install NGINX
RUN apt-get update && apt-get install -y nginx && apt-get clean

# Copy published .NET application
COPY --from=build /out ./

RUN mkdir -p /certs

COPY --from=build certs/ /certs/

# Copy certificates for NGINX
COPY --from=build --chmod=644 /certs/emulator.crt /usr/local/share/ca-certificates/emulator.crt
COPY --from=build --chmod=644 /certs/ /certs/

# Set correct permissions for certificates
RUN chmod 644 -R /certs/emulator.crt 
RUN chmod 644 -R /certs/emulator.key

RUN update-ca-certificates

# Copy NGINX configuration
RUN mkdir -p /var/log/nginx
COPY nginx.conf /etc/nginx/nginx.conf

# Expose 4997 so host can reach it
EXPOSE 4997

# Run both the .NET app and NGINX
CMD service nginx start && dotnet AzureKeyVaultEmulator.dll
