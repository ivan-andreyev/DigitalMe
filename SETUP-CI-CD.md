# Настройка CI/CD для автоматического деплоя в GCloud

## ✅ Что уже настроено:

1. **GitHub Actions workflow** создан в `.github/workflows/deploy.yml`
2. **Service Account** создан: `github-actions@digitalme-470613.iam.gserviceaccount.com`
3. **Права назначены**:
   - `roles/cloudbuild.builds.editor` - для запуска Cloud Build
   - `roles/run.developer` - для деплоя в Cloud Run  
   - `roles/storage.admin` - для работы с Container Registry
4. **Service Account ключ** создан: `github-actions-key.json`

## ⚡ Что нужно сделать вручную:

### 1. Добавить секрет в GitHub репозиторий

1. Иди на https://github.com/ivpet/DigitalMe/settings/secrets/actions
2. Нажми **"New repository secret"**
3. Name: `GCP_SA_KEY`
4. Value: **скопируй всё содержимое файла `github-actions-key.json`**

### 2. Проверить работу

После добавления секрета:
1. Сделай коммит в master: `git push origin master`
2. GitHub Action запустится автоматически
3. Проверь результат на: https://github.com/ivpet/DigitalMe/actions

## 🚀 Результат

После каждого пуша в master:
- Автоматически запустится Cloud Build
- Соберутся Docker образы для API и Web
- Деплой произойдёт в Cloud Run:
  - **API**: https://digitalme-api-llig7ks2ca-uc.a.run.app
  - **Web**: https://digitalme-web-llig7ks2ca-uc.a.run.app

## 🔧 Ручной запуск деплоя

Можно также запустить деплой вручную:
```bash
# В корне проекта
gcloud builds submit --config=cloudbuild.yaml --project=digitalme-470613
```

## 🛡️ Безопасность

- Service Account ключ НЕ добавлен в репозиторий
- Все секреты хранятся в GitHub Secrets
- Минимальные права для Service Account