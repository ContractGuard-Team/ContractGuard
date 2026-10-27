# ContractGuard — Frontend (bản demo)

Prototype bấm được với dữ liệu giả, dùng để nhóm đánh giá layout và luồng sử dụng. Chưa có backend.

```bash
cd frontend
npm install
npm run dev      # http://localhost:5173
npm run build    # kiểm tra kiểu + build
```

## Đi thử

- `/dang-nhap`: nhập email bất kỳ và mật khẩu từ 8 ký tự.
- `/hop-dong`: thư viện. Hợp đồng "dịch vụ điện toán đám mây" và "mua bán nội thất" có văn bản đầy đủ.
- `/hop-dong/hd-cloud`: workspace. Bấm vào điều khoản được tô hoặc ghi chú lề để mở rủi ro; tab Hỏi đáp có câu hỏi gợi ý.
- `/hop-dong/moi`: tải một tệp PDF bất kỳ lên để xem tiến trình phân tích giả lập.
- `/to-chuc`: thành viên và lời mời.

Hướng thiết kế ghi trong `../PRODUCT.md`. Danh sách phần code demo cần xóa hoặc thay sau này nằm ở [DEMO-CLEANUP.md](DEMO-CLEANUP.md).

## Quy ước truy cập dữ liệu

Frontend lấy dữ liệu qua backend API; không truy cập trực tiếp object S3 riêng tư hay database. Bản demo hiện dùng dữ liệu giả trong `src/demo/`.

Style giao diện ghi ở [design.md](design.md).
