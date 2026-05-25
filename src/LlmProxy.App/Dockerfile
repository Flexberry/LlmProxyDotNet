FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.slnx ./
COPY src/LlmProxy.Core/LlmProxy.Core.csproj ./LlmProxy.Core/
COPY src/LlmProxy.Infrastructure/LlmProxy.Infrastructure.csproj ./LlmProxy.Infrastructure/
COPY src/LlmProxy.App/LlmProxy.App.csproj ./LlmProxy.App/

# Restore dependencies
RUN dotnet restore "./src/LlmProxy.App/LlmProxy.App.csproj"

# Copy source code
COPY src/LlmProxy.Core/. ./LlmProxy.Core/
COPY src/LlmProxy.Infrastructure/. ./LlmProxy.Infrastructure/
COPY src/LlmProxy.App/. ./LlmProxy.App/

# Build
WORKDIR "/src/src/LlmProxy.App"
RUN dotnet build "LlmProxy.App.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "LlmProxy.App.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 4000

# Create non-root user
RUN useradd -m -u 1000 appuser
USER appuser

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "LlmProxy.App.dll"]
