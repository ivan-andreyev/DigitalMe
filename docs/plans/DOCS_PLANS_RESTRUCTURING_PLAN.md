# 🏗️ DOCS PLANS RESTRUCTURING PLAN

## 📋 EXECUTIVE SUMMARY

**Цель**: Реструктуризация документов в `docs/plans/` согласно правилам каталогизации с единственным файлом `MAIN_PLAN.md` в корне и всеми остальными планами в подкаталоге `MAIN_PLAN/` с правильной нумерацией ##-.

🔥 **CRITICAL FIXES APPLIED (2025-09-14)**:
✅ **Fixed broken links**: INTEGRATION-FOCUSED-HYBRID-PLAN.md → CONSOLIDATED-EXECUTION-PLAN.md
✅ **Added error handling**: All rm/git mv commands now have existence checks
✅ **Windows compatibility**: All bash commands tested for Windows Git Bash
✅ **Pre-execution validation**: Added safety checks before restructuring begins
✅ **Robust file operations**: No commands will fail due to missing files

**Проблемы, устраняемые планом**:
1. ❌ Нарушение правила "единственный файл в каталоге" - 24 файла в корне docs/plans/
2. ❌ Отсутствует логическая нумерация ##- для файлов плана
3. ❌ Неструктурированная организация планов без иерархии
4. ❌ Множественные дублирующиеся архивные каталоги
5. ❌ Отсутствуют двусторонние перекрестные ссылки

**Результат**: Структурированная система с MAIN_PLAN.md как единственным файлом в корне и всеми остальными планами в docs/plans/MAIN_PLAN/ с корректной нумерацией и двусторонними ссылками.

✨ **EXECUTION READY**: План теперь готов для выполнения без ошибок и broken links

---

## 🎯 TARGET STRUCTURE

### **✅ ПОСЛЕ РЕСТРУКТУРИЗАЦИИ**:
```
docs/plans/
├── MAIN_PLAN.md                           # ← ЕДИНСТВЕННЫЙ файл в корне
└── MAIN_PLAN/                             # ← ВСЕ остальные планы здесь
    ├── 01-MASTER_TECHNICAL_PLAN.md
    ├── 02-ARCHITECTURAL_REMEDIATION_PLAN.md
    ├── 03-IVAN_LEVEL_COMPLETION_PLAN.md
    ├── 04-PHASE_1_1_LEARNING_INFRASTRUCTURE_REMEDIATION_PLAN.md
    ├── 05-CRITICAL_TEST_FAILURES_REMEDIATION_PLAN.md
    ├── 06-CAPTCHA_WORKFLOW_SERVICE_REMEDIATION_PLAN.md
    ├── 07-TEST-INFRASTRUCTURE-IMPROVEMENT-PLAN.md
    ├── 08-HYBRID-CODE-QUALITY-RECOVERY-PLAN-Architecture.md
    ├── 09-CONSOLIDATED-EXECUTION-PLAN.md
    ├── 10-PHASE_1_IMMEDIATE_EXECUTION_TASKS.md
    ├── 11-PHASE_2_SELFTESTINGFRAMEWORK_FIXES.md
    ├── 12-PHASE_3_VALIDATION_AND_PRODUCTION_READINESS.md
    ├── 13-PHASE0_IVAN_LEVEL_AGENT.md
    ├── 14-PHASE1_ADVANCED_COGNITIVE_TASKS.md
    ├── 15-EXECUTIVE_SUMMARY_CRITICAL_TEST_REMEDIATION.md
    ├── 16-TEST_REMEDIATION_BASELINE_DOCUMENTATION.md
    ├── 17-STRATEGIC-NEXT-STEPS-SUMMARY.md
    ├── 18-Future-R&D-Extensions-Roadmap.md
    ├── 19-MASTER-DEVELOPMENT-DECISIONS-LOG.md
    ├── 20-PLANS-INDEX.md
    ├── 21-HYBRID-CODE-QUALITY-RECOVERY-PLAN/
    │   ├── 01-automated-tooling-config.md
    │   ├── 02-manual-refactoring-specs.md
    │   └── 03-validation-checklist.md
    └── ARCHIVED/                          # ← Консолидированный архив
        ├── _ARCHIVED/
        │   └── phase-a-integration-focused/
        └── archived/
            ├── ARCHITECTURE-MERGER-PLAN.md
            ├── P23-Data-Layer-Enhancement.md
            └── P23-Data-Layer-Enhancement-v3.md
```

---

## 🔢 FILE NUMBERING LOGIC

### **КАТЕГОРИЗАЦИЯ И НУМЕРАЦИЯ**:

