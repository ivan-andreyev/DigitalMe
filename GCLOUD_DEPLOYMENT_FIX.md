# 🚀 GCloud Deployment Fix Guide

## 🚨 CRITICAL: Service Account Permissions Issue

### Problem
GitHub Actions deployment fails with:
```
PERMISSION_DENIED: caller does not have permission to act as service account
projects/digitalme-470613/serviceAccounts/115271460646222152701
```

### Root Cause
The `github-actions@digitalme-470613.iam.gserviceaccount.com` service account lacks necessary permissions to impersonate the Cloud Build service account.

## 🛠️ SOLUTION 1: Add IAM Roles (IMMEDIATE)

Run these commands in Google Cloud Console or CLI:

```bash
# Add Service Account Token Creator role
gcloud projects add-iam-policy-binding digitalme-470613 \
  --member="serviceAccount:github-actions@digitalme-470613.iam.gserviceaccount.com" \
  --role="roles/iam.serviceAccountTokenCreator"

# Add Cloud Build Editor role
gcloud projects add-iam-policy-binding digitalme-470613 \
  --member="serviceAccount:github-actions@digitalme-470613.iam.gserviceaccount.com" \
  --role="roles/cloudbuild.builds.editor"

# Add Cloud Run Admin role
gcloud projects add-iam-policy-binding digitalme-470613 \
  --member="serviceAccount:github-actions@digitalme-470613.iam.gserviceaccount.com" \
  --role="roles/run.admin"

# Add Service Account User role
gcloud projects add-iam-policy-binding digitalme-470613 \
  --member="serviceAccount:github-actions@digitalme-470613.iam.gserviceaccount.com" \
  --role="roles/iam.serviceAccountUser"
```

## 🔒 SOLUTION 2: Workload Identity Federation (RECOMMENDED)

More secure approach using Workload Identity:

### Step 1: Create Workload Identity Pool
```bash
gcloud iam workload-identity-pools create github-pool \
  --project=digitalme-470613 \
  --location=global \
  --display-name="GitHub Actions Pool"
```

### Step 2: Create Provider
```bash
gcloud iam workload-identity-pools providers create-oidc github-provider \
  --project=digitalme-470613 \
  --location=global \
  --workload-identity-pool=github-pool \
  --display-name="GitHub Actions Provider" \
  --attribute-mapping="google.subject=assertion.sub,attribute.actor=assertion.actor,attribute.repository=assertion.repository" \
  --issuer-uri="https://token.actions.githubusercontent.com"
```

### Step 3: Bind Service Account
```bash
gcloud iam service-accounts add-iam-policy-binding \
  github-actions@digitalme-470613.iam.gserviceaccount.com \
  --project=digitalme-470613 \
  --role="roles/iam.workloadIdentityUser" \
  --member="principalSet://iam.googleapis.com/projects/PROJECT_NUMBER/locations/global/workloadIdentityPools/github-pool/attribute.repository/ivan-andreyev/DigitalMe"
```

### Step 4: Update deploy.yml
```yaml
- name: Authenticate to Google Cloud
  uses: google-github-actions/auth@v2
  with:
    workload_identity_provider: 'projects/PROJECT_NUMBER/locations/global/workloadIdentityPools/github-pool/providers/github-provider'
    service_account: 'github-actions@digitalme-470613.iam.gserviceaccount.com'
```

## 📊 Current Deployment Status

### ✅ Working Components
- ✅ Cloud Run services: `digitalme-api`, `digitalme-web`
- ✅ Cloud Build configuration (`cloudbuild.yaml`)
- ✅ Docker images build process
- ✅ Environment variables and secrets configuration
- ✅ PostgreSQL Cloud SQL integration

### ❌ Broken Components
- ❌ GitHub Actions automatic deployment (permissions)
- ❌ CI/CD pipeline (MAUI dependency issue) - **FIXED**
- ❌ Security scanning workflows

### 🔧 Recent Fixes Applied
- ✅ Fixed MAUI dependency issues in `develop-ci.yml` and `ci-cd.yml`
- ✅ Re-enabled Docker build step in CI/CD pipeline
- ✅ Updated restore and build steps to exclude MAUI projects

## 🚀 Deployment URLs

After successful deployment:
- **API**: https://digitalme-api-llig7ks2ca-uc.a.run.app
- **Web**: https://digitalme-web-llig7ks2ca-uc.a.run.app

## 🔄 Manual Deployment

Until permissions are fixed, use manual deployment:
```bash
gh workflow run deploy.yml
```

## ⚠️ Security Notes

1. **Never commit service account keys to repository**
2. **Use Workload Identity Federation for production**
3. **Regularly rotate secrets and API keys**
4. **Monitor Cloud Build logs for security issues**

## 📋 Next Steps

1. ✅ Fix Service Account permissions (run Solution 1 commands)
2. ✅ Test deployment with `gh workflow run deploy.yml`
3. ✅ Monitor successful deployment to Cloud Run
4. ⏸️ Consider implementing Workload Identity Federation for enhanced security

---

**Last Updated**: 2025-09-17
**Status**: 🔧 **READY FOR DEPLOYMENT** (after permissions fix)