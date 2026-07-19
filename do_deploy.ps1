$ErrorActionPreference = "Stop"

Write-Host "Submitting build to Cloud Build..."
gcloud builds submit --tag europe-west3-docker.pkg.dev/gen-lang-client-0493260544/serviceapotheke-repo/api:latest .

Write-Host "Deploying to Cloud Run..."
gcloud run deploy serviceapotheke-api --region europe-west1 --image europe-west3-docker.pkg.dev/gen-lang-client-0493260544/serviceapotheke-repo/api:latest --update-env-vars ConnectionStrings__DefaultConnection="Host=/cloudsql/gen-lang-client-0493260544:europe-west3:serviceapotheke-db-2;Database=serviceapotheke-db;Username=appuser;Password=ServiceApotheke2026Strong" --set-cloudsql-instances gen-lang-client-0493260544:europe-west3:serviceapotheke-db-2 --set-secrets="JwtSettings__Secret=serviceapotheke-jwt-secret:latest,SmtpSettings__Password=serviceapotheke-smtp-password:latest" --quiet

Write-Host "DONE"
