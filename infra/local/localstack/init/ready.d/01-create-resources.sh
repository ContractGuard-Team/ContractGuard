#!/bin/bash
# Runs inside LocalStack on every start (state is not persisted).
# Keep names in sync with backend appsettings (Storage, Queue sections).
set -euo pipefail

BUCKET=contractguard-local

# $1 = queue name, $2 = visibility timeout in seconds
create_queue_with_dlq() {
  local name=$1
  local visibility=$2

  local dlq_url dlq_arn
  dlq_url=$(awslocal sqs create-queue --queue-name "$name-dlq" --query QueueUrl --output text)
  dlq_arn=$(awslocal sqs get-queue-attributes --queue-url "$dlq_url" \
    --attribute-names QueueArn --query Attributes.QueueArn --output text)

  cat > "/tmp/$name-attributes.json" <<EOF
{
  "VisibilityTimeout": "$visibility",
  "RedrivePolicy": "{\"deadLetterTargetArn\":\"$dlq_arn\",\"maxReceiveCount\":\"3\"}"
}
EOF
  awslocal sqs create-queue --queue-name "$name" \
    --attributes "file:///tmp/$name-attributes.json" > /dev/null
  echo "Created queue $name (+ $name-dlq)"
}

awslocal s3 mb "s3://$BUCKET"
create_queue_with_dlq analysis-requests 900
create_queue_with_dlq analysis-results 60
