# Baseline Validation Checklist

## BEFORE ANY REFACTORING WORK:

### 1. Full Solution Build Status
```bash
# Check ENTIRE solution build status
dotnet build --verbosity normal

# Count errors per project  
dotnet build 2>&1 | grep -c "error CS"

# Document baseline in commit message
```

### 2. Test Suite Status
```bash
# Run ALL tests, not just target project
dotnet test --logger console --verbosity quiet

# Document test counts: 
# - Total: X/Y passing
# - Unit: A/B passing  
# - Integration: C/D passing
```

### 3. Project-Specific Validation
```bash
# Validate target project independently
dotnet build ProjectName/ProjectName.csproj

# Check dependencies are not broken
dotnet build --verbosity normal | grep "depends on"
```

### 4. Commit Baseline State
```bash
git commit -m "BASELINE: Document pre-refactoring state
- Solution errors: X total
- Core project: Y errors  
- Tests: A/B passing
- Known issues: [list]"
```

### 5. Scope Definition
- Clearly document WHAT will be changed
- Explicitly exclude problematic projects from scope
- Set success criteria BEFORE starting

## EXAMPLE FAILURE:
❌ "Fixed everything" when Web project had pre-existing errors  
✅ "Fixed Core project only, Web errors out of scope"

## AFTER REFACTORING:
- Compare ONLY target scope 
- Document any NEW regressions
- Mark pre-existing issues clearly