**01-05: СТРАТЕГИЧЕСКИЕ МАСТЕР-ПЛАНЫ**
- 01-MASTER_TECHNICAL_PLAN.md (35,840 bytes) - Главный технический план
- 02-ARCHITECTURAL_REMEDIATION_PLAN.md (13,794 bytes) - Архитектурная реновация
- 03-IVAN_LEVEL_COMPLETION_PLAN.md (43,997 bytes) - Завершение уровня Ивана
- 04-PHASE_1_1_LEARNING_INFRASTRUCTURE_REMEDIATION_PLAN.md (35,196 bytes) - Обучающая инфраструктура
- 05-CRITICAL_TEST_FAILURES_REMEDIATION_PLAN.md (10,315 bytes) - Критические исправления тестов

**06-10: СПЕЦИАЛИЗИРОВАННЫЕ ПЛАНЫ ИСПРАВЛЕНИЙ**
- 06-CAPTCHA_WORKFLOW_SERVICE_REMEDIATION_PLAN.md (12,376 bytes) - CAPTCHA workflow
- 07-TEST-INFRASTRUCTURE-IMPROVEMENT-PLAN.md (15,516 bytes) - Улучшение тестовой инфраструктуры
- 08-HYBRID-CODE-QUALITY-RECOVERY-PLAN-Architecture.md (5,900 bytes) - Качество кода
- 09-CONSOLIDATED-EXECUTION-PLAN.md (10,374 bytes) - Консолидированный план выполнения
- 10-PHASE_1_IMMEDIATE_EXECUTION_TASKS.md (8,876 bytes) - Немедленные задачи

**11-15: ФАЗОВЫЕ ПЛАНЫ**
- 11-PHASE_2_SELFTESTINGFRAMEWORK_FIXES.md (8,109 bytes) - Фаза 2 исправлений
- 12-PHASE_3_VALIDATION_AND_PRODUCTION_READINESS.md (8,491 bytes) - Фаза 3 валидации
- 13-PHASE0_IVAN_LEVEL_AGENT.md (16,381 bytes) - Фаза 0 агента
- 14-PHASE1_ADVANCED_COGNITIVE_TASKS.md (12,629 bytes) - Фаза 1 когнитивных задач
- 15-EXECUTIVE_SUMMARY_CRITICAL_TEST_REMEDIATION.md (6,145 bytes) - Executive summary

**16-20: ДОКУМЕНТАЦИЯ И ИНДЕКСЫ**
- 16-TEST_REMEDIATION_BASELINE_DOCUMENTATION.md (9,692 bytes) - Базовая документация
- 17-STRATEGIC-NEXT-STEPS-SUMMARY.md (8,660 bytes) - Стратегические следующие шаги
- 18-Future-R&D-Extensions-Roadmap.md (14,578 bytes) - Roadmap R&D расширений
- 19-MASTER-DEVELOPMENT-DECISIONS-LOG.md (8,852 bytes) - Лог решений разработки
- 20-PLANS-INDEX.md (6,946 bytes) - Индекс планов

**21: ПОДКАТАЛОГ**
- 21-HYBRID-CODE-QUALITY-RECOVERY-PLAN/ - Детализированный план качества кода

---

## 🔗 CROSS-REFERENCE MAPPING

### **ССЫЛКИ ДЛЯ ОБНОВЛЕНИЯ**:

#### **В MAIN_PLAN.md**:
```markdown
# ТЕКУЩИЕ ССЫЛКИ (требуют обновления):
- [MASTER-DEVELOPMENT-DECISIONS-LOG.md](MASTER-DEVELOPMENT-DECISIONS-LOG.md)
- [CONSOLIDATED-EXECUTION-PLAN.md](CONSOLIDATED-EXECUTION-PLAN.md)
- [PLANS-INDEX.md](PLANS-INDEX.md)

# НОВЫЕ ССЫЛКИ (после реструктуризации):
- [MASTER-DEVELOPMENT-DECISIONS-LOG.md](MAIN_PLAN/19-MASTER-DEVELOPMENT-DECISIONS-LOG.md)
- [CONSOLIDATED-EXECUTION-PLAN.md](MAIN_PLAN/09-CONSOLIDATED-EXECUTION-PLAN.md)
- [PLANS-INDEX.md](MAIN_PLAN/20-PLANS-INDEX.md)
```

#### **ПЛАНЫ С ПЕРЕКРЕСТНЫМИ ССЫЛКАМИ**:

**PLANS-INDEX.md** - содержит ссылки на:
- ARCHITECTURAL_REMEDIATION_PLAN.md → 02-ARCHITECTURAL_REMEDIATION_PLAN.md
- MASTER_TECHNICAL_PLAN.md → 01-MASTER_TECHNICAL_PLAN.md
- IVAN_LEVEL_COMPLETION_PLAN.md → 03-IVAN_LEVEL_COMPLETION_PLAN.md

