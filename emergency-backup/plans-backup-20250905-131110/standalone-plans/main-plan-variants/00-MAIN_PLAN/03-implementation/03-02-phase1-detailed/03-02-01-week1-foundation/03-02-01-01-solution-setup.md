# Day 1: Solution Setup & Package References

**Родительский план**: [../03-02-01-week1-foundation.md](../03-02-01-week1-foundation.md)

## Day 1: Solution Setup (2 hours)
**Задача**: Создание структуры .NET 8 solution

**Файлы для создания**:
- `DigitalMe.sln:1` - создать solution файл
- `src/DigitalMe.API/DigitalMe.API.csproj:1-15` - Web API проект
- `src/DigitalMe.Core/DigitalMe.Core.csproj:1-10` - бизнес-логика
- `src/DigitalMe.Data/DigitalMe.Data.csproj:1-12` - доступ к данным
- `src/DigitalMe.Integrations/DigitalMe.Integrations.csproj:1-10` - внешние API

**Команды выполнения**:
```bash
dotnet new sln -n DigitalMe
dotnet new webapi -n DigitalMe.API -o src/DigitalMe.API --framework net8.0
dotnet new classlib -n DigitalMe.Core -o src/DigitalMe.Core --framework net8.0  
dotnet new classlib -n DigitalMe.Data -o src/DigitalMe.Data --framework net8.0
dotnet new classlib -n DigitalMe.Integrations -o src/DigitalMe.Integrations --framework net8.0
dotnet sln add src/**/*.csproj
```

**Критерии успеха (измеримые)**:
- ✅ `dotnet build` выполняется успешно (0 ошибок)
- ✅ `dotnet run --project src/DigitalMe.API` запускается на порту 5000
- ✅ Swagger UI доступен на `/swagger` (HTTP 200)
- ✅ Health check endpoint `/health` возвращает 200 OK

## Day 1-2: Package References Setup (1 hour)

**Файл**: `src/DigitalMe.API/DigitalMe.API.csproj:8-20`
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="8.0.10" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
```

**Файл**: `src/DigitalMe.Data/DigitalMe.Data.csproj:8-15`
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.10" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
```

**Файл**: `src/DigitalMe.Integrations/DigitalMe.Integrations.csproj:8-12`
```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.1" />
<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="8.0.0" />
<PackageReference Include="Polly" Version="8.4.2" />
```

**Команды проверки**:
```bash
dotnet restore
dotnet list package --include-transitive | grep -E "(EntityFrameworkCore|Npgsql|Serilog)"
```

## Next Steps
- [Day 2: Database Context Setup](03-02-01-02-database-context.md)
- [Day 3: Entity Models Implementation](03-02-01-03-entity-models.md)
- [Day 4: DI Container & Configuration](03-02-01-04-di-configuration.md)