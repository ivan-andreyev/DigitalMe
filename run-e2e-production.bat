@echo off
set TEST_ENV=production
dotnet test tests/DigitalMe.Tests.E2E --verbosity normal --filter "Category=E2E" --logger "console;verbosity=detailed"