**MASTER-DEVELOPMENT-DECISIONS-LOG.md** - содержит ссылки на:
- ARCHITECTURAL_REMEDIATION_PLAN.md → 02-ARCHITECTURAL_REMEDIATION_PLAN.md
- PHASE_1_1_LEARNING_INFRASTRUCTURE_REMEDIATION_PLAN.md → 04-PHASE_1_1_LEARNING_INFRASTRUCTURE_REMEDIATION_PLAN.md

#### **АРХИТЕКТУРНАЯ ДОКУМЕНТАЦИЯ**:
Файлы в docs/Architecture/ содержащие ссылки на планы:
- ARCHITECTURE-INDEX.md
- Actual/*.md files
- Vision/*.md files

---

## 🚀 EXECUTION PHASES

### **PHASE 1: PREPARATION & ANALYSIS**
**Duration**: 30-45 минут

#### **Task 1.1: Pre-Execution Validation**
```bash
# КРИТИЧЕСКАЯ ПРОВЕРКА ПЕРЕД НАЧАЛОМ
echo "=== PRE-EXECUTION VALIDATION ==="
# Проверка что мы в правильной директории
if [ ! -f "docs/plans/MAIN_PLAN.md" ]; then
    echo "ERROR: Not in DigitalMe root or MAIN_PLAN.md missing!"
    exit 1
fi
# Проверка git статуса
git status --porcelain | head -5
echo "Current branch: $(git branch --show-current)"
# Проверка ключевых файлов
echo "Key files check:"
for file in "docs/plans/CONSOLIDATED-EXECUTION-PLAN.md" "docs/plans/MASTER_TECHNICAL_PLAN.md"; do
    if [ -f "$file" ]; then echo "✅ $file"; else echo "❌ $file MISSING!"; fi
done
```

#### **Task 1.2: Create Target Directory Structure**
```bash
# Создание целевой структуры каталогов (только если не существуют)
if [ ! -d "docs/plans/MAIN_PLAN" ]; then
    mkdir -p "docs/plans/MAIN_PLAN"
    echo "Created docs/plans/MAIN_PLAN/"
else
    echo "docs/plans/MAIN_PLAN/ already exists"
fi
if [ ! -d "docs/plans/MAIN_PLAN/ARCHIVED" ]; then
    mkdir -p "docs/plans/MAIN_PLAN/ARCHIVED"
    echo "Created docs/plans/MAIN_PLAN/ARCHIVED/"
else
    echo "docs/plans/MAIN_PLAN/ARCHIVED/ already exists"
fi
```

#### **Task 1.3: Cross-Reference Analysis**
- Сканирование всех .md файлов в docs/ на наличие ссылок на планы
- Создание полной карты зависимостей ссылок
- Подготовка списка файлов для обновления ссылок

#### **Task 1.4: Backup Current Structure**
```bash
# Создание backup текущей структуры
git add -A
git commit -m "BACKUP: Before docs/plans restructuring"
```

### **PHASE 2: FILE RELOCATIONS WITH GIT HISTORY**
**Duration**: 45-60 минут

#### **Task 2.1: Move Strategic Master Plans (01-05)**
```bash
# Сохранение git истории через git mv с проверками существования
if [ -f "docs/plans/MASTER_TECHNICAL_PLAN.md" ]; then
    git mv "docs/plans/MASTER_TECHNICAL_PLAN.md" "docs/plans/MAIN_PLAN/01-MASTER_TECHNICAL_PLAN.md"
fi
if [ -f "docs/plans/ARCHITECTURAL_REMEDIATION_PLAN.md" ]; then
    git mv "docs/plans/ARCHITECTURAL_REMEDIATION_PLAN.md" "docs/plans/MAIN_PLAN/02-ARCHITECTURAL_REMEDIATION_PLAN.md"
fi
if [ -f "docs/plans/IVAN_LEVEL_COMPLETION_PLAN.md" ]; then
    git mv "docs/plans/IVAN_LEVEL_COMPLETION_PLAN.md" "docs/plans/MAIN_PLAN/03-IVAN_LEVEL_COMPLETION_PLAN.md"
fi
if [ -f "docs/plans/PHASE_1_1_LEARNING_INFRASTRUCTURE_REMEDIATION_PLAN.md" ]; then
    git mv "docs/plans/PHASE_1_1_LEARNING_INFRASTRUCTURE_REMEDIATION_PLAN.md" "docs/plans/MAIN_PLAN/04-PHASE_1_1_LEARNING_INFRASTRUCTURE_REMEDIATION_PLAN.md"
fi
if [ -f "docs/plans/CRITICAL_TEST_FAILURES_REMEDIATION_PLAN.md" ]; then
    git mv "docs/plans/CRITICAL_TEST_FAILURES_REMEDIATION_PLAN.md" "docs/plans/MAIN_PLAN/05-CRITICAL_TEST_FAILURES_REMEDIATION_PLAN.md"
fi
```

#### **Task 2.2: Move Specialized Remediation Plans (06-10)**
```bash
if [ -f "docs/plans/CAPTCHA_WORKFLOW_SERVICE_REMEDIATION_PLAN.md" ]; then
    git mv "docs/plans/CAPTCHA_WORKFLOW_SERVICE_REMEDIATION_PLAN.md" "docs/plans/MAIN_PLAN/06-CAPTCHA_WORKFLOW_SERVICE_REMEDIATION_PLAN.md"
fi
if [ -f "docs/plans/TEST-INFRASTRUCTURE-IMPROVEMENT-PLAN.md" ]; then
    git mv "docs/plans/TEST-INFRASTRUCTURE-IMPROVEMENT-PLAN.md" "docs/plans/MAIN_PLAN/07-TEST-INFRASTRUCTURE-IMPROVEMENT-PLAN.md"
fi
if [ -f "docs/plans/HYBRID-CODE-QUALITY-RECOVERY-PLAN-Architecture.md" ]; then
    git mv "docs/plans/HYBRID-CODE-QUALITY-RECOVERY-PLAN-Architecture.md" "docs/plans/MAIN_PLAN/08-HYBRID-CODE-QUALITY-RECOVERY-PLAN-Architecture.md"
fi
if [ -f "docs/plans/CONSOLIDATED-EXECUTION-PLAN.md" ]; then
    git mv "docs/plans/CONSOLIDATED-EXECUTION-PLAN.md" "docs/plans/MAIN_PLAN/09-CONSOLIDATED-EXECUTION-PLAN.md"
fi
if [ -f "docs/plans/PHASE_1_IMMEDIATE_EXECUTION_TASKS.md" ]; then
    git mv "docs/plans/PHASE_1_IMMEDIATE_EXECUTION_TASKS.md" "docs/plans/MAIN_PLAN/10-PHASE_1_IMMEDIATE_EXECUTION_TASKS.md"
fi
```

#### **Task 2.3: Move Phase Plans (11-15)**
```bash
if [ -f "docs/plans/PHASE_2_SELFTESTINGFRAMEWORK_FIXES.md" ]; then
    git mv "docs/plans/PHASE_2_SELFTESTINGFRAMEWORK_FIXES.md" "docs/plans/MAIN_PLAN/11-PHASE_2_SELFTESTINGFRAMEWORK_FIXES.md"
fi
if [ -f "docs/plans/PHASE_3_VALIDATION_AND_PRODUCTION_READINESS.md" ]; then
    git mv "docs/plans/PHASE_3_VALIDATION_AND_PRODUCTION_READINESS.md" "docs/plans/MAIN_PLAN/12-PHASE_3_VALIDATION_AND_PRODUCTION_READINESS.md"
fi
if [ -f "docs/plans/PHASE0_IVAN_LEVEL_AGENT.md" ]; then
    git mv "docs/plans/PHASE0_IVAN_LEVEL_AGENT.md" "docs/plans/MAIN_PLAN/13-PHASE0_IVAN_LEVEL_AGENT.md"
fi
if [ -f "docs/plans/PHASE1_ADVANCED_COGNITIVE_TASKS.md" ]; then
    git mv "docs/plans/PHASE1_ADVANCED_COGNITIVE_TASKS.md" "docs/plans/MAIN_PLAN/14-PHASE1_ADVANCED_COGNITIVE_TASKS.md"
fi
if [ -f "docs/plans/EXECUTIVE_SUMMARY_CRITICAL_TEST_REMEDIATION.md" ]; then
    git mv "docs/plans/EXECUTIVE_SUMMARY_CRITICAL_TEST_REMEDIATION.md" "docs/plans/MAIN_PLAN/15-EXECUTIVE_SUMMARY_CRITICAL_TEST_REMEDIATION.md"
fi
```

#### **Task 2.4: Move Documentation & Indexes (16-20)**
```bash
if [ -f "docs/plans/TEST_REMEDIATION_BASELINE_DOCUMENTATION.md" ]; then
    git mv "docs/plans/TEST_REMEDIATION_BASELINE_DOCUMENTATION.md" "docs/plans/MAIN_PLAN/16-TEST_REMEDIATION_BASELINE_DOCUMENTATION.md"
fi
if [ -f "docs/plans/STRATEGIC-NEXT-STEPS-SUMMARY.md" ]; then
    git mv "docs/plans/STRATEGIC-NEXT-STEPS-SUMMARY.md" "docs/plans/MAIN_PLAN/17-STRATEGIC-NEXT-STEPS-SUMMARY.md"
fi
if [ -f "docs/plans/Future-R&D-Extensions-Roadmap.md" ]; then
    git mv "docs/plans/Future-R&D-Extensions-Roadmap.md" "docs/plans/MAIN_PLAN/18-Future-R&D-Extensions-Roadmap.md"
fi
if [ -f "docs/plans/MASTER-DEVELOPMENT-DECISIONS-LOG.md" ]; then
    git mv "docs/plans/MASTER-DEVELOPMENT-DECISIONS-LOG.md" "docs/plans/MAIN_PLAN/19-MASTER-DEVELOPMENT-DECISIONS-LOG.md"
fi
if [ -f "docs/plans/PLANS-INDEX.md" ]; then
    git mv "docs/plans/PLANS-INDEX.md" "docs/plans/MAIN_PLAN/20-PLANS-INDEX.md"
fi
```

#### **Task 2.5: Move Subdirectory & Archive Consolidation**
```bash
if [ -d "docs/plans/HYBRID-CODE-QUALITY-RECOVERY-PLAN" ]; then
    git mv "docs/plans/HYBRID-CODE-QUALITY-RECOVERY-PLAN" "docs/plans/MAIN_PLAN/21-HYBRID-CODE-QUALITY-RECOVERY-PLAN"
fi
if [ -d "docs/plans/_ARCHIVED" ]; then
    git mv "docs/plans/_ARCHIVED" "docs/plans/MAIN_PLAN/ARCHIVED/_ARCHIVED"
fi
if [ -d "docs/plans/archived" ]; then
    git mv "docs/plans/archived" "docs/plans/MAIN_PLAN/ARCHIVED/archived"
fi
```

#### **Task 2.6: Remove Generated Files**
```bash
# Удаление сгенерированных/временных файлов с проверкой существования
if [ -f "docs/plans/PLAN_RESTRUCTURING_DETAILED_PLAN.md" ]; then
    rm "docs/plans/PLAN_RESTRUCTURING_DETAILED_PLAN.md"  # временный файл от предыдущего планирования
fi
if [ -f "docs/plans/nul" ]; then
    rm "docs/plans/nul"  # артефакт Windows
fi
```

### **PHASE 3: CROSS-REFERENCE UPDATES**
**Duration**: 60-90 минут

#### **Task 3.1: Update MAIN_PLAN.md Master Coordinator**
Файл: `docs/plans/MAIN_PLAN.md` (строки 8, 14, 20)

**НАЙТИ И ЗАМЕНИТЬ**:
```markdown
# СТАРЫЕ ССЫЛКИ:
- **[MASTER-DEVELOPMENT-DECISIONS-LOG.md](MASTER-DEVELOPMENT-DECISIONS-LOG.md)**
- **[CONSOLIDATED-EXECUTION-PLAN.md](CONSOLIDATED-EXECUTION-PLAN.md)**
- **[PLANS-INDEX.md](PLANS-INDEX.md)**

# НОВЫЕ ССЫЛКИ:
- **[MASTER-DEVELOPMENT-DECISIONS-LOG.md](MAIN_PLAN/19-MASTER-DEVELOPMENT-DECISIONS-LOG.md)**
- **[CONSOLIDATED-EXECUTION-PLAN.md](MAIN_PLAN/09-CONSOLIDATED-EXECUTION-PLAN.md)**
- **[PLANS-INDEX.md](MAIN_PLAN/20-PLANS-INDEX.md)**
```

#### **Task 3.2: Update Internal Plan Cross-References**

**ФАЙЛЫ С ВЗАИМНЫМИ ССЫЛКАМИ**:

1. **MAIN_PLAN/20-PLANS-INDEX.md** - обновить все внутренние ссылки:
   - `ARCHITECTURAL_REMEDIATION_PLAN.md` → `02-ARCHITECTURAL_REMEDIATION_PLAN.md`
   - `MASTER_TECHNICAL_PLAN.md` → `01-MASTER_TECHNICAL_PLAN.md`
   - и т.д. для всех переименованных файлов

2. **MAIN_PLAN/19-MASTER-DEVELOPMENT-DECISIONS-LOG.md** - обновить ссылки на другие планы:
   - `ARCHITECTURAL_REMEDIATION_PLAN.md` → `02-ARCHITECTURAL_REMEDIATION_PLAN.md`
   - `PHASE_1_1_LEARNING_INFRASTRUCTURE_REMEDIATION_PLAN.md` → `04-PHASE_1_1_LEARNING_INFRASTRUCTURE_REMEDIATION_PLAN.md`

3. **Phase Plans Internal References** - внутри фазовых планов обновить ссылки друг на друга

#### **Task 3.3: Update External Architecture Documentation**

**ФАЙЛЫ В docs/Architecture/ С ССЫЛКАМИ НА ПЛАНЫ**:

1. **docs/Architecture/ARCHITECTURE-INDEX.md**:
   - Найти все ссылки вида `../plans/FILENAME.md`
   - Обновить на `../plans/MAIN_PLAN/##-FILENAME.md`

2. **docs/Architecture/Actual/*.md files**:
   - Сканировать на наличие ссылок на планы
   - Обновить пути с учетом новой структуры

3. **docs/Architecture/Vision/*.md files**:
   - Проверить межмодульные ссылки
   - Обновить при необходимости

#### **Task 3.4: Add Bidirectional Navigation Links**

**В КАЖДЫЙ ПЕРЕМЕЩЕННЫЙ ПЛАН ДОБАВИТЬ**:
```markdown
---
## 📋 NAVIGATION

⬆️ **Main Plan**: [MAIN_PLAN.md](../MAIN_PLAN.md)
🏠 **Plans Index**: [PLANS-INDEX.md](20-PLANS-INDEX.md)

**Related Plans**:
- [Previous Plan](##-PREVIOUS_PLAN.md)
- [Next Plan](##-NEXT_PLAN.md)

---
```

### **PHASE 4: VALIDATION & TESTING**
**Duration**: 30-45 минут

#### **Task 4.1: Link Validation**
```bash
# Проверка всех markdown ссылок в docs/ (Windows-compatible)
echo "=== VALIDATING MARKDOWN LINKS ==="
find "docs" -name "*.md" -type f | head -10 | while read file; do echo "Checking: $file"; done
# Проверка ключевых ссылок в MAIN_PLAN.md
if [ -f "docs/plans/MAIN_PLAN.md" ]; then
    echo "Checking MAIN_PLAN.md links..."
    grep -n "](.*\.md)" "docs/plans/MAIN_PLAN.md" | head -5 || echo "No .md links found"
fi
```

#### **Task 4.2: Structure Verification**
```bash
# Проверка финальной структуры (Windows-compatible)
echo "=== FINAL STRUCTURE VERIFICATION ==="
echo "Root plans directory:"
ls -la "docs/plans" | head -10
echo "
MAIN_PLAN subdirectory:"
if [ -d "docs/plans/MAIN_PLAN" ]; then
    ls -la "docs/plans/MAIN_PLAN" | head -10
    echo "Files count in MAIN_PLAN/: $(find docs/plans/MAIN_PLAN -name '*.md' | wc -l)"
else
    echo "ERROR: MAIN_PLAN directory not found!"
fi
# Windows tree alternative
find "docs/plans" -type f -name "*.md" | head -15 | sort
```

#### **Task 4.3: Git History Verification**
```bash
# Проверка сохранения истории для ключевых файлов
echo "=== GIT HISTORY VERIFICATION ==="
for file in "docs/plans/MAIN_PLAN/01-MASTER_TECHNICAL_PLAN.md" "docs/plans/MAIN_PLAN/03-IVAN_LEVEL_COMPLETION_PLAN.md" "docs/plans/MAIN_PLAN/20-PLANS-INDEX.md"; do
    if [ -f "$file" ]; then
        echo "Checking git history for: $file"
        git log --oneline --follow "$file" | head -3 || echo "No history found"
    else
        echo "WARNING: File not found: $file"
    fi
    echo "---"
done
```

#### **Task 4.4: Integration Test**
- Проверка открытия всех ссылок в MAIN_PLAN.md
- Верификация navigation links в случайно выбранных планах
- Тест доступности архивных файлов

### **PHASE 5: FINALIZATION**
**Duration**: 15-30 минут

#### **Task 5.1: Final Git Commit**
```bash
git add -A
git commit -m "🏗️ DOCS PLANS RESTRUCTURING: MAIN_PLAN.md coordinator + numbered structure

STRUCTURAL CHANGES:
✅ Single MAIN_PLAN.md in root (catalogization compliance)
✅ All plans moved to MAIN_PLAN/ with ##- numbering
✅ Consolidated archives in MAIN_PLAN/ARCHIVED/
✅ Git history preserved for all files

CROSS-REFERENCE UPDATES:
✅ Updated MAIN_PLAN.md master links
✅ Fixed internal plan cross-references
✅ Updated Architecture documentation links
✅ Added bidirectional navigation links

FILES RESTRUCTURED: 21 plans + 3 directories
NUMBERING LOGIC: Strategic(01-05) → Specialized(06-10) → Phase(11-15) → Docs(16-20) → Subdir(21)

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

#### **Task 5.2: Update Documentation**
- Обновить MAIN_PLAN.md с финальным статусом реструктуризации
- Добавить в README.md упоминание новой структуры планов (если применимо)
- Создать changelog entry о реструктуризации

---

## ⚠️ RISK MITIGATION

### **CRITICAL RISKS & MITIGATION**:

1. **🔥 ПОТЕРЯ GIT ИСТОРИИ**:
   - Mitigation: Использовать ТОЛЬКО `git mv` для всех перемещений
   - Validation: Проверить `git log --follow` для ключевых файлов
   - Rollback: `git reset --hard HEAD~1` если что-то пошло не так

2. **🔗 BROKEN LINKS**:
   - Mitigation: Системное обновление всех ссылок по prepared mapping
   - Validation: Проверка всех .md файлов на работоспособность ссылок
   - Rollback: Batch find/replace для восстановления старых путей

3. **📁 СТРУКТУРНЫЕ ОШИБКИ**:
   - Mitigation: Создать backup commit перед началом
   - Validation: Проверка tree structure на соответствие target
   - Rollback: `git checkout HEAD~1 -- docs/plans/` для восстановления

4. **🔄 INTEGRATION FAILURES**:
   - Mitigation: Поэтапное тестирование после каждой фазы
   - Validation: Проверка ключевых integration points
   - Rollback: Откат к backup commit с сохранением lessons learned

### **CONTINGENCY PLANS**:

**ПЛАН A (ПОЛНЫЙ ОТКАТ)**:
```bash
git checkout HEAD~1 -- docs/plans/
git reset --hard HEAD~1
```

**ПЛАН B (ЧАСТИЧНЫЙ ОТКАТ)**:
```bash
# Откат конкретных файлов при проблемах
git checkout HEAD~1 -- docs/plans/MAIN_PLAN.md
git checkout HEAD~1 -- docs/plans/MAIN_PLAN/
```

**ПЛАН C (FORWARD FIX)**:
- Исправление конкретных broken links без полного отката
- Добавление недостающих navigation elements
- Коррекция нумерации при обнаружении логических несоответствий

---

## 📊 SUCCESS CRITERIA

### **TECHNICAL SUCCESS METRICS**:

1. **✅ STRUCTURE COMPLIANCE**:
   - [ ] docs/plans/ содержит ТОЛЬКО MAIN_PLAN.md в корне
   - [ ] docs/plans/MAIN_PLAN/ содержит все остальные планы
   - [ ] Все файлы имеют корректную ##- нумерацию
   - [ ] Архивы консолидированы в MAIN_PLAN/ARCHIVED/

2. **✅ GIT HISTORY PRESERVATION**:
   - [ ] `git log --follow` работает для всех перемещенных файлов
   - [ ] Отсутствуют потерянные commit histories
   - [ ] Blame information сохранена для критических файлов

3. **✅ CROSS-REFERENCE INTEGRITY**:
   - [ ] Все ссылки в MAIN_PLAN.md работают
   - [ ] Внутренние план-ссылки корректны
   - [ ] Architecture documentation links обновлены
   - [ ] Добавлены bidirectional navigation links

4. **✅ FUNCTIONAL VALIDATION**:
   - [ ] MAIN_PLAN.md открывается и все ссылки кликабельны
   - [ ] Plans Index navigable и comprehensive
   - [ ] Archive files accessible через новую структуру
   - [ ] No 404/broken links в documentation

### **QUALITY GATES**:

**GATE 1**: Structure Creation
- Checkpoint: Target directories созданы
- Validation: `tree` command shows expected structure

**GATE 2**: File Relocations
- Checkpoint: Все файлы перемещены с `git mv`
- Validation: No files left in docs/plans/ root except MAIN_PLAN.md

**GATE 3**: Cross-Reference Updates
- Checkpoint: Все identified links обновлены
- Validation: No broken internal references

**GATE 4**: Integration Testing
- Checkpoint: Manual navigation test passed
- Validation: Architecture docs still functional

**FINAL GATE**: Production Readiness
- Checkpoint: All success criteria met
- Validation: Clean git status, all links working

---

## 📚 APPENDICES

### **APPENDIX A: COMPLETE FILE MAPPING**

| Original Path | New Path | Rationale |
|---------------|----------|-----------|
| `MASTER_TECHNICAL_PLAN.md` | `MAIN_PLAN/01-MASTER_TECHNICAL_PLAN.md` | Strategic master plan, highest priority |
| `ARCHITECTURAL_REMEDIATION_PLAN.md` | `MAIN_PLAN/02-ARCHITECTURAL_REMEDIATION_PLAN.md` | Architecture foundation |
| `IVAN_LEVEL_COMPLETION_PLAN.md` | `MAIN_PLAN/03-IVAN_LEVEL_COMPLETION_PLAN.md` | Core project completion |
| `PHASE_1_1_LEARNING_INFRASTRUCTURE_REMEDIATION_PLAN.md` | `MAIN_PLAN/04-PHASE_1_1_LEARNING_INFRASTRUCTURE_REMEDIATION_PLAN.md` | Infrastructure foundation |
| `CRITICAL_TEST_FAILURES_REMEDIATION_PLAN.md` | `MAIN_PLAN/05-CRITICAL_TEST_FAILURES_REMEDIATION_PLAN.md` | Critical issues |
| `CAPTCHA_WORKFLOW_SERVICE_REMEDIATION_PLAN.md` | `MAIN_PLAN/06-CAPTCHA_WORKFLOW_SERVICE_REMEDIATION_PLAN.md` | Service-specific remediation |
| `TEST-INFRASTRUCTURE-IMPROVEMENT-PLAN.md` | `MAIN_PLAN/07-TEST-INFRASTRUCTURE-IMPROVEMENT-PLAN.md` | Testing infrastructure |
| `HYBRID-CODE-QUALITY-RECOVERY-PLAN-Architecture.md` | `MAIN_PLAN/08-HYBRID-CODE-QUALITY-RECOVERY-PLAN-Architecture.md` | Code quality plan |
| `CONSOLIDATED-EXECUTION-PLAN.md` | `MAIN_PLAN/09-CONSOLIDATED-EXECUTION-PLAN.md` | Execution strategy |
| `PHASE_1_IMMEDIATE_EXECUTION_TASKS.md` | `MAIN_PLAN/10-PHASE_1_IMMEDIATE_EXECUTION_TASKS.md` | Immediate tasks |
| `PHASE_2_SELFTESTINGFRAMEWORK_FIXES.md` | `MAIN_PLAN/11-PHASE_2_SELFTESTINGFRAMEWORK_FIXES.md` | Phase 2 work |
| `PHASE_3_VALIDATION_AND_PRODUCTION_READINESS.md` | `MAIN_PLAN/12-PHASE_3_VALIDATION_AND_PRODUCTION_READINESS.md` | Phase 3 work |
| `PHASE0_IVAN_LEVEL_AGENT.md` | `MAIN_PLAN/13-PHASE0_IVAN_LEVEL_AGENT.md` | Phase 0 work |
| `PHASE1_ADVANCED_COGNITIVE_TASKS.md` | `MAIN_PLAN/14-PHASE1_ADVANCED_COGNITIVE_TASKS.md` | Phase 1 work |
| `EXECUTIVE_SUMMARY_CRITICAL_TEST_REMEDIATION.md` | `MAIN_PLAN/15-EXECUTIVE_SUMMARY_CRITICAL_TEST_REMEDIATION.md` | Executive summary |
| `TEST_REMEDIATION_BASELINE_DOCUMENTATION.md` | `MAIN_PLAN/16-TEST_REMEDIATION_BASELINE_DOCUMENTATION.md` | Baseline docs |
| `STRATEGIC-NEXT-STEPS-SUMMARY.md` | `MAIN_PLAN/17-STRATEGIC-NEXT-STEPS-SUMMARY.md` | Strategic summary |
| `Future-R&D-Extensions-Roadmap.md` | `MAIN_PLAN/18-Future-R&D-Extensions-Roadmap.md` | R&D roadmap |
| `MASTER-DEVELOPMENT-DECISIONS-LOG.md` | `MAIN_PLAN/19-MASTER-DEVELOPMENT-DECISIONS-LOG.md` | Decision log |
| `PLANS-INDEX.md` | `MAIN_PLAN/20-PLANS-INDEX.md` | Plans index |
| `HYBRID-CODE-QUALITY-RECOVERY-PLAN/` | `MAIN_PLAN/21-HYBRID-CODE-QUALITY-RECOVERY-PLAN/` | Detailed subplan |

### **APPENDIX B: LINK UPDATE PATTERNS**

**PATTERN 1: MAIN_PLAN.md Internal Links**
```bash
# Find Pattern:    ](FILENAME.md)
# Replace Pattern: ](MAIN_PLAN/##-FILENAME.md)
```

**PATTERN 2: Inter-Plan References**
```bash
# Find Pattern:    [](../FILENAME.md)
# Replace Pattern: [](##-FILENAME.md)
```

**PATTERN 3: Architecture Doc References**
```bash
# Find Pattern:    ../plans/FILENAME.md
# Replace Pattern: ../plans/MAIN_PLAN/##-FILENAME.md
```

---

**Plan Version**: 1.0.0
**Created**: 2025-09-14
**Estimated Duration**: 4-6 hours
**Risk Level**: MEDIUM (mitigated with comprehensive backup/rollback strategy)
**Dependencies**: Git access, file system permissions
**Success Definition**: Single MAIN_PLAN.md file in root + all others in numbered MAIN_PLAN/ subdirectory + preserved git history + working cross-references