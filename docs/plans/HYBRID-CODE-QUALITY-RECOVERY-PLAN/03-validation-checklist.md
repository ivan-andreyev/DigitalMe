# Validation Checklist & Quality Gates

**Родительский план**: [HYBRID-CODE-QUALITY-RECOVERY-PLAN.md](../HYBRID-CODE-QUALITY-RECOVERY-PLAN.md)

## Цель раздела
Обеспечить систематическую валидацию всех изменений с измеримыми критериями качества и готовность к продакшену.

## Входные зависимости
- [ ] Phase 1: Автоматические исправления завершены
- [ ] Phase 2: Ручные рефакторинги выполнены
- [ ] Все коммиты в git сохранены с checkpoint'ами

## Валидационная матрица

### Уровень 1: Базовые проверки (MUST PASS)

#### 1.1 Компиляция и билд
```bash
# Проверка успешной компиляции
dotnet build --configuration Release --verbosity normal
# Ожидаемый результат: Build succeeded. 0 Warning(s). 0 Error(s).

# Проверка отсутствия критических предупреждений
dotnet build 2>&1 | grep -E "(error|Error)" | wc -l
# Ожидаемый результат: 0
```

#### 1.2 Тестовое покрытие (КРИТИЧНО)
```bash
# Полный прогон тестового пакета
dotnet test --logger:console --verbosity normal --collect:"XPlat Code Coverage"
# Ожидаемый результат: 154/154 tests passed

# Проверка времени выполнения тестов
dotnet test --logger:console | grep "Total tests"
# Ожидаемый результат: время не увеличилось >20% от baseline
```

#### 1.3 StyleCop соответствие (КОЛИЧЕСТВЕННАЯ МЕТРИКА)
```bash
# Подсчет StyleCop нарушений после всех изменений
dotnet build --verbosity normal 2>&1 | grep "warning SA" | wc -l
# Целевой результат: ≤10 (снижение с 47)

# Детальный анализ оставшихся нарушений
dotnet build --verbosity normal 2>&1 | grep "warning SA" | sort | uniq -c
# Результат: только допустимые категории (например SA1600 для internal methods)
```

### Уровень 2: Архитектурные метрики (КАЧЕСТВЕННЫЕ)

#### 2.1 SOLID принципы соответствие
```bash
# TODO: Проверка через статический анализ
# Инструмент: NDepend или SonarQube
# Критерии:
# - Отсутствие циклических зависимостей
# - Abstractness vs Instability анализ  
# - Cohesion & Coupling метрики
```

#### 2.2 Размеры файлов и классов
```bash
# Проверка отсутствия больших файлов
find src/ -name "*.cs" -exec wc -l {} \; | awk '$1 > 500 { print $2 " has " $1 " lines" }'
# Ожидаемый результат: пустой вывод (нет файлов >500 строк)

# Проверка размеров классов
grep -n "class\|interface" src/**/*.cs | wc -l
# TODO: Automated class size analysis
```

#### 2.3 Цикломатическая сложность
```bash
# TODO: Анализ сложности методов
# Критерий: все методы <10 cyclomatic complexity
# Инструмент: Visual Studio Code Metrics или dotnet-complexity
```

### Уровень 3: Функциональные проверки (INTEGRATION)

#### 3.1 Локальное развертывание
```bash
# Проверка запуска приложения
cd src/DigitalMe.Web
dotnet run --environment=Development &
sleep 10

# Проверка health endpoint
curl -f http://localhost:5000/health
# Ожидаемый результат: HTTP 200 OK

# Завершение процесса
pkill -f "dotnet run"
```

#### 3.2 Database operations
```bash
# Проверка миграций
dotnet ef database update --startup-project src/DigitalMe.Web
# Ожидаемый результат: успешное применение миграций

# Проверка интеграционных тестов с БД
dotnet test --filter "Category=Integration"  
# Ожидаемый результат: все интеграционные тесты проходят
```

#### 3.3 API endpoints функциональность
```bash
# TODO: Автоматические API тесты
# Проверка основных endpoints:
# - GET /api/users
# - POST /api/profiles  
# - GET /api/interviews
# Ожидаемый результат: все endpoints возвращают корректные статус коды
```

### Уровень 4: Производительность и безопасность

#### 4.1 Performance benchmarks
```bash
# TODO: Load testing с baseline сравнением
# Инструмент: NBomber или k6
# Критерии:
# - Response time не увеличилось >10%
# - Memory usage не увеличилось >15%
# - CPU usage остался в допустимых пределах
```

#### 4.2 Security scanning
```bash
# TODO: Security vulnerabilities scan
# Инструмент: OWASP Dependency Check
dotnet list package --vulnerable
# Ожидаемый результат: нет новых уязвимостей
```

## Автоматизированные проверки

### Git pre-commit hooks
```bash
#!/bin/sh
# .git/hooks/pre-commit

echo "Running quality gates..."

# Форматирование проверка
dotnet format --verify-no-changes --severity error
if [ $? -ne 0 ]; then
    echo "FAIL: Code formatting issues detected"
    exit 1
fi

# Быстрые тесты
dotnet test --filter "Category!=Integration" --verbosity quiet
if [ $? -ne 0 ]; then
    echo "FAIL: Unit tests failing"
    exit 1
fi

echo "Quality gates passed!"
```

