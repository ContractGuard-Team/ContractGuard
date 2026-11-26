# Staging AWS cho demo (CDK, C#)

Stack `ContractGuard-StagingAws` ở `ap-southeast-1`, tài khoản `699061129385` (SD-18, `docs/04` C7, C9). Chỉ bật khoảng 3 ngày quanh buổi showcase; staging hằng ngày chạy trên Railway (xem cuối file).

**Không phải production** (`docs/04` C9): chưa có domain, SES còn sandbox, DB dùng chung với staging Railway. Production thật cần domain + ACM, SES production access, DB và S3/SQS riêng.

```
Trình duyệt ──https──► CloudFront ─┬─ /*      ──► S3 (SPA, OAC)
                                   └─ /api/*  ──► VPC origin ──http──► ALB nội bộ ──► ECS Fargate ARM64 (API)
API ──► Supabase staging (dùng chung, C9) · S3/SQS bộ dev (import) · SES (không đọc DynamoDB của worker, C9)
Worker AI (tài khoản 333596350642) ──► S3/SQS bộ dev, như staging (docs/07)
```

- Không domain (`docs/04` C9): app ở `https://<id>.cloudfront.net` (output `AppUrl`). ALB chỉ nghe HTTP; API lấy scheme từ `CloudFront-Forwarded-Proto`.
- Bucket và 3 queue là bộ `contractguard-dev-*` được **import**: stack không tạo, `cdk destroy` không xóa. Policy cho worker đã đặt tay sẵn trên chúng.
- Không NAT gateway: task API ở subnet public có IP public, security group chỉ cho ALB vào. ALB ở subnet isolated, chỉ CloudFront tới được.
- Secret tạo tay, không đi qua CloudFormation: `contractguard/staging-aws/database` (chuỗi kết nối, dạng text) và `contractguard/staging-aws/google` (JSON `{"ClientId": "...", "ClientSecret": "..."}`).

## Checklist (Git Bash, `export AWS_PROFILE=contractguard-admin`)

**Trước lần deploy đầu**

1. `aws login --profile contractguard-admin` (tài khoản `699061129385`).
2. AWS Budgets 50% / 80%, email cả nhóm. Tạo trên console, cấp tài khoản (SD-20), không thuộc stack.
3. Database: **dùng chung Supabase staging** (`docs/04` C9), không tạo project mới. Nhớ bật lại project nếu nó đang bị tạm dừng.
4. Tạo hai secret ở trên trong `ap-southeast-1`. Secret database là chuỗi kết nối Supabase staging qua Supavisor **session** `:5432`, `SSL Mode=Require` (C1).
5. Migration mới (nếu có) chạy trước khi deploy, trong thư mục `backend`: `ASPNETCORE_ENVIRONMENT=Cloud dotnet ef database update --project src/ContractGuard.Infrastructure --startup-project src/ContractGuard.Api` (đọc `appsettings.Cloud.local.json`).
6. Kiểm tra mã hóa của 3 queue dev (`aws sqs get-queue-attributes --attribute-names KmsMasterKeyId SqsManagedSseEnabled`). Nếu là KMS khóa riêng thì phải thêm `kms:Decrypt`/`kms:GenerateDataKey` cho task role.
7. (Bỏ) Staging AWS không đọc bảng DynamoDB của worker, nên nhóm AI không phải đổi resource policy. Trang phân tích hiện danh sách bước từ message `progress` trên SQS, không có thanh %.
8. SES sandbox: xác minh email từng người nhận (nhóm, tài khoản test, giám khảo); xin production access. Bị từ chối thì đổi `Email__Provider` sang `gmail` (cần thêm secret Gmail như Railway).

**Deploy** (Docker phải chạy: CDK build image API ARM64)

```bash
npx aws-cdk@2.1149.0 bootstrap aws://699061129385/ap-southeast-1   # lần đầu
(cd frontend && npm ci && npm run build)                             # BucketDeployment đọc frontend/build/client
cd infra/cdk && npx aws-cdk@2.1149.0 deploy ContractGuard-StagingAws
```

Hoặc chạy tay workflow **Deploy: Staging (AWS)** (`.github/workflows/deploy-staging-aws.yml`, nhánh `main`): migration, build SPA, `cdk deploy`. Workflow đăng nhập AWS bằng GitHub OIDC với role `contractguard-github-deploy`, không cần secret trên GitHub. Role này tạo **một lần** từ máy, sau bootstrap (stack riêng, `cdk destroy` stack demo không xóa nó):

```bash
cd infra/cdk && npx aws-cdk@2.1149.0 deploy ContractGuard-GitHubDeploy
```

**Sau deploy** (domain CloudFront đổi sau mỗi lần destroy rồi deploy lại, nên làm lại bước 10–11)

9. **Tắt service `api` trên Railway** suốt thời gian staging AWS chạy. Hai API cùng đọc `analysis-results` thì kết quả và câu trả lời chat của bên kia bị xóa. Bật lại sau `cdk destroy`.
10. CORS của bucket dev: `aws s3api get-bucket-cors --bucket contractguard-dev-documents-699061129385` → thêm `AppUrl` vào `AllowedOrigins` của rule hiện có → `put-bucket-cors`. Không ghi đè các origin cũ (Railway, localhost).
11. Google OAuth client: thêm redirect URI `<AppUrl>/api/v1/auth/google/callback`. Nếu client còn ở chế độ Testing thì thêm test user.

Tắt API mà vẫn giữ stack: `deploy ContractGuard-StagingAws -c apiTasks=0`. Không gửi job cho worker: `-c workerEnabled=false`. Xem hướng dẫn chi tiết và script tắt/bật nhanh tại [OPERATIONS.md](OPERATIONS.md); script bật/tắt kèm 2 EC2 của nhóm AI (proxy, worker) bằng profile `se-cli`, vì máy AI chỉ chạy khi web chạy. Sau showcase: `cdk destroy ContractGuard-StagingAws` (giữ `ContractGuard-GitHubDeploy`), rồi bật lại Railway `api`.

## Staging trên Railway

Project `contractguard`, environment `staging`, region Singapore:

| Service | Build | Ghi chú |
|---|---|---|
| `web` | `frontend/Dockerfile` (nginx + SPA) | Domain `https://contractguardweb.up.railway.app`; `API_UPSTREAM=http://${{api.RAILWAY_PRIVATE_DOMAIN}}:8080` |
| `api` | `backend/Dockerfile`, context là gốc repo | Không có domain public. Biến: `ASPNETCORE_ENVIRONMENT=Production`, bucket/queue `contractguard-dev-*`, SES, `Database__ConnectionString` và access key AWS (secret) |

GitHub Actions: chạy tay workflow **Deploy: Staging (Railway)** (`.github/workflows/deploy-staging-railway.yml`, chọn nhánh `main`). Workflow chạy migration rồi deploy `api`, sau đó `web`. Cần secret `RAILWAY_TOKEN` (project token của env staging) và `STAGING_DATABASE_URL`; thiếu thì workflow bỏ qua và chỉ để lại thông báo. Không chạy khi staging AWS đang bật.

Deploy từ máy (khi workflow chưa có secret):

```bash
railway up --ci -p <project> -e staging -s api                       # ở gốc repo
railway up frontend --path-as-root --ci -p <project> -e staging -s web
```

Đăng nhập Google ở staging đã bật (2026-11-21, OAuth client chế độ Testing, chỉ test user đăng nhập được). Từ C9, Data Protection key lưu trong DB, nên deploy API không còn đăng xuất mọi người.
