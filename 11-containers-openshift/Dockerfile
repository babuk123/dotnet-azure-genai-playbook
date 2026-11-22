# Multi-stage build for the 03-rest-api sample
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY 03-rest-api/RestApi.csproj 03-rest-api/
RUN dotnet restore 03-rest-api/RestApi.csproj
COPY 03-rest-api/ 03-rest-api/
RUN dotnet publish 03-rest-api/RestApi.csproj -c Release -o /app --no-restore

# Small runtime image; runs as non-root (required by OpenShift's restricted SCC)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "RestApi.dll"]
