# Hướng Dẫn Tắt / Bật Hạ Tầng AWS (Staging Demo)

Tài liệu này tổng hợp các lệnh và script giúp **dừng (tắt)** và **bật lại** hạ tầng `ContractGuard-StagingAws` trên AWS một cách nhanh chóng.

---

## ⚠️ Nguyên tắc an toàn quan trọng

> [!WARNING]
> **Xung đột queue giữa AWS và Railway:**
> - Khi **BẬT** API trên AWS, bạn **BẮT BUỘC phải tắt service `api` trên Railway** (để tránh 2 API cùng tranh nhau đọc queue `analysis-results` và làm mất kết quả phân tích / chat của nhau).
> - Khi **TẮT** API trên AWS, bạn có thể bật lại service `api` trên Railway để nhóm tiếp tục phát triển hằng ngày.

---

## Dùng Script Có Sẵn (Nhanh nhất)

Trong thư mục `infra/cdk/` đã có sẵn 2 script quản lý:
- **PowerShell (Windows)**: `manage-aws-staging.ps1`
- **Bash (Git Bash / Linux)**: `manage-aws-staging.sh`

```powershell
# Chạy từ thư mục infra/cdk
cd infra/cdk

# 1. Tắt API (Scale về 0 task)
.\manage-aws-staging.ps1 stop

# 2. Bật lại API (Scale lên 1 task)
.\manage-aws-staging.ps1 start

# 3. Kiểm tra trạng thái hiện tại
.\manage-aws-staging.ps1 status
```
*(Nếu dùng Git Bash thì thay bằng `./manage-aws-staging.sh stop|start|status`)*

---

## Cách 1: Tắt / Bật Nhanh Bằng Lệnh Trực Tiếp (Scale ECS Fargate về 0 task)

- **Mục đích**: Tắt container API khi hết giờ làm việc/test. Tiết kiệm chi phí compute Fargate.
- **Thời gian**: ~30 giây đến 1 phút.
- **Ưu điểm lớn nhất**: **Giữ nguyên URL CloudFront (`AppUrl`)**, không phải cấu hình lại CORS S3 hay Google OAuth.

### Dành cho PowerShell (Windows Terminal)

#### 👉 TẮT (Stop API):
```powershell
# Tắt task API trên AWS ECS
$Cluster = (aws ecs list-clusters --profile contractguard-admin --region ap-southeast-1 --query "clusterArns[?contains(@, 'ContractGuard-StagingAws')] | [0]" --output text)
$Service = (aws ecs list-services --cluster $Cluster --profile contractguard-admin --region ap-southeast-1 --query "serviceArns[?contains(@, 'Api')] | [0]" --output text)
aws ecs update-service --cluster $Cluster --service $Service --desired-count 0 --profile contractguard-admin --region ap-southeast-1
Write-Host "✅ Đã gửi lệnh tắt API ECS (DesiredCount = 0). Đừng quên bật lại API trên Railway nếu cần." -ForegroundColor Green
```

#### 👉 BẬT LẠI (Start API):
```powershell
# Bật lại task API trên AWS ECS
$Cluster = (aws ecs list-clusters --profile contractguard-admin --region ap-southeast-1 --query "clusterArns[?contains(@, 'ContractGuard-StagingAws')] | [0]" --output text)
$Service = (aws ecs list-services --cluster $Cluster --profile contractguard-admin --region ap-southeast-1 --query "serviceArns[?contains(@, 'Api')] | [0]" --output text)
aws ecs update-service --cluster $Cluster --service $Service --desired-count 1 --profile contractguard-admin --region ap-southeast-1
Write-Host "✅ Đã gửi lệnh bật API ECS (DesiredCount = 1). Nhớ TẮT service api trên Railway!" -ForegroundColor Yellow
```

