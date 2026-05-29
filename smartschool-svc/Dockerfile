# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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
RUN dotnet publish SmartSchool.Api/SmartSchool.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "SmartSchool.Api.dll"]
