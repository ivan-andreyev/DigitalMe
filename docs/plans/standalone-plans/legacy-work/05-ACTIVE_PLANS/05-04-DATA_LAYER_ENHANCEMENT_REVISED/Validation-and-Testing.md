# Final Validation & Testing

## Integration Test Commands:
```powershell
# 1. Compilation verification
dotnet build DigitalMe.csproj --verbosity quiet
if ($LASTEXITCODE -ne 0) { Write-Error "Compilation failed" }

# 2. Generate migration
dotnet ef migrations add BaseEntityRefactor --project DigitalMe.csproj
if ($LASTEXITCODE -ne 0) { Write-Error "Migration generation failed" }

# 3. Validate migration SQL (dry run)
dotnet ef database update --dry-run --project DigitalMe.csproj

# 4. Run unit tests
dotnet test tests/DigitalMe.Tests.Unit/DigitalMe.Tests.Unit.csproj --logger "console;verbosity=detailed"

# 5. Entity instantiation test
dotnet run --project DigitalMe.csproj -- --test-entities
```

## Rollback Procedure:
```powershell
# If migration issues occur:
dotnet ef database update <PreviousMigrationName> --project DigitalMe.csproj
dotnet ef migrations remove --project DigitalMe.csproj
```

## Success Validation Criteria:
1. **Zero Compilation Errors:** All builds succeed
2. **Migration Success:** BaseEntityRefactor migration generates cleanly
3. **Database Compatibility:** Dry-run shows no breaking changes
4. **Test Pass Rate:** All existing unit tests pass
5. **Entity Creation:** New entities can be instantiated without errors