#!/bin/bash
set -e

PROJECT_ID="gen-lang-client-0493260544"
REGION="europe-west3"
SERVICE_NAME="serviceapotheke-api"
REPO_NAME="serviceapotheke-repo"
IMAGE_TAG="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO_NAME}/${SERVICE_NAME}:$(date +%s)"
DB_INSTANCE="${PROJECT_ID}:${REGION}:serviceapotheke-db"

echo "1. Authenticating with Google Cloud..."
/usr/lib/google-cloud-sdk/bin/gcloud auth configure-docker ${REGION}-docker.pkg.dev --quiet

echo "2. Building the Container Image..."
docker build -t ${IMAGE_TAG} .

echo "3. Pushing to Artifact Registry..."
docker push ${IMAGE_TAG}

echo "4. Deploying to Cloud Run..."
# Inject production secrets
ENCRYPTION_KEY="ABCDEFGHIJKLMNOPQRSTUVWXYZ123456"
JWT_SECRET="vTccveQUGQTOL56EI0X/o3R1wHtjIjoed0NusZ9fKoY="
STRIPE_SECRET="sk_test_DUMMY_KEY_FOR_TESTING"
STRIPE_WEBHOOK_SECRET="whsec_vHsSfexyX3iU3cEipLS0MdBvwlPsEDhX"

/usr/lib/google-cloud-sdk/bin/gcloud run deploy ${SERVICE_NAME} \
  --image ${IMAGE_TAG} \
  --project ${PROJECT_ID} \
  --region ${REGION} \
  --ingress all \
  --add-cloudsql-instances ${DB_INSTANCE} \
  --set-env-vars "DB_ENCRYPTION_KEY=${ENCRYPTION_KEY},ConnectionStrings__DefaultConnection=Host=/cloudsql/${DB_INSTANCE};Database=serviceapotheke-db;Username=appuser;Password=ServiceApotheke2026Strong,SmtpSettings__Server=smtp.ionos.de,SmtpSettings__Port=587,SmtpSettings__SenderName=Service Apotheke,SmtpSettings__SenderEmail=team@serviceapotheke.tech,SmtpSettings__Username=team@serviceapotheke.tech,SmtpSettings__Password=H:Vzs;8.xG=bw+Y,JwtSettings__Secret=${JWT_SECRET},Stripe__SecretKey=${STRIPE_SECRET},Stripe__WebhookSecret=${STRIPE_WEBHOOK_SECRET}" \
  --allow-unauthenticated \
  --quiet

echo "Deployment Successful."
