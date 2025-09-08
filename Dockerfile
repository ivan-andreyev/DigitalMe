# DigitalMe - Multi-stage Docker Build
# Optimized for production deployment

ARG DOTNET_VERSION=8.0

###################
# Build Stage
###################
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Copy solution and project files
COPY DigitalMe.sln .
COPY DigitalMe/DigitalMe.csproj ./DigitalMe/
COPY src/DigitalMe.Web/DigitalMe.Web.csproj ./src/DigitalMe.Web/
COPY tests/DigitalMe.Tests.Unit/DigitalMe.Tests.Unit.csproj ./tests/DigitalMe.Tests.Unit/
COPY tests/DigitalMe.Tests.Integration/DigitalMe.Tests.Integration.csproj ./tests/DigitalMe.Tests.Integration/

# Restore dependencies
RUN dotnet restore DigitalMe.sln

# Copy source code
COPY . .

# Build application
RUN dotnet build DigitalMe.sln --configuration Release --no-restore

# Run tests
RUN dotnet test tests/DigitalMe.Tests.Unit/DigitalMe.Tests.Unit.csproj \
    --configuration Release --no-build --verbosity normal

# Publish application
RUN dotnet publish DigitalMe/DigitalMe.csproj \
    --configuration Release \
    --no-build \
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
EXPOSE 8081

# Health check - use dotnet --info instead of curl for reliability
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD dotnet --info || exit 1

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS="http://+:8080;https://+:8081" \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_HTTPS_PORTS=8081 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    TZ=UTC

# Set entry point
ENTRYPOINT ["dotnet", "DigitalMe.dll"]