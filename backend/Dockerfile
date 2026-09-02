# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/LocalLive.Domain/LocalLive.Domain.csproj", "src/LocalLive.Domain/"]
COPY ["src/LocalLive.Application/LocalLive.Application.csproj", "src/LocalLive.Application/"]
COPY ["src/LocalLive.Infrastructure/LocalLive.Infrastructure.csproj", "src/LocalLive.Infrastructure/"]
COPY ["src/LocalLive.Api/LocalLive.Api.csproj", "src/LocalLive.Api/"]
RUN dotnet restore "src/LocalLive.Api/LocalLive.Api.csproj"

COPY src/ src/
WORKDIR /src/src/LocalLive.Api
RUN dotnet publish "LocalLive.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LocalLive.Api.dll"]
