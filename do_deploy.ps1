$ErrorActionPreference = "Stop"

Write-Host "Submitting build to Cloud Build..."
gcloud builds submit --tag europe-west3-docker.pkg.dev/gen-lang-client-0493260544/serviceapotheke-repo/api:latest .

Write-Host "Deploying to Cloud Run..."
gcloud run deploy serviceapotheke-api --region europe-west1 --image europe-west3-docker.pkg.dev/gen-lang-client-0493260544/serviceapotheke-repo/api:latest --set-cloudsql-instances gen-lang-client-0493260544:europe-west3:serviceapotheke-db-2 --update-secrets="JwtSettings__Secret=serviceapotheke-jwt-secret:latest,SmtpSettings__Password=serviceapotheke-smtp-password:latest,AdminSettings__Password=serviceapotheke-admin-password:latest" --quiet

Write-Host "DONE"

