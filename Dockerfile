# DigitalMe - Multi-stage Docker Build
# Optimized for production deployment

ARG DOTNET_VERSION=8.0

###################
# Build Stage
###################
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# No additional system dependencies needed for production build
# Tests run in CI/CD pipeline with their own environment

# Copy solution and project files (excluding MAUI for Docker compatibility)
COPY DigitalMe.sln .
COPY src/DigitalMe/DigitalMe.csproj ./src/DigitalMe/
COPY src/DigitalMe.Web/DigitalMe.Web.csproj ./src/DigitalMe.Web/
# Test projects not needed for production Docker image

# Restore dependencies (excluding MAUI for Docker compatibility)
RUN dotnet restore src/DigitalMe/DigitalMe.csproj && \
    dotnet restore src/DigitalMe.Web/DigitalMe.Web.csproj

# Copy source code
COPY . .

# Build application (excluding MAUI for Docker compatibility)
RUN dotnet build src/DigitalMe/DigitalMe.csproj --configuration Release --no-restore && \
    dotnet build src/DigitalMe.Web/DigitalMe.Web.csproj --configuration Release --no-restore

# Tests are run in CI/CD pipeline, not in Docker build
# This reduces build time and complexity

# Publish application
RUN dotnet publish src/DigitalMe/DigitalMe.csproj \
    --configuration Release \
    --output /app/publish \
    --runtime linux-x64 \
    --self-contained false

###################
# Runtime Stage
###################
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime

# Create non-root user for security
RUN groupadd -r digitalme && useradd -r -g digitalme digitalme

# Set working directory
WORKDIR /app

# Install additional runtime dependencies if needed
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Create directories and set permissions
RUN mkdir -p /app/logs /app/data /app/backups && \
    chown -R digitalme:digitalme /app

# Switch to non-root user
USER digitalme

# Expose ports
EXPOSE 8080

# Health check - use dotnet --info instead of curl for reliability
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD dotnet --info || exit 1

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS="http://+:8080" \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    TZ=UTC

# Set entry point
ENTRYPOINT ["dotnet", "DigitalMe.dll"]
