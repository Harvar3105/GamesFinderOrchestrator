# 1. Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files
COPY GamesFinder.sln ./
COPY src/GamesFinder.Orchestrator.API/*.csproj ./src/GamesFinder.Orchestrator.API/
COPY src/GamesFinder.Orchestrator.Consumers/*.csproj ./src/GamesFinder.Orchestrator.Consumers/
COPY src/GamesFinder.Orchestrator.Domain/*.csproj ./src/GamesFinder.Orchestrator.Domain/
COPY src/GamesFinder.Orchestrator.Publisher/*.csproj ./src/GamesFinder.Orchestrator.Publisher/
COPY src/GamesFinder.Orchestrator.Repositories/*.csproj ./src/GamesFinder.Orchestrator.Repositories/
COPY src/GamesFinder.Orchestrator.Services/*.csproj ./src/GamesFinder.Orchestrator.Services/
COPY src/GamesFinder.Orchestrator.Utils/*.csproj ./src/GamesFinder.Orchestrator.Utils/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY src/. ./src/
WORKDIR /app/src/GamesFinder.Orchestrator.API

# Publish the application
RUN dotnet publish -c Debug -o /app/publish

# 2. Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Open ports
EXPOSE 5000
EXPOSE 5001

# Run
ENTRYPOINT ["dotnet", "GamesFinder.Orchestrator.API.dll"]
