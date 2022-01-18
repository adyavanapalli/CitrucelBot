variable "project" {
  type = string
}

variable "region" {
  type = string
}

variable "zone" {
  type = string
}

variable "bot_token" {
  type      = string
  sensitive = true
}

variable "chat_id" {
  type      = string
  sensitive = true
}
