#!/usr/bin/env bash
set -e

ACTION="$1"
PROFILE="contractguard-admin"
REGION="ap-southeast-1"

if [[ "$ACTION" != "start" && "$ACTION" != "stop" && "$ACTION" != "status" ]]; then
  echo "Sử dụng: ./manage-aws-staging.sh [start|stop|status]"
  exit 1
fi

echo "🔍 Đang tìm cluster và service ECS..."
CLUSTER=$(aws ecs list-clusters --profile "$PROFILE" --region "$REGION" --query "clusterArns[?contains(@, 'ContractGuard-StagingAws')] | [0]" --output text)
if [[ -z "$CLUSTER" || "$CLUSTER" == "None" ]]; then
  echo "❌ Không tìm thấy cluster ECS ContractGuard-StagingAws."
  exit 1
fi

SERVICE=$(aws ecs list-services --cluster "$CLUSTER" --profile "$PROFILE" --region "$REGION" --query "serviceArns[?contains(@, 'Api')] | [0]" --output text)
if [[ -z "$SERVICE" || "$SERVICE" == "None" ]]; then
  echo "❌ Không tìm thấy service Api trong cluster."
  exit 1
fi

case "$ACTION" in
  stop)
    echo "🛑 Đang tắt API ECS (DesiredCount = 0)..."
    aws ecs update-service --cluster "$CLUSTER" --service "$SERVICE" --desired-count 0 --profile "$PROFILE" --region "$REGION" > /dev/null
    echo "✅ Đã gửi lệnh tắt! Container API sẽ dừng lại."
    echo "💡 Bạn có thể bật lại service 'api' trên Railway."
    ;;
  start)
    echo "🚀 Đang bật API ECS (DesiredCount = 1)..."
    aws ecs update-service --cluster "$CLUSTER" --service "$SERVICE" --desired-count 1 --profile "$PROFILE" --region "$REGION" > /dev/null
    echo "✅ Đã gửi lệnh bật!"
    echo "⚠️  QUAN TRỌNG: Nhớ TẮT service 'api' trên Railway để tránh xung đột queue SQS!"
    ;;
  status)
    aws ecs describe-services --cluster "$CLUSTER" --services "$SERVICE" --profile "$PROFILE" --region "$REGION" \
      --query "services[0].{Service:serviceName, DesiredCount:desiredCount, RunningCount:runningCount, Status:status}" \
      --output table
    ;;
esac
