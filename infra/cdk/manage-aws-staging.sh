#!/usr/bin/env bash
set -e

# Turns the AWS staging demo on and off together with the two EC2 machines of the AI team (account
# 333596350642): the Squid proxy and the worker. The AI machines run only while the web runs, to save cost.
# Usage: ./manage-aws-staging.sh start|stop|status [--no-ai]

ACTION="$1"
WITH_AI=true
[[ "$2" == "--no-ai" ]] && WITH_AI=false
PROFILE="contractguard-admin"
AI_PROFILE="${AI_PROFILE:-se-cli}"
REGION="ap-southeast-1"

if [[ "$ACTION" != "start" && "$ACTION" != "stop" && "$ACTION" != "status" ]]; then
  echo "Sử dụng: ./manage-aws-staging.sh [start|stop|status] [--no-ai]"
  exit 1
fi

# Instance id of an AI machine by its Name tag (ids change if the AI team rebuilds a machine).
ai_instance() {
  aws ec2 describe-instances --profile "$AI_PROFILE" --region "$REGION" \
    --filters "Name=tag:Name,Values=$1" "Name=instance-state-name,Values=pending,running,stopping,stopped" \
    --query "Reservations[0].Instances[0].InstanceId" --output text
}

ai_start() {
  # The worker reaches the Internet (model, Qdrant) only through the proxy, so the proxy comes up first.
  for NAME in ContractRisk-Proxy ContractRisk-AI-Worker; do
    ID=$(ai_instance "$NAME")
    if [[ -z "$ID" || "$ID" == "None" ]]; then
      echo "❌ Không tìm thấy EC2 $NAME (profile $AI_PROFILE)."
      exit 1
    fi
    # A machine stopped a moment ago is still "stopping" and refuses to start until it is "stopped".
    STATE=$(aws ec2 describe-instances --instance-ids "$ID" --profile "$AI_PROFILE" --region "$REGION" --query "Reservations[0].Instances[0].State.Name" --output text)
    if [[ "$STATE" == "stopping" ]]; then
      aws ec2 wait instance-stopped --instance-ids "$ID" --profile "$AI_PROFILE" --region "$REGION"
    fi
    echo "🚀 Bật $NAME ($ID)..."
    aws ec2 start-instances --instance-ids "$ID" --profile "$AI_PROFILE" --region "$REGION" > /dev/null
    aws ec2 wait instance-running --instance-ids "$ID" --profile "$AI_PROFILE" --region "$REGION"
  done
  echo "✅ EC2 của nhóm AI đã chạy. Worker cần thêm 1-2 phút để các container lên."
}

ai_stop() {
  for NAME in ContractRisk-AI-Worker ContractRisk-Proxy; do
    ID=$(ai_instance "$NAME")
    if [[ -n "$ID" && "$ID" != "None" ]]; then
      echo "🛑 Tắt $NAME ($ID)..."
      aws ec2 stop-instances --instance-ids "$ID" --profile "$AI_PROFILE" --region "$REGION" > /dev/null
    fi
  done
}

ai_status() {
  aws ec2 describe-instances --profile "$AI_PROFILE" --region "$REGION" \
    --filters "Name=tag:Name,Values=ContractRisk-Proxy,ContractRisk-AI-Worker" \
    --query "Reservations[].Instances[].{Name:Tags[?Key=='Name']|[0].Value, Id:InstanceId, State:State.Name}" \
    --output table
}

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
    if $WITH_AI; then ai_stop; fi
    echo "✅ Đã gửi lệnh tắt!"
    echo "💡 Bạn có thể bật lại service 'api' trên Railway (khi đó cần bật EC2 AI bằng tay nếu muốn phân tích)."
    ;;
  start)
    if $WITH_AI; then ai_start; fi
    echo "🚀 Đang bật API ECS (DesiredCount = 1)..."
    aws ecs update-service --cluster "$CLUSTER" --service "$SERVICE" --desired-count 1 --profile "$PROFILE" --region "$REGION" > /dev/null
    echo "✅ Đã gửi lệnh bật!"
    echo "⚠️  QUAN TRỌNG: Nhớ TẮT service 'api' trên Railway để tránh xung đột queue SQS!"
    ;;
  status)
    aws ecs describe-services --cluster "$CLUSTER" --services "$SERVICE" --profile "$PROFILE" --region "$REGION" \
      --query "services[0].{Service:serviceName, DesiredCount:desiredCount, RunningCount:runningCount, Status:status}" \
      --output table
    if $WITH_AI; then ai_status; fi
    ;;
esac
