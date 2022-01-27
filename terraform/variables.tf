variable "project" {
  type        = string
  default     = "citrucelbot"
  description = "The default project to manage resources in."
}

variable "region" {
  type        = string
  default     = "us-east1"
  description = "The default region to manage resources in."
}

variable "zone" {
  type        = string
  default     = "us-east1-b"
  description = "The default zone to manage resources in."
}

variable "bot_token" {
  type        = string
  sensitive   = true
  description = "The authentication token for this bot to access the Telegram API."
}

variable "chat_id" {
  type        = string
  sensitive   = true
  description = "The unique identifier for the target chat where the bot will send messages to."
}
