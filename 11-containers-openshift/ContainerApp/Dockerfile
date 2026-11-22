# syntax=docker/dockerfile:1
# ---------- build stage: full SDK (~800 MB), never shipped ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy the project file first: restore is cached until dependencies change
COPY ContainerApp.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore /p:UseAppHost=false

# ---------- runtime stage: chiseled = no shell, no package manager, non-root (~110 MB) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "ContainerApp.dll"]
