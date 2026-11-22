# ContractGuard — System Design

| | |
|---|---|
| Phiên bản | 0.7 — Chốt bước 1 (Q1–Q3) và bước 2–8 (§4); SD-04 đổi sang ASP.NET Core Identity; 2026-11-17 đổi database sang Supabase, model AI tự host trên EC2 (CR-GEN-003), nền tảng worker do nhóm AI chọn (`04` mục C1–C3); 2026-11-20/21 chat qua worker, staging trên Railway, tiến độ từ DynamoDB của worker (`04` mục C6–C8) |
| Ngày | 2026-10-27, sửa 2026-11-22 |
| Đầu vào | [01-product-spec.md](01-product-spec.md) v1.2 |
| Cách làm | Từng bước. Mỗi bước kết thúc bằng các câu hỏi thảo luận, chốt xong mới sang bước sau. |

## 0. Lộ trình thiết kế

| Bước | Nội dung | Trạng thái |
|---|---|---|
| 1 | Ràng buộc và ước lượng quy mô (back-of-the-envelope) | Đã chốt 2026-10-27 (§3) |
| 2 | Kiến trúc tổng thể: các thành phần, ranh giới service, luồng chính | Đã chốt 2026-10-27 (§4) |
| 3 | Mô hình dữ liệu: database, file storage, vector, multi-tenancy | Đã chốt 2026-10-27 (§4) |
| 4 | Thiết kế API và hợp đồng dữ liệu giữa C# và Python (`extraction.json`) | Đã chốt 2026-10-27 (§4) |
| 5 | Đi sâu: pipeline phân tích bất đồng bộ (queue, retry, trạng thái job) | Đã chốt 2026-10-27 (§4) |
| 6 | Đi sâu: chatbot (RAG, streaming, chống prompt injection) | Tạm chốt (SD-14 chốt lại trước phase 4) |
| 7 | Đi sâu: phân quyền, cách ly tenant, upload an toàn | Đã chốt 2026-10-27 (§4) |
| 8 | Vận hành: triển khai, quan sát, chi phí, môi trường dev/staging | Đã chốt 2026-10-27 (§4) |

---

## 1. Ràng buộc

Trong thực tế, ràng buộc của nhóm làm (người, thời gian, tiền) quyết định kiến trúc không kém gì yêu cầu sản phẩm.

