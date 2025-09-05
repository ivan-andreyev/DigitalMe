# 🌿 DigitalMe Git Flow Strategy

## Стратегия веток

### 📋 Обзор веток

- **`master`** - Продукция, защищённая ветка
- **`develop`** - Основная ветка разработки
- **`feature/*`** - Ветки новых функций (будущее)
- **`hotfix/*`** - Срочные исправления (будущее)

## 🔀 Workflow процесс

### 1. Повседневная разработка
```bash
# Всегда работаем в develop
git checkout develop
git pull origin develop

# Создание новых изменений
# ... ваша работа ...
git add .
git commit -m "feat: добавление новой функции"
git push origin develop
```

### 2. Слияние в master (Production)
```bash
# Создание Pull Request develop → master
# После ревью и тестирования:
git checkout master
git pull origin master
git merge develop
git push origin master
# ✅ Автоматически запускается полный CI/CD pipeline
```

## ⚡ CI/CD Конфигурация

### 🔨 develop ветка
- **Быстрая сборка** (Debug режим)
- **Unit тесты только** (без интеграционных)
- **Timeout: 10 минут** (быстрая обратная связь)
- **Проверка стиля кода** (только для PR)

**Workflow:** `.github/workflows/develop-ci.yml`

### 🚀 master ветка (Production)
- **Полная сборка** (Release режим)  
- **Все тесты** (Unit + Integration)
- **Docker builds** и registry push
- **Security scanning**
- **Автоматический deploy** в production
- **Timeout: 15 минут**

**Workflow:** `.github/workflows/ci-cd.yml`

## 📊 Сравнение процессов

| Аспект | develop | master |
|--------|---------|--------|
| **Сборка** | Debug, быстро | Release, полно |
| **Тесты** | Unit только | Unit + Integration |
| **Docker** | ❌ Не собирается | ✅ Полный build + push |
| **Deploy** | ❌ Нет | ✅ Production |
| **Security** | ❌ Пропускается | ✅ Полное сканирование |
| **Время** | ~5 минут | ~15 минут |

## 🛡️ Защита веток

### Master ветка
- **Прямые push запрещены** (требуется PR)
- **Обязательный code review**
- **Все проверки должны пройти**
- **Актуальность с основной веткой**

### Develop ветка  
- **Свободные commit'ы**
- **Быстрая обратная связь**
- **Code review для PR (рекомендуется)**

## 📝 Соглашения о коммитах

### Формат
```
type(scope): краткое описание

Подробное описание (опционально)
```

### Типы
- **feat** - новая функция
- **fix** - исправление бага  
- **docs** - документация
- **style** - форматирование
- **refactor** - рефакторинг
- **test** - тесты
- **chore** - технические задачи

### Примеры
```bash
git commit -m "feat(auth): добавление JWT авторизации"
git commit -m "fix(api): исправление null reference в UserController"
git commit -m "docs(readme): обновление инструкций по установке"
```

## 🚀 Будущие расширения

### Feature ветки
```bash
# Создание feature ветки от develop
git checkout develop
git checkout -b feature/user-authentication
# ... работа ...
git push origin feature/user-authentication
# PR: feature/user-authentication → develop
```

### Hotfix ветки
```bash
# Критический баг в production
git checkout master
git checkout -b hotfix/critical-security-fix
# ... быстрое исправление ...
git push origin hotfix/critical-security-fix
# PR: hotfix/critical-security-fix → master
```

## 🎯 Рекомендации

### ✅ Хорошие практики
- Работайте в develop для ежедневной разработки
- Делайте частые небольшие коммиты
- Используйте описательные сообщения коммитов
- Тестируйте локально перед push
- Master только через PR с ревью

### ❌ Избегайте
- Прямых push в master
- Больших коммитов с множественными изменениями  
- Неописательных сообщений ("fix", "update")
- Коммитов без локального тестирования
- Пропуска code review для важных изменений

## 📞 Команды для работы

### Ежедневная работа
```bash
# Переключение на develop
git checkout develop

# Получение последних изменений  
git pull origin develop

# Проверка статуса
git status

# Быстрая проверка компиляции
./scripts/test-ci-locally.sh
```

### Подготовка к production
```bash
# Убедитесь что develop готов
git checkout develop
git status  # должен быть clean

# Создание PR develop → master через GitHub UI
# После прохождения ревью - master автоматически обновится
```

---

**🎯 Цель:** Быстрая разработка в develop + стабильная продукция в master

**🔄 Процесс:** develop (ежедневно) → PR → master (production) → автодеплой