# Google Cloud Permissions Fix

## Проблема
GitHub Actions не может деплоить в Google Cloud из-за ошибки разрешений:
```
PERMISSION_DENIED: caller does not have permission to act as service account
```

## Решение

### 1. Проверить текущие разрешения
```bash
# В Google Cloud Console
gcloud projects get-iam-policy digitalme-470613
```

### 2. Исправить разрешения для service account
```bash
# Дать права на Cloud Run
gcloud projects add-iam-policy-binding digitalme-470613 \
    --member="serviceAccount:github-actions@digitalme-470613.iam.gserviceaccount.com" \
    --role="roles/run.admin"

# Дать права на Cloud Build
gcloud projects add-iam-policy-binding digitalme-470613 \
    --member="serviceAccount:github-actions@digitalme-470613.iam.gserviceaccount.com" \
    --role="roles/cloudbuild.builds.editor"

# Дать права на Container Registry
gcloud projects add-iam-policy-binding digitalme-470613 \
    --member="serviceAccount:github-actions@digitalme-470613.iam.gserviceaccount.com" \
    --role="roles/storage.admin"

# Дать права Service Account User (КРИТИЧНО!)
gcloud projects add-iam-policy-binding digitalme-470613 \
    --member="serviceAccount:github-actions@digitalme-470613.iam.gserviceaccount.com" \
    --role="roles/iam.serviceAccountUser"
```

### 3. Проверить secrets в GitHub
- `GCP_SA_KEY` должен содержать корректный JSON ключ от service account
- Service account должен иметь права выше

### 4. Временное решение
До исправления разрешений используется:
- ✅ **CI/CD Pipeline** - билд и тесты работают
- ✅ **Docker Images** - собираются в GitHub Container Registry  
- 🚧 **Cloud Deploy** - отключен, используй manual workflow

### 5. После исправления разрешений
1. Вернуть Cloud Build деплой в `ci-cd.yml`
2. Включить автоматический push trigger в `deploy.yml`