### CI/CD pipeline проверки
```yaml
# GitHub Actions / Azure DevOps
name: Quality Gate Validation

on: [push, pull_request]

jobs:
  quality-gates:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0'
    
    - name: Build
      run: dotnet build --configuration Release
    
    - name: Test
      run: dotnet test --logger trx --results-directory TestResults/
    
    - name: StyleCop Check
      run: |
        VIOLATIONS=$(dotnet build 2>&1 | grep "warning SA" | wc -l)
        if [ $VIOLATIONS -gt 10 ]; then
          echo "StyleCop violations: $VIOLATIONS (max: 10)"
          exit 1
        fi
    
    - name: File Size Check  
      run: |
        LARGE_FILES=$(find src/ -name "*.cs" -exec wc -l {} \; | awk '$1 > 500')
        if [ ! -z "$LARGE_FILES" ]; then
          echo "Large files detected: $LARGE_FILES"
          exit 1
        fi
```

## Ручные проверки (Code Review Checklist)

### Архитектурный ревью
- [ ] **Single Responsibility**: Каждый класс имеет единственную ответственность
- [ ] **Dependency Injection**: Все зависимости инжектируются через конструктор
- [ ] **Interface Segregation**: Интерфейсы не перегружены ненужными методами
- [ ] **Open/Closed**: Код открыт для расширения, закрыт для модификации
- [ ] **Liskov Substitution**: Производные классы заменяемы базовыми

### Качество кода ревью
- [ ] **Naming**: Имена классов, методов, переменных self-descriptive
- [ ] **Comments**: Код самодокументирующийся, комментарии только где необходимо
- [ ] **Error Handling**: Proper exception handling без silent failures
- [ ] **Resource Management**: using statements для IDisposable объектов
- [ ] **Thread Safety**: Concurrent access корректно обработан

### Тестовое покрытие ревью
- [ ] **Test Organization**: Тесты логически сгруппированы
- [ ] **Test Naming**: Имена тестов описывают what/when/then
- [ ] **Test Data**: Тестовые данные создаются через фабрики
- [ ] **Mocking**: Mock объекты используются для изоляции unit тестов
- [ ] **Integration Tests**: Покрывают критические интеграционные сценарии

## Критерии прохождения (Quality Gates)

### Обязательные критерии (БЛОКИРУЮЩИЕ)
1. **Build Success**: Проект компилируется без ошибок
2. **All Tests Pass**: 154/154 тестов проходят
3. **StyleCop Compliance**: ≤10 нарушений StyleCop
4. **No Large Files**: 0 файлов >500 строк
5. **No Broken Dependencies**: Все зависимости разрешимы

### Рекомендуемые критерии (НЕ БЛОКИРУЮЩИЕ)
1. **Performance**: Response time regression <10%
2. **Memory Usage**: Memory increase <15%
3. **Code Coverage**: Maintain current coverage level
4. **Documentation**: Public APIs документированы
5. **Security**: No new security vulnerabilities

## Откат процедуры

### Автоматический откат при неудаче
```bash
#!/bin/bash
# rollback-on-failure.sh

echo "Running quality gates..."
./run-quality-gates.sh

if [ $? -ne 0 ]; then
    echo "Quality gates failed! Rolling back..."
    
    # Откат к последнему успешному коммиту
    git reset --hard HEAD~1
    
    # Восстановление базы данных
    dotnet ef database update --startup-project src/DigitalMe.Web
    
    # Проверка после отката
    dotnet test --verbosity quiet
    
    echo "Rollback completed. Please fix issues and retry."
    exit 1
fi

echo "Quality gates passed!"
```

### Ручной откат инструкции
```bash
# В случае критических проблем:

# 1. Полный откат к началу процесса
git reset --hard HEAD~N  # где N = количество коммитов to rollback

# 2. Восстановление состояния БД
dotnet ef database drop
dotnet ef database update

# 3. Очистка временных файлов
dotnet clean
rm -rf bin/ obj/

# 4. Валидация восстановления
dotnet build
dotnet test
```

## Отчетность и мониторинг

### Ежедневный отчет качества
```bash
# daily-quality-report.sh
echo "=== Daily Quality Report ==="
echo "Date: $(date)"
echo ""

echo "StyleCop Violations:"
dotnet build 2>&1 | grep "warning SA" | wc -l

echo "Test Results:"
dotnet test --logger:console --verbosity quiet

echo "Large Files:"
find src/ -name "*.cs" -exec wc -l {} \; | awk '$1 > 300'

echo "Build Time:"
time dotnet build --verbosity quiet
```

### Качественные метрики dashboard
```bash
# TODO: Интеграция с monitoring tools
# - SonarQube для code quality metrics
# - Application Insights для performance metrics  
# - GitHub/Azure DevOps для build/test metrics
```

## Временная оценка валидации
**Общее время**: 30 минут
- **Автоматические проверки**: 15 минут
- **Ручной ревью**: 10 минут  
- **Отчетность**: 5 минут