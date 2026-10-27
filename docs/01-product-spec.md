# ContractGuard — Đặc tả sản phẩm (Product Specification)

> **AI-Powered Contract Risk Intelligence Platform**
> Nền tảng quản lý hợp đồng và đánh giá rủi ro hợp đồng B2B bằng AI.

| | |
|---|---|
| Phiên bản tài liệu | 1.2 — sửa theo review lần 1 (phân quyền, phạm vi "chỉ giải thích", quy tắc 7 nhóm rủi ro) |
| Ngày | 2026-10-21 |
| Trạng thái | Chờ nhóm review |
| Nguồn đã hợp nhất | `Idea/Tổng quan.md`, `Idea/Project.md`, `Idea/Định hướng.md`, `Idea/Phạm vi công việc.md`, `Idea/Hợp đồng.pdf`, `Idea/Tech stack and flow.pdf`, `Idea/Mẫu stack.jpg` |
| Tài liệu tiếp theo | `02-system-design.md` (System Design) |

Tài liệu này chỉ trả lời câu hỏi **sản phẩm làm gì, cho ai, đến mức nào** (WHAT). Câu hỏi **xây như thế nào** (HOW: kiến trúc, công nghệ, hạ tầng AWS) được chuyển sang giai đoạn System Design. Các đề xuất kỹ thuật đã có trong thư mục `Idea/` được ghi lại ở [Phụ lục B](#phụ-lục-b--câu-hỏi-kiến-trúc-chuyển-sang-system-design) làm đầu vào cho giai đoạn đó.

---

## 1. Tóm tắt sản phẩm

**Một câu:** ContractGuard giúp doanh nghiệp đọc hiểu hợp đồng do đối tác gửi sang. Hệ thống phân tích từng điều khoản trên 7 nhóm rủi ro, giải thích vì sao có rủi ro, gợi ý hướng xử lý và chấm điểm rủi ro cho toàn bộ hợp đồng. Hệ thống cũng là nơi lưu trữ, chia sẻ và theo dõi hợp đồng của cả tổ chức.

**One-liner (EN):** *ContractGuard is an AI-powered contract risk intelligence platform that analyzes every clause across legal, financial, operational, compliance, cybersecurity, privacy and third-party dimensions, explains each risk, recommends mitigations, and produces a contextual risk profile, all inside a shared contract workspace.*

**Ba trụ cột của sản phẩm:**

1. **Phân tích rủi ro:** phát hiện, giải thích, chấm điểm và gợi ý hướng xử lý.
2. **Trợ lý hội thoại theo hợp đồng:** một chatbot nhắn tin bằng văn bản (không có voice). Người dùng hỏi đáp tự nhiên về hợp đồng đang xem, câu trả lời luôn kèm trích dẫn điều khoản. Chatbot chỉ trả lời trong phạm vi hợp đồng, không phải chatbot đa năng.

> **Phạm vi hiện tại: chỉ giải thích, không sửa hợp đồng.** Hệ thống không viết lại, không soạn câu chữ thay thế và không tạo bản hợp đồng đã chỉnh sửa. Hệ thống chỉ chỉ ra rủi ro, giải thích và gợi ý hướng xử lý bằng lời, ví dụ "nên yêu cầu đối tác quy định thời hạn thông báo sự cố cụ thể". Các tính năng sửa hợp đồng nằm ở [mục 6.7](#67-tính-năng-mở-rộng-sau-mvp).
3. **Quản lý hợp đồng:** thư viện hợp đồng, phiên bản, trạng thái duyệt, chia sẻ quyền theo email (kiểu Google Workspace).

---

## 2. Vấn đề và giải pháp

### 2.1 Quy trình hiện tại (chưa có ContractGuard)

| Bước | Thực tế | Hệ quả |
|---|---|---|
| Tiếp nhận | Đối tác gửi hợp đồng mẫu (PDF/Word/bản scan) qua email hoặc Zalo. | Hợp đồng nằm rải rác, không có nơi lưu tập trung. |
| Đọc thủ công | Sales hoặc Giám đốc tự đọc 20–50 trang. Thường chỉ xem giá trị và lịch thanh toán. | Bỏ sót điều khoản phạt vi phạm, đơn phương chấm dứt, bồi thường, bảo mật dữ liệu. |
| Chỉnh sửa | Tự gạch xóa trong Word theo cảm tính. | Không có căn cứ để thuyết phục đối tác khi đàm phán. |
| Phê duyệt | Giám đốc ký trong tâm thế không chắc chắn. | Có thể có "bẫy" pháp lý mà không ai phát hiện. |
| Tranh chấp | Chỉ phát hiện mình ở thế bất lợi khi sự cố đã xảy ra. | Thiệt hại tài chính, mất dữ liệu, kẹt hợp đồng. |

### 2.2 Quy trình mới (có ContractGuard)

1. **Upload:** kéo thả file PDF, DOCX hoặc ảnh scan vào web, không cần cài đặt gì.
2. **Khai báo ngữ cảnh:** cho biết bên mình là bên nào trong hợp đồng, đối tác cung cấp gì, có xử lý dữ liệu cá nhân hoặc dữ liệu nhạy cảm không. Hệ thống tự điền sẵn những gì trích xuất được, người dùng chỉ cần xác nhận.
3. **AI phân tích (mục tiêu 30–45 giây):** đọc file (kể cả OCR), tách điều khoản, phân loại, đánh giá rủi ro, đối chiếu với bộ rule rủi ro và các căn cứ pháp lý chọn lọc.
4. **Xem kết quả:** điểm rủi ro tổng, điểm theo từng nhóm, danh sách finding. Rủi ro được tô màu ngay trên văn bản hợp đồng.
5. **Hỏi đáp và xử lý:** hỏi trợ lý (ví dụ "Nếu mình muốn chấm dứt sớm thì sao?"). Đánh dấu từng finding là chấp nhận rủi ro, cần đàm phán hoặc không đúng.
6. **Hành động:** xuất báo cáo PDF hoặc DOCX, chia sẻ cho người duyệt, upload phiên bản mới sau khi đàm phán.

---

## 3. Khách hàng mục tiêu

### 3.1 Định vị

- **Thị trường chính:** doanh nghiệp SME Việt Nam, đặc biệt các doanh nghiệp không có bộ phận pháp chế riêng.
- **Tình huống sử dụng chính:** thẩm định hợp đồng **do đối tác gửi sang** trước khi ký. Ví dụ: hợp đồng dịch vụ, hợp đồng mua hàng, hợp đồng với nhà cung cấp phần mềm hoặc cloud.
- **Ngôn ngữ hợp đồng:** tiếng Việt và tiếng Anh.
- **Điểm khác biệt:** phân tích rất sâu ba nhóm Cybersecurity, Data & Privacy và Third-Party. Các công cụ rà soát hợp đồng thông thường hầu như bỏ qua ba nhóm này, trong khi rủi ro ở đây ngày càng lớn với mọi hợp đồng có dữ liệu đi qua bên thứ ba.

### 3.2 Personas

| Persona | Mục tiêu | Cần gì từ ContractGuard |
|---|---|---|
| **Giám đốc / Chủ doanh nghiệp** (người ký) | Quyết định nhanh có nên ký không. | Điểm tổng, top rủi ro Critical/High, khuyến nghị một dòng (ví dụ "Không nên duyệt khi chưa xử lý 8 finding High/Critical"). |
| **Nhân viên Sales / Mua hàng / Vận hành** (người nhận hợp đồng) | Chuẩn bị nội dung đàm phán với đối tác. | Giải thích dễ hiểu, gợi ý những điểm cần đàm phán lại, báo cáo xuất được để gửi đi. |
| **Người review chuyên môn** (pháp chế, IT/Security, có thể thuê ngoài) | Kiểm tra kỹ từng điểm. | Trích dẫn nguyên văn, vị trí trong file, checklist cybersecurity, căn cứ tham khảo. |
| **Owner tổ chức** | Quản lý thành viên và quyền truy cập. | Mời thành viên qua email, cấp hoặc thu hồi quyền Owner. |

---

## 4. Nguyên tắc sản phẩm

Đây là các nguyên tắc bắt buộc. Mọi tính năng phải tuân theo chúng.

1. **Hỗ trợ ra quyết định, không thay luật sư.** Hệ thống không kết luận một điều khoản là "trái pháp luật". Kết luận đúng là **"rủi ro tiềm ẩn, cần xem xét"**, kèm căn cứ tham khảo nếu có. Lý do: việc áp dụng luật phụ thuộc quốc gia, loại giao dịch và hoàn cảnh cụ thể. Mọi báo cáo đều có dòng miễn trừ trách nhiệm (disclaimer).
2. **Không có bằng chứng thì không có finding.** Mỗi finding phải trích nguyên văn điều khoản và vị trí của nó (trang, tọa độ). Riêng finding loại "thiếu điều khoản" phải ghi rõ đã tìm những gì mà không thấy.
3. **Rủi ro luôn được đánh giá từ góc nhìn của bên mình.** Một điều khoản thanh toán trả trước 80% là rủi ro cho bên mua nhưng có lợi cho bên bán. Vì vậy người dùng phải xác định bên mình là bên nào.
4. **Giải thích được và truy vết được.** Mỗi kết quả lưu lại nó được tạo bởi phiên bản rule, prompt và model nào, để có thể tái hiện và đánh giá lại.
5. **Hợp đồng là dữ liệu không tin cậy (untrusted input).** File upload có thể chứa mã độc hoặc câu lệnh nhằm lừa AI bỏ qua rủi ro (prompt injection). Hệ thống phải xử lý mọi nội dung hợp đồng như dữ liệu, không bao giờ như chỉ dẫn.
6. **Dữ liệu của mỗi tổ chức được cách ly tuyệt đối.** Không truy vấn, tìm kiếm, chat hay RAG nào được phép chạm vào dữ liệu của tổ chức khác.

---

## 5. Phạm vi MVP

| Hạng mục | Trong MVP | Ghi chú |
|---|---|---|
| Loại hợp đồng | Service Agreement, Vendor Agreement, Purchase Agreement, MSA, SOW | Hợp đồng demo chính: Cloud/SaaS Service Agreement |
| Ngôn ngữ hợp đồng | Tiếng Việt, tiếng Anh | Một hợp đồng song ngữ được xử lý như một văn bản |
| Ngôn ngữ giao diện và kết quả | Tiếng Việt, tiếng Anh | Giải thích và khuyến nghị theo ngôn ngữ người dùng chọn. Trích dẫn điều khoản giữ nguyên văn gốc. |
| Định dạng file | PDF (có lớp text), PDF scan, DOCX, ảnh JPG/PNG | Giới hạn đề xuất: ≤ 20 MB, ≤ 100 trang |
| Loại điều khoản nhận diện | 21 loại ([mục 7.2](#72-danh-mục-loại-điều-khoản-clause-taxonomy)) | Kèm một nhóm "Khác" |
| Nhóm rủi ro | 7 nhóm ([mục 7.1](#71-bảy-nhóm-rủi-ro-risk-domains)) | |
| Bộ kiểm tra Cyber/Privacy/Third-party | 20 check ([mục 7.3](#73-bộ-kiểm-tra-cybersecurity--privacy--third-party)) | |
| Căn cứ pháp lý | Bộ nhỏ chọn lọc, vài chục điều luật Việt Nam và chuẩn quốc tế ([mục 7.7](#77-cơ-sở-tri-thức-knowledge-base)) | Chỉ dùng làm căn cứ tham khảo |

---

## 6. Yêu cầu chức năng

**Mức ưu tiên:**

- **M (Must):** bắt buộc có trong MVP.
- **S (Should):** nên có trong MVP, được cắt nếu thiếu thời gian.
- **L (Later):** làm sau MVP.

### 6.1 Tài khoản, tổ chức và phân quyền

Mô hình phân quyền mô phỏng Google Workspace/Drive: quyền được cấp theo **email**, ở hai cấp là tổ chức và từng hợp đồng. Mỗi cấp chỉ có **hai vai trò: Owner và Member**. MVP chưa cần phân quyền chi tiết hơn.

| Cấp | Owner | Member |
|---|---|---|
| **Tổ chức** | Mời và xóa thành viên, cấp hoặc thu hồi quyền Owner, sửa cài đặt tổ chức. Có thể có nhiều Owner. | Tạo hợp đồng mới, làm việc với các hợp đồng được chia sẻ. |
| **Hợp đồng** | Người tạo hợp đồng. Có mọi quyền của Member, cộng thêm: chia sẻ, thu hồi chia sẻ, chuyển quyền Owner, xóa hợp đồng. | Người được chia sẻ. Xem kết quả, chat, xuất báo cáo, upload phiên bản mới, chạy lại phân tích, đổi trạng thái hợp đồng và trạng thái finding. |

| ID | Yêu cầu | Ưu tiên |
|---|---|---|
| ACC-01 | Đăng ký, đăng nhập bằng email và mật khẩu. Có xác thực email và quên mật khẩu. | M |
| ACC-02 | Đăng nhập bằng Google (OIDC). | S |
| ACC-03 | Mỗi người dùng thuộc một **tổ chức** (tenant). Người tạo tổ chức là Owner tổ chức. | M |
| ACC-04 | Owner tổ chức mời thành viên qua email. Người được mời nhận link tham gia. | M |
| ACC-05 | Vai trò cấp tổ chức: **Owner** và **Member** (xem bảng trên). Tổ chức luôn phải còn ít nhất một Owner. | M |
| ACC-06 | Hợp đồng mặc định chỉ người tạo thấy. Owner hợp đồng có thể chia sẻ cho từng email trong tổ chức, hoặc cho toàn tổ chức. | M |
| ACC-07 | Vai trò cấp hợp đồng: **Owner** và **Member** (xem bảng trên). | M |
| ACC-08 | Thu hồi quyền chia sẻ. Có hiệu lực ngay lập tức. | M |
| ACC-09 | MFA cho tài khoản. | S |
| ACC-10 | Chia sẻ hợp đồng cho email **ngoài tổ chức** (khách, luật sư thuê ngoài). | L |
| ACC-11 | SSO doanh nghiệp (SAML). | L |

### 6.2 Quản lý hợp đồng

| ID | Yêu cầu | Ưu tiên |
|---|---|---|
| CON-01 | Upload hợp đồng bằng kéo thả hoặc chọn file. Kiểm tra định dạng và kích thước ngay ở bước upload. | M |
| CON-02 | Khai báo ngữ cảnh khi upload: **bên mình là bên nào** (bên mua/khách hàng hay bên bán/nhà cung cấp), tên đối tác, loại hợp đồng, đối tác cung cấp gì, có xử lý dữ liệu cá nhân hoặc nhạy cảm không, mức độ quan trọng của dịch vụ. | M |
| CON-03 | Metadata tự trích xuất: các bên, loại hợp đồng, giá trị, đồng tiền, ngày hiệu lực, ngày hết hạn, luật áp dụng. Người dùng sửa được. | M |
| CON-04 | Thư viện hợp đồng: danh sách, tìm theo tên hoặc đối tác, lọc theo mức rủi ro, trạng thái, loại hợp đồng, sắp xếp. | M |
| CON-05 | Trạng thái duyệt của hợp đồng: `Mới` → `Đang phân tích` → `Chờ review` → `Đang đàm phán` → `Đã duyệt` hoặc `Từ chối`. Owner và Member của hợp đồng đều được chuyển trạng thái. | M |
| CON-06 | **Phiên bản:** upload V2, V3 cho cùng một hợp đồng. Mỗi phiên bản có kết quả phân tích riêng. Xem được lịch sử phiên bản. | M |
| CON-07 | Xóa hợp đồng: xóa vĩnh viễn file gốc, dữ liệu trích xuất, embedding, finding và lịch sử chat. | M |
| CON-08 | Nhật ký hoạt động (audit log) trên mỗi hợp đồng: ai upload, xem, chia sẻ, xuất, đổi trạng thái. | S |
| CON-09 | Gắn tag hoặc thư mục cho hợp đồng. | L |
| CON-10 | Nhắc trước ngày hết hạn hoặc gia hạn tự động của hợp đồng. | L |

### 6.3 Phân tích hợp đồng

| ID | Yêu cầu | Ưu tiên |
|---|---|---|
| ANA-01 | Tự động phân tích ngay sau khi upload và xác nhận ngữ cảnh. | M |
| ANA-02 | Hiển thị tiến trình theo bước (đang đọc file → đang tách điều khoản → đang đánh giá → đang chấm điểm → hoàn tất). Báo lỗi rõ lý do nếu thất bại. | M |
| ANA-03 | Trích xuất văn bản, kể cả OCR cho bản scan, và giữ lại cấu trúc (mục, điều, khoản) cùng vị trí trên trang. | M |
| ANA-04 | Tách hợp đồng thành từng điều khoản (clause segmentation) và phân loại vào 21 loại điều khoản. | M |
| ANA-05 | Đánh giá rủi ro từng điều khoản trên 7 nhóm rủi ro, sinh finding theo cấu trúc chuẩn ([mục 7.5](#75-cấu-trúc-một-finding)). Mỗi finding có đúng một nhóm chính. | M |
| ANA-06 | Phát hiện **điều khoản bị thiếu**, ví dụ không có quyền kiểm toán hoặc không có thời hạn thông báo sự cố. Thiếu điều khoản cũng là một loại rủi ro. | M |
| ANA-07 | Chạy bộ 20 check Cyber/Privacy/Third-party và trả về trạng thái từng check (PASS / PARTIAL / MISSING). | M |
| ANA-08 | Chấm điểm 0–100 cho từng nhóm rủi ro và cho cả hợp đồng, có điều chỉnh theo ngữ cảnh ([mục 7.6](#76-nguyên-tắc-chấm-điểm)). | M |
| ANA-09 | Gắn căn cứ tham khảo (điều luật hoặc điều khoản chuẩn) cho finding khi tìm được trong cơ sở tri thức. | M |
| ANA-10 | Chạy lại phân tích cho một phiên bản, ví dụ sau khi người dùng sửa ngữ cảnh. | M |
| ANA-11 | Phát hiện nội dung có dấu hiệu prompt injection trong hợp đồng và cảnh báo người dùng. | M |
| ANA-12 | Xác định nhóm nào áp dụng hoặc N/A dựa trên ngữ cảnh. Bật lại nhóm N/A và nhắc người dùng nếu nội dung hợp đồng mâu thuẫn với ngữ cảnh đã khai báo. | M |

### 6.4 Xem kết quả (Risk Dashboard và Document Viewer)

| ID | Yêu cầu | Ưu tiên |
|---|---|---|
| VIEW-01 | Tổng quan: điểm tổng 0–100 kèm mức rủi ro, điểm của từng nhóm (nhóm không áp dụng hiển thị N/A kèm lý do), số finding theo severity, khuyến nghị tổng. | M |
| VIEW-02 | Danh sách finding: lọc theo nhóm rủi ro, severity, trạng thái xử lý. | M |
| VIEW-03 | Document viewer: hiển thị hợp đồng gốc, tô màu điều khoản theo severity. Bấm vào điều khoản thì mở finding tương ứng, và ngược lại. | M |
| VIEW-04 | Checklist Cybersecurity: bảng 20 check kèm trạng thái và link tới điều khoản liên quan. | M |
| VIEW-05 | Chi tiết finding: Why, Impact, Recommendation (hướng xử lý bằng lời), căn cứ tham khảo. | M |
| VIEW-06 | Xử lý finding: đánh dấu `Chấp nhận rủi ro`, `Cần đàm phán`, `Không đúng` (false positive), `Đã xử lý`. Có ghi chú kèm theo. | S |

### 6.5 Trợ lý hội thoại theo hợp đồng

Trợ lý là một **chatbot dạng nhắn tin văn bản** (không có voice), nằm cạnh document viewer. Nó đóng vai một "chuyên viên đã đọc kỹ hợp đồng này", **không** phải chatbot tổng quát, và chỉ giải thích chứ không viết lại hợp đồng.

| ID | Yêu cầu | Ưu tiên |
|---|---|---|
| CHAT-01 | Hỏi đáp tự nhiên (Việt/Anh) về hợp đồng và phiên bản đang mở. | M |
| CHAT-02 | Mọi câu trả lời dựa trên nội dung hợp đồng, kết quả phân tích và cơ sở tri thức. Câu trả lời trích dẫn điều khoản, bấm vào trích dẫn thì nhảy tới vị trí trong viewer. | M |
| CHAT-03 | Nếu hợp đồng không đề cập tới điều được hỏi, trợ lý nói rõ là không có thay vì suy đoán. | M |
| CHAT-04 | Gợi ý câu hỏi nhanh, ví dụ "Rủi ro lớn nhất là gì?", "Điều khoản chấm dứt có bất lợi cho mình không?". | S |
| CHAT-05 | Yêu cầu viết lại một điều khoản theo hướng có lợi hơn cho bên mình (xem EXT-07). | L |
| CHAT-06 | Lưu lịch sử hội thoại theo hợp đồng. Những người được chia sẻ hợp đồng xem được lịch sử chat. | S |
| CHAT-07 | Hỏi đáp trên **toàn bộ thư viện**, ví dụ "Hợp đồng nào có trách nhiệm bồi thường vô hạn?". Chỉ trong phạm vi hợp đồng người dùng có quyền xem. | L |

Phạm vi truy cập của trợ lý luôn bị giới hạn bởi quyền của người hỏi ([nguyên tắc 6](#4-nguyên-tắc-sản-phẩm)).

### 6.6 Xuất báo cáo

| ID | Yêu cầu | Ưu tiên |
|---|---|---|
| EXP-01 | Xuất báo cáo rủi ro dạng **PDF**: thông tin hợp đồng, điểm tổng, điểm theo nhóm, danh sách finding đầy đủ, checklist cyber, disclaimer. | M |
| EXP-02 | Xuất cùng báo cáo đó dạng **DOCX**, để người dùng chỉnh sửa tiếp trước khi gửi đối tác. | M |
| EXP-03 | Chọn ngôn ngữ báo cáo (Việt/Anh) và phạm vi (tất cả finding hay chỉ High/Critical). | S |
| EXP-04 | **Redline DOCX:** sinh file hợp đồng có track changes và comment giải thích ngay tại vị trí rủi ro. Chỉ áp dụng được khi file gốc là DOCX. | L |

### 6.7 Tính năng mở rộng (sau MVP)

| ID | Tính năng | Mô tả |
|---|---|---|
| EXT-01 | **So sánh phiên bản** (Compare Mode) | So sánh V1 với V2, chỉ ra điểm hợp đồng bị yếu đi (ví dụ thông báo sự cố đổi từ "24 giờ" thành "promptly") và mức thay đổi điểm rủi ro. |
| EXT-02 | **Company Policy / Playbook** | Tổ chức upload chính sách nội bộ (ví dụ "nhà cung cấp quan trọng phải báo sự cố ≤ 24h, lưu log ≥ 180 ngày"). Hệ thống chấm hợp đồng theo chính sách đó: Đạt / Một phần / Không đạt. |
| EXT-03 | **Framework Mapping** | Ánh xạ finding sang NIST CSF 2.0, ISO/IEC 27001, SOC 2. |
| EXT-04 | **Tự động rà soát lại** | Khi bộ rule hoặc policy thay đổi, tự chạy lại các hợp đồng liên quan và cảnh báo nếu mức rủi ro thay đổi. |
| EXT-05 | **Redline DOCX** | Như EXP-04. |
| EXT-06 | **Chat trên toàn thư viện** | Như CHAT-07. |
| EXT-07 | **Đề xuất câu chữ sửa đổi** | Sinh đoạn điều khoản thay thế cho từng finding, và cho phép viết lại điều khoản qua chat (CHAT-05). Đây là bước đầu tiên khi chuyển từ "chỉ giải thích" sang "hỗ trợ sửa hợp đồng". |

---

## 7. Mô hình rủi ro (Risk Model)

### 7.1 Bảy nhóm rủi ro (Risk Domains)

Mỗi nhóm trả lời một câu hỏi mà người sắp ký hợp đồng cần biết.

| Nhóm | Câu hỏi nhóm trả lời | Phạm vi | Ví dụ rủi ro điển hình |
|---|---|---|---|
| **Legal** | Khi có chuyện, mình chịu trách nhiệm đến đâu và giải quyết tranh chấp ở đâu? | Giới hạn trách nhiệm, bồi thường, bảo hành, chấm dứt, bất khả kháng, bảo mật thông tin kinh doanh (NDA), luật áp dụng, tranh chấp | Trách nhiệm vô hạn (Critical). Chỉ đối tác được đơn phương chấm dứt (High). Tranh chấp giải quyết tại trọng tài nước ngoài theo luật nước ngoài (Medium/High). |
| **Financial** | Mình phải trả bao nhiêu, khi nào, và có thể mất thêm tiền ở đâu? | Giá, thanh toán, trả trước, phí, điều chỉnh giá, gia hạn, phạt vi phạm, lãi chậm trả | Trả trước 80% (High). Phạt chậm thanh toán 0,5%/ngày (High). Tự gia hạn kèm tăng giá mà không báo trước (High). |
| **Operational** | Đối tác có giao đúng thứ mình cần, đúng hạn, đúng chất lượng không? | Phạm vi công việc, giao hàng, SLA, service credit, nghiệm thu, hỗ trợ | Phạm vi mơ hồ ("hỗ trợ khi cần") (Medium). Uptime 99% (~3,65 ngày downtime/năm) mà không đền bù (Medium/High). Không có tiêu chí nghiệm thu (High). |
| **Compliance** | Hợp đồng có khiến mình vi phạm nghĩa vụ với cơ quan nhà nước không? | Thuế và hóa đơn, giấy phép ngành, quy định chuyên ngành, lưu hồ sơ, báo cáo cho cơ quan quản lý (không gồm dữ liệu cá nhân) | Không quy định xuất hóa đơn VAT, nên mình không khấu trừ được chi phí (High). Không yêu cầu đối tác có giấy phép bắt buộc của ngành (High). |
| **Cybersecurity** | Nếu hệ thống của đối tác bị tấn công, hệ thống và dữ liệu của mình có được bảo vệ không? | Biện pháp bảo mật kỹ thuật và tổ chức, sự cố an ninh, sao lưu và khôi phục, chứng chỉ bảo mật | Thời hạn thông báo sự cố mơ hồ (Critical). Chỉ cam kết "appropriate security measures" (High). |
| **Data & Privacy** | Dữ liệu mình giao cho đối tác thuộc về ai, được dùng vào việc gì và đi đâu? | Sở hữu dữ liệu, mục đích xử lý, chuyển dữ liệu ra nước ngoài, lưu trữ, xóa và trả lại, nghĩa vụ theo pháp luật bảo vệ dữ liệu cá nhân | Đối tác được dùng dữ liệu khách hàng của mình "để cải thiện dịch vụ" (High). Đối tác giữ dữ liệu sau khi chấm dứt hợp đồng (High/Critical). |
| **Third-Party** | Ngoài bên ký với mình, còn ai khác chạm vào dịch vụ hoặc dữ liệu? | Nhà thầu phụ, subprocessor, nghĩa vụ áp xuống bên thứ ba (flow-down), trách nhiệm liên đới, quyền kiểm toán đối tác | Đối tác được thuê nhà thầu phụ tùy ý (High). Đối tác không chịu trách nhiệm về lỗi của nhà thầu phụ (High). |

7 nhóm này trộn hai cách chia khác nhau. Legal, Financial và Operational chia theo **loại thiệt hại**, còn Compliance, Cybersecurity, Privacy và Third-Party chia theo **chủ đề**. Vì vậy một vấn đề có thể thuộc nhiều nhóm cùng lúc. Ví dụ "trách nhiệm vô hạn khi lộ dữ liệu" vừa là Legal, Financial, vừa là Privacy. Ba quy tắc dưới đây giải quyết chuyện này.

#### Quy tắc 1: Mỗi finding có đúng một nhóm chính

- **Nhóm chính** (primary domain): đúng một nhóm, và **chỉ nhóm chính được tính điểm**. Nhờ vậy một vấn đề không bị cộng điểm nhiều lần.
- **Nhóm phụ** (secondary domains): không hoặc nhiều nhóm, chỉ dùng để lọc và hiển thị.
- Nhóm chính được xác định bằng rule cố định, không để LLM tự chọn. Finding sinh ra từ một check (CY, PR, TP) thì lấy nhóm của check đó. Các finding còn lại lấy nhóm mặc định của loại điều khoản ([mục 7.2](#72-danh-mục-loại-điều-khoản-clause-taxonomy)), sau đó điều chỉnh theo quy tắc 3.

#### Quy tắc 2: Nhóm có thể "Không áp dụng" (N/A)

Không phải hợp đồng nào cũng liên quan đủ 7 nhóm. Ví dụ hợp đồng mua bàn ghế không có dữ liệu nào đi qua đối tác. Nếu nhóm Cybersecurity hiện 0/100, người dùng sẽ hiểu nhầm là "an toàn". Vì vậy:

| Nhóm | Khi nào áp dụng |
|---|---|
| Legal, Financial, Operational, Compliance | Luôn áp dụng |
| Cybersecurity | Đối tác cung cấp dịch vụ CNTT, phần mềm hoặc cloud, hoặc được truy cập hệ thống hay dữ liệu của mình |
| Data & Privacy | Đối tác tiếp nhận, lưu trữ hoặc xử lý dữ liệu của mình, đặc biệt dữ liệu cá nhân |
| Third-Party | Hợp đồng dịch vụ (đối tác có thể giao lại việc cho bên khác), hoặc nhóm Cybersecurity hay Privacy đang áp dụng |

- Mức áp dụng được tính từ ngữ cảnh khai báo lúc upload (CON-02).
- Nhóm không áp dụng hiển thị **N/A** kèm lý do, và không tính vào điểm tổng.
- **Chống khai báo sai:** nếu hợp đồng có điều khoản thuộc một nhóm đang bị đánh dấu N/A (ví dụ khai "không có dữ liệu" nhưng hợp đồng có điều khoản xử lý dữ liệu), hệ thống bật lại nhóm đó và nhắc người dùng kiểm tra lại ngữ cảnh.

#### Quy tắc 3: Ranh giới giữa các nhóm

Khi một finding có thể thuộc nhiều nhóm, nhóm chính được chọn theo thứ tự ưu tiên: **chủ đề cụ thể thắng loại thiệt hại chung.**

1. Liên quan dữ liệu của mình hoặc dữ liệu cá nhân (sở hữu, mục đích, lưu trữ, xóa, chuyển ra nước ngoài, nghĩa vụ theo luật bảo vệ dữ liệu cá nhân) → **Privacy**. Áp dụng cả khi đó là nghĩa vụ luật định.
2. Liên quan biện pháp bảo vệ hệ thống, dữ liệu, hoặc sự cố an ninh → **Cybersecurity**. Áp dụng cả khi nội dung là trách nhiệm bồi thường cho sự cố bảo mật (CY-12).
3. Liên quan bên ngoài hai bên ký (nhà thầu phụ, subprocessor, flow-down, quyền kiểm toán đối tác) → **Third-Party**.
4. Nghĩa vụ với cơ quan nhà nước không liên quan dữ liệu (thuế, hóa đơn, giấy phép, báo cáo) → **Compliance**.
5. Các trường hợp còn lại chia theo loại thiệt hại:
   - Tiền thanh toán, phí, phạt, lãi → **Financial**.
   - Trách nhiệm, quyền, chấm dứt, tranh chấp → **Legal**. Giới hạn trách nhiệm vẫn là Legal, dù nội dung nói về tiền.
   - Việc thực hiện, chất lượng dịch vụ → **Operational**.

Một số trường hợp hay nhầm:

| Trường hợp | Nhóm chính | Nhóm phụ |
|---|---|---|
| NDA về bí mật kinh doanh | Legal | — |
| NDA có phạm vi gồm dữ liệu cá nhân | Privacy | Legal |
| Trách nhiệm vô hạn (chung) | Legal | Financial |
| Sự cố bảo mật bị loại trừ khỏi trách nhiệm bồi thường | Cybersecurity | Legal |
| Phạt vi phạm vượt mức thông thường | Financial | Legal |
| Không có quyền kiểm toán bảo mật đối tác | Third-Party | Cybersecurity |
| Đối tác không cam kết tuân thủ luật bảo vệ dữ liệu cá nhân | Privacy | Compliance |

### 7.2 Danh mục loại điều khoản (Clause Taxonomy)

Mỗi loại điều khoản có **một nhóm chính mặc định**. Một finding cụ thể có thể đổi sang nhóm chính khác theo quy tắc 3 ở [mục 7.1](#71-bảy-nhóm-rủi-ro-risk-domains).

| Mã | Loại điều khoản | Nhóm chính mặc định | Ngoại lệ thường gặp |
|---|---|---|---|
| C01 | Scope of Work / Services (Phạm vi công việc) | Operational | |
| C02 | Payment Terms (Thanh toán) | Financial | |
| C03 | Fees & Price Adjustment (Phí và điều chỉnh giá) | Financial | |
| C04 | Late Payment & Penalties (Trễ hạn và phạt vi phạm) | Financial | |
| C05 | Term & Renewal (Thời hạn và gia hạn) | Financial | Không có quyền thoát khỏi gia hạn thì là Legal |
| C06 | Termination (Chấm dứt hợp đồng) | Legal | Xóa hoặc trả dữ liệu khi chấm dứt thì là Privacy |
| C07 | Limitation of Liability (Giới hạn trách nhiệm) | Legal | Loại trừ trách nhiệm cho sự cố bảo mật thì là Cybersecurity |
| C08 | Indemnification (Bồi thường thiệt hại) | Legal | |
| C09 | Warranties & Disclaimers (Bảo hành và khước từ) | Legal | |
| C10 | Governing Law & Dispute Resolution (Luật áp dụng và tranh chấp) | Legal | |
| C11 | SLA & Service Credits (Cam kết dịch vụ) | Operational | |
| C12 | Acceptance (Nghiệm thu) | Operational | |
| C13 | Force Majeure (Bất khả kháng) | Legal | |
| C14 | Confidentiality (Bảo mật thông tin) | Legal | Phạm vi gồm dữ liệu cá nhân thì là Privacy |
| C15 | Data Ownership & Processing (Sở hữu và xử lý dữ liệu) | Privacy | |
| C16 | Security Standards & Safeguards (Tiêu chuẩn an toàn thông tin) | Cybersecurity | |
| C17 | Incident Notification (Thông báo sự cố) | Cybersecurity | |
| C18 | Data Retention & Deletion (Lưu trữ và xóa dữ liệu) | Privacy | |
| C19 | Subcontracting / Subprocessors (Nhà thầu phụ) | Third-Party | |
| C20 | Audit Rights (Quyền kiểm toán đối tác) | Third-Party | |
| C21 | Regulatory, Tax & Invoicing (Tuân thủ, thuế, hóa đơn, giấy phép) | Compliance | |
| C99 | Khác / không phân loại được | — | |

### 7.3 Bộ kiểm tra Cybersecurity / Privacy / Third-Party

Bộ check dưới đây trả lời câu hỏi: *một hợp đồng an toàn về cybersecurity cần quy định những gì?* Mỗi check trả về một trong bốn trạng thái:

- **PASS:** có và đủ cụ thể.
- **PARTIAL:** có nhưng mơ hồ hoặc thiếu ý.
- **MISSING:** không có.
- **N/A:** nhóm của check không áp dụng cho hợp đồng này (quy tắc 2).

Check ở trạng thái PARTIAL hoặc MISSING sinh ra finding. Nhóm chính của finding lấy theo tiền tố mã check: CY là Cybersecurity, PR là Privacy, TP là Third-Party.

| ID | Check | Ví dụ trường hợp fail |
|---|---|---|
| CY-01 | Yêu cầu bảo mật đo lường được | Chỉ ghi "appropriate security measures" |
| CY-02 | Mã hóa dữ liệu at-rest và in-transit | Không nhắc tới mã hóa |
| CY-03 | Kiểm soát truy cập, least privilege | Không giới hạn ai được truy cập dữ liệu |
| CY-04 | Xác thực đa yếu tố (MFA) | Không yêu cầu MFA cho truy cập quản trị |
| CY-05 | Ghi log và giám sát an ninh, thời gian lưu log | Không có yêu cầu log |
| CY-06 | Quản lý lỗ hổng, SLA vá lỗi | Không có thời hạn vá lỗ hổng nghiêm trọng |
| CY-07 | Kiểm thử bảo mật (pentest, đánh giá định kỳ) | Không có |
| CY-08 | **Thời hạn thông báo sự cố** (24/48/72h) | "notify promptly", không có con số cụ thể |
| CY-09 | Quy trình ứng phó và leo thang sự cố | Không có đầu mối liên lạc và quy trình |
| CY-10 | Sao lưu, khôi phục thảm họa, liên tục kinh doanh (RTO/RPO) | Không cam kết RTO/RPO |
| CY-11 | Chứng chỉ, chuẩn tuân thủ (ISO 27001, SOC 2…) | Không yêu cầu duy trì chứng chỉ |
| CY-12 | Trách nhiệm khi xảy ra sự cố bảo mật | Sự cố bảo mật bị loại trừ khỏi trách nhiệm bồi thường |
| PR-01 | Quyền sở hữu dữ liệu thuộc khách hàng | Không xác định ai sở hữu dữ liệu |
| PR-02 | Giới hạn mục đích xử lý dữ liệu | Nhà cung cấp được dùng dữ liệu cho "mục đích kinh doanh" chung chung |
| PR-03 | Vị trí lưu trữ, chuyển dữ liệu ra nước ngoài | Không quy định |
| PR-04 | Thời hạn lưu trữ dữ liệu | Không giới hạn |
| PR-05 | Xóa hoặc trả dữ liệu và thu hồi truy cập khi chấm dứt, có xác nhận xóa | "Supplier may retain customer data after termination" |
| TP-01 | Kiểm soát nhà thầu phụ (phê duyệt hoặc thông báo trước) | "Vendor may engage subcontractors at its discretion" |
| TP-02 | Nghĩa vụ bảo mật áp xuống nhà thầu phụ (flow-down) | Không có |
| TP-03 | Quyền kiểm toán bảo mật của khách hàng | Không có quyền audit |

Bộ check được xây dựa trên NIST CSF 2.0 (nhóm GV.SC: Cybersecurity Supply Chain Risk Management), NIST SP 800-161r1 (C-SCRM) và ISO/IEC 27001:2022 Annex A 5.19–5.23 (Supplier relationships).

### 7.4 Thang mức độ nghiêm trọng (Severity)

| Severity | Màu | Ý nghĩa | Hành động gợi ý |
|---|---|---|---|
| **Critical** | Đỏ đậm | Có thể gây thiệt hại nghiêm trọng hoặc không giới hạn, hoặc mất kiểm soát dữ liệu nhạy cảm. | Không ký khi chưa đàm phán lại |
| **High** | Đỏ | Bất lợi rõ rệt cho bên mình. | Đàm phán trước khi ký |
| **Medium** | Vàng | Chưa tối ưu, có thể chấp nhận nếu có lý do. | Nên cải thiện |
| **Low** | Xanh | Thông lệ hoặc chấp nhận được. | Ghi nhận |

Severity **phụ thuộc ngữ cảnh**. Cùng là "không có thời hạn thông báo sự cố", với nhà cung cấp bàn ghế văn phòng thì có thể chỉ Low. Với nhà cung cấp hệ thống lương trên cloud xử lý dữ liệu cá nhân của nhân viên thì phải là Critical.

### 7.5 Cấu trúc một finding

| Trường | Mô tả | Ví dụ |
|---|---|---|
| ID | Mã finding | `CY-08` hoặc `F-0017` |
| Tiêu đề | Một câu ngắn | No defined security incident notification period |
| Loại | `Điều khoản có vấn đề` hoặc `Thiếu điều khoản` | Điều khoản có vấn đề |
| Nhóm chính | Đúng một nhóm, dùng để tính điểm (quy tắc 1) | Cybersecurity |
| Nhóm phụ | Không hoặc nhiều nhóm, chỉ để lọc | Compliance |
| Danh mục | Sub-category trong nhóm | Incident Response |
| Điều khoản | Mã clause, trích nguyên văn, vị trí (trang, tọa độ) | Điều 17.2: "The Provider shall notify the Customer of material security incidents." |
| Severity | Low / Medium / High / Critical | High |
| Why | Vì sao đây là rủi ro | Hợp đồng không quy định nhà cung cấp phải thông báo trong bao lâu. |
| Impact | Tác động tiềm ẩn | Thông báo muộn làm chậm ứng phó, khoanh vùng sự cố và đánh giá nghĩa vụ pháp lý. |
| Recommendation | Hướng xử lý, diễn đạt bằng lời, không phải câu chữ thay thế | Yêu cầu đối tác quy định thời hạn thông báo cụ thể (ví dụ 24h) và quy trình leo thang. |
| Căn cứ tham khảo | Điều luật hoặc chuẩn liên quan, nếu có | ISO/IEC 27001:2022 A.5.20 |
| Trạng thái xử lý | Open / Chấp nhận / Cần đàm phán / Không đúng / Đã xử lý | Open |
| Truy vết | Phiên bản rule, prompt, model, schema đã tạo ra finding | (nội bộ) |

### 7.6 Nguyên tắc chấm điểm

- **Thang điểm:** 0–100, điểm càng cao thì rủi ro càng lớn. Có điểm cho từng nhóm và điểm tổng.
- **Mỗi finding chỉ được tính vào nhóm chính của nó.** Nhóm phụ không cộng điểm.
- **Nhóm N/A** không có điểm và không tham gia điểm tổng. Trọng số của nhóm đó được chia lại cho các nhóm đang áp dụng.
- **Không dùng trung bình cộng.** Điểm tính theo trọng số của severity, mức tác động, khả năng xảy ra và tầm quan trọng của loại điều khoản.
- **Hệ số ngữ cảnh:** loại hợp đồng, mức quan trọng của nhà cung cấp và dịch vụ, độ nhạy cảm của dữ liệu (lấy từ CON-02). Hệ số này có thể nâng hoặc hạ trọng số của từng nhóm, ví dụ dịch vụ cloud xử lý dữ liệu cá nhân thì nhóm Cyber và Privacy nặng hơn.
- **Finding Critical đặt điểm sàn.** Một finding Critical không bị "pha loãng" bởi nhiều điều khoản tốt.
- **Ngưỡng mức rủi ro (đề xuất ban đầu, sẽ hiệu chỉnh khi đánh giá):**

| Điểm | Mức |
|---|---|
| 0–29 | Low |
| 30–54 | Medium |
| 55–84 | High |
| 85–100 | Critical |

- Trọng số và công thức được khai báo trong file cấu hình có version, không hard-code. Công thức cụ thể được chốt ở giai đoạn System Design và hiệu chỉnh bằng bộ dữ liệu đánh giá.

### 7.7 Cơ sở tri thức (Knowledge Base)

MVP dùng một **bộ nhỏ chọn lọc** gồm vài chục điều luật và điều khoản chuẩn. Bộ này chỉ dùng làm căn cứ tham khảo cho finding, không dùng để kết luận "vi phạm".

| Nguồn | Nội dung dùng tới (ứng viên) |
|---|---|
| Luật Thương mại 2005 | Phạt vi phạm và mức trần 8% (Điều 300–301), bồi thường thiệt hại (Điều 302) |
| Bộ luật Dân sự 2015 | Thỏa thuận phạt vi phạm (Điều 418), đơn phương chấm dứt hợp đồng (Điều 428) |
| Pháp luật bảo vệ dữ liệu cá nhân | Luật Bảo vệ dữ liệu cá nhân 2025, Nghị định 13/2023/NĐ-CP |
| Chuẩn quốc tế | NIST CSF 2.0, NIST SP 800-161r1, ISO/IEC 27001:2022 Annex A |
| Bộ rule rủi ro nội bộ | Risk matrix và bộ 20 check do nhóm biên soạn |

> ⚠️ Nhóm cần xác minh lại số điều và hiệu lực của từng văn bản tại thời điểm đưa vào hệ thống. Danh sách trên là đề xuất tham khảo, chưa phải bộ chốt.

---

## 8. Yêu cầu phi chức năng

Các con số dưới đây là **đề xuất ban đầu**, sẽ được kiểm chứng ở giai đoạn System Design và đánh giá.

| Nhóm | Yêu cầu |
|---|---|
| **Hiệu năng** | Phân tích ≤ 45 giây (p50) với hợp đồng có lớp text ≤ 20 trang. Hợp đồng scan ≤ 50 trang: ≤ 3 phút. Chat: chữ đầu tiên xuất hiện ≤ 3 giây (streaming). |
| **Chất lượng AI** | Trên bộ dữ liệu có nhãn: recall của finding High/Critical ≥ 85%, precision ≥ 70%, F1 phân loại điều khoản ≥ 0,85. Ưu tiên không bỏ sót hơn là báo nhầm. |
| **Bảo mật** | Mã hóa at-rest và in-transit. Cách ly dữ liệu giữa các tổ chức ở mọi tầng: database, file storage, vector store, chat. Phân quyền kiểm tra ở phía server. Quyền hạ tầng tối thiểu. Có audit log. |
| **An toàn AI** | Chống prompt injection. Không lộ dữ liệu giữa các tenant qua RAG. Không dùng dữ liệu khách hàng để huấn luyện model. |
| **Riêng tư** | Xóa hợp đồng là xóa toàn bộ dữ liệu liên quan. Dữ liệu chỉ được xử lý trong phạm vi cung cấp dịch vụ. |
| **Truy vết** | Mỗi kết quả lưu phiên bản rule, prompt, model và schema. Mỗi lần phân tích có một correlation ID xuyên suốt hệ thống. |
| **Độ tin cậy** | Phân tích lỗi thì tự thử lại. Lỗi hẳn thì báo người dùng kèm lý do, không lưu kết quả dở dang. |
| **Khả dụng** | MVP chưa yêu cầu high availability. Chấp nhận downtime khi bảo trì. |
| **Chi phí** | Đo chi phí LLM trên mỗi hợp đồng ngay từ đầu, đặt ngân sách sau khi có baseline. |
| **Khả dụng (usability)** | Giao diện Việt/Anh. Ưu tiên desktop, xem được trên tablet. |

---

## 9. Yêu cầu bảo mật của nền tảng

### 9.1 Luồng upload an toàn

```
Upload → Kiểm tra loại file thật (magic bytes) và kích thước
       → Quét mã độc
       → Làm sạch tài liệu (bỏ macro, embedded object, script)
       → Trích xuất nội dung
       → Phát hiện prompt injection
       → Đưa vào AI (nội dung hợp đồng luôn được đánh dấu là dữ liệu, không phải chỉ dẫn)
```

### 9.2 Threat model (tóm tắt, chi tiết là deliverable riêng)

- **Tác nhân đe dọa:** người dùng ác ý, tài khoản bị chiếm, đối tác gửi tài liệu độc hại, kẻ tấn công bên ngoài, người trong nội bộ.
- **Bề mặt tấn công:** upload, API, xác thực và phân quyền, LLM, vector store, database, file xuất báo cáo, chức năng chia sẻ, trang quản trị.
- **Rủi ro riêng của AI:** prompt injection (ví dụ hợp đồng chứa câu "bỏ qua các điều khoản trách nhiệm"), lộ thông tin nhạy cảm, rò rỉ dữ liệu chéo tenant, truy xuất trái phép qua RAG, đầu độc cơ sở tri thức (RAG poisoning), cấp quyền quá mức cho AI.
- **Định dạng phân tích:** Threat → Attack vector → Asset → Impact → Security control.

### 9.3 Kiểm thử bảo mật

Kiểm thử cách ly tenant, kiểm thử phân quyền chia sẻ, bộ test prompt injection, quét lỗ hổng ứng dụng và container.

---

## 10. Kịch bản demo

**Bối cảnh:** Công ty A (khách hàng) chuẩn bị ký hợp đồng sử dụng dịch vụ cloud của nhà cung cấp B. Hợp đồng này có đủ cả 7 nhóm rủi ro.

1. Người dùng upload `VendorCloud_ServiceAgreement.pdf` và khai báo: bên mình là khách hàng, dịch vụ xử lý dữ liệu cá nhân của nhân viên.
2. Hệ thống phát hiện 24 điều khoản và phân loại chúng.
3. Kết quả: 8 Low, 6 Medium, 7 High, 3 Critical. **Điểm tổng 79/100 (High).**
4. Các finding Critical: trách nhiệm bồi thường vô hạn, không có thời hạn thông báo sự cố bảo mật, nhà cung cấp được giữ dữ liệu sau khi chấm dứt hợp đồng.
5. Checklist Cyber: Encryption PASS, MFA MISSING, Incident notification PARTIAL, Audit rights MISSING, Subprocessors PARTIAL, Data deletion MISSING…
6. Người dùng hỏi trợ lý: *"Nếu B bị lộ dữ liệu thì họ phải báo cho mình trong bao lâu?"*. Trợ lý trả lời rằng hợp đồng không quy định thời hạn, trích Điều 17.2, giải thích tác động và gợi ý nên yêu cầu đối tác bổ sung thời hạn cụ thể.
7. Khuyến nghị tổng: *"Không nên duyệt khi chưa xử lý 10 finding High/Critical."*
8. Xuất báo cáo DOCX để gửi đối tác đàm phán.
9. (Mở rộng) Upload V2 sau đàm phán và xem phần so sánh với V1.

> Lưu ý: các con số trên là kịch bản minh họa. Kết quả thật phụ thuộc hợp đồng mẫu mà nhóm soạn.

---

## 11. Lộ trình

| Giai đoạn | Nội dung | Đầu ra |
|---|---|---|
| **P0 — Nội dung nghiệp vụ** | Chốt clause taxonomy, risk taxonomy, 20 check, risk matrix, bộ căn cứ pháp lý. Soạn và gán nhãn bộ hợp đồng mẫu. | Taxonomy, rule ban đầu, dataset có nhãn |
| **P1 — System Design** | Kiến trúc, mô hình dữ liệu, API, schema dữ liệu trích xuất, threat model. | `02-system-design.md` |
| **P2 — Nền tảng** | Tài khoản, tổ chức, phân quyền, upload an toàn, thư viện hợp đồng. | Upload và quản lý hợp đồng chạy được |
| **P3 — Đọc hiểu tài liệu** | Parse, OCR, tách và phân loại điều khoản, trích xuất metadata. | Hợp đồng chuyển thành dữ liệu có cấu trúc |
| **P4 — Risk Engine** | Đánh giá 7 nhóm rủi ro, 20 check cyber, phát hiện thiếu điều khoản, căn cứ tham khảo. | Findings |
| **P5 — Chấm điểm và giải thích** | Chấm điểm theo ngữ cảnh, tổng hợp, khuyến nghị. | Risk profile |
| **P6 — Giao diện** | Dashboard, document viewer, trợ lý hội thoại. | Luồng end-to-end trên UI |
| **P7 — Xuất báo cáo** | PDF, DOCX. | Báo cáo |
| **P8 — Đánh giá và hardening** | Đo chất lượng AI, chi phí, kiểm thử bảo mật, test end-to-end với kịch bản demo. | Báo cáo đánh giá |
| **P9 — Mở rộng** | Compare, Policy, Framework mapping, Redline, auto re-run. | Theo thời gian còn lại |

---

## 12. Ngoài phạm vi và tầm nhìn

**Ngoài phạm vi MVP:** chỉnh sửa hoặc viết lại nội dung hợp đồng (đề xuất câu chữ, redline), ký điện tử, soạn hợp đồng từ đầu, tư vấn pháp lý có giá trị pháp lý, tích hợp ERP/CLM, ứng dụng mobile native, thanh toán và gói cước.

**Tầm nhìn dài hạn:**

```
Phân tích hợp đồng → So sánh hợp đồng → Đánh giá rủi ro nhà cung cấp
→ Cybersecurity due diligence → Giám sát nhà cung cấp liên tục
→ Giám sát tuân thủ hợp đồng → Trợ lý đàm phán → Nền tảng quản lý rủi ro bên thứ ba
```

---

## 13. Thuật ngữ

| Thuật ngữ | Nghĩa |
|---|---|
| Clause | Một điều khoản (điều/khoản) trong hợp đồng |
| Clause segmentation | Tách văn bản hợp đồng thành từng điều khoản |
| Finding | Một phát hiện rủi ro, gắn với một điều khoản hoặc một điều khoản bị thiếu |
| Risk Domain | Nhóm rủi ro (7 nhóm) |
| Nhóm chính (primary domain) | Nhóm duy nhất mà một finding được tính điểm vào |
| N/A (Không áp dụng) | Nhóm rủi ro không liên quan tới hợp đồng đang xét, không tính vào điểm tổng |
| Severity | Mức độ nghiêm trọng: Low / Medium / High / Critical |
| Tenant / Tổ chức | Một doanh nghiệp khách hàng, đơn vị cách ly dữ liệu |
| RAG | Retrieval-Augmented Generation: tra cứu tài liệu liên quan rồi đưa vào LLM để trả lời có căn cứ |
| Prompt injection | Văn bản cố tình chứa câu lệnh nhằm điều khiển LLM làm sai nhiệm vụ |
| Subprocessor / Fourth-party | Bên thứ ba mà nhà cung cấp thuê lại để xử lý dữ liệu của mình |
| Playbook / Policy | Bộ tiêu chuẩn nội bộ doanh nghiệp đặt ra cho hợp đồng |
| Redline | Bản hợp đồng có đánh dấu sửa đổi (track changes) |

---

## Phụ lục A — Các mâu thuẫn đã giải quyết

| # | Vấn đề | Các tài liệu nói gì | Quyết định |
|---|---|---|---|
| 1 | Tên sản phẩm | ContractGuard (đa số) và LegalShield AI (`Định hướng.md`) | **ContractGuard** |
| 2 | Khách hàng mục tiêu | Enterprise vendor-risk, bám chuẩn NIST (`Project.md`) và SME Việt Nam với luật Việt Nam (`Định hướng.md`) | **Kết hợp:** SME Việt Nam, hợp đồng Việt/Anh, giữ 7 nhóm rủi ro, làm sâu Cyber/Privacy/Third-party |
| 3 | Cách đưa ra kết luận pháp lý | "Không tuyên bố trái luật" (`Project.md`) và "highlight điều khoản vi phạm luật" (`Định hướng.md`) | **"Rủi ro tiềm ẩn cần xem xét" kèm căn cứ tham khảo.** Không kết luận vi phạm. |
| 4 | Cơ sở dữ liệu luật Việt Nam | Không nhắc tới, và có Hybrid RAG luật Việt Nam | **Bộ nhỏ chọn lọc** trong MVP |
| 5 | Số nhóm rủi ro | 7 nhóm (đa số) và 6 nhóm (sơ đồ v2 gộp Privacy vào Bảo mật) | **7 nhóm**, Privacy tách riêng |
| 6 | Thang severity | Low/Medium/High/Critical, Low/Medium/High (sơ đồ v2), và cờ đỏ/vàng/xanh | **4 mức**, có bảng màu tương ứng |
| 7 | Hình thức tương tác chính | Dashboard (đa số) và chat với AI (sơ đồ v2) | **Workspace hợp đồng** = dashboard + viewer + trợ lý hội thoại. Không phải chatbot thuần. |
| 8 | Xuất file | Báo cáo PDF/Word là "làm sau" và Redline DOCX là cốt lõi | **Báo cáo PDF + DOCX trong MVP.** Redline để sau. |
| 9 | Góc nhìn đánh giá rủi ro | Không tài liệu nào nói rõ | **Bổ sung:** người dùng khai báo bên mình là bên nào (CON-02) |
| 10 | Phân quyền | "Giống Google Workspace, cấp qua email" | Hai cấp (tổ chức và hợp đồng), mỗi cấp chỉ có **Owner và Member**. Chia sẻ ngoài tổ chức để sau. |
| 11 | Phân công thành viên | Ba cách chia khác nhau | Bỏ ra khỏi tài liệu này theo yêu cầu |
| 12 | Hệ thống có sửa hợp đồng không | Redline, đề xuất câu chữ (`Định hướng.md`) | **MVP chỉ giải thích.** Đề xuất câu chữ và redline để sau (EXT-05, EXT-07). |
| 13 | Dạng trợ lý | "Chat với AI về hợp đồng" (sơ đồ v2) | Chatbot nhắn tin văn bản, không có voice |
| 14 | 7 nhóm chồng lấn nhau | Các tài liệu liệt kê 7 nhóm nhưng không nói cách xử lý khi một vấn đề thuộc nhiều nhóm | Giữ 7 nhóm, thêm 3 quy tắc: một nhóm chính cho mỗi finding, trạng thái N/A, quy ước ranh giới ([mục 7.1](#71-bảy-nhóm-rủi-ro-risk-domains)). Tách C20 thành C20 (Audit Rights) và C21 (Regulatory, Tax & Invoicing). |

## Phụ lục B — Câu hỏi kiến trúc chuyển sang System Design

Các tài liệu trong `Idea/` đã đề xuất công nghệ nhưng chưa thống nhất với nhau. Những điểm dưới đây sẽ được quyết định ở giai đoạn System Design, mỗi điểm kèm phân tích trade-off.

| # | Câu hỏi | Các phương án đang có |
|---|---|---|
| 1 | Ước lượng quy mô (số tổ chức, số hợp đồng/tháng, số trang, token) | Chưa có, cần làm back-of-the-envelope trước tiên |
| 2 | Compute | AWS Lambda (`Tổng quan.md`) hay ECS Fargate (Tech stack PDF) |
| 3 | Database | DynamoDB hay Aurora Serverless (`Tổng quan.md`), hay RDS PostgreSQL (Tech stack) |
| 4 | Vector store | Bedrock Knowledge Bases / OpenSearch Serverless, hay pgvector, hay Qdrant |
| 5 | Chấm điểm nằm ở đâu | Trong AI worker, hay rule engine C# (`Phạm vi công việc.md`, Tech stack), hay cả hai (sơ đồ v2) |
| 6 | Điều phối AI | Pipeline tuần tự, hay multi-agent LangGraph (sơ đồ v2) |
| 7 | Cập nhật trạng thái realtime | Polling `GET /jobs/{id}` (Tech stack), hay SignalR/WebSocket (sơ đồ v2). Chat streaming cũng cần quyết. |
| 8 | Cache | Có cần Redis không (sơ đồ v2) |
| 9 | Kiến trúc trợ lý hội thoại | RAG trên clause của hợp đồng, lưu lịch sử, streaming, chống injection |
| 10 | Sinh báo cáo PDF/DOCX | Đồng bộ hay bất đồng bộ, đặt ở service nào |
| 11 | Quét mã độc | ClamAV hay GuardDuty Malware Protection for S3 |
| 12 | Mạng | Không dùng NAT Gateway, task ở public subnet (Tech stack): trade-off giữa chi phí và bảo mật |
| 13 | Model | Tài liệu cũ ghi Claude 3 / Nova. Cần chọn lại model hiện có trên Bedrock theo từng tác vụ (phân loại, trích xuất, giải thích, chat). |
| 14 | Hai ngôn ngữ lập trình | C# cho service và Python cho worker: lợi ích có đủ lớn so với chi phí vận hành hai stack không |

## Phụ lục C — Câu hỏi sản phẩm còn mở (đã đặt mặc định)

| Câu hỏi | Mặc định trong tài liệu này |
|---|---|
| Owner tổ chức có tự động xem được mọi hợp đồng không? | Không. Giống Google Drive, phải được chia sẻ. |
| Member của hợp đồng có được upload phiên bản mới và đổi trạng thái không? | Có. Chỉ chia sẻ và xóa là dành riêng cho Owner. || Thời gian lưu hợp đồng | Đến khi người dùng xóa |
| Một người có thuộc nhiều tổ chức không? | Không trong MVP |
