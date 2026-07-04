# --- Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Eerst enkel het projectbestand → restore-laag wordt gecachet zolang de deps niet wijzigen.
COPY PharmaDocs.Api.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app --no-restore

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./

# Container Apps termineert TLS aan de ingress en stuurt HTTP door naar deze poort.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PharmaDocs.Api.dll"]
