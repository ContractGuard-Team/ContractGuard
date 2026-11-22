# Hạ tầng AWS cho production (CDK, C#)

Stack `ContractGuard-Production` ở `ap-southeast-1`, tài khoản `699061129385` (SD-18, `docs/04` C7). Chỉ bật khoảng 3 ngày quanh buổi showcase; staging chạy trên Railway (xem cuối file).

```
Trình duyệt ──https──► CloudFront ─┬─ /*      ──► S3 (SPA, OAC)
                                   └─ /api/*  ──► VPC origin ──► ALB nội bộ ──► ECS Fargate ARM64 (API)
API ──► Supabase · S3 documents · SQS requests/results · SES
Worker AI (tài khoản 333596350642) ──► S3 documents · SQS (bucket/queue policy, docs/07 §5)
```

- Không NAT gateway: task API ở subnet public có IP public, security group chỉ cho ALB vào. ALB ở subnet isolated, chỉ CloudFront tới được.
- Chuỗi kết nối database nằm trong Secrets Manager `contractguard/production/database`, tạo tay một lần, không đi qua CloudFormation.

## Chưa deploy được, cần làm trước

- **Domain + HTTPS tới ALB:** chứng chỉ ACM, listener 443, VPC origin `HTTPS_ONLY`, alias domain cho CloudFront. Listener http hiện tại làm API trả lỗi cookie `Secure` (xem comment `shortcut:` trong `AppStack.cs`).
- Project Supabase production (C5) và secret ở trên.
- Profile AWS có quyền admin, ví dụ `aws login --profile contractguard-admin`.

## Deploy (Git Bash, `export AWS_PROFILE=contractguard-admin`)

```bash
npx aws-cdk@2.1149.0 bootstrap aws://699061129385/ap-southeast-1   # lần đầu
(cd frontend && npm ci && npm run build)                             # BucketDeployment đọc frontend/build/client
cd infra/cdk && npx aws-cdk@2.1149.0 deploy
```

Cần Docker đang chạy: CDK build image API cho ARM64 (SDK chạy native rồi biên dịch chéo). Migration chạy riêng bằng `dotnet ef database update` với chuỗi kết nối production (C5). Tắt API: `deploy -c apiTasks=0`. Sau showcase: `cdk destroy` (bucket documents được giữ lại, xóa tay nếu không cần).

## Staging trên Railway

Project `contractguard`, environment `staging`, region Singapore:

| Service | Build | Ghi chú |
|---|---|---|
| `web` | `frontend/Dockerfile` (nginx + SPA) | Domain `https://contractguardweb.up.railway.app`; `API_UPSTREAM=http://${{api.RAILWAY_PRIVATE_DOMAIN}}:8080` |
| `api` | `backend/Dockerfile`, context là gốc repo | Không có domain public. Biến: `ASPNETCORE_ENVIRONMENT=Production`, bucket/queue `contractguard-dev-*`, SES, `Database__ConnectionString` và access key AWS (secret) |

Deploy từ máy (repo của org chưa nối GitHub với Railway):

```bash
railway up --ci -p <project> -e staging -s api                       # ở gốc repo
railway up frontend --path-as-root --ci -p <project> -e staging -s web
```

Đăng nhập Google ở staging đã bật (2026-11-21, OAuth client chế độ Testing, chỉ test user đăng nhập được). Còn thiếu ở staging: SES sandbox chỉ gửi tới người nhận đã xác minh; Data Protection key không lưu bền nên mỗi lần deploy API mọi người phải đăng nhập lại.
