# Kế hoạch dọn dẹp sau demo

Bản demo frontend dùng dữ liệu giả và giả lập backend. Tài liệu này liệt kê những gì phải xóa hoặc thay thế khi nối với service C# và AI worker thật, để không có code demo nào lọt vào sản phẩm.

## Cách tìm nhanh

Mọi chỗ làm tắt đều có comment bắt đầu bằng `shortcut:`. Liệt kê bằng một trong hai cách:

```bash
grep -rn "shortcut:" frontend/src
```

hoặc chạy lệnh `/ponytail-debt` trong Claude Code. **Dọn xong khi lệnh này không còn trả về kết quả nào.**

## 1. Xóa hẳn

| Mục | Vị trí | Ghi chú |
|---|---|---|
| Dữ liệu giả (hợp đồng, finding, thành viên, câu trả lời chat soạn sẵn) | `src/demo/mock.ts` | Xóa cả file. Kiểu dữ liệu trong `src/model.ts` được giữ lại. |
| Store trong bộ nhớ | `src/demo/store.tsx` | Xóa cả file, thay bằng lớp gọi API (xem mục 2). |
| Thư mục `src/demo/` | | Sau hai file trên, thư mục phải rỗng thì xóa luôn. |
| Link "Xem màn hình nhận lời mời" | `src/pages/Login.tsx` | Chỉ để nhóm xem màn hình lời mời trong demo. |
| Banner "kết quả lấy từ hợp đồng mẫu" | `src/pages/workspace/Workspace.tsx` | Cùng điều kiện `id.startsWith('hd-moi-')`. |
| Màn "Bản demo chưa có văn bản của hợp đồng này" | `src/pages/workspace/Workspace.tsx` (biến `noDetail`) | Khi có API, mọi hợp đồng đều có văn bản. Thay bằng skeleton lúc đang tải. |
| Ghi chú "Bản demo chỉ trả lời được các câu hỏi soạn sẵn" | `src/pages/workspace/ChatPanel.tsx` | Xóa cùng hàm `answerFor` và trường `demoNote`. |
| Ảnh chụp kiểm thử | `.playwright-mcp/` ở gốc repo | Đã được ignore, xóa khi không cần. |

## 2. Thay bằng hành vi thật

| Hành vi demo | Vị trí | Thay bằng |
|---|---|---|
| Đăng nhập không kiểm tra, chuyển thẳng vào app | `Login.tsx` | Amazon Cognito (OIDC), lưu phiên, chặn route khi chưa đăng nhập |
| Tự điền metadata sau 1,2 giây với giá trị cố định | `NewContract.tsx` (`pick`) | Xin presigned URL, upload thẳng lên S3, nhận metadata từ kết quả trích xuất |
| Tiến trình phân tích chạy theo đồng hồ | `workspace/AnalysisProgress.tsx`, `ANALYSIS_STEPS` trong store | Poll `GET /jobs/{id}` (hoặc WebSocket), map `queued → parsing → extracting → scoring → done/failed` vào các bước đang hiển thị |
| Chat khớp từ khóa với câu trả lời soạn sẵn | `workspace/ChatPanel.tsx` (`answerFor`, interval hiện chữ dần) | API chat streaming; giữ nguyên cú pháp trích dẫn `[[9.1]]` hoặc thống nhất định dạng citation với nhóm AI |
| Xuất Word bằng HTML đổi đuôi `.doc` | `Report.tsx` (`download`) | Tải tệp `.docx` do backend sinh. PDF có thể giữ `window.print()` hoặc cũng do backend sinh. |
| Chia sẻ, mời, đổi vai trò, đổi trạng thái chỉ đổi state | `demo/store.tsx` | Gọi API tương ứng. Mọi kiểm tra quyền (Owner/Member) phải làm ở server; kiểm tra trên UI chỉ để ẩn nút. |
| "Gửi lại" lời mời chỉ hiện toast | `Members.tsx` | API gửi lại email mời |
| Hợp đồng SOW "đang phân tích" lúc mở trang | `demo/store.tsx` (`analysisStart`) | Mất theo store |

## 3. Giữ lại (không phải code demo)

- `src/model.ts`: kiểu dữ liệu, nhãn tiếng Việt, ngưỡng điểm. Đối chiếu với JSON Schema chung trong thư mục `/contracts` khi chốt hợp đồng dữ liệu với backend.
- `src/components/`: AppShell, các thành phần UI và rủi ro.
- Toàn bộ `src/pages/`, sau khi đã thay các mục ở bảng 2.
- Token màu và font trong `src/index.css`, `PRODUCT.md` ở gốc repo.

## 4. Trình tự đề xuất

1. Chốt với nhóm AI định dạng `extraction.json` và finding. So với `src/model.ts`, sửa kiểu nếu lệch.
2. Thêm lớp gọi API (ví dụ `src/api/`), và một provider thay `StoreProvider` với cùng tên hàm để các trang ít phải sửa.
3. Thay từng hành vi ở bảng 2, mỗi mục một PR nhỏ.
4. Xóa `src/demo/` và các mục ở bảng 1.
5. Chạy `grep -rn "shortcut:" frontend/src` để chắc không còn mục nào, rồi `npm run build`.
