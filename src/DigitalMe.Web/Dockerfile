FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution file
COPY ["DigitalMe.sln", "."]

# Copy project files
COPY ["src/DigitalMe.Web/DigitalMe.Web.csproj", "src/DigitalMe.Web/"]
COPY ["DigitalMe/DigitalMe.csproj", "DigitalMe/"]

# Restore dependencies
RUN dotnet restore "src/DigitalMe.Web/DigitalMe.Web.csproj"

# Copy source code
COPY . .
WORKDIR "/src/src/DigitalMe.Web"

# Build application
RUN dotnet build "./DigitalMe.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DigitalMe.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DigitalMe.Web.dll"]