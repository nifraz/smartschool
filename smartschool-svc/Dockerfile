# syntax=docker/dockerfile:1
ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION
WORKDIR /src
COPY *.sln ./
COPY SmartSchool.Api/*.csproj           ./SmartSchool.Api/
COPY SmartSchool.Graphql/*.csproj       ./SmartSchool.Graphql/
COPY SmartSchool.Resources/*.csproj     ./SmartSchool.Resources/
COPY SmartSchool.Schema/*.csproj        ./SmartSchool.Schema/
COPY SmartSchool.Service/*.csproj       ./SmartSchool.Service/
COPY SmartSchool.Utility/*.csproj       ./SmartSchool.Utility/
RUN dotnet restore SmartSchool.Api/SmartSchool.Api.csproj
COPY . .
RUN dotnet publish SmartSchool.Api/SmartSchool.Api.csproj \
    -c ${BUILD_CONFIGURATION} -o /app /p:UseAppHost=false

# ── Production runtime ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "SmartSchool.Api.dll"]

# ── Debug runtime (Debug build + vsdbg for VS Code attach) ──────────────────
FROM runtime AS debug
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl procps \
    && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg \
    && apt-get remove -y curl \
    && apt-get autoremove -y \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_ENVIRONMENT=Development
