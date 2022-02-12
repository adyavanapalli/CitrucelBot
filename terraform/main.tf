terraform {
  backend "gcs" {
    bucket = "citrucelbot.starsandmanifolds.xyz"
  }
  required_providers {
    google = {
      source = "hashicorp/google"
    }
    random = {
      source = "hashicorp/random"
    }
  }
}

provider "google" {
  project = var.project
  region  = var.region
  zone    = var.zone
}

resource "random_pet" "google_pubsub_topic" {
}

resource "random_pet" "google_cloud_scheduler_job" {
}

resource "random_pet" "google_cloud_tasks_queue" {
}

resource "random_pet" "google_storage_bucket" {
}

resource "random_pet" "google_cloudfunctions_function" {
}

resource "random_pet" "google_service_account" {
}

resource "google_project_service" "iam_service" {
  service = "iam.googleapis.com"
}

resource "google_project_service" "pubsub_service" {
  service = "pubsub.googleapis.com"

  disable_dependent_services = true
}

resource "google_project_service" "cloudresourcemanager_service" {
  service = "cloudresourcemanager.googleapis.com"
}

resource "google_project_service" "cloudbuild_service" {
  service = "cloudbuild.googleapis.com"

  depends_on = [
    google_project_service.cloudresourcemanager_service
  ]
}

resource "google_project_service" "cloudfunctions_service" {
  service = "cloudfunctions.googleapis.com"

  depends_on = [
    google_project_service.cloudresourcemanager_service
  ]
}

resource "google_project_service" "cloudscheduler_service" {
  service = "cloudscheduler.googleapis.com"

  depends_on = [
    google_project_service.cloudresourcemanager_service
  ]
}

resource "google_project_service" "cloudtasks_service" {
  service = "cloudtasks.googleapis.com"

  depends_on = [
    google_project_service.cloudresourcemanager_service
  ]
}

resource "google_service_account" "service_account" {
  account_id = random_pet.google_service_account.id

  depends_on = [
    google_project_service.iam_service
  ]
}

resource "google_project_iam_binding" "iam_binding" {
  project = var.project
  role    = "roles/cloudtasks.enqueuer"

  members = [
    "serviceAccount:${google_service_account.service_account.email}"
  ]
}

resource "google_pubsub_topic" "topic" {
  name = random_pet.google_pubsub_topic.id

  depends_on = [
    google_project_service.pubsub_service
  ]
}

resource "google_cloud_scheduler_job" "job" {
  name = random_pet.google_cloud_scheduler_job.id

  pubsub_target {
    topic_name = google_pubsub_topic.topic.id

    data = base64encode("GNU Terry Pratchett")
  }
  schedule  = "0 18 * * *"
  time_zone = "America/New_York"

  depends_on = [
    google_project_service.cloudscheduler_service
  ]
}

resource "google_cloud_tasks_queue" "queue" {
  location = var.region

  name = random_pet.google_cloud_tasks_queue.id
  stackdriver_logging_config {
    sampling_ratio = 1.0
  }

  depends_on = [
    google_project_service.cloudtasks_service
  ]
}

resource "google_storage_bucket" "bucket" {
  name     = random_pet.google_storage_bucket.id
  location = var.region

  uniform_bucket_level_access = true
  logging {
    log_bucket = "_Default"
  }
}

data "archive_file" "source" {
  output_path = "CitrucelBot.zip"
  source_dir  = "../CitrucelBot"
  type        = "zip"
  excludes    = fileset("../CitrucelBot", "{bin,obj}/**")
}

resource "google_storage_bucket_object" "object" {
  name   = "${data.archive_file.source.output_sha}-${data.archive_file.source.output_path}"
  bucket = google_storage_bucket.bucket.name
  source = data.archive_file.source.output_path
}

resource "google_cloudfunctions_function" "function" {
  name    = random_pet.google_cloudfunctions_function.id
  runtime = "dotnet3"

  available_memory_mb = 128
  entry_point         = "CitrucelBot.Function"
  environment_variables = {
    BOT_TOKEN = var.bot_token
    CHAT_ID   = var.chat_id
    QUEUE_ID  = google_cloud_tasks_queue.queue.id
  }
  event_trigger {
    event_type = "google.pubsub.topic.publish"
    resource   = google_pubsub_topic.topic.id
  }
  ingress_settings      = "ALLOW_INTERNAL_ONLY"
  max_instances         = 1
  service_account_email = google_service_account.service_account.email
  source_archive_bucket = google_storage_bucket.bucket.name
  source_archive_object = google_storage_bucket_object.object.name

  depends_on = [
    google_project_service.cloudbuild_service,
    google_project_service.cloudfunctions_service
  ]
}
