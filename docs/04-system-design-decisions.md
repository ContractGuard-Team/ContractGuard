# ContractGuard — Phiếu quyết định system design

| | |
|---|---|
| Ngày | 2026-10-27 |
| Người quyết định | SE của nhóm (Approver) |
| Đầu vào | [02-system-design.md](02-system-design.md) bước 1 (đã chốt), [SRS v1.0](spec/03-SRS.md), [03-implementation-phases.md](03-implementation-phases.md) §4, `Idea/Tech stack and flow.pdf`, `security/docs/` và `contracts/` của đồng đội |
| Kết quả | Approver đã trả lời 2026-10-27; kết quả ghi ở `02-system-design.md` §4. SD-04 ban đầu chọn Cognito, sau đó Approver hủy: dùng phương án A (ASP.NET Core Identity); bỏ MFA khỏi MVP (DEC-PRD-022) |

## Cách dùng phiếu

- Mỗi mục có: bối cảnh (yêu cầu nào trong SRS bị ảnh hưởng), các phương án, trade-off, **đề xuất**, và dòng **Quyết định**.
- **Để trống dòng Quyết định nghĩa là đồng ý với đề xuất** (cùng quy ước với phiếu OQ, DEC-GEN-023). Muốn chọn khác thì ghi phương án và lý do, hoặc sửa thẳng nội dung đề xuất.
- Các mục được sắp theo thứ tự cần chốt: mục gắn **[Chặn phase 0–1]** phải chốt trước khi code.
- Việc của nhóm AI (chọn model, loại máy GPU và quota, đếm token tiếng Việt) không nằm trong phiếu này.
- Ba điểm đổi ngày 2026-11-17 (Supabase, nền tảng worker, model tự host) ở mục "Đổi sau khi chốt".

## Tóm tắt đề xuất

