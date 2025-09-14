# Automated Tooling Configuration

**⬅️ Back to:** [MAIN_PLAN.md](../../MAIN_PLAN.md) - Central entry point for all plans

**📋 Parent Plan:** [21-HYBRID-CODE-QUALITY-RECOVERY-PLAN.md](../21-HYBRID-CODE-QUALITY-RECOVERY-PLAN.md) - Coordinator

**📋 Related Files:**
- [02-manual-refactoring-specs.md](02-manual-refactoring-specs.md) - Manual refactoring specs
- [03-validation-checklist.md](03-validation-checklist.md) - Validation checklist

## Цель раздела
Настроить и активировать инструменты автоматического исправления для устранения 47 стилевых нарушений кода за 30 минут.

## Входные зависимости
- [ ] StyleCop.Analyzers уже добавлен в проект (ПОДТВЕРЖДЕНО)
- [ ] Microsoft.CodeAnalysis.Analyzers уже добавлен в проект (ПОДТВЕРЖДЕНО)
- [ ] 154/154 тестов проходят (базовая линия установлена)

## Конфигурационные файлы

### .editorconfig расширение
```ini
# StyleCop автоисправления
root = true

[*.cs]
# Formatting rules
dotnet_analyzer_diagnostic.SA1028.severity = error  # Trailing whitespace
dotnet_analyzer_diagnostic.SA1200.severity = error  # Using directives placement
dotnet_analyzer_diagnostic.SA1309.severity = none   # Field underscore prefix (allow)
dotnet_analyzer_diagnostic.SA1101.severity = none   # Prefix local calls with this
dotnet_analyzer_diagnostic.SA1633.severity = none   # File header requirement

# Naming conventions
dotnet_analyzer_diagnostic.SA1300.severity = error  # Element should begin with uppercase
dotnet_analyzer_diagnostic.SA1302.severity = error  # Interface names should begin with I
dotnet_analyzer_diagnostic.SA1303.severity = error  # Const field names should begin with uppercase

# Documentation rules  
dotnet_analyzer_diagnostic.SA1600.severity = warning # Elements should be documented
dotnet_analyzer_diagnostic.SA1601.severity = warning # Partial elements should be documented

# Layout rules
dotnet_analyzer_diagnostic.SA1500.severity = error  # Braces for multi-line statements
dotnet_analyzer_diagnostic.SA1501.severity = error  # Statement should not be on single line
dotnet_analyzer_diagnostic.SA1502.severity = error  # Element should not be on single line

# Ordering rules
dotnet_analyzer_diagnostic.SA1201.severity = error  # Elements should appear in correct order
dotnet_analyzer_diagnostic.SA1202.severity = error  # Elements should be ordered by access
dotnet_analyzer_diagnostic.SA1203.severity = error  # Constants should appear before fields
```

### Настройка IDE для автоисправлений

#### Visual Studio Code settings.json
```json
{
  "editor.formatOnSave": true,
  "editor.formatOnType": true,
  "editor.codeActionsOnSave": {
    "source.fixAll": true,
    "source.organizeImports": true
  },
  "omnisharp.enableEditorConfigSupport": true,
  "omnisharp.enableRoslynAnalyzers": true
}
```

#### Visual Studio IDE настройки
```xml
<!-- Добавить в .csproj для принудительной активации -->
<PropertyGroup>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest</AnalysisLevel>
  <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  <WarningsAsErrors />
  <RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
</PropertyGroup>
```

## Команды автоматического исправления

### Базовые команды форматирования
```bash
# Форматирование всех файлов проекта
dotnet format --severity error --verbosity diagnostic

# Исправление только предупреждений StyleCop
dotnet format --include-generated --severity warning

# Организация using statements
dotnet format --fix-style --severity suggestion
```

### Пакетное исправление в IDE
```bash
# Visual Studio: Tools -> Code Cleanup -> Run Code Cleanup Profile 1
# Или через Command Palette в VS Code: "Format Document"
# Применить ко всем файлам в папке src/
```

## Валидация автоматических исправлений

### Проверка количества нарушений
```bash
# До исправления - подсчет StyleCop warnings
dotnet build --verbosity normal 2>&1 | grep "warning SA" | wc -l
# Ожидаемый результат: ~47

# После исправления  
dotnet build --verbosity normal 2>&1 | grep "warning SA" | wc -l
# Целевой результат: ≤10
```

### Проверка форматирования
```bash
# Проверка что dotnet format ничего не меняет
dotnet format --verify-no-changes --verbosity diagnostic
# Ожидаемый результат: "No files need formatting"
```

### Проверка тестов после автоисправлений
```bash
# Убедиться что автоматические изменения не сломали тесты
dotnet test --logger:console --verbosity normal
# Ожидаемый результат: 154/154 passed
```

## Специфические настройки для DigitalMe проекта

### CustomWebApplicationFactory исключения
```xml
<!-- Добавить в Directory.Build.props для исключения специфических правил -->
<PropertyGroup>
  <!-- Временно отключить для интеграционных тестов -->
  <NoWarn>$(NoWarn);SA1600;SA1601</NoWarn>
</PropertyGroup>
```

### Конфигурация для тестовых проектов
```ini
# В .editorconfig для Tests/ папки
[Tests/**/*.cs]
dotnet_analyzer_diagnostic.SA1600.severity = none   # Test methods don't need XML docs
dotnet_analyzer_diagnostic.SA1601.severity = none   # Partial test classes ok
dotnet_analyzer_diagnostic.SA1200.severity = warning # Using statements less strict
```

## Мониторинг результатов

### Автоматическая проверка на каждом билде
```yaml
# GitHub Actions / Azure DevOps проверка
- name: Check Code Quality
  run: |
    dotnet format --verify-no-changes
    dotnet build --verbosity normal > build.log
    VIOLATIONS=$(grep "warning SA" build.log | wc -l)
    if [ $VIOLATIONS -gt 10 ]; then
      echo "Too many StyleCop violations: $VIOLATIONS"
      exit 1
    fi
```

### Локальные git hooks
```bash
# pre-commit hook для автоматического форматирования
#!/bin/sh
dotnet format --include $(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$' | tr '\n' ' ')
git add $(git diff --name-only --diff-filter=ACM | grep '\.cs$')
```

## Критерии завершения раздела

- [ ] .editorconfig дополнен правилами StyleCop
- [ ] IDE настроена для автоматических исправлений
- [ ] dotnet format успешно исправил форматирование
- [ ] StyleCop violations снижены с 47 до ≤10
- [ ] 154/154 тестов продолжают проходить
- [ ] Автоматические проверки настроены для CI/CD

## Временная оценка
**Фактическое время**: 30 минут
- 10 минут: настройка .editorconfig и IDE
- 10 минут: выполнение автоматических исправлений  
- 10 минут: валидация результатов и настройка проверок