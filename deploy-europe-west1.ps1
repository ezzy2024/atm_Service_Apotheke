$ErrorActionPreference = "Stop"

$PROJECT_ID = "gen-lang-client-0493260544"
$REGION = "europe-west1"
$IMAGE_TAG = "europe-west3-docker.pkg.dev/$PROJECT_ID/serviceapotheke-repo/api:latest"
$DB_INSTANCE = "$PROJECT_ID:europe-west3:serviceapotheke-db-2"

Write-Host "Submitting build to Cloud Build..."
gcloud builds submit --tag $IMAGE_TAG .

Write-Host "Deploying to Cloud Run..."
gcloud run deploy serviceapotheke-api --region $REGION --image $IMAGE_TAG --set-cloudsql-instances $DB_INSTANCE --update-secrets="JwtSettings__Secret=serviceapotheke-jwt-secret:latest,SmtpSettings__Password=serviceapotheke-smtp-password:latest" --quiet

Write-Host "Deployment Complete."