| # | Câu hỏi | Đề xuất | Chặn |
|---|---|---|---|
| SD-01 | Kiểu kiến trúc | Modular monolith C# + worker Python + hàng đợi | Phase 0 |
| SD-02 | Chạy service ở đâu | ECS Fargate (ARM) cho API, Fargate Spot cho worker. **Đổi 2026-11-17 (C2):** worker do nhóm AI chọn | Phase 7 |
| SD-03 | Cổng vào và domain | Một domain: CloudFront → S3 (SPA) và `/api/*` → ALB | Phase 1 |
| SD-04 | Xác thực | ASP.NET Core Identity tự quản, cookie httpOnly | Phase 1 |
| SD-05 | Phân quyền và cách ly không gian | Lọc theo không gian ở EF Core + kiểm tra quyền theo tài nguyên | Phase 1 |
| SD-06 | Database | RDS PostgreSQL 18 + pgvector, một instance nhỏ. **Đổi 2026-11-17 (C1):** Supabase; **2026-11-18 (C4):** bảng app ở schema `app` | Phase 0 |
| SD-07 | File storage | Một bucket, tách prefix `quarantine/` và `clean/`, SSE-KMS khóa AWS quản lý | Phase 1 |
| SD-08 | Upload và quét mã độc | Presigned POST giới hạn kích thước; GuardDuty Malware Protection for S3 | Phase 1–2 |
| SD-09 | API giữa frontend và backend | REST + OpenAPI, sinh client TypeScript | Phase 1 |
| SD-10 | Hợp đồng dữ liệu C# ↔ Python | Mở rộng `contracts/extraction.schema.json` của đồng đội, sinh code hai phía | Phase 0 |
| SD-11 | Truyền kết quả qua hàng đợi | Message chỉ chứa ID + đường dẫn S3; cùng một hàng đợi cho tiến trình và kết quả | Phase 1 |
| SD-12 | Worker khởi động từ 0 | Autoscaling theo độ dài hàng đợi; bật sẵn 1 worker theo lịch giờ demo. **Đổi 2026-11-17 (C2, C3):** nhóm AI quyết | Phase 7 |
| SD-13 | Retry và trạng thái job | SQS retry 3 lần + DLQ; C# giữ máy trạng thái, bỏ message trùng | Phase 1 |
| SD-14 | Chatbot: ai truy xuất, ai gọi model | C# truy xuất pgvector và gọi model; prompt do nhóm AI quản lý trong repo. **Đổi 2026-11-17 (C3):** gọi endpoint tự host thay Bedrock | Phase 4 |
| SD-15 | Streaming câu trả lời chat | Server-Sent Events (SSE) | Phase 4 |
| SD-16 | Email | Amazon SES; local dùng Mailpit. **Đổi 2026-11-18 (C5):** người gửi `contractguard.info@gmail.com`, không domain | Phase 1 |
| SD-17 | Môi trường | Docker Compose local (Postgres, LocalStack, Mailpit); AWS dựng sau. **Đổi 2026-11-18 (C5):** có staging và production, Supabase ở Tokyo | Phase 0 |
| SD-18 | Hạ tầng dưới dạng code và CI/CD | AWS CDK (C#), GitHub Actions với OIDC, không có khóa AWS tĩnh | Phase 7 |
| SD-19 | Quan sát hệ thống | Log JSON có `job_id` lên CloudWatch; trace để sau | Phase 7 |
| SD-20 | Kiểm soát chi phí | Tắt môi trường ngoài giờ demo; cảnh báo ngân sách | Phase 7 |

---

## Bước 2 — Kiến trúc tổng thể

### SD-01 Kiểu kiến trúc **[Chặn phase 0–1]**

**Bối cảnh:** 1 SE lo toàn bộ nền tảng; AI viết Python; hệ thống ít request nhưng nặng LLM (SD §2.8 K1). Tech stack của nhóm đã vạch: một service C# + worker Python + SQS.

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Modular monolith C# + worker Python + hàng đợi** | Một nơi deploy cho API, ít thành phần phải vận hành; ranh giới C#/Python rõ qua schema; worker tách riêng nên lỗi hoặc chạy nặng không làm treo API | Phải giữ module nội bộ gọn gàng (Auth, Workspace, Contract, Analysis, Chat, Export) để không thành "big ball of mud" |
| B. Microservices (auth, contract, analysis, chat riêng) | Mỗi service scale và deploy riêng | Gấp nhiều lần công vận hành, mạng, quan sát; 1 SE không kham nổi; không có tải để cần tách |
| C. Serverless toàn bộ (Lambda) | Không tốn tiền khi không dùng | Docling và model ML làm image nặng, cold start lâu; .NET trên Lambda phải thiết kế lại theo hàm; khó chạy local |

**Đề xuất:** A. Trong project C#, chia thư mục theo module nghiệp vụ; module chỉ gọi nhau qua interface, không truy cập thẳng bảng của module khác.

**Quyết định:**

### SD-02 Chạy service ở đâu

**Bối cảnh:** Q1 đã chốt **không giữ worker chạy sẵn**. API phải luôn bật. Ngân sách credit có hạn (SD §2.7).

| Phương án | Ưu | Nhược | Chi phí ước tính |
|---|---|---|---|
| **A. ECS Fargate (ARM): API 1 task luôn bật; worker Fargate Spot, 0→N** | Đúng tech stack; không quản lý máy chủ; worker scale về 0 | Phải có ALB hoặc cổng vào khác; Spot có thể bị thu hồi giữa job (SQS tự giao lại) | API ~$8/tháng; worker theo job, ~$1 |
| B. Một EC2 nhỏ chạy Docker Compose (API + worker) | Rẻ nhất, giống hệt môi trường local | Tự vá OS, tự lo TLS; worker không scale; một máy chết là chết hết | ~$12–15/tháng |
| C. App Runner cho API | Có HTTPS sẵn, không cần ALB | Không có ARM; worker vẫn cần ECS; thêm một dịch vụ phải học | ~$10–25/tháng |

**Đề xuất:** A. B chỉ đáng chọn nếu credit gần hết.

**Quyết định:** A cho API. Phần worker đổi ngày 2026-11-17: nhóm AI chọn nền tảng (mục C2).

### SD-03 Cổng vào và domain **[Chặn phase 1]**

**Bối cảnh:** Frontend là SPA tĩnh (DEC-GEN-027). Cách cổng vào ảnh hưởng tới cách lưu phiên đăng nhập (SD-04).

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Một domain: CloudFront phục vụ SPA từ S3 và chuyển `/api/*` về ALB** | Cùng origin nên dùng được cookie httpOnly, không cần CORS; một chứng chỉ TLS; ẩn ALB sau CloudFront | Cấu hình CloudFront phức tạp hơn một chút (hai origin, tắt cache cho `/api/*`) |
| B. Hai domain: `app.` (CloudFront) và `api.` (ALB) | Cấu hình từng phần đơn giản | Phải bật CORS; cookie khác domain khó, thường phải chuyển sang token trong JavaScript (dễ bị XSS lấy mất) |

**Đề xuất:** A. Cấu hình thêm: CloudFront trả `index.html` cho đường dẫn không có file (403/404 → 200) để route của SPA tải lại được.

**Quyết định:** Bổ sung 2026-11-07, Approver đồng ý: CloudFront gọi ALB bằng HTTPS và security group chỉ cho ALB gọi API. Cookie `Secure` buộc API phải biết request gốc là HTTPS, nên API đọc `X-Forwarded-Proto`; đoạn CloudFront tới ALB dùng HTTP thì API ở Production trả 500, còn API lộ ra ngoài thì ai cũng tự khai được header này.

---

## Bước 7 — Xác thực, phân quyền, upload an toàn

(Đưa lên trước vì chặn phase 1.)

### SD-04 Xác thực: Cognito hay tự quản **[Chặn phase 1]**

**Bối cảnh:** SRS yêu cầu những luật rất cụ thể:
- Sai 5 lần với cùng email thì khóa đăng nhập bằng mật khẩu 15 phút, vẫn cho đặt lại mật khẩu (SEC-AUTH-006).
- Rate limit theo IP 30 req/phút cho đăng nhập, 10 req/phút cho quên mật khẩu (SEC-AUTH-007).
- Quên mật khẩu: chờ 3 phút giữa hai lần gửi, tối đa 5 email/giờ, luôn trả cùng thông báo (FR-AUTH-014).
- MFA bằng OTP qua email, hiệu lực 10 phút (FR-AUTH-013).
- Đăng nhập Google gộp với tài khoản email cùng địa chỉ (FR-AUTH-011).

Threat model của IA (`security/docs/THREAT_MODEL.md`, T01) đang giả định Cognito.

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. ASP.NET Core Identity trong service C#** | Có sẵn khóa tài khoản cấu hình được đúng 5 lần / 15 phút; có token đặt lại mật khẩu, 2FA qua email, đăng nhập Google; rate limit bằng middleware có sẵn của ASP.NET Core; không thêm dịch vụ AWS; chạy local y hệt production | SE tự chịu trách nhiệm phần bảo mật (lưu mật khẩu, phiên, CSRF); IA phải review kỹ; phải sửa T01 trong threat model |
| B. Amazon Cognito | Phần nhạy cảm (lưu mật khẩu, MFA) do AWS lo; khớp threat model hiện tại; có sẵn Google federation | Luật khóa, cooldown quên mật khẩu, giới hạn 5 email/giờ là của Cognito, không chỉnh đúng như SRS được → phải làm change request nới SRS, hoặc tự viết Lambda trigger (phức tạp hơn cả A); khó chạy local; giao diện đăng nhập phải tự làm hoặc dùng trang có sẵn khó tùy biến |

**Đề xuất:** A, vì SRS đã chốt các luật mà Cognito không cấu hình được. Kèm theo:
- Phiên đăng nhập bằng cookie httpOnly, `Secure`, `SameSite=Lax` (nhờ SD-03); bật chống CSRF cho mọi request ghi.
- Lưu mật khẩu bằng hasher mặc định của Identity.
- IA cập nhật T01 thành "giả mạo hoặc đánh cắp cookie phiên".

**Nếu chọn B:** cần change request cho SEC-AUTH-006, FR-AUTH-014 (dùng luật của Cognito).

**Quyết định:**
    - Sử dụng cognito, hãy thay đổi lại các mục bị ảnh hưởng

### SD-05 Phân quyền và cách ly không gian làm việc **[Chặn phase 1]**

**Bối cảnh:** Mỗi hợp đồng thuộc một không gian (cá nhân hoặc tổ chức, DEC-PRD-020); không ai được thấy dữ liệu không gian khác (NFR-SEC-003, BR-DATA-001); quyền kiểm tra ở server (NFR-SEC-004). IA đã nêu lỗi BOLA (T04).

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Lọc ở tầng ứng dụng: global query filter của EF Core theo các không gian người dùng được vào + kiểm tra quyền theo từng tài nguyên** | Đơn giản, test được bằng integration test; một chỗ duy nhất định nghĩa "được thấy gì" | Câu SQL viết tay bỏ qua filter; phải có test cách ly bắt buộc |
| B. Row-Level Security của PostgreSQL | Cách ly ngay ở database, quên filter cũng không lộ | Phức tạp với mô hình chia sẻ (hợp đồng cá nhân chia cho người ngoài); khó debug; EF Core phải đặt biến phiên mỗi kết nối |

**Đề xuất:** A, kèm bộ test cách ly chạy trong CI: người dùng A gọi mọi endpoint với ID của B đều bị từ chối. Cân nhắc B sau MVP.

**Quyết định:**

### SD-08 Upload và quét mã độc **[Chặn phase 1–2]**

**Bối cảnh:**
- Giới hạn 20 MB, 100 trang (FR-UPL-004, 005).
- Xác định loại file thật bằng nội dung (FR-UPL-001).
- Quét mã độc trước khi trích xuất (FR-UPL-006, 007).
- Loại macro, object nhúng, script (FR-UPL-008).
- Frontend đẩy file thẳng lên S3 (tech stack).

**Cách upload:**

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Presigned POST có điều kiện `content-length-range` ≤ 20 MB, khóa file do server đặt** | S3 tự từ chối file quá cỡ; file không đi qua C# | Cần thêm bước xác nhận "đã upload xong" từ frontend |
| B. Presigned PUT | Đơn giản hơn | Không giới hạn được kích thước lúc upload, phải kiểm tra sau |
| C. Upload qua API C# | Kiểm tra được mọi thứ ngay | File lớn đi qua API; tốn băng thông và bộ nhớ của task luôn bật |

**Quét mã độc:**

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Amazon GuardDuty Malware Protection for S3** | Dịch vụ quản lý, tự quét khi file vào prefix `quarantine/`, gắn tag kết quả và phát sự kiện; không làm nặng image worker | Tính tiền theo dung lượng quét (vài GB/tháng thì chỉ vài USD, cần kiểm tra trang giá); local phải giả lập |
| B. ClamAV trong worker | Miễn phí, chạy local được | Image nặng thêm, phải tải cơ sở dữ liệu virus mỗi lần khởi động → cold start chậm thêm (Q1 đã chọn scale từ 0) |

**Đề xuất:** Upload A + quét A. Luồng: upload vào `quarantine/` → GuardDuty báo sạch → C# chuyển sang `clean/` và tạo job. Kiểm tra loại file thật, đếm trang và làm sạch tài liệu (FR-UPL-001, 005, 008) do worker làm trước khi parse. Ở local, bỏ qua bước quét bằng cờ cấu hình.

**Quyết định:**

---

## Bước 3 — Dữ liệu

### SD-06 Database **[Chặn phase 0]**

**Bối cảnh:** Dữ liệu nhỏ (< 1 GB, SD §2.5); cần cả dữ liệu quan hệ lẫn vector embedding; mã hóa khi lưu (NFR-SEC-001).

| Phương án | Ưu | Nhược | Chi phí |
|---|---|---|---|
| **A. RDS PostgreSQL 18 + pgvector, `db.t4g.micro`, single-AZ** | Một hệ thống cho cả quan hệ và vector; có thể nằm trong free tier; Postgres chạy local bằng Docker | Luôn bật; dừng instance được nhưng AWS tự bật lại sau 7 ngày | ~$15/tháng |
| B. Aurora Serverless v2 (có thể giảm về 0) | Tự giảm về 0 khi không dùng | Lần đầu sau khi ngủ chậm vài giây; giá mỗi đơn vị cao hơn; cấu hình phức tạp hơn | Thấp khi không dùng, cao khi chạy |
| C. Postgres + một vector database riêng (OpenSearch, Qdrant) | Tìm kiếm vector mạnh hơn | Thừa với ~1.000 phiên bản hợp đồng; thêm một hệ thống phải vận hành | +$20 trở lên |

**Đề xuất:** A. Bảng theo SRS §9 (tên tiếng Anh, DEC-GEN-025); migration bằng EF Core; chỉ mục HNSW cho embedding. Số chiều vector do nhóm AI chốt khi chọn model embedding.

**Quyết định:** A. Ngày 2026-10-29 đổi bản PostgreSQL từ 16 sang 18, vì lúc này chưa có dữ liệu và bản 18 được hỗ trợ lâu hơn. pgvector chỉ dùng tính năng có từ 0.8.1 trở xuống, vì đó là bản RDS PG 18 có lúc ra mắt. **Đổi ngày 2026-11-17: Supabase thay RDS (mục C1).**

### SD-07 File storage **[Chặn phase 1]**

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Một bucket, prefix `quarantine/`, `clean/{workspace}/{contract}/{version}/`, `results/{job_id}/`; SSE-KMS với khóa do AWS quản lý, bật S3 Bucket Key** | Đơn giản; quyền IAM chia theo prefix; mã hóa đạt NFR-SEC-001 mà không tốn phí khóa riêng | Quyền theo prefix phải viết cẩn thận |
| B. Bucket riêng cho quarantine và cho file sạch | Ranh giới rõ hơn, chính sách đơn giản hơn | Thêm bucket phải quản lý; xóa vĩnh viễn (F-CTR-006) phải dọn nhiều nơi |

**Đề xuất:** A. Xóa vĩnh viễn hợp đồng = xóa prefix của hợp đồng + dòng DB + embedding. Tắt versioning của bucket để xóa là mất thật.

**Quyết định:**

---

## Bước 4 — API và hợp đồng dữ liệu

### SD-09 API giữa frontend và backend **[Chặn phase 1]**

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. REST + OpenAPI sinh tự động từ ASP.NET Core; sinh kiểu TypeScript cho frontend** | Quen thuộc; frontend có kiểu đúng với backend; tài liệu API có sẵn | Phải chạy lại bước sinh code khi API đổi (đưa vào CI) |
| B. GraphQL | Frontend lấy đúng dữ liệu cần | Thêm thư viện và cách nghĩ mới; phân quyền theo trường phức tạp |
| C. tRPC hoặc gRPC-web | Kiểu an toàn đầu cuối | tRPC chỉ cho TypeScript; gRPC-web cần proxy |

**Đề xuất:** A. Quy ước: tiền tố `/api/v1`, lỗi theo chuẩn Problem Details (RFC 9457) kèm mã ERR-* của SRS §14.1.

**Quyết định:**

### SD-10 Hợp đồng dữ liệu C# ↔ Python **[Chặn phase 0]**

**Bối cảnh:** Đồng đội đã có `contracts/extraction.schema.json` (facts + evidence, worker trả kết quả **không tin cậy**, C# chấm điểm) và `security/docs/CONTRACT_FOR_TEAM.md` (biên bản chốt giao diện chưa ký). SRS cần nhiều hơn: điều khoản kèm vị trí trên trang, loại điều khoản, finding theo 7 nhóm có giải thích và khuyến nghị, 20 check, metadata, cảnh báo prompt injection, embedding.

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Mở rộng schema của đồng đội (tăng `schema_version`), thêm `clauses`, `findings`, `checks`, `metadata`, `injection_warnings`; sinh lớp C# và model Pydantic từ schema** | Giữ quy tắc "kết quả LLM không tin cậy, C# kiểm tra bằng chứng" đã thống nhất; một nguồn sự thật | Phải họp chốt với AI và IA (CONTRIBUTING yêu cầu cả ba bên duyệt) |
| B. Viết schema mới theo SRS | Khớp SRS ngay | Bỏ công việc đã có của đồng đội; dễ xung đột |
| C. Không dùng schema, thống nhất bằng tài liệu | Nhanh lúc đầu | Lỗi lệch kiểu dữ liệu chỉ lộ ra lúc chạy |

**Đề xuất:** A. Phân vai trong pipeline:
- Worker (LLM) đề xuất finding kèm `clause_id` và trích dẫn.
- C# kiểm tra trích dẫn có thật trong văn bản đã parse, rồi chấm điểm bằng rule engine YAML (F-RISK-003).
- Finding có trích dẫn sai thì bị loại và ghi log.

Embedding điều khoản do worker tính, gửi kèm kết quả để C# ghi vào pgvector.

**Quyết định:** A, với thay đổi ngày 2026-11-06: code Python trong worker tính điểm theo công thức có version, model không tự đưa ra con số. Worker tự loại finding sai trích dẫn trước khi tính điểm; C# từ chối cả kết quả nếu còn trích dẫn sai, và kiểm tra giới hạn của điểm. Chi tiết ở `05-ai-diagram-vs-system-design.md` §4.

### SD-11 Truyền kết quả qua hàng đợi **[Chặn phase 1]**

**Bối cảnh:** Kết quả trích xuất ~200 KB (SD §2.5), vượt giới hạn 256 KB của SQS khi hợp đồng dài. UI cần hiện bước hiện tại (FR-ANA-002) nhưng worker không được ghi DB.

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Message chỉ chứa `job_id`, loại sự kiện và đường dẫn S3; worker gửi cả sự kiện tiến trình (`parsing`, `extracting`...) lẫn kết quả vào `analysis-results`** | Không vướng giới hạn kích thước; C# là nơi duy nhất cập nhật trạng thái | Thêm một lần đọc S3 |
| B. Gửi cả JSON trong message | Ít bước hơn | Vỡ khi kết quả > 256 KB |

**Đề xuất:** A.

**Quyết định:**

---

## Bước 5 — Pipeline phân tích bất đồng bộ

### SD-12 Worker khởi động từ 0

**Bối cảnh:** Q1 = không giữ worker chạy sẵn. Các mục tiêu NFR-PERF-001..003 được đo **khi worker đã chạy sẵn**, nên khởi động chậm không vi phạm SRS. Tuy vậy, job đầu tiên sau khi worker ngủ sẽ chờ thêm 1–3 phút (SD §2.6). Buổi demo không được chờ (SC-GEN-005). Phải chịu được 10 phân tích đồng thời (NFR-SCAL-001).

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. ECS service worker, số task 0→3, scale theo số message trong `analysis-requests`; lịch tự bật 1 task trong giờ demo và giờ test** | Đúng Q1; buổi demo vẫn nhanh; giới hạn 3 task để không vượt quota Bedrock | Autoscaling phản ứng chậm ~1 phút; phải nhớ đặt lịch |
| B. EventBridge Pipes: mỗi message SQS chạy một ECS task riêng | Không cần chỉnh autoscaling; mỗi job một task sạch | Mỗi job đều chịu cold start; khó giới hạn số task đồng thời |
| C. Lambda container cho worker | Khởi động nhanh hơn Fargate | Image Docling nặng; giới hạn 15 phút mỗi lần chạy; khó gọi song song trong một job |

**Đề xuất:** A. Mỗi task xử lý tối đa ~4 job song song (asyncio, có giới hạn số lời gọi Bedrock đồng thời), 3 task × 4 = 12 ≥ 10 (NFR-SCAL-001). Nhóm AI chốt số này sau khi có quota thật.

**Quyết định:** Đổi ngày 2026-11-17: nhóm AI quyết cách chạy và scale worker; model tự host thay Bedrock (mục C2, C3).

### SD-13 Retry và trạng thái job **[Chặn phase 1]**

**Bối cảnh:** Phải tự thử lại ít nhất một lần trước khi báo thất bại (FR-ANA-003, 004). IA đã nêu rủi ro message trùng hoặc đến sai thứ tự (T08).

**Đề xuất (một phương án, vì các lựa chọn khác đều kém hơn rõ rệt):**
- SQS: `maxReceiveCount = 3` rồi vào DLQ; visibility timeout 15 phút, worker gia hạn khi job còn chạy.
- Trong worker: mỗi lời gọi Bedrock retry có backoff khi bị giới hạn tốc độ.
- C# giữ máy trạng thái `queued → parsing → extracting → scoring → done | failed`; bỏ qua sự kiện cũ hơn trạng thái hiện tại; kết quả cùng `job_id` chỉ ghi một lần (idempotent).
- Một job có message trong DLQ thì C# chuyển `failed` kèm lý do (ERR theo §14.1).
- Job kẹt quá 20 phút không có sự kiện mới thì đánh dấu `failed`.

**Quyết định:**

---

## Bước 6 — Chatbot

### SD-14 Chatbot: ai truy xuất, ai gọi model

**Bối cảnh:**
- Chỉ trả lời từ hợp đồng và phiên bản đang mở, kết quả phân tích và cơ sở tri thức (FR-CHAT-002, NFR-SEC-007).
- Mỗi câu trả lời phải có trích dẫn (FR-CHAT-003).
- Chữ đầu tiên ≤ 15 giây (NFR-PERF-004).
- Worker không chạm DB.
- IA nêu rủi ro truy xuất nhầm dữ liệu không gian khác (T06).

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. C# lo hết: kiểm tra quyền → tìm điều khoản trong pgvector (lọc theo hợp đồng trước khi xếp hạng) → gọi Bedrock bằng AWS SDK .NET (stream) → kiểm tra trích dẫn có thật. Prompt nằm trong repo (thư mục `prompts/`), do nhóm AI viết và quản lý phiên bản** | Không thêm service luôn bật; quyền và truy xuất ở cùng một chỗ, đúng T06; nhanh vì không qua hàng đợi | Logic RAG viết bằng C#, nhóm AI ít quen; thử nghiệm prompt phải qua code C# hoặc một script thử riêng |
| B. Service chat Python riêng (FastAPI), luôn bật, đọc pgvector chỉ đọc | Nhóm AI làm hết bằng Python | Thêm một service luôn bật (trái K5); phá quy tắc "worker không chạm DB"; quyền phải kiểm tra ở hai nơi |
| C. Chat đi qua hàng đợi như phân tích | Giữ ranh giới hiện tại | Chậm (thêm thời gian qua hàng đợi và cold start), khó đạt 15 giây với worker scale từ 0 |

**Đề xuất:** A. Nhóm AI cung cấp: prompt, cách chia đoạn và model embedding, bộ câu hỏi thử để đo chất lượng.

**Quyết định:**
    - Hiện tại vẫn giữ hướng A, nhưng vẫn giữ lại ý kiến B. Phần chatbot chưa triển khai liền nên vẫn chưa cần thiết chốt phần này

### SD-15 Streaming câu trả lời chat

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Server-Sent Events (SSE)** | Một chiều server → trình duyệt, đúng nhu cầu; dùng HTTP thường, đi qua CloudFront và ALB | Cần gửi tín hiệu giữ kết nối mỗi vài giây để CloudFront không cắt kết nối im lặng quá lâu |
| B. WebSocket (SignalR) | Hai chiều, có thể dùng cho cả tiến trình phân tích | Thêm thư viện; cấu hình ALB và CloudFront cho WebSocket; thừa với MVP |
| C. Polling | Đơn giản nhất | Không thấy chữ chạy dần; khó cảm nhận đạt 15 giây |

**Đề xuất:** A cho chat. Tiến trình phân tích vẫn gọi `GET /jobs/{id}` định kỳ như tech stack.

**Quyết định:**

---

## Bước 8 — Vận hành

### SD-16 Email **[Chặn phase 1]**

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Amazon SES** | Cùng AWS, rẻ; Identity gửi email qua SMTP hoặc SDK | Tài khoản mới ở sandbox: chỉ gửi tới địa chỉ đã xác minh. Xin ra khỏi sandbox mất ~1 ngày và cần domain đã xác minh |
| B. Dịch vụ ngoài (Resend, Mailgun...) | Dễ bắt đầu | Thêm nhà cung cấp, thêm bí mật phải quản lý |

**Đề xuất:** A. Local dùng Mailpit (bắt mọi email, xem trên web). Cho demo: xác minh sẵn email của nhóm và giám khảo, hoặc xin ra khỏi sandbox sớm. Cần mua hoặc có sẵn một domain.

**Quyết định:**

### SD-17 Môi trường **[Chặn phase 0]**

**Bối cảnh:** Q2 = dựng AWS sau, chưa chốt tuần.

**Đề xuất:**
- **Local:** Docker Compose gồm PostgreSQL + pgvector, LocalStack (giả lập S3 và SQS), Mailpit. Bedrock gọi thật bằng tài khoản của nhóm AI. Mỗi người chạy được toàn bộ luồng trên máy mình.
- **AWS:** một môi trường duy nhất (staging, dùng luôn để demo), dựng khi luồng demo chạy được ở local.
- Không làm môi trường production riêng trong MVP.

Phương án khác: dựng AWS ngay từ đầu để phát hiện lỗi hạ tầng sớm, đổi lại tốn credit vài tháng (~$55/tháng, SD §2.7).

**Quyết định:**

### SD-18 Hạ tầng dưới dạng code và CI/CD

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. AWS CDK bằng C#; GitHub Actions đăng nhập AWS qua OIDC** | Cùng ngôn ngữ với SE; không có khóa AWS tĩnh trong GitHub (đúng T10) | CDK chỉ dùng cho AWS |
| B. Terraform | Phổ biến, không phụ thuộc AWS | Thêm một ngôn ngữ (HCL) cho 1 SE |
| C. Tạo tay trên console | Nhanh lần đầu | Không tái tạo được, dễ sai, khó tắt bật tiết kiệm |

**Đề xuất:** A. CI hiện có (`security-checks.yml`) giữ nguyên; thêm workflow riêng cho `backend/`, `worker/`, `frontend/`, chỉ chạy khi thư mục đó đổi.

**Quyết định:**

### SD-19 Quan sát hệ thống

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Log JSON có `job_id`, `user_id`, không có nội dung hợp đồng; gửi CloudWatch Logs; ghi chi phí mỗi lần gọi model (NFR-PERF-005)** | Đủ để debug luồng hai service bằng `job_id`; không tốn thêm | Không có trace trực quan |
| B. Thêm OpenTelemetry + X-Ray | Xem được thời gian từng bước | Thêm collector, thêm cấu hình |

**Đề xuất:** A cho MVP; thêm B khi cần tối ưu độ trễ. IA kiểm tra log không chứa nội dung hợp đồng (T11).

**Quyết định:**

### SD-20 Kiểm soát chi phí

**Đề xuất:**
- AWS Budgets cảnh báo ở 50%, 80% credit.
- Script hoặc lệnh CDK để giảm API về 0 task và dừng RDS ngoài giờ test (RDS tự bật lại sau 7 ngày, cần dừng lại).
- Worker đã scale về 0 (Q1).
- Cân nhắc bỏ ALB nếu ngân sách căng: thay bằng API Gateway HTTP API + VPC Link (tính tiền theo request), đổi lại cấu hình phức tạp hơn.

**Quyết định:**
    - Trước khi triển khai lên AWS, cần phải nhắc lại với các thành viên cấu hình billing, cảnh báo
---

## Đổi sau khi chốt (2026-11-17, 2026-11-18, 2026-11-20, 2026-11-21, 2026-11-23)

Approver đổi ba điểm khi chuẩn bị Phase 7 (C1–C3), thêm C4–C5 ngày 2026-11-18 khi nối Supabase, theo drawio `architecture/contractguard-architecture.drawio`. Phần dưới ghi lại hệ quả và trade-off để cả nhóm biết đã đánh đổi gì. Các mục SD cũ giữ nguyên để lưu vết; dòng Quyết định của chúng trỏ về đây.

### C1 Database: Supabase thay RDS (đổi SD-06, SD-20)

**Bối cảnh:** SD-06 chốt RDS PostgreSQL 18 + pgvector. Approver chọn Supabase (PostgreSQL có sẵn pgvector, quản lý ngoài tài khoản AWS của nhóm).

| | RDS (SD-06 cũ) | **Supabase (Approver chọn)** |
|---|---|---|
| Chi phí | ~$15/tháng, trừ vào credit FCAJ; phải dừng ngoài giờ test | Gói Free $0 (500 MB, đủ cho MVP); gói Pro $25/tháng nếu cần. **Không** trừ vào credit AWS |
| Vận hành | Trong VPC; CDK dựng; tự dừng/bật | Không phải dựng; bảng điều khiển có sẵn. Gói Free **tạm dừng project sau 7 ngày không có truy cập**, phải bật lại tay trước buổi test và demo |
| Mạng | API gọi trong VPC, không ra internet | API trên Fargate gọi ra internet có TLS (`SSL Mode=Require`). Kết nối trực tiếp của Supabase chỉ có IPv6, nên dùng **pooler Supavisor ở chế độ session** (IPv4, cổng 5432). Chế độ transaction (cổng 6543) không hợp với prepared statement của Npgsql và migration |
| Region | `ap-southeast-1` | Chọn region Singapore khi tạo project (Supabase chạy trên AWS ở region đó) |
| Dữ liệu | Trong tài khoản AWS của nhóm | Nội dung hợp đồng (văn bản điều khoản, embedding, chat) nằm ở nhà cung cấp thứ ba. **IA cần xem lại** NFR-PRIV và threat model |
| Sao lưu | Snapshot tự động của RDS | Gói Free không có bản sao lưu tải về được; Pro có sao lưu hằng ngày 7 ngày |
| Code | Không đổi | Không đổi: vẫn Npgsql + EF Core + pgvector. Chỉ đổi chuỗi kết nối (đặt trong Secrets Manager) |

**Cần Approver xem:** PRD CON-PRJ-003 ghi "hạ tầng triển khai trên AWS (chương trình FCAJ), region Singapore". Supabase ở Singapore và chạy trên hạ tầng AWS, nhưng không nằm trong tài khoản FCAJ. Nếu Approver hiểu CON-PRJ-003 là "mọi thành phần trong tài khoản AWS của nhóm" thì cần một change request; nếu hiểu là "region và nhà cung cấp hạ tầng" thì không cần.

**Quyết định:** Supabase (Approver chốt 2026-11-17). SD-20 bỏ việc dừng RDS; thêm việc bật lại project Supabase nếu bị tạm dừng.

### C2 Nền tảng chạy worker: nhóm AI quyết (đổi SD-02 phần worker, SD-12)

**Bối cảnh:** SD-02 chốt worker chạy Fargate Spot 0→N, SD-12 chốt scale theo số message. Nhóm AI đang chạy worker bằng Docker trên EC2 ở tài khoản AWS của họ (`docs/07`). Approver để nhóm AI chọn nền tảng.

**SE giữ ranh giới, nền tảng nào cũng phải đáp ứng:**
- Đọc `analysis-requests`, ghi `results/` lên S3 và gửi message lên `analysis-results`, theo `contracts/`. Không chạm database.
- Dùng IAM role, không dùng access key tĩnh (`docs/07` §6).
- Đạt NFR-SCAL-001 (10 phân tích đồng thời) và NFR-PERF-001 (p50 ≤ 2 phút cho 20 trang) khi đã chạy sẵn.
- Bật sẵn trước giờ demo và giờ test, vì Q1 cho phép worker tắt ngoài giờ.

| Nếu nhóm AI chọn | Hệ quả |
|---|---|
| EC2 + Docker Compose | Đơn giản, giống local; máy bật là tính tiền; không tự scale, nên 10 job đồng thời phải chạy song song trong một máy |
| ECS (Fargate hoặc EC2 có GPU) | Scale theo hàng đợi như SD-12; phải viết thêm task definition, autoscaling |

**Quyết định:** Nhóm AI quyết (Approver 2026-11-17). API vẫn chạy ECS Fargate ARM như SD-02.

### C3 Model AI tự host trên EC2 thay Bedrock (CR-GEN-003, DEC-GEN-029; đổi SD-12, SD-13, SD-14, SD-17, SD-20)

**Bối cảnh:** model AI (phân tích, embedding, chatbot) do nhóm AI tự host bằng Docker trên EC2, region `ap-southeast-1`, thay Amazon Bedrock. Spec đã sửa qua CR-GEN-003.

| Mục | Trước (Bedrock) | Sau (tự host) |
|---|---|---|
| Chi phí (SD §2.4, §2.7) | Theo token | Theo giờ máy GPU bật, kể cả khi không có job. Cần lịch bật/tắt (SD-20) |
| Giới hạn tốc độ (SD-12, SD-13) | Quota token/phút của Bedrock | Số request song song mà máy GPU phục vụ được. Worker và API vẫn giới hạn số lời gọi đồng thời và retry có backoff khi endpoint quá tải |
| Quota | Quota Bedrock | Quota vCPU cho họ instance G, VT; account mới thường bằng 0 (RISK-GEN-002) |
| Chat (SD-14 A) | C# gọi Bedrock bằng AWS SDK .NET | C# truy xuất pgvector rồi gọi **endpoint HTTP** của model (stream). Port `IChatResponder` không đổi. Câu hỏi cũng cần endpoint embedding |
| Local (SD-17) | Gọi Bedrock thật | Gọi endpoint model của nhóm AI nếu có, hoặc `FakeChatResponder` để thử đường đi |

**Câu hỏi cho nhóm AI** (chặn phần chat thật của Phase 7):
1. Serving stack và định dạng API: có tương thích OpenAI (`/v1/chat/completions`, `/v1/embeddings`) không? Nếu có, C# dùng được thư viện có sẵn.
2. API (Fargate, tài khoản SE) gọi endpoint (EC2, tài khoản AI) qua đường nào: internet có TLS và khóa dịch vụ, hay VPC peering? Chưa chốt thì không mở cổng model ra internet mà không có xác thực.
3. Model embedding và số chiều vector (bảng pgvector đang chờ).

**Quyết định:** Tự host trên EC2 (Approver chốt 2026-11-17, DEC-GEN-029).

**Cập nhật 2026-11-26 (CR-GEN-004, DEC-GEN-034 chờ duyệt):** spec chỉ ràng buộc AI chạy bằng Docker trên EC2 ở `ap-southeast-1`; chạy model trên máy hay gọi qua API, và model nào, do nhóm AI quyết. Ba câu hỏi ở trên hết là việc của SE: chat đi qua worker (C6), API không gọi model, không có bảng embedding phía SE. Chi phí GPU và quota GPU chỉ còn áp dụng nếu nhóm AI chọn máy GPU; hiện EC2 của worker là `m7i-flex.large` và chỉ bật khi web bật (`docs/07` §5).

### C4 Database dùng chung với nhóm AI: schema `app` (đổi quy tắc "chỉ C# ghi DB", liên quan SD-06, X2)

**Bối cảnh:** project Supabase (do nhóm AI tạo, Tokyo, PostgreSQL 17.11) đã có kho văn bản pháp luật của nhóm AI trước khi backend migrate. Đọc ngày 2026-11-18, nguồn là `data_pipeline/migrations/001_legal_schema.sql`, `postgres_writer.py` và `mcp_server/supabase_reader.py` trong repo `contract-risk-management`:

| Bảng (`public`) | Dòng | Dùng để |
|---|---|---|
| `legal_documents` | 65 | Văn bản luật lấy từ vanban.chinhphu.vn: số ký hiệu, loại, hiệu lực, `replaced_by`, `s3_prefix` |
| `legal_document_versions` | 65 | Mỗi lần tải: sha256, key PDF trên S3 của AI, cách trích (text/ocr), trạng thái xử lý |
| `legal_articles` | 786 | Từng Điều: nội dung, chương, hiệu lực `valid_from/to`, `is_current`, bị sửa bởi văn bản nào |
| `legal_relations` | 549 | Quan hệ sửa đổi/thay thế giữa các văn bản, trạng thái áp dụng (`applied`, `needs_review`...) |
| `ingest_jobs` | 65 | Hàng đợi/nhật ký nạp từng văn bản |
| `crawl_runs` | 2 | Nhật ký mỗi lần crawl |

Data pipeline của AI ghi qua PostgREST bằng secret key. MCP gateway cho Agent 2/3 đọc cũng qua PostgREST bằng secret key, và trong code chỉ cho đọc. RLS bật, không có policy cho anon. Vector nằm ở Qdrant, không ở pgvector.

**Rủi ro nếu bảng app nằm chung ở `public`:**
1. Supabase cấp quyền mặc định cho `anon`/`authenticated` trên bảng mới trong `public`. Thiếu RLS thì ai có publishable key (key công khai) cũng đọc/ghi được `user_account`, `contract`.
2. Secret key chạy dưới `service_role`, bỏ qua RLS. `list_tables` của MCP trả về mọi bảng được mở, và khi `SUPABASE_TABLES` trống thì agent LLM, vốn có thể bị prompt injection từ nội dung hợp đồng, đọc được user và hợp đồng của mọi workspace.

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Schema riêng `app` cho bảng app** | PostgREST không mở `app`; `anon`, `authenticated`, `service_role` không có quyền `USAGE` trên `app`. Chỉ thêm một migration | DB local cũ phải tạo lại một lần |
| B. Để ở `public`, bật RLS cho từng bảng | Không đổi code EF | Secret key vẫn bỏ qua RLS; phải nhớ bật RLS cho mỗi bảng mới |
| C. Tách project Supabase cho AI | Cô lập hẳn | Gói Free chỉ 2 project, không đủ khi có cả staging và production |

**Phân quyền:**

| | Schema `app` | `public.legal_*`, `ingest_jobs`, `crawl_runs` |
|---|---|---|
| Sở hữu schema | SE (EF migration trong `backend/`) | AI (`data_pipeline/migrations/*.sql`) |
| Ghi | Chỉ API C# | Chỉ data pipeline AI |
| Đọc | API C# | Agent qua MCP; C# chỉ đọc khi X2/SD-14 chốt |
| Kết nối | Npgsql, user `postgres` (mật khẩu chỉ SE giữ) | PostgREST, secret key |

Quy tắc "chỉ C# ghi DB" đổi thành: C# là nơi duy nhất ghi **dữ liệu nghiệp vụ** (schema `app`); data pipeline AI ghi **kho tham chiếu pháp luật**. Dữ liệu trao đổi theo từng job vẫn đi qua `contracts/` (S3/SQS), không qua DB.

**Còn mở:** X2/SD-14. Chatbot C# có cần đọc kho pháp luật không? Nếu có thì đọc bảng `public.legal_*` hay đi qua endpoint của AI? Vector đang ở Qdrant, không ở pgvector.

**Quyết định:** A (Approver chọn 2026-11-18). Đã làm: `HasDefaultSchema("app")`, bảng lịch sử migration cũng ở `app`, migration `MoveToAppSchema`; staging đã migrate ngày 2026-11-18.

### C5 Môi trường staging + production, email (đổi SD-16, SD-17, C1)

**Bối cảnh:** SD-17 chốt một môi trường AWS (staging dùng luôn cho demo), không có production. Approver muốn có cả staging và production. Local vẫn không chạm Supabase.

| Môi trường | ASP.NET | Database | Email |
|---|---|---|---|
| Local | `Development` (profile `http`) | Postgres trong Docker | Mailpit |
| Staging | `Cloud` (profile `aws` trên máy dev); sau này Fargate | Supabase project hiện tại (`lglozpndorcilcfqtgce`, Tokyo) | SES, sandbox |
| Production | Fargate (Phase 7) | Supabase project mới, Tokyo | SES (xin production access) hoặc dự phòng Gmail SMTP |

- **Region:** cả hai project Supabase ở Tokyo (`ap-northeast-1`), AWS vẫn ở `ap-southeast-1`. Mỗi truy vấn từ API sang DB thêm khoảng 70 ms. Lệch với CON-PRJ-003 ("region Singapore") theo cách hiểu đã nêu ở C1.
- **Migration:** chạy `dotnet ef database update` bằng chuỗi kết nối của từng môi trường (staging: `appsettings.Cloud.local.json`; production: biến `Database__ConnectionString`, chạy một lần). Phase 7 chuyển vào GitHub Actions. Không chuyển dữ liệu từ local lên.
- **Mật khẩu `postgres`:** chỉ SE giữ. Production lưu trong Secrets Manager.
- **Kho pháp luật:** nhóm AI chạy `001`/`002_*.sql` và pipeline vào từng project. Staging để thử, production chỉ nạp dữ liệu đã kiểm tra.
- **Email (SD-16):** người gửi là `contractguard.info@gmail.com`, xác minh dạng email identity trên SES (`ap-southeast-1`, tài khoản `699061129385`). Sandbox tính theo tài khoản và region, nên staging và production dùng chung một trạng thái.
  - Identity đã xác minh (trước 2026-11-18). IAM của API chỉ cần `ses:SendEmail` trên ARN của identity.
  - Còn trong sandbox thì phải xác minh từng người nhận (thành viên nhóm, tài khoản test, giám khảo).
  - **Rủi ro:** gửi với địa chỉ `@gmail.com` qua SES không đạt DMARC alignment (SPF/DKIM là của amazonses.com), nên thư dễ vào spam hoặc bị từ chối, và AWS thường không cho ra khỏi sandbox nếu không có domain riêng. **Dự phòng** khi bị từ chối: gửi qua SMTP của Gmail (`smtp.gmail.com:587`, App Password, khoảng 500 thư/ngày) bằng `SmtpEmailSender` có sẵn, thêm username/password và TLS vào `EmailOptions`.
  - **2026-11-22:** Railway chặn SMTP ra ngoài với gói dưới Pro (thử Gmail SMTP bị timeout), nên staging dùng `Email:Provider = gmail`: `GmailEmailSender` gửi qua Gmail API (HTTPS), bằng refresh token scope `gmail.send` của `contractguard.info@gmail.com`. Có domain thì quay về `ses` chỉ bằng biến môi trường.

**Quyết định:** như bảng trên (Approver chọn 2026-11-18).

### C6 Chatbot do worker AI trả lời qua SQS (đổi SD-14 A, SD-15)

**Bối cảnh:** SD-14 A chốt C# tự truy xuất rồi gọi model. Nhóm AI muốn tự trả lời chat trong worker (đã có agent, prompt, kho pháp luật), theo cùng kiểu "JSON trên S3 + message SQS" như phân tích. Approver chọn hướng này ngày 2026-11-20.

| | SD-14 A (cũ) | **Qua worker (Approver chọn)** |
|---|---|---|
| Ai gọi model | C# | Worker Python |
| Ngữ cảnh | C# truy xuất pgvector | Backend gửi toàn bộ điều khoản và finding của run trong phiếu câu hỏi; worker tự truy xuất thêm nếu cần |
| Đường đi | HTTP stream tới model | `chat_requested` trên queue riêng `chat-requests` (phiếu ở `results/{job_id}/request.json`, `contracts/chat-request.v1.schema.json`); câu trả lời `chat_answered` về `analysis-results` |
| Stream (SD-15) | Từng token | Không stream thật: câu trả lời về một lần, backend tách theo từ để giữ hiệu ứng gõ. SSE tới trình duyệt giữ nguyên |
| Đổi quyền AWS | Endpoint model, khóa dịch vụ | Một queue mới cùng DLQ; role EC2 đọc được queue đó. Không đổi quyền S3, vì key phiếu khớp `results/*/request.json` sẵn có |

- **Queue riêng:** worker đọc mỗi lần 1 message, một job phân tích mất khoảng 13 phút; đi chung `analysis-requests` thì câu hỏi chat chờ quá 3 phút (timeout của backend).
- **Câu trả lời trong message:** chỉ vài KB, nên không cần thêm file và quyền `PutObject`.
- **Ghép câu trả lời:** backend giữ câu hỏi đang chờ trong bộ nhớ (`ChatAnswerWaiter`). Chỉ đúng với một instance API; chạy nhiều instance thì phải lưu câu trả lời vào DB rồi poll.
- **Lỗi:** worker gửi `failed` với `job_id` của câu hỏi, hoặc không trả lời trong 3 phút: người dùng nhận ERR-CHAT-001, câu hỏi vẫn được lưu.
- **Trust boundary:** như cũ. `ChatService` bỏ trích dẫn tới điều khoản không thuộc run; `answer_text` tối đa 8000 ký tự.
- **Rủi ro:** trên Fargate, ALB cắt kết nối im lặng quá 60 giây (CloudFront 30 giây). Đã xử lý ở C9: event `ping` mỗi 15 giây.

Code: `WorkerChatResponder`, `ChatAnswerWaiter`, cấu hình `Chat:Responder=worker` (môi trường `Cloud`) và `Queues:ChatRequests`. Development và e2e vẫn dùng `fake`.

**Quyết định:** qua worker (Approver chọn 2026-11-20).

### C7 Staging trên Railway; production trên AWS chỉ bật quanh buổi showcase (đổi SD-02, SD-17, SD-20, C5)

**Bối cảnh:** staging trên AWS theo SD-02/03 (CloudFront + ALB + Fargate) tốn khoảng $40/tháng, phần lớn là phí theo giờ của ALB và Fargate, dù chỉ ~5 thành viên dùng. Approver chọn đưa staging lên Railway và chỉ dựng production trên AWS khoảng 3 ngày quanh buổi showcase.

| Môi trường | Chạy ở đâu | Dùng tài nguyên AWS nào | Chi phí |
|---|---|---|---|
| Staging | Railway, project `contractguard`, environment `staging`, region Singapore. Service `web` (nginx + SPA, domain public) chuyển `/api/` sang service `api` qua mạng nội bộ của Railway; `api` không có domain public | Bộ dev: bucket và queue `contractguard-dev-*` (worker AI đã nối vào đây, `docs/07`), SES; access key của user `contractguard-dev-api-local` đặt trong biến Railway. DB là Supabase staging như C5 | Theo RAM/CPU dùng thật, vài $/tháng |
| Production | AWS `ap-southeast-1`, CDK ở `infra/cdk/` (CloudFront + ALB + Fargate ARM64), có domain và HTTPS tới ALB theo SD-03 | Bucket, queue riêng của stack | ~$40/tháng nếu bật cả tháng, nên chỉ bật ~3 ngày (~$4–5) |

**Đánh đổi:**
- Staging lệch khỏi CON-PRJ-003 ("hạ tầng trên AWS") như cách hiểu ở C1; production dùng để demo vẫn chạy trên AWS.
- Staging không thử được hạ tầng AWS (ALB, CloudFront, IAM role của task). Cần dựng production sớm hơn ngày demo vài ngày để còn thời gian sửa lỗi hạ tầng.
- Access key tĩnh nằm ở Railway (khác T10 "không khóa AWS tĩnh"). Chỉ có quyền của user API dev; xoay khóa nếu lộ. Production dùng IAM role của task.
- Vẫn giữ một origin cho trình duyệt (SD-03): nginx proxy `/api/`, cookie không cần CORS.

**Đi kèm trong code:**
- API đọc IP client từ `X-Forwarded-For` lùi 2 bước: Railway edge → nginx, hoặc CloudFront → ALB. Nhờ vậy rate limit đăng nhập tính theo người dùng thật, và giá trị client tự gửi (nằm bên trái) không lừa được.
- nginx của frontend là template: địa chỉ API (`API_UPSTREAM`) và DNS resolver lấy từ môi trường, chạy được cả Docker Compose lẫn Railway; chuyển tiếp `X-Forwarded-Proto` của Railway để API đặt cookie `Secure`.
- Image API biên dịch chéo (`--platform=$BUILDPLATFORM`), build được cho x64 (Railway) và ARM64 (Fargate).
- Production (CDK): bucket mã hóa SSE-S3 thay SSE-KMS (SD-07) vì khóa KMS do AWS quản lý không dùng được từ tài khoản AI; không NAT gateway; ALB internal qua VPC origin. Trước khi deploy production cần thêm domain, chứng chỉ ACM và listener HTTPS.

**Quyết định:** staging trên Railway, production trên AWS bật ~3 ngày quanh showcase, staging dùng lại bộ dev (Approver chọn 2026-11-20, 2026-11-21).

### C8 Tiến độ chi tiết: API đọc thẳng bảng DynamoDB của worker AI (lệch quy tắc "trao đổi qua `contracts/`")

**Bối cảnh:** trang phân tích chỉ có 4 bước, vì worker gửi `progress` khi đổi bước lớn. Bảng `contract-risk-jobs` của worker (tài khoản AI `333596350642`, on-demand) có `progress` 0–100 và `stage` chi tiết theo từng agent và tool (vd `agent_2_risk_scoring.tool.web_search`). Người dùng muốn thanh % và dòng "AI đang làm gì".

| Phương án | Ưu | Nhược |
|---|---|---|
| A. Thêm `percent`, `activity` vào message `progress` | Đúng ranh giới `contracts/`; không cần quyền chéo tài khoản | Đổi schema chung, worker phải gửi thêm message |
| **B. API đọc thẳng DynamoDB (Approver chọn)** | Không sửa worker; có ngay dữ liệu đang được ghi | API phụ thuộc cấu trúc bảng nội bộ của worker; cần quyền chéo tài khoản |

**Cách làm:**
- `GET /api/v1/jobs/{id}` thêm `activity: { percent, stage }` khi run đang chạy. `DynamoDbWorkerActivityReader` gọi `GetItem` khóa `job_id = "contract:<contract_id>"`, chỉ chiếu `active_job_id, stage, progress`, và chỉ nhận khi `active_job_id` là job đang xem.
- Lỗi, timeout 2 giây hoặc không có dòng thì `activity = null`; trang vẫn hiện 4 bước. Không cấu hình `WorkerActivity:TableArn` thì không đọc (local, test, e2e).
- Web: thanh 8px kiểu thước rủi ro, màu `ink-secondary`; mã `stage` đổi sang tiếng Việt ở `workerActivityLabel` (`frontend/app/lib/labels.ts`), mã lạ thì chỉ hiện %.
- Quyền: resource policy trên bảng (tài khoản AI) cho principal của API, chỉ `dynamodb:GetItem`, giới hạn `dynamodb:Attributes` trong `job_id, active_job_id, stage, progress` và `dynamodb:Select = SPECIFIC_ATTRIBUTES`. Dòng còn chứa `output`, `details` sinh từ hợp đồng, API không đọc được. Phía SE: `dynamodb:GetItem` trên ARN bảng cho user API (staging) hoặc task role (production).

**Ràng buộc với nhóm AI:** đổi tên bảng, khóa hoặc ba trường trên thì báo SE trước (`docs/07` §12). Đổi tên stage thì trang chỉ mất dòng chữ, vẫn còn %.

**Quyết định:** B (Approver chọn 2026-11-21).

### C9 Staging AWS (demo): không domain, dùng lại bộ dev, lưu khóa phiên vào DB (đổi SD-03, C5, C7)

**Bối cảnh:** tài khoản SE mới có SES, bucket và queue bộ dev. Stack CDK (C7) chờ domain, ACM và listener HTTPS. Worker AI chỉ có một máy EC2 đọc queue dev. Approver chọn 2026-11-23:

**Tên gọi (Approver đổi 2026-11-23):** stack này là **staging AWS (demo)**, không phải production: chưa có domain, SES còn sandbox, DB dùng chung với staging Railway. Stack `ContractGuard-StagingAws`, secret `contractguard/staging-aws/*` (thay `ContractGuard-Production`, `contractguard/production/*`). Production thật (domain + ACM, SES production access, DB và S3/SQS riêng) chưa làm. `ASPNETCORE_ENVIRONMENT` vẫn là `Production`: đó là chế độ chạy của .NET (cookie `Secure`, chặn responder giả), giống Railway.

| Mục | Chọn | Đánh đổi |
|---|---|---|
| Domain | **Không mua.** Dùng `https://<id>.cloudfront.net`. ALB vẫn nghe HTTP trong VPC origin; API lấy scheme từ header `CloudFront-Forwarded-Proto` (`Program.cs`) | Không tốn tiền, không chờ DNS. Lệch C7 ("có domain"). URL xấu, và đổi mỗi lần `cdk destroy` rồi deploy lại, nên phải sửa lại CORS bucket và redirect URI của Google. Không xác minh được domain trên SES |
| S3/SQS | **Dùng lại `contractguard-dev-*`** (CDK import, không tạo mới) | Worker không phải đổi gì; `cdk destroy` không xóa tài liệu. Nhưng hai staging cùng đọc `analysis-results`, và consumer **xóa** message của job lạ (`ResultOutcome.Ignored`), nên **phải tắt service `api` trên Railway khi staging AWS chạy**. Dữ liệu demo nằm chung bucket với dữ liệu thử |
| Email | **SES sandbox**; Approver xin production access | Phải xác minh từng người nhận. Bị từ chối thì đổi `Email__Provider=gmail` |
| Khóa phiên | **Data Protection key lưu ở bảng `app.data_protection_keys`** | Hết cảnh mọi người bị đăng xuất sau mỗi lần restart hoặc deploy (cả staging). Khóa nằm ở dạng thường trong Supabase; ai đọc được schema `app` thì giả được cookie. **IA xem lại**; mã hóa khóa khi có KMS dùng được |

- **Database: dùng chung Supabase staging** (Approver chọn 2026-11-23, đổi C5 "Supabase project mới"). Hai môi trường vốn không chạy cùng lúc được (dùng chung S3/SQS), nên tách DB cũng không tách được gì thật. Đổi lại: không tạo project mới, không đụng giới hạn gói Free, migration và kho pháp luật có sẵn. Demo bằng tài khoản và workspace mới để không lẫn dữ liệu thử (SD-05). Muốn hai môi trường chạy song song thì phải tách cả DB lẫn S3/SQS.
- **Không đọc DynamoDB của worker ở staging AWS** (Approver chọn 2026-11-23): không phải nhờ nhóm AI đổi resource policy bảng `contract-risk-jobs`. Trang phân tích chỉ hiện danh sách bước từ message `progress` trên SQS, mất thanh % và dòng "AI đang làm gì" (C8). Staging Railway vẫn có, vì còn biến `WorkerActivity__TableArn`.
- SSE gửi event `ping` mỗi 15 giây khi worker chưa trả lời, vì CloudFront cắt origin im lặng 30 giây (rủi ro đã nêu ở C6).

**Quyết định:** như bảng trên (Approver chọn 2026-11-23).

## Điểm phát sinh khi code

### P3-02 Ghi chú lề khi vùng giữa hẹp

**Bối cảnh:** DESIGN §5 đặt finding ở cột ghi chú lề 244px cạnh tờ hợp đồng 680px. Khi panel chat mở trên laptop 14", vùng giữa thường hẹp hơn 948px (680 + 24 + 244), nên không đủ chỗ cho cột lề.

| Phương án | Mô tả | Ưu | Nhược |
|---|---|---|---|
| **A (đã code, đề xuất)** | Hẹp hơn 948px thì ghi chú chuyển vào trong tờ giấy, ngay dưới dòng loại điều khoản, mỗi ghi chú một dòng (ký hiệu và tiêu đề). Bấm vào thì mở chi tiết ngay dưới điều khoản | Không che chữ; dùng được bằng bàn phím và trình đọc màn hình; không cần popover | Tờ giấy dài hơn một chút |
| B | Thu ghi chú thành chấm màu ở mép phải, bấm thì mở popover | Giữ nguyên chiều dài văn bản | Chấm nhỏ khó bấm (dưới 40px); popover che văn bản; phải quản lý focus |
| C | Ẩn ghi chú, chỉ còn danh sách ở tab Tổng quan | Đơn giản | Mất liên kết hai chiều tại chỗ (FR-VIEW-010) |

Đổi chế độ bằng container query trên vùng giữa, không theo độ rộng màn hình, nên kéo panel chat là đổi ngay.

**Quyết định:**

### P5-01 Ai được dùng link mời tham gia tổ chức

**Bối cảnh:** FR-ORG-004 viết "người được mời xác nhận tham gia qua link mời", nhưng không nói người bấm link có phải đăng nhập đúng email đã được mời hay không. Link nằm trong email, nên có thể bị chuyển tiếp hoặc dán vào chat nhóm.

| Phương án | Mô tả | Ưu | Nhược |
|---|---|---|---|
| **A (đã code, đề xuất)** | Chỉ tài khoản có email trùng email được mời (không phân biệt hoa thường) mới tham gia được. Tài khoản khác nhận lỗi 403 "lời mời gửi tới email khác" (`OrganizationService.cs`, `AcceptAsync`) | Link bị chuyển tiếp không dùng được; Owner tổ chức biết chắc ai vào tổ chức (tổ chức xem được mọi hợp đồng của mình) | Người dùng có hai email phải nhờ Owner mời lại đúng email |
| B | Ai đăng nhập và giữ link còn hạn đều tham gia được | Tiện khi người được mời dùng email khác | Lộ link là lộ quyền vào tổ chức; trái tinh thần cách ly không gian (BR-DATA-001) |

**Quyết định:**

### P6-01 Đăng nhập Google gặp tài khoản email chưa xác thực, hoặc đang bị khóa

**Bối cảnh:** FR-AUTH-011 gộp tài khoản email và Google theo email. SRS không nói hai trường hợp sau:

1. **Email chưa xác thực.** Ai cũng đăng ký được bằng email của người khác, kèm mật khẩu do mình đặt. Nếu chủ thật của email sau đó đăng nhập Google và hệ thống gộp nguyên tài khoản, người đăng ký trước vẫn đăng nhập được bằng mật khẩu đó. Kiểu tấn công này gọi là chiếm tài khoản đăng ký trước.
2. **Đang bị khóa 15 phút** vì nhập sai mật khẩu 5 lần (SEC-AUTH-006).

| Phương án | Mô tả | Ưu | Nhược |
|---|---|---|---|
| **A (đã code, đề xuất)** | (1) Gộp, nhưng gỡ mật khẩu cũ và đánh dấu email đã xác thực, vì Google đã chứng minh chủ email. Muốn có mật khẩu thì dùng "quên mật khẩu". (2) Google vẫn vào được, vì khóa chỉ áp dụng cho đăng nhập bằng mật khẩu (SEC-AUTH-006 ghi "khóa đăng nhập bằng mật khẩu") | Chặn được tấn công; chủ thật không bị kẹt | Người tự đăng ký nhưng chưa bấm link xác thực sẽ mất mật khẩu vừa đặt |
| B | (1) Từ chối đăng nhập Google, bảo người dùng xác thực email trước. (2) Chặn cả Google khi đang khóa | Không đổi dữ liệu tài khoản | Chủ thật bị kẹt: link xác thực gửi tới chính họ, nhưng mật khẩu do người khác đặt |

Code: `backend/src/ContractGuard.Infrastructure/Identity/GoogleAccounts.cs`, `Api/Endpoints/GoogleLoginEndpoints.cs`. Chỉ gộp khi Google báo `email_verified = true`.

**Quyết định:**

### P7-01 Mức rủi ro trong `scoring`: tin mức của worker (trả lời `docs/06` §3.1 câu 1)

**Bối cảnh:** backend suy lại mức từ điểm theo ngưỡng SRS BR-RISK-010 (`< 30` low, `< 55` medium, `< 85` high, còn lại critical) và **bỏ cả kết quả** khi worker ghi mức khác. Worker tính mức theo policy có phiên bản của mình (`scoring.rule_version`, hiện `4.5b-1.2.2`): domain high từ 12 điểm, medium từ 5; tổng high từ 35, medium từ 15; không có critical. Hai output thật trên bucket dev (2026-11-18, 2026-11-20) qua hết kiểm tra khác nhưng bị loại vì `operational` 5 điểm worker ghi `medium`, SE suy ra `low`. Mỗi lần như vậy mất trọn lượt gọi model.

| Phương án | Ưu | Nhược |
|---|---|---|
| **A. Tin mức của worker (Approver chọn 2026-11-23)** | Không phải đổi worker; mức trên web khớp lời giải thích của AI; ngưỡng nằm ở một chỗ có phiên bản | Lệch BR-RISK-010 của SRS đã baseline: cần change request để cập nhật ngưỡng |
| B. Backend tính lại mức theo SRS | Giữ đúng SRS | Web có thể ghi mức khác lời giải thích của AI |
| C. Nhóm AI đổi policy theo 30/55/85 | Backend giữ nguyên | Phải chờ nhóm AI sửa và deploy lại |

**Đã làm (A):** `ExtractionConsistencyValidator` thôi so mức với ngưỡng, và scoring không còn làm hỏng cả kết quả:

- Khối `scoring` hỏng cấu trúc (thiếu hoặc trùng domain, domain áp dụng mà thiếu điểm, domain N/A mà có điểm hoặc thiếu lý do): **bỏ khối scoring**, ghi log cảnh báo, vẫn lưu clause và finding.
- Finding thuộc domain N/A (BR-RISK-003) và finding critical mà tổng dưới 55 (FR-RISK-022): **chỉ ghi log**, giữ nguyên scoring. Policy hiện tại luôn để `cybersecurity` là N/A trong khi check `CY-*` vẫn sinh finding cybersecurity, nên nếu bỏ scoring ở đây thì gần như lần nào cũng mất điểm.
- Kiểm tra clause, trích dẫn, trang, 20 check, domain của finding từ check: giữ nguyên, sai vẫn bỏ cả kết quả.

**Còn mở:** change request cho BR-RISK-010 (ngưỡng theo policy của worker); nhóm AI xem lại `cybersecurity` N/A và mức critical.

**Quyết định:** A (Approver chọn 2026-11-23).

## Việc của các nhóm khác (để theo dõi, không cần quyết ở đây)

| Việc | Nhóm | Ảnh hưởng tới |
|---|---|---|
| Chọn model cho từng bước, model embedding và số chiều vector | AI | SD-06, SD-14 |
| Chọn loại máy GPU, serving stack, xin quota GPU (C3) | AI | C2, C3, Phase 7 |
| Nền tảng chạy worker (C2) | AI | SD-12, NFR-SCAL-001 |
| Xem lại NFR-PRIV và threat model khi dữ liệu nằm ở Supabase (C1); sửa policy IAM bỏ quyền Bedrock (C3) | IA | `security/` |
| Đếm token tiếng Việt thật | AI | Chi phí, độ trễ |
| Cập nhật threat model theo SD-04, SD-08, SD-14 | IA | `security/docs/THREAT_MODEL.md` |
| Ký biên bản chốt giao diện sau SD-10 | AI, IA, SE | `security/docs/CONTRACT_FOR_TEAM.md` |
