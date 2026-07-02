#!/bin/bash
set -e

PROJECT_ID="gen-lang-client-0493260544"
REGION="europe-west3"
SERVICE_NAME="serviceapotheke-api"
DB_INSTANCE="${PROJECT_ID}:${REGION}:serviceapotheke-db"
DB_NAME="serviceapotheke-db"
DB_USER="appuser"
DB_PASS="ServiceApotheke2026Strong"

# Secrets
ENCRYPTION_KEY="ABCDEFGHIJKLMNOPQRSTUVWXYZ123456"
JWT_SECRET="vTccveQUGQTOL56EI0X/o3R1wHtjIjoed0NusZ9fKoY="
STRIPE_SECRET="sk_test_DUMMY_KEY_FOR_TESTING"
STRIPE_WEBHOOK_SECRET="whsec_vHsSfexyX3iU3cEipLS0MdBvwlPsEDhX"

echo "Starting Production Deployment for $SERVICE_NAME using Cloud Buildpacks..."

gcloud run deploy ${SERVICE_NAME} \
  --source . \
  --region ${REGION} \
  --ingress internal-and-cloud-load-balancing \
  --add-cloudsql-instances ${DB_INSTANCE} \
  --set-env-vars "DB_ENCRYPTION_KEY=${ENCRYPTION_KEY},ConnectionStrings__DefaultConnection=Host=/cloudsql/${DB_INSTANCE};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASS},SmtpSettings__Server=smtp.ionos.de,SmtpSettings__Port=587,SmtpSettings__SenderName=Service Apotheke,SmtpSettings__SenderEmail=team@serviceapotheke.tech,SmtpSettings__Username=team@serviceapotheke.tech,SmtpSettings__Password=H:Vzs;8.xG=bw+Y,JwtSettings__Secret=${JWT_SECRET},Stripe__SecretKey=${STRIPE_SECRET},Stripe__WebhookSecret=${STRIPE_WEBHOOK_SECRET}" \
  --quiet

echo "Deployment complete. Configuration synchronized."
