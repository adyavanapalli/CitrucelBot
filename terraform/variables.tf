variable "project" {
  type    = string
  default = "citrucelbot"
}

variable "region" {
  type    = string
  default = "us-east1"
}

variable "zone" {
  type    = string
  default = "us-east1-b"
}

variable "bot_token" {
  type      = string
  sensitive = true
}

variable "chat_id" {
  type      = string
  sensitive = true
}
