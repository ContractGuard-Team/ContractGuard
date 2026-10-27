

| Nhóm | Sở hữu | Phạm vi công việc |
| ----- | ----- | ----- |
| SE | Service C\#, hạ tầng AWS, frontend | REST API, auth và multi-tenant, database, engine chạy rule và scoring, nhận kết quả từ worker, so sánh version, đối chiếu policy, CDK và CI/CD, giao diện React |
| AI | Worker Python trọn gói | Parse và tách clause, prompt và trích xuất thuộc tính qua Bedrock, sinh phần giải thích và khuyến nghị, bộ dữ liệu mẫu và đánh giá chất lượng, tối ưu chi phí LLM, đóng gói container của worker |
| IA | Nội dung rủi ro và bảo mật hệ thống | Định nghĩa 15–20 security check, viết nội dung rule (severity, hệ số ngữ cảnh) cho Cybersecurity, Privacy, Third-Party, mẫu policy công ty, và bảo mật của chính Contract Guard |

### **Nhóm AI chia ba hướng**

Ba người nên mỗi người giữ một mảng để không giẫm chân nhau:

1. **Hiểu tài liệu:** parse PDF/DOCX, tách clause, phân loại clause.  
2. **Trích xuất:** prompt, schema đầu ra, gọi model, giải thích và khuyến nghị.  
3. **Đánh giá:** bộ hợp đồng mẫu có nhãn, regression test mỗi lần đổi prompt, đo độ chính xác và chi phí.

### **Vai trò IA có hai nửa**

* **Chuyên gia nghiệp vụ:** đây là người quyết định thế nào là rủi ro về mặt bảo mật. SE xây engine, nhưng IA viết nội dung rule chạy trong engine đó, và gán nhãn phần cybersecurity trong bộ dữ liệu mẫu của nhóm AI.  
* **Bảo mật sản phẩm:** threat model, quyền IAM tối thiểu, mã hóa, kiểm thử cách ly dữ liệu giữa các tenant, và prompt injection. Điểm cuối đáng chú ý riêng: hợp đồng do người dùng upload có thể chứa câu lệnh nhằm đánh lừa LLM bỏ qua rủi ro.

### **Ba điểm các nhóm phải làm chung**

* **Schema `extraction.json`:** SE và AI cùng chốt, IA bổ sung các thuộc tính bảo mật cần trích xuất.  
* **Rule YAML:** IA viết nội dung, SE định nghĩa định dạng và chạy nó.  
* **Bộ dữ liệu mẫu:** AI xây và vận hành, IA gán nhãn phần chuyên môn.