#### 👉 KIỂM TRA TRẠNG THÁI (Status):
```powershell
$Cluster = (aws ecs list-clusters --profile contractguard-admin --region ap-southeast-1 --query "clusterArns[?contains(@, 'ContractGuard-StagingAws')] | [0]" --output text)
$Service = (aws ecs list-services --cluster $Cluster --profile contractguard-admin --region ap-southeast-1 --query "serviceArns[?contains(@, 'Api')] | [0]" --output text)
aws ecs describe-services --cluster $Cluster --services $Service --profile contractguard-admin --region ap-southeast-1 --query "services[0].{DesiredCount:desiredCount, RunningCount:runningCount, Status:status}" --output table
```

---

### Dành cho Git Bash / Linux / macOS

#### 👉 TẮT (Stop API):
```bash
CLUSTER=$(aws ecs list-clusters --profile contractguard-admin --region ap-southeast-1 --query "clusterArns[?contains(@, 'ContractGuard-StagingAws')] | [0]" --output text)
SERVICE=$(aws ecs list-services --cluster "$CLUSTER" --profile contractguard-admin --region ap-southeast-1 --query "serviceArns[?contains(@, 'Api')] | [0]" --output text)
aws ecs update-service --cluster "$CLUSTER" --service "$SERVICE" --desired-count 0 --profile contractguard-admin --region ap-southeast-1
echo "✅ Đã tắt API ECS (DesiredCount = 0)."
```

#### 👉 BẬT LẠI (Start API):
```bash
CLUSTER=$(aws ecs list-clusters --profile contractguard-admin --region ap-southeast-1 --query "clusterArns[?contains(@, 'ContractGuard-StagingAws')] | [0]" --output text)
SERVICE=$(aws ecs list-services --cluster "$CLUSTER" --profile contractguard-admin --region ap-southeast-1 --query "serviceArns[?contains(@, 'Api')] | [0]" --output text)
aws ecs update-service --cluster "$CLUSTER" --service "$SERVICE" --desired-count 1 --profile contractguard-admin --region ap-southeast-1
echo "✅ Đã bật API ECS (DesiredCount = 1). Nhớ tắt service api trên Railway!"
```

---

## Cách 2: Hạ Toàn Bộ Hạ Tầng (`cdk destroy`) - Dừng dài hạn

Dùng khi đã kết thúc showcase/demo hoặc không dùng trong nhiều tuần, muốn **tiết kiệm 100% chi phí** (kể cả phí duy trì ALB ~$16/tháng).

> [!NOTE]
> S3 tài liệu (`contractguard-dev-documents-*`) và 3 hàng đợi SQS dev là tài nguyên import sẵn, `cdk destroy` **hoàn toàn không xóa** chúng. Dữ liệu tài liệu và DB Supabase an toàn.

### 👉 HẠ TOÀN BỘ (Destroy):
Chạy từ thư mục gốc dự án hoặc thư mục `infra/cdk`:
```powershell
cd infra/cdk
npx aws-cdk@2.1149.0 destroy ContractGuard-StagingAws --profile contractguard-admin
```
*(Sau đó bật lại service `api` trên Railway để làm việc bình thường)*

---

### 👉 TRIỂN KHAI LẠI TỪ ĐẦU (Deploy lại khi có demo):

#### Bước 1: Build Frontend và Deploy Stack
```powershell
# 1. Build frontend SPA
cd frontend
npm ci
npm run build

# 2. Deploy lại CDK
cd ../infra/cdk
npx aws-cdk@2.1149.0 deploy ContractGuard-StagingAws --profile contractguard-admin
```
*(Hoặc chạy tay workflow **Deploy: Staging (AWS)** trên GitHub Actions nhánh `main`)*

#### Bước 2: Cập nhật cấu hình theo URL CloudFront mới
Sau khi deploy xong, CloudFormation output sẽ in ra `AppUrl` (dạng `https://<random-id>.cloudfront.net`). Cần làm 3 việc:
1. **Tắt service `api` trên Railway**.
2. **Cập nhật CORS S3 bucket**:
   Thêm `AppUrl` mới vào `AllowedOrigins` của bucket `contractguard-dev-documents-699061129385` (không xóa các origin localhost / railway cũ).
3. **Cập nhật Google OAuth**:
   Vào Google Cloud Console > Credentials > OAuth 2.0 Client ID > thêm Redirect URI:
   `<AppUrl>/api/v1/auth/google/callback`
