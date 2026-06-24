# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY nuget.config .
COPY src/DigiDocumentManagement/*.csproj src/DigiDocumentManagement/
RUN dotnet restore src/DigiDocumentManagement/DigiDocumentManagement.csproj
COPY src/ src/
RUN dotnet publish src/DigiDocumentManagement/DigiDocumentManagement.csproj -c Release -o /app

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /data/documents
VOLUME /data
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "DigiDocumentManagement.dll"]
