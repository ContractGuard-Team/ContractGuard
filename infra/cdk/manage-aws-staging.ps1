param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("start", "stop", "status")]
    [string]$Action
)

$Profile = "contractguard-admin"
$Region = "ap-southeast-1"

Write-Host "🔍 Đang tìm cluster và service ECS của ContractGuard-StagingAws..." -ForegroundColor Cyan

$Cluster = (aws ecs list-clusters --profile $Profile --region $Region --query "clusterArns[?contains(@, 'ContractGuard-StagingAws')] | [0]" --output text)
if (-not $Cluster -or $Cluster -eq "None") {
    Write-Error "❌ Không tìm thấy cluster ECS ContractGuard-StagingAws. Hãy kiểm tra lại stack hoặc profile AWS."
    exit 1
}

$Service = (aws ecs list-services --cluster $Cluster --profile $Profile --region $Region --query "serviceArns[?contains(@, 'Api')] | [0]" --output text)
if (-not $Service -or $Service -eq "None") {
    Write-Error "❌ Không tìm thấy service Api trong cluster $Cluster."
    exit 1
}

switch ($Action) {
    "stop" {
        Write-Host "🛑 Đang tắt API ECS (Scale về 0 task)..." -ForegroundColor Yellow
        aws ecs update-service --cluster $Cluster --service $Service --desired-count 0 --profile $Profile --region $Region | Out-Null
        Write-Host "✅ Đã gửi lệnh tắt! Task API sẽ dừng sau vài giây." -ForegroundColor Green
        Write-Host "💡 Nhớ bật lại service 'api' trên Railway nếu nhóm cần test tiếp." -ForegroundColor Gray
    }
    "start" {
        Write-Host "🚀 Đang bật API ECS (Scale lên 1 task)..." -ForegroundColor Yellow
        aws ecs update-service --cluster $Cluster --service $Service --desired-count 1 --profile $Profile --region $Region | Out-Null
        Write-Host "✅ Đã gửi lệnh bật! Container đang được khởi động." -ForegroundColor Green
        Write-Host "⚠️  QUAN TRỌNG: Hãy đảm bảo đã TẮT service 'api' trên Railway để tránh xung đột SQS!" -ForegroundColor Red
    }
    "status" {
        Write-Host "📊 Trạng thái service Api:" -ForegroundColor Cyan
        aws ecs describe-services --cluster $Cluster --services $Service --profile $Profile --region $Region `
            --query "services[0].{Cluster:clusterArn, Service:serviceName, DesiredCount:desiredCount, RunningCount:runningCount, Status:status}" `
            --output table
    }
}
