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

# Copy published .NET application
COPY --from=build /out ./

# Expose 4997 so host can reach it
EXPOSE 4997

# When the container starts we EXPECT a volume to map to certs/
# and we then need to install the certificates into the container for trusted SSL.
# If this fails the container kills, which is expected and good.
ENTRYPOINT ["bash", "-c", "cp /certs/emulator.crt /usr/local/share/ca-certificates/emulator.crt && update-ca-certificates && exec dotnet AzureKeyVaultEmulator.dll"]