| Ràng buộc | Giá trị | Hệ quả cho thiết kế |
|---|---|---|
| Nhân sự | 1 SE (fullstack, C#/.NET + React), 3 AI (Python), 1 IA (bảo mật) | Chỉ một người lo toàn bộ nền tảng, nên càng ít thành phần phải tự vận hành càng tốt. Code AI và code nền tảng cần ranh giới rõ để 3 người AI không phải đụng vào database hay API. |
| Ngôn ngữ | C# cho service nền tảng, Python cho AI worker | Hai stack, nên cần hợp đồng dữ liệu (JSON Schema) dùng chung giữa hai bên. |
| Thời gian | Trên 4 tháng (giả định khoảng 18 tuần) | Có thời gian cho eval và hardening, nhưng không đủ để tự vận hành hạ tầng phức tạp. |
| Hạ tầng | AWS. Mỗi thành viên có khoảng $200 credit (chương trình FCAJ). Region giả định: `ap-southeast-1` (Singapore). | Ngân sách là ràng buộc **cứng**, xem mục 2.7. |
| Mục đích | Sản phẩm demo thực tập, có thể có vài người dùng thử | Không cần high availability. Ưu tiên chi phí thấp và dễ demo. |

---

## 2. Ước lượng quy mô (back-of-the-envelope)

### 2.1 Vì sao ước lượng trước khi vẽ kiến trúc

Ước lượng giúp trả lời câu hỏi: **phần nào của hệ thống thật sự khó?** Nếu không ước lượng, người ta dễ thiết kế cho vấn đề không tồn tại (ví dụ sharding database cho vài trăm hợp đồng) mà bỏ sót vấn đề thật (ví dụ hết credit vì chạy eval, hoặc vượt quota LLM trong buổi demo).

Cách làm: ghi rõ mọi giả định thành số, tính ra lưu lượng, token, dung lượng, độ trễ, chi phí, rồi rút kết luận. Sai số gấp 2–3 lần không sao. Mục tiêu là biết **bậc độ lớn**.

### 2.2 Giả định

| # | Giả định | Giá trị | Ghi chú |
|---|---|---|---|
| A1 | Số trang mỗi hợp đồng | Trung bình 20, tối đa 100 | Theo spec |
| A2 | Token mỗi trang | ~800 | Tiếng Anh ~650, tiếng Việt ~1.000 do dấu. **Cần đo lại** bằng API đếm token trên hợp đồng thật. |
| A3 | Số điều khoản mỗi hợp đồng | ~40 | Hợp đồng demo có 24 |
| A4 | Pipeline LLM cho một lần phân tích | 9 lời gọi: 1 phân loại điều khoản và metadata, 7 đánh giá theo nhóm rủi ro, 1 chạy 20 check cyber | Pipeline giả định để ước lượng, sẽ chốt ở bước 5 |
| A5 | Số người dùng demo/pilot | ~20 người, ~5 tổ chức | |
| A6 | Bộ dữ liệu eval | 20 hợp đồng có nhãn | Eval (evaluation): chạy pipeline trên các hợp đồng đã có đáp án, rồi đo recall, precision, chi phí và độ trễ |
| A7 | Tần suất chạy eval | 1 lần/tuần trên toàn bộ dataset, trong 6 tuần | |
| A8 | Chatbot | ~10 câu hỏi mỗi hợp đồng | |

### 2.3 Lưu lượng và mức đồng thời

| Đại lượng | Tính | Kết quả |
|---|---|---|
| Phân tích do người dùng (cả dự án) | 20 người × 20 hợp đồng, cộng 25% chạy lại | **~500 lần** |
| Phân tích do eval (cả dự án) | 20 hợp đồng × 1 lần/tuần × 6 tuần | **120 lần** |
| Tổng số lần phân tích | | **~620 lần** |
| Số câu chat | 500 hợp đồng × 10 câu | **~5.000 câu** |
| Request API lúc cao điểm (buổi demo) | 30 người cùng xem, UI hỏi trạng thái job mỗi 3 giây | **~10 request/giây** |
| Số phân tích đồng thời lúc cao điểm | 30 người upload gần như cùng lúc | **~30 job** |

**Nhận xét:**

- Eval chỉ chiếm khoảng 20% số lần phân tích. Phần lớn đến từ giả định người dùng (20 người × 20 hợp đồng). **Giả định này có thể vẫn cao** với một buổi demo thực tập, nên xem lại khi có kế hoạch demo cụ thể.
- 10 request/giây là rất nhỏ. Một container .NET nhỏ nhất cũng xử lý được hàng trăm request/giây. **Đây không phải bài toán chịu tải về lượng request.**

### 2.4 Token và chi phí LLM

**Token cho một lần phân tích** (hợp đồng 20 trang ≈ 16k token):

| Bước | Loại model | Input | Output |
|---|---|---|---|
| Phân loại điều khoản và trích metadata | Nhỏ | ~18k (cả hợp đồng + hướng dẫn) | ~3k (chỉ trả mã điều khoản và vị trí, không chép lại văn bản) |
| Đánh giá 7 nhóm rủi ro | Lớn | 7 × ~10k (hướng dẫn và rule ~4k + điều khoản liên quan ~6k) = ~70k | 7 × ~2k = ~14k |
| 20 check Cyber/Privacy/Third-party | Lớn | ~12k | ~3k |
| Thinking (suy luận nội bộ, tính như output) | Lớn | — | ~10k |
| **Tổng** | | **~100k** | **~30k** |

> **Đổi 2026-11-17 (DEC-GEN-029, CR-GEN-003):** model tự host bằng Docker trên EC2, không dùng Bedrock. Chi phí giờ tính theo **giờ máy GPU bật**, không theo token; bảng giá token dưới đây chỉ còn giá trị so sánh. Số token mỗi lần phân tích vẫn dùng để ước lượng độ trễ và cỡ context cần có.
>
> **Chọn model: tạm hoãn.** Trước đây nhóm cân nhắc 3 hướng:
> - Model open-weight có sẵn trên Bedrock (trả theo token).
> - Bedrock Custom Model Import (trả theo thời gian model hoạt động, không theo token, nên cần cách ước lượng riêng).
> - Claude trên cloud.
>
> Các bảng dưới chỉ dùng giá Claude làm **mốc tham khảo** để biết bậc độ lớn chi phí. Phần này sẽ được tính lại khi nhóm chốt model.

**Giá model** (giá API của Anthropic tháng 10/2026, USD cho mỗi 1 triệu token):

| Model | Input | Output | Đọc từ cache |
|---|---|---|---|
| Claude Haiku 5.5 (`claude-haiku-5-5`) | $0,10 | $0,50 | — |
| Claude Sonnet 5.5 (`claude-sonnet-5-5`) | $2 | $10 | $0,20 |
| Claude Opus 5.5 (`claude-opus-5-5`) | $4 | $20 | $0,20 |

> Claude trên Amazon Bedrock có bảng giá riêng. Thường gần với giá trên, nhưng **phải kiểm tra trên trang giá Bedrock** và kiểm tra model nào có ở region đã chọn.

**Chi phí một lần phân tích theo từng phương án model:**

| Phương án | Cách tính | Chi phí/lần |
|---|---|---|
| A. Haiku 5.5 cho mọi bước | 100k × $0,10 + 30k × $0,50 | **~$0,03** |
| B. Haiku phân loại, Sonnet 5.5 đánh giá | $0,003 + (82k × $2 + 27k × $10) ≈ $0,44, trừ ~$0,06 nhờ cache phần hướng dẫn dùng chung | **~$0,38** |
| C. Haiku phân loại, Opus 5.5 đánh giá | $0,003 + (82k × $4 + 27k × $20) | **~$0,87** |

**Chi phí một câu chat** (input ~10k gồm hướng dẫn, điều khoản tìm được, tóm tắt finding và lịch sử; output ~2k kể cả thinking): Haiku ~$0,002, Sonnet ~$0,04, Opus ~$0,08.

**Tổng chi phí LLM cả dự án:**

| Phương án | Phân tích (620 lần) | Chat (5.000 câu, cùng model) | Tổng |
|---|---|---|---|
| A | ~$20 | ~$10 | **~$30** |
| B | ~$235 | ~$200 | **~$435** |
| C | ~$540 | ~$400 | **~$940** |

**Nhận xét:**

- Lựa chọn model làm chi phí chênh nhau **khoảng 30 lần**. Quyết định này phải dựa trên **kết quả eval**, không dựa vào cảm tính.
- Với 120 lần chạy, eval chỉ tốn khoảng $4–100 tùy model. Phần tốn nhiều hơn là người dùng phân tích và chat.
- Model nào dùng cho bước nào phải là **cấu hình**, không hard-code. Như vậy đổi model, kể cả đổi giữa open-weight và Claude, không cần sửa code.

### 2.5 Dung lượng lưu trữ

| Dữ liệu | Mỗi phiên bản hợp đồng | 1.000 phiên bản |
|---|---|---|
| File gốc | ~2 MB | ~3–5 GB |
| Kết quả trích xuất (JSON) | ~200 KB | ~200 MB |
| Dòng dữ liệu nghiệp vụ (clause, finding, điểm, chat) | ~50 KB | ~50 MB |
| Vector embedding | 40 clause × 1.024 chiều × 4 byte ≈ 160 KB | ~160 MB |

**Nhận xét:** dưới 5 GB file và dưới 1 GB database. **Dung lượng không phải vấn đề.** Vì vậy việc chọn database ở bước 3 sẽ dựa trên tính năng và độ đơn giản, không dựa trên khả năng mở rộng. Ví dụ, PostgreSQL kèm pgvector chứa được cả dữ liệu quan hệ lẫn vector trong một hệ thống duy nhất.

### 2.6 Độ trễ một lần phân tích

Spec đặt mục tiêu ≤ 45 giây (p50) cho hợp đồng 20 trang.

| Giai đoạn | Ước lượng | Ghi chú |
|---|---|---|
| Upload và kiểm tra file | 1–3 s | |
| Parse PDF có lớp text (Docling, CPU) | 5–15 s | Không có OCR: bản scan đã bỏ theo CR-GEN-001 |
| Phân loại điều khoản (model nhỏ) | 5–10 s | |
| Đánh giá 7 nhóm (chạy **song song**) | 15–40 s | Phụ thuộc chủ yếu vào số token output. Nếu chạy tuần tự sẽ mất vài phút. |
| Chấm điểm và lưu kết quả | < 2 s | |
| **Tổng khi worker đã chạy sẵn** | **~30–70 s** | |
| Khởi động worker từ 0 (cold start) | **+30–120 s** | Image Python có Docling và model ML nặng cỡ vài GB |

**Nhận xét:**

- Mục tiêu 45 giây chỉ đạt được nếu **chạy song song** và **giới hạn độ dài output**.
- Ý tưởng "worker scale từ 0" trong file Tech stack giúp tiết kiệm tiền nhưng làm **mất mục tiêu 45 giây** với job đầu tiên sau một thời gian không ai dùng. Đây là một trade-off kinh điển giữa chi phí và độ trễ (xem câu hỏi thảo luận Q1).

### 2.7 Chi phí hạ tầng và ngân sách

Ước tính chi phí cố định mỗi tháng nếu chạy 24/7 ở Singapore. Đây là số làm tròn, cần kiểm tra lại bằng AWS Pricing Calculator.

| Thành phần | ~USD/tháng | Ghi chú |
|---|---|---|
| ~~RDS PostgreSQL `db.t4g.micro` + 20 GB~~ Supabase (đổi 2026-11-17) | ~~15~~ 0 | Gói Free; không trừ vào credit AWS. Pro $25/tháng nếu cần sao lưu hoặc không muốn bị tạm dừng sau 7 ngày (`04` mục C1) |
| Application Load Balancer | ~18 | Tốn tiền kể cả khi không có request nào |
| Fargate cho API (0,25 vCPU, 0,5 GB, ARM, chạy 24/7) | ~8 | |
| Địa chỉ IPv4 public (2–3 cái) | ~8–11 | AWS tính phí IPv4 public từ 2024 |
| KMS, Secrets Manager, CloudWatch, S3, CloudFront, SQS | ~5 | |
| **Tổng cố định** | **~55** (~40 với Supabase Free) | **5 tháng ≈ $275** (~$200) |
| Worker phân tích (chạy theo job) | ~1 | 2 vCPU/4 GB × ~1 phút/job × vài trăm job. Nền tảng do nhóm AI chọn (`04` mục C2) |
| Máy GPU chạy model tự host (tài khoản nhóm AI) | theo giờ bật | Chưa chọn loại máy. Tính tiền cả lúc không có job, nên chỉ bật trong giờ test và demo (`04` mục C3) |

**Đặt cạnh ngân sách:**

- Credit thường gắn với **từng account AWS** và không tự cộng dồn. Một account $200 **không đủ** để chạy hạ tầng 24/7 trong 5 tháng, chưa tính tiền LLM.
- Ở quy mô này, **chi phí cố định (ALB, database, IPv4) lớn hơn chi phí theo lượng dùng**. Đây là bài học quan trọng: khi hệ thống nhỏ, mỗi thành phần "luôn bật" đều đắt.

**Hướng xử lý (đề xuất, chốt ở bước 8):**

1. **3 tháng đầu dev local:** chạy Docker Compose gồm PostgreSQL + pgvector và các giả lập S3/SQS. Model AI gọi endpoint của nhóm AI (trước 2026-11-17: Bedrock thật).
2. **Khoảng 6 tuần cuối mới dựng môi trường AWS staging/demo**, và tắt hoặc thu nhỏ vào ban đêm. (Đổi 2026-11-21, `04` mục C7: staging chạy trên Railway, AWS chỉ dựng production quanh showcase.)
3. **Chia credit theo vai trò:** account của SE chạy hạ tầng, account của các thành viên AI dùng cho máy GPU, thử nghiệm và eval model.
4. Hỏi mentor FCAJ xem có gom được credit qua AWS Organizations không.

### 2.8 Kết luận cho các bước sau

| # | Kết luận | Ảnh hưởng tới |
|---|---|---|
| K1 | Hệ thống **ít request nhưng nặng LLM**. Điểm nghẽn là chi phí, độ trễ và quota của LLM, không phải CPU hay database. | Bước 2, 5 |
| K2 | Phân tích phải chạy **bất đồng bộ** qua hàng đợi, có **giới hạn số lời gọi LLM đồng thời** và retry có backoff. | Bước 5 |
| K3 | Model cho từng bước phải **cấu hình được**. Việc chọn model **hoãn lại** và sẽ do eval quyết định. Từ 2026-11-17 chỉ chọn trong các model tự host được trên EC2 (DEC-GEN-029). | Bước 4, 5 |
| K4 | Dung lượng nhỏ, nên ưu tiên **một database đơn giản** (PostgreSQL + pgvector) thay vì nhiều hệ thống chuyên biệt. | Bước 3 |
| K5 | Chi phí cố định chi phối ngân sách, nên **hạn chế thành phần luôn bật**. Cân nhắc lại ALB, số IP public và cách chạy worker. | Bước 2, 8 |
| K6 | Đạt 45 giây đòi hỏi chạy song song theo nhóm rủi ro và có worker chạy sẵn. Scale từ 0 xung đột với mục tiêu này. | Bước 5, 8 |

### 2.9 Rủi ro cần kiểm tra ngay (trước bước 2)

| Rủi ro | Vì sao quan trọng | Cách kiểm tra |
|---|---|---|
| **Credit FCAJ có đủ cho máy GPU không** (đổi 2026-11-17, trước là credit cho Bedrock) | Máy GPU tính tiền theo giờ bật; $200 mỗi account hết nhanh nếu quên tắt máy | Pricing Calculator cho loại máy nhóm AI chọn; AWS Budgets |
| Region có loại máy GPU cần dùng không, còn capacity không | Có thể phải đổi loại máy hoặc chạy model nhỏ hơn (RISK-SRS-003) | EC2 console → Instance types ở `ap-southeast-1` |
| Quota vCPU cho instance GPU (họ G, VT) | Account mới thường bằng 0, không bật được máy (RISK-GEN-002) | Service Quotas. Xin tăng quota **sớm**, vì có thể mất nhiều ngày. |
| Số token thật cho tiếng Việt | A2 có thể sai 1,5–2 lần, kéo theo sai chi phí | Đếm token trên 3–5 hợp đồng mẫu |

---

## 3. Câu hỏi thảo luận trước khi sang bước 2

- **Q1. Độ trễ hay chi phí?** Có giữ một worker luôn chạy sẵn (thêm khoảng $10–30/tháng) để đạt 45 giây không? Hay chấp nhận job đầu tiên chậm 1–2 phút, chỉ bật worker chạy sẵn vào giờ demo?
- **Q2. Môi trường:** có đồng ý dev local 3 tháng đầu và chỉ dựng AWS từ khoảng tuần 12 không?
- **Q3. Mục tiêu độ trễ:** có nới NFR thành p50 ≤ 60 giây và p95 ≤ 2 phút (khi worker đã chạy sẵn) cho thực tế hơn không?

### Kết quả (chốt 2026-10-27)

| Câu | Quyết định | Hệ quả |
|---|---|---|
| Q1 | **Không** giữ worker chạy sẵn. Worker scale từ 0. | Job đầu tiên sau khi worker ngủ chờ thêm 1–3 phút. Không vi phạm NFR-PERF-001..003 vì các mục tiêu này đo khi worker đã chạy sẵn. Cách giữ buổi demo nhanh: xem SD-12. |
| Q2 | Dev local trước, **dựng AWS sau**; thời điểm chưa chốt. | Môi trường local phải chạy được toàn bộ luồng (SD-17). |
| Q3 | Đã giãn mục tiêu độ trễ: text 20 trang p50 ≤ 2 phút, p95 ≤ 5 phút; bản scan ≤ 50 trang p50 ≤ 5 phút (đã bỏ theo CR-GEN-001); chatbot chữ đầu tiên p50 ≤ 15 giây (DEC-GEN-022, SRS NFR-PERF-001..004). | Thay mục tiêu 45 giây ở §2.6. |
| Rủi ro §2.9 | Region Singapore có model cần dùng (xác nhận của Approver). Credit, quota và đếm token là việc của **nhóm AI**. | Theo dõi ở `04-system-design-decisions.md`, mục "Việc của các nhóm khác". |

---

## 4. Quyết định bước 2–8 (chốt 2026-10-27)

Phân tích, phương án và trade-off của từng mục ở [04-system-design-decisions.md](04-system-design-decisions.md). Mục không ghi quyết định riêng nghĩa là Approver đồng ý đề xuất.

| # | Quyết định |
|---|---|
| SD-01 | Modular monolith C# (`backend/`) + worker Python (`worker/`) + SQS. Module nội bộ C#: Auth, Workspace, Contract, Analysis, Chat, Export |
| SD-02 | ECS Fargate ARM: API 1 task luôn bật. Worker: **đổi 2026-11-17**, nhóm AI chọn nền tảng (`04` mục C2); nhóm AI chạy Docker Compose trên một EC2. **Đổi 2026-11-21 (`04` mục C7):** Fargate chỉ dùng cho production, bật ~3 ngày quanh showcase; staging chạy trên Railway |
| SD-03 | Một domain: CloudFront phục vụ SPA từ S3 và chuyển `/api/*` về ALB; lỗi 403/404 trả `index.html`. Bổ sung 2026-11-07: CloudFront gọi ALB bằng HTTPS; security group chỉ cho ALB gọi API; API đọc giao thức gốc từ `X-Forwarded-Proto` |
| SD-04 | **ASP.NET Core Identity** trong service C#: khóa 5 lần / 15 phút theo email, token đặt lại mật khẩu, đăng nhập Google; phiên bằng cookie httpOnly `Secure` `SameSite=Lax`, chống CSRF cho request ghi. Không dùng Cognito. Không có MFA trong MVP (DEC-PRD-022) |
| SD-05 | Lọc theo không gian bằng global query filter của EF Core + kiểm tra quyền theo tài nguyên; bộ test cách ly trong CI |
| SD-06 | **Đổi 2026-11-17: Supabase** (PostgreSQL + pgvector; 2026-11-18: hai project staging/production ở Tokyo, bảng app ở schema `app`, kho pháp luật của AI ở `public`, `04` mục C4, C5; kết nối qua pooler Supavisor chế độ session, TLS; `04` mục C1). Trước đó: RDS PostgreSQL 18 + pgvector, `db.t4g.micro`, single-AZ; migration bằng EF Core; chỉ mục HNSW. Đổi từ 16 sang 18 ngày 2026-10-29 vì lúc này chưa có dữ liệu, và bản 18 được hỗ trợ đến 11/2030 |
| SD-07 | Một bucket, prefix `quarantine/`, `clean/{workspace}/{contract}/{version}/`, `results/{job_id}/`; tắt versioning. **Đổi (`04` mục C7):** SSE-S3 thay SSE-KMS, vì khóa KMS do AWS quản lý không dùng được từ tài khoản AI |
| SD-08 | Presigned POST giới hạn 20 MB; GuardDuty Malware Protection for S3 quét `quarantine/`; worker kiểm tra loại file thật, số trang, làm sạch tài liệu; local bỏ qua quét bằng cờ cấu hình |
| SD-09 | REST `/api/v1` + OpenAPI sinh tự động, sinh kiểu TypeScript cho frontend; lỗi theo Problem Details kèm mã ERR-* |
| SD-10 | Mở rộng `contracts/extraction.schema.json` (tăng `schema_version`): thêm `clauses`, `findings`, `checks`, `metadata`, `injection_warnings`, embedding; sinh lớp C# và Pydantic. Worker sinh finding kèm trích dẫn, tự loại finding sai trích dẫn, rồi tính điểm bằng code Python theo công thức có version (model không tự đưa ra con số); C# kiểm tra trích dẫn và giới hạn của điểm, rồi lưu. Đổi nơi chấm điểm từ C# sang Python ngày 2026-11-06 (`docs/05` §4). Cần AI, IA, SE cùng duyệt. Bản đang dùng là `extraction.v2`; 2026-11-21 bỏ `embeddings` và 3 trường thừa (`docs/07` §13). Lớp C# và model Pydantic hiện viết tay, kiểm bằng JSON Schema |
| SD-11 | Message SQS chỉ chứa `job_id`, loại sự kiện, đường dẫn S3; tiến trình và kết quả cùng đi qua `analysis-results`. Thêm queue `chat-requests` cho câu hỏi chat; câu trả lời `chat_answered` mang luôn chữ (`04` mục C6). Ngoại lệ: API đọc % tiến độ thẳng từ bảng DynamoDB `contract-risk-jobs` của worker (`04` mục C8) |
| SD-12 | **Đổi 2026-11-17:** nhóm AI quyết cách chạy và scale worker, miễn đạt NFR-SCAL-001 và bật sẵn trong giờ demo, giờ test (`04` mục C2). Trước đó: ECS service 0→3 task, scale theo số message |
| SD-13 | SQS `maxReceiveCount = 3` rồi DLQ, visibility 15 phút có gia hạn; retry có backoff khi gọi model (đổi 2026-11-17: endpoint tự host thay Bedrock); C# giữ máy trạng thái, idempotent theo `job_id`; job im lặng quá 20 phút thì `failed` |
| SD-14 | **Đổi 2026-11-20 (`04` mục C6): worker AI trả lời chat.** C# ghi phiếu `chat-request.v1` (câu hỏi, 10 lượt gần nhất, điều khoản và finding của run) lên S3, gửi `chat_requested` lên queue `chat-requests`; worker trả `chat_answered` về `analysis-results`; C# chờ tối đa 3 phút, lọc trích dẫn sai rồi lưu. Trước đó: tạm theo A (C# truy xuất pgvector và gọi model) |
| SD-15 | Server-Sent Events cho chat; tiến trình phân tích vẫn gọi `GET /jobs/{id}` định kỳ. Từ C6 câu trả lời về một lần, backend tách theo từ để giữ hiệu ứng gõ. Chưa có tín hiệu giữ kết nối (`shortcut:` ở `ChatEndpoints.cs`), cần thêm trước khi lên ALB |
| SD-16 | Amazon SES; local dùng Mailpit. 2026-11-18: người gửi `contractguard.info@gmail.com` (email identity, không domain); sandbox thì xác minh từng người nhận; dự phòng Gmail SMTP nếu không ra được sandbox (`04` mục C5) |
| SD-17 | Local: Docker Compose (PostgreSQL + pgvector thay cho Supabase, LocalStack, Mailpit); model gọi endpoint của nhóm AI hoặc dùng bản giả (`FakeChatResponder`). Chế độ Cloud ở máy dev (2026-11-17): môi trường `Cloud`, profile `aws`, dùng Supabase, Amazon SES và S3/SQS thật; secret nằm trong `appsettings.Cloud.local.json` không commit. Đổi 2026-11-18 (`04` mục C5): Cloud trỏ vào Supabase staging; có thêm production (Supabase project riêng, Tokyo), API production dựng ở Phase 7. Đổi 2026-11-21 (`04` mục C7): staging chạy trên Railway (`web` nginx + SPA, `api` nội bộ), dùng bucket và queue `contractguard-dev-*` cùng Supabase staging; production trên AWS (`infra/cdk/`) chỉ bật quanh showcase. |
| SD-18 | AWS CDK (C#); GitHub Actions đăng nhập AWS qua OIDC; workflow riêng cho `backend/`, `worker/`, `frontend/` theo đường dẫn |
| SD-19 | Log JSON có `job_id`, `user_id`, không có nội dung hợp đồng, lên CloudWatch; ghi chi phí mỗi lần gọi model; trace để sau |
| SD-20 | AWS Budgets cảnh báo 50%, 80%; **đổi 2026-11-21 (`04` mục C7):** staging trên Railway (vài $/tháng), stack AWS production chỉ bật ~3 ngày quanh showcase (~$4–5) thay vì tắt API ngoài giờ test; tắt máy GPU của model ngoài giờ test (nhóm AI); bật lại project Supabase nếu bị tạm dừng; **trước khi triển khai lên AWS phải nhắc mọi thành viên cấu hình billing và cảnh báo trên tài khoản của mình** |
