param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("start", "stop", "status")]
    [string]$Action,

    # Leave the two EC2 machines of the AI team as they are.
    [switch]$NoAi
)

# Turns the AWS staging demo on and off together with the two EC2 machines of the AI team (account
# 333596350642): the Squid proxy and the worker. The AI machines run only while the web runs, to save cost.

$AwsProfile = "contractguard-admin"
$AiProfile = if ($env:AI_PROFILE) { $env:AI_PROFILE } else { "se-cli" }
$Region = "ap-southeast-1"

# Instance id of an AI machine by its Name tag (ids change if the AI team rebuilds a machine).
function Get-AiInstance([string]$Name) {
    aws ec2 describe-instances --profile $AiProfile --region $Region `
        --filters "Name=tag:Name,Values=$Name" "Name=instance-state-name,Values=pending,running,stopping,stopped" `
        --query "Reservations[0].Instances[0].InstanceId" --output text
}

function Start-AiMachines {
    # The worker reaches the Internet (model, Qdrant) only through the proxy, so the proxy comes up first.
    foreach ($Name in "ContractRisk-Proxy", "ContractRisk-AI-Worker") {
        $Id = Get-AiInstance $Name
        if (-not $Id -or $Id -eq "None") {
            Write-Error "❌ Không tìm thấy EC2 $Name (profile $AiProfile)."
            exit 1
        }
        # A machine stopped a moment ago is still "stopping" and refuses to start until it is "stopped".
        $State = aws ec2 describe-instances --instance-ids $Id --profile $AiProfile --region $Region --query "Reservations[0].Instances[0].State.Name" --output text
        if ($State -eq "stopping") { aws ec2 wait instance-stopped --instance-ids $Id --profile $AiProfile --region $Region }
        Write-Host "🚀 Bật $Name ($Id)..." -ForegroundColor Yellow
        aws ec2 start-instances --instance-ids $Id --profile $AiProfile --region $Region | Out-Null
        aws ec2 wait instance-running --instance-ids $Id --profile $AiProfile --region $Region
    }
    Write-Host "✅ EC2 của nhóm AI đã chạy. Worker cần thêm 1-2 phút để các container lên." -ForegroundColor Green
}

function Stop-AiMachines {
    foreach ($Name in "ContractRisk-AI-Worker", "ContractRisk-Proxy") {
        $Id = Get-AiInstance $Name
        if ($Id -and $Id -ne "None") {
            Write-Host "🛑 Tắt $Name ($Id)..." -ForegroundColor Yellow
            aws ec2 stop-instances --instance-ids $Id --profile $AiProfile --region $Region | Out-Null
        }
    }
}

Write-Host "🔍 Đang tìm cluster và service ECS của ContractGuard-StagingAws..." -ForegroundColor Cyan

$Cluster = (aws ecs list-clusters --profile $AwsProfile --region $Region --query "clusterArns[?contains(@, 'ContractGuard-StagingAws')] | [0]" --output text)
if (-not $Cluster -or $Cluster -eq "None") {
    Write-Error "❌ Không tìm thấy cluster ECS ContractGuard-StagingAws. Hãy kiểm tra lại stack hoặc profile AWS."
    exit 1
}

$Service = (aws ecs list-services --cluster $Cluster --profile $AwsProfile --region $Region --query "serviceArns[?contains(@, 'Api')] | [0]" --output text)
if (-not $Service -or $Service -eq "None") {
    Write-Error "❌ Không tìm thấy service Api trong cluster $Cluster."
    exit 1
}

switch ($Action) {
    "stop" {
        Write-Host "🛑 Đang tắt API ECS (Scale về 0 task)..." -ForegroundColor Yellow
        aws ecs update-service --cluster $Cluster --service $Service --desired-count 0 --profile $AwsProfile --region $Region | Out-Null
        if (-not $NoAi) { Stop-AiMachines }
        Write-Host "✅ Đã gửi lệnh tắt!" -ForegroundColor Green
        Write-Host "💡 Có thể bật lại service 'api' trên Railway (khi đó cần bật EC2 AI bằng tay nếu muốn phân tích)." -ForegroundColor Gray
    }
    "start" {
        if (-not $NoAi) { Start-AiMachines }
        Write-Host "🚀 Đang bật API ECS (Scale lên 1 task)..." -ForegroundColor Yellow
        aws ecs update-service --cluster $Cluster --service $Service --desired-count 1 --profile $AwsProfile --region $Region | Out-Null
        Write-Host "✅ Đã gửi lệnh bật! Container đang được khởi động." -ForegroundColor Green
        Write-Host "⚠️  QUAN TRỌNG: Hãy đảm bảo đã TẮT service 'api' trên Railway để tránh xung đột SQS!" -ForegroundColor Red
    }
    "status" {
        Write-Host "📊 Trạng thái service Api:" -ForegroundColor Cyan
        aws ecs describe-services --cluster $Cluster --services $Service --profile $AwsProfile --region $Region `
            --query "services[0].{Cluster:clusterArn, Service:serviceName, DesiredCount:desiredCount, RunningCount:runningCount, Status:status}" `
            --output table
        if (-not $NoAi) {
            Write-Host "📊 EC2 của nhóm AI:" -ForegroundColor Cyan
            aws ec2 describe-instances --profile $AiProfile --region $Region `
                --filters "Name=tag:Name,Values=ContractRisk-Proxy,ContractRisk-AI-Worker" `
                --query "Reservations[].Instances[].{Name:Tags[?Key=='Name']|[0].Value, Id:InstanceId, State:State.Name}" `
                --output table
        }
    }
}
