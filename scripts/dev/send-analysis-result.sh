#!/usr/bin/env bash
# Plays the AI worker's part for one job, against LocalStack only (no AWS, no cost).
# Same behaviour as send-analysis-result.ps1. Run from the repository root. The result it sends is
# the example in contracts/examples, not an analysis of the uploaded file.
#
# A job exists only when the API runs with Analysis__WorkerEnabled=true (compose: ANALYSIS_WORKER_ENABLED=true).
# Upload a contract and start its analysis in the web app first.
#
#   job=$(docker compose exec -T postgres psql -U contractguard -d contractguard -At -c "SELECT analysis_run_id FROM analysis_run ORDER BY started_at DESC LIMIT 1;")
#   scripts/dev/send-analysis-result.sh "$job" progress splitting_clauses
#   scripts/dev/send-analysis-result.sh "$job" completed
set -euo pipefail

JOB_ID=${1:?usage: send-analysis-result.sh JOB_ID [progress|completed|failed|invalid|poison|pong] [STEP]}
EVENT=${2:-completed}
STEP=${3:-splitting_clauses}
BUCKET=contractguard-local
QUEUE=analysis-results
SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)

local_aws() {
  docker compose exec -T \
    -e AWS_ACCESS_KEY_ID=test -e AWS_SECRET_ACCESS_KEY=test -e AWS_DEFAULT_REGION=ap-southeast-1 \
    localstack aws --endpoint-url http://localhost:4566 "$@"
}

send_message() {
  local body=$1 url
  url=$(local_aws sqs get-queue-url --queue-name "$QUEUE" --query QueueUrl --output text | tr -d '\r')
  printf '%s' "$body" | local_aws sqs send-message --queue-url "$url" --message-body file:///dev/stdin > /dev/null
  echo "Sent to $QUEUE: $body"
}

case "$EVENT" in
  progress) send_message "{\"message_version\":\"1\",\"event_type\":\"progress\",\"job_id\":\"$JOB_ID\",\"step\":\"$STEP\"}" ;;
  failed)   send_message "{\"message_version\":\"1\",\"event_type\":\"failed\",\"job_id\":\"$JOB_ID\",\"failure_reason\":\"Simulated failure\",\"error_code\":\"ERR-UPL-004\"}" ;;
  pong)     send_message "{\"message_version\":\"1\",\"event_type\":\"pong\",\"job_id\":\"$JOB_ID\"}" ;;
  poison)   send_message "{\"message_version\":\"1\",\"event_type\":\"completed\"}" ;;
  completed|invalid)
    row=$(docker compose exec -T postgres psql -U contractguard -d contractguard -tAc \
      "SELECT r.contract_id, v.version_number FROM analysis_run r JOIN contract_version v ON v.contract_version_id = r.contract_version_id WHERE r.analysis_run_id = '$JOB_ID';" | tr -d '\r')
    [ -n "$row" ] || { echo "Job $JOB_ID not found in analysis_run." >&2; exit 1; }
    contract_id=${row%%|*}
    version=${row##*|}
    json=$(sed -e "s/{{JOB_ID}}/$JOB_ID/" -e "s/{{TENANT_ID}}/dev/" -e "s/{{CONTRACT_ID}}/$contract_id/" -e "s/{{CONTRACT_VERSION}}/$version/" \
      "$SCRIPT_DIR/../../contracts/examples/extraction.v2.example.json")
    if [ "$EVENT" = invalid ]; then
      json=${json/\"clauses\": \[/\"clauses\": \"not-an-array\", \"unused\": [}
    fi
    key="results/$JOB_ID/extraction.json"
    printf '%s' "$json" | local_aws s3 cp - "s3://$BUCKET/$key" --content-type application/json > /dev/null
    echo "Uploaded s3://$BUCKET/$key"
    send_message "{\"message_version\":\"1\",\"event_type\":\"completed\",\"job_id\":\"$JOB_ID\",\"result_key\":\"$key\"}"
    ;;
  *) echo "Unknown event $EVENT" >&2; exit 1 ;;
esac
