# **TỔNG QUAN DỰ ÁN (PROJECT SPECIFICATION)**

##  **TÊN DỰ ÁN: ContractGuard**

**AI-Powered B2B Contract Risk Intelligence Platform**

*(Nền tảng phân tích và đánh giá rủi ro hợp đồng B2B ứng dụng Trí tuệ nhân tạo và Bảo mật)*

##  **I. TẬP TRUNG CỐT LÕI (CORE MVP \- BẮT BUỘC HOÀN THÀNH)**

### **1\. Luồng xử lý chính (Core Pipeline)**

* **Upload Gateway:** Nhận file `PDF/DOCX` từ người dùng.  
* **Document Processing & Parsing:** Trích xuất văn bản, nhận diện cấu trúc hợp đồng và chia nhỏ thành từng điều khoản (`Clause Segmentation`).  
* **Risk Engine Analysis:** Chạy phân tích rủi ro song song trên 7 nhóm rủi ro (`Risk Domains`):  
  * **Legal Risk:** Hạn chế trách nhiệm, bồi thường, cơ quan tài phán.  
  * **Financial Risk:** Điều khoản thanh toán, chi phí ẩn, phạt vi phạm.  
  * **Operational Risk:** SLA, cam kết giao hàng, bảo hành.  
  * **Compliance Risk:** Báo cáo, kiểm toán, tuân thủ pháp lý.  
  * **Cybersecurity Risk\* (Rất sâu):** Mã hóa, MFA, thời hạn thông báo sự cố (Incident Notification).  
  * **Data & Privacy Risk\* (Rất sâu):** Quyền sở hữu dữ liệu, lưu trữ và xóa dữ liệu sau chấm dứt hợp đồng.  
  * **Third-Party Risk\* (Rất sâu):** Quản lý nhà thầu phụ (Subcontractor/Fourth-party risk).  
* **Risk Scoring & Aggregation:** Tổng hợp và tính điểm rủi ro cho từng điều khoản và điểm tổng quát cho cả hợp đồng (thang điểm 0–100).  
* **Risk Dashboard & Detailed Findings:** Hiển thị chi tiết từng điều khoản bị phát hiện rủi ro kèm giải thích (`Why`), mức độ nghiêm trọng (`Severity`), và đề xuất chỉnh sửa (`Recommendation`).

### **2\. Kiến trúc Hạ tầng AWS (AWS Console Supported Infrastructure)**

Nhóm tận dụng các dịch vụ của AWS để xây dựng hệ thống:

* **Storage:** AWS S3 (Lưu trữ hợp đồng gốc và bản lưu tĩnh).  
* **Processing / OCR / Document Intelligence:** AWS Textract / Amazon Bedrock (sử dụng các dòng model như Claude 3 / Nova) để trích xuất và phân tích cấu trúc điều khoản.  
* **Compute / Backend:** AWS Lambda (Serverless Architecture) hoặc Amazon ECS/Fargate để điều phối luồng xử lý.  
* **Database:** Amazon DynamoDB hoặc Amazon Aurora Serverless (Quản lý metadata, kết quả phân tích).  
* **AI Vector Search:** Amazon Bedrock Knowledge Bases / Amazon OpenSearch Serverless để truy vấn tri thức rủi ro.

##  **II. PHÂN CÔNG VAI TRÒ TRONG NHÓM (5 THÀNH VIÊN)**

### **1\. SE (Software Engineer) – *Full Stack & Integration***

* **Nhiệm vụ chính (Cốt lõi):**  
  * Xây dựng Giao diện Web chính: Upload file, Xem tài liệu (Document Viewer), Bảng điều khiển rủi ro (Risk Dashboard).  
  * Thiết kế Backend APIs kết nối giao diện với các AWS Cloud Services & AI Engines.  
  * Tích hợp Authentication / Authorization.  
*  *Làm sau nếu còn thời gian:*  
  * Xuất Báo cáo kết quả ra file PDF/Word.  
  * Giao diện so sánh 2 phiên bản hợp đồng (`Compare Mode UI`).

### **2\. AI Member 1 – *Contract Understanding Engineer***

* **Nhiệm vụ chính (Cốt lõi):**  
  * Làm chủ **Document Intelligence Engine**: Xử lý PDF/DOCX, trích xuất văn bản và cấu trúc hóa điều khoản (`Clause Segmentation & Classification`).  
  * Trích xuất thông tin thực thể cơ bản (`Entity Extraction`): Các bên tham gia, Loại hợp đồng, Giá trị, Ngày hiệu lực.  
  * Đầu ra: Trả về file `Structured JSON` chuẩn xác cho AI 2 & AI 3\.  
*  *Làm sau nếu còn thời gian:*  
  * Tối ưu hóa xử lý các hợp đồng định dạng bảng phức tạp hoặc scan kém chất lượng.

### **3\. AI Member 2 – *Contract Risk Intelligence Engineer***

* **Nhiệm vụ chính (Cốt lõi):**  
  * Đánh giá rủi ro chi tiết từng điều khoản đối với 4 nhóm rủi ro: **Legal, Financial, Operational, Compliance**.  
  * Áp dụng Prompt Engineering / Bedrock RAG để đối chiếu điều khoản với bộ quy chuẩn rủi ro.  
  * Tạo câu giải thích (`Why`), Đánh giá rủi ro (`Severity`) và Đề xuất giảm thiểu (`Recommendation`).

