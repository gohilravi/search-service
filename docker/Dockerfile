# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SearchService.sln", "./"]
COPY ["src/SearchService.API/SearchService.API.csproj", "src/SearchService.API/"]
COPY ["src/SearchService.Core/SearchService.Core.csproj", "src/SearchService.Core/"]
COPY ["src/SearchService.Application/SearchService.Application.csproj", "src/SearchService.Application/"]
COPY ["src/SearchService.Infrastructure/SearchService.Infrastructure.csproj", "src/SearchService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "SearchService.sln"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/src/SearchService.API"
RUN dotnet build "SearchService.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "SearchService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SearchService.API.dll"]

