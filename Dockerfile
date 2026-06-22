# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AjuriIA.sln .
COPY src/AjuriIA.API/AjuriIA.API.csproj src/AjuriIA.API/
RUN dotnet restore src/AjuriIA.API/AjuriIA.API.csproj

COPY src/AjuriIA.API/ src/AjuriIA.API/
RUN dotnet publish src/AjuriIA.API/AjuriIA.API.csproj \
    -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AjuriIA.API.dll"]