### **4\. AI Member 3 – *Risk Scoring & Context Intelligence Engineer***

* **Nhiệm vụ chính (Cốt lõi):**  
  * Xây dựng Thuật toán Chấm điểm Rủi ro (`Risk Scoring Engine`): Quy đổi toàn bộ Findings thành thang điểm rủi ro hợp lý (không dùng trung bình cộng đơn thuần mà tính theo trọng số mức độ ảnh hưởng).  
  * Phối hợp với AI 2 để tạo bản tóm tắt rủi ro tổng thể cho toàn bộ hợp đồng.  
*  *Làm sau nếu còn thời gian:*  
  * **Context-Aware Engine:** Tính toán trọng số rủi ro dựa trên bối cảnh doanh nghiệp (VD: Vendor bán phần mềm Cloud có rủi ro Cyber cao hơn Vendor bán bàn ghế).  
  * **Compare Mode (AI Logic):** Phân tích sự chênh lệch giữa hai phiên bản hợp đồng (Version 1 vs Version 2).  
  * **Company Policy Engine:** So sánh điều khoản hợp đồng với Chính sách riêng của công ty.

### **5\. ATTT (Cybersecurity & Third-Party Risk Engineer)**

* **Nhiệm vụ chính (Cốt lõi):**  
  * **Đóng góp Nội dung AI (50%):** Xây dựng bộ quy chuẩn kiểm tra (`Cybersecurity Clause Taxonomy`) cho nhóm **Cybersecurity, Privacy, Third-Party Risk** (Ví dụ: kiểm tra SLA báo sự cố, mã hóa, quyền kiểm toán, nhà thầu phụ).  
  * **Bảo mật Nền tảng (50%):**  
    * Thiết kế luồng Secure Upload (Kiểm tra loại file, chống Prompt Injection đầu vào).  
    * Đảm bảo tính riêng tư dữ liệu đa khách hàng (`Multi-tenant Security`) trong AWS/Bedrock.  
*  *Làm sau nếu còn thời gian:*  
  * Lập Báo cáo Mô hình mối đe dọa (`Threat Modeling Document`) chi tiết cho bài báo cáo đồ án.  
  * Đánh giá lỗ hổng bảo mật nền tảng (Pentest).

##  **III. CHỨC NĂNG BỔ SUNG (LÀM SAU CÙNG NẾU CÒN THỜI GIAN)**

Các chức năng dưới đây rất hay để làm nổi bật sản phẩm nhưng **chỉ triển khai sau khi Core MVP đã chạy mượt mà**:

1. **Chức năng So sánh Hợp đồng (Contract Comparison Mode):** Cho phép upload V1 và V2 để highlight điểm yếu đi của hợp đồng (VD: Thời gian thông báo sự cố bị đổi từ `24h` thành `promptly`).  
2. **Chức năng Đánh giá theo Policy Công ty (Company Policy Engine):** Cho phép doanh nghiệp upload Policy nội bộ để AI tự động kiểm tra xem hợp đồng của Vendor có tuân thủ Policy đó hay không.  
3. **Map Policy vào Chuẩn Quốc tế (Framework Mapping):** Ánh xạ các điều khoản bị vi phạm sang các khung chuẩn như NIST CSF 2.0, ISO 27001, SOC 2\.  
4. **Xuất báo cáo tự động (Report Export):** Đóng gói toàn bộ kết quả phân tích thành file báo cáo định dạng PDF/Word chuyên nghiệp.

##  **IV. LỘ TRÌNH THỰC HIỆN TỐI GIẢN (REFINED ROADMAP)**

* **Phase 1: Chuẩn bị Taxonomy & Kiến trúc AWS Console** *(SE \+ AI2 \+ ATTT)*  
  * Khởi tạo tài nguyên trên AWS (S3, Bedrock, DynamoDB).  
  * Định nghĩa cấu trúc JSON đầu ra & Bộ quy chuẩn kiểm tra Cyber Risk.  
* **Phase 2: Xử lý Đọc & Phân loại văn bản (Parsing Engine)** *(AI 1\)*  
  * Xây dựng pipeline đọc PDF/DOCX và phân đoạn điều khoản (`Clause Segmentation`).  
* **Phase 3: Xây dựng Risk Engine & Cybersecurity Engine** *(AI 2 \+ AI 3 \+ ATTT)*  
  * Phát triển các Prompts/Rules đánh giá rủi ro cho 7 domains.  
  * Lập công thức tính điểm rủi ro tổng hợp.  
* **Phase 4: Hoàn thiện Giao diện & Tích hợp Hệ thống** *(SE \+ ATTT)*  
  * Dựng UI Risk Dashboard & Viewer.  
  * Kiểm thử luồng Secure Upload & Kiểm thử toàn bộ hệ thống (End-to-End Test) với kịch bản hợp đồng Cloud Service Agreement mẫu.  
* **Phase 5 (Nếu dư thời gian):** Phát triển Compare Mode / Policy Engine.

