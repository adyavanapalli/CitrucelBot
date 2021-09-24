#!/usr/bin/env bash
# create-scheduler-job.sh

set -o errexit
set -o nounset
set -o pipefail
set -o posix
set -o xtrace

MESSAGE="Time to Citrucel it up folks!"
TELEGRAM_BOT_API_BASE_URL="https://api.telegram.org/bot$BOT_TOKEN"
TELEGRAM_BOT_API_METHOD="/sendMessage"

URL="$TELEGRAM_BOT_API_BASE_URL$TELEGRAM_BOT_API_METHOD?chat_id=$CHAT_ID&text=$MESSAGE"

gcloud scheduler jobs create http CitrucelBotJob \
    --schedule="0 20 * * *" \
    --uri="$URL" \
    --time-zone="America/New_York"
