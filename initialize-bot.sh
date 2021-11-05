#!/usr/bin/env bash
#
# initialize-bot.sh
#   This script creates the following Google Cloud Platform (GCP) resources:
#     1. A Cloud Pub/Sub topic.
#     2. A Cloud Scheduler job that publishes messages to the aforementioned
#        topic.
#     3. A Cloud Tasks queue.
#     4. A Secret Manager secret key.
#     5. A service account with the Cloud Tasks Enqueuer and Secret Manager
#        Secret Accessor roles.
#     6. A Cloud Functions function that triggers when messages are published to
#        the aforementioned topic and creates a Cloud Tasks task in the
#        aforementioned queue.

set -o errexit
set -o nounset
set -o pipefail
set -o posix
set -o xtrace


create_pubsub_topic() {
    gcloud pubsub topics create citrucel-topic
}

create_scheduler_job() {
    gcloud scheduler jobs create pubsub citrucel-job \
        --message-body="citrucel-job" \
        --schedule="0 18 * * *" \
        --time-zone="America/New_York" \
        --topic="citrucel-topic"
}

create_task_queue() {
    gcloud tasks queues create citrucel-queue \
        --log-sampling-ratio="1.0"
}

create_secret_manager_key() {
    echo -n "$CITRUCEL_BOT_TOKEN" | gcloud secrets create citrucel-secret \
        --data-file="-"
}

create_service_account() {
    gcloud iam service-accounts create citrucel-sa

    gcloud projects add-iam-policy-binding citrucelbot \
        --member="serviceAccount:citrucel-sa@citrucelbot.iam.gserviceaccount.com" \
        --role="roles/cloudtasks.enqueuer"

    gcloud projects add-iam-policy-binding citrucelbot \
        --member='serviceAccount:citrucel-sa@citrucelbot.iam.gserviceaccount.com' \
        --role='roles/secretmanager.secretAccessor'
}

create_cloud_function() {
    gcloud beta functions deploy citrucel-function \
        --env-vars-file="env/citrucel-function.yaml" \
        --entry-point="Citrucel.Function" \
        --ingress-settings="internal-only" \
        --max-instances="1" \
        --memory="128MB" \
        --runtime="dotnet3" \
        --service-account="citrucel-sa@citrucelbot.iam.gserviceaccount.com" \
        --region="us-east1" \
        --set-secrets "BOT_TOKEN=citrucel-secret:latest" \
        --source="Citrucel" \
        --trigger-topic="citrucel-topic"
}

main() {
    create_pubsub_topic
    create_scheduler_job
    create_task_queue
    create_secret_manager_key
    create_service_account
    create_cloud_function
}

main
