FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY AzureKeyVaultEmulator.sln ./ 
COPY src/AzureKeyVaultEmulator/*.csproj src/AzureKeyVaultEmulator/
COPY src/AzureKeyVaultEmulator.Shared/*.csproj src/AzureKeyVaultEmulator.Shared/
RUN dotnet restore src/AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj

COPY . ./
WORKDIR /app/src/AzureKeyVaultEmulator
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=https://+:4997
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/certs/emulator.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=emulator

COPY --from=build /out ./

EXPOSE 4997

ENTRYPOINT ["dotnet", "AzureKeyVaultEmulator.dll"]