# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for layer-cached restore
COPY LudoKingSln.sln ./
COPY src/LudoKing.Shared/LudoKing.Shared.csproj src/LudoKing.Shared/
COPY src/LudoKing.Interfaces/LudoKing.Interfaces.csproj src/LudoKing.Interfaces/
COPY src/LudoKing.Domain/LudoKing.Domain.csproj src/LudoKing.Domain/
COPY src/LudoKing.Data/LudoKing.Data.csproj src/LudoKing.Data/
COPY src/LudoKing.Infrastructure/LudoKing.Infrastructure.csproj src/LudoKing.Infrastructure/
COPY src/LudoKing.API/LudoKing.API.csproj src/LudoKing.API/

RUN dotnet restore src/LudoKing.API/LudoKing.API.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/LudoKing.API/LudoKing.API.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "LudoKing.API.dll"]
