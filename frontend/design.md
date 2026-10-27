# ContractGuard — Design (frontend)

Ghi lại style của prototype để code thật giữ đúng giao diện. Nguồn sự thật là token trong [src/index.css](src/index.css) và các component trong [src/components/](src/components/); file này chỉ tóm tắt và giải thích lý do. Định hướng sản phẩm ở [../docs/PRODUCT.md](../docs/PRODUCT.md).

## 1. Nguyên tắc

1. **Hợp đồng là nhân vật chính.** Văn bản hợp đồng ở trung tâm; phân tích bám vào văn bản (ghi chú lề, tô sáng).
2. **Không có bằng chứng thì không hiện.** Mọi rủi ro và câu trả lời chat dẫn về điều khoản cụ thể, bấm vào là tới.
3. **Màu không bao giờ đứng một mình.** Severity luôn có ký hiệu hình học và chữ. Màu chủ đạo chỉ dành cho hành động.
4. **Giải thích, không phán xét.** Dùng "rủi ro tiềm ẩn, cần xem xét", không kết luận "trái luật", không dọa người dùng.

Giọng điệu: điềm tĩnh, chính xác, đáng tin. Rủi ro nghiêm trọng phải nổi bật nhưng không la hét.

**Tránh:** donut gauge điểm số, hero metric gradient, lưới card giống hệt nhau, gradient tím xanh, glassmorphism, nền kem/be, eyebrow chữ in hoa giãn chữ, chatbot làm trung tâm màn hình, thẩm mỹ "legal tech" (navy và vàng kim, con dấu, búa).

## 2. Font

| Vai trò | Font | Dùng cho |
|---|---|---|
| Giao diện (`font-sans`) | Be Vietnam Pro 400/500/600/700 | Mọi chữ giao diện |
| Văn bản hợp đồng (`font-serif`) | Source Serif 4 Variable | Nội dung hợp đồng (16px, dòng 1.75) và trích dẫn điều khoản |

Cả hai hỗ trợ đầy đủ dấu tiếng Việt. Văn bản hợp đồng dùng chữ có chân để khác hẳn lớp giao diện.

Cỡ chữ: `body` 14px, dòng 1.5. Thang đang dùng (px, theo số lần xuất hiện): 12.5, 13.5, 13, 12, 15, 14, 11.5, 22, 16, 19. Giữ trong thang này, không thêm cỡ mới. Tiêu đề dùng `text-wrap: balance`, đoạn văn dùng `text-wrap: pretty`.

## 3. Màu

Toàn bộ bằng OKLCH, khai báo trong `@theme` ở `index.css`. Bảng lạnh, hơi ngả xanh lục (hue ~200–225). Chỉ có chế độ sáng (`color-scheme: light`).

**Bề mặt và chữ**

| Token | Giá trị | Dùng cho |
|---|---|---|
| `canvas` | `oklch(0.958 0.006 200)` | Nền trang (mặt bàn) |
| `paper` | `oklch(1 0 0)` | Trang hợp đồng, panel, control; trắng như giấy |
| `sidebar` | `oklch(0.937 0.008 200)` | Thanh bên |
| `line` | `oklch(0.89 0.008 200)` | Đường kẻ nhẹ |
| `line-strong` | `oklch(0.8 0.01 205)` | Viền control |
| `ink` | `oklch(0.235 0.014 225)` | Chữ chính |
| `ink-2` | `oklch(0.4 0.014 225)` | Chữ phụ |
| `ink-3` | `oklch(0.505 0.012 225)` | Chữ mờ, placeholder (vẫn ≥ 4.5:1) |

**Màu chủ đạo** (xanh lục lam, chỉ cho hành động): `primary` `oklch(0.45 0.074 200)`, `primary-hover` `0.395`, `primary-ink` `0.36`, `primary-soft` `oklch(0.94 0.026 200)`.

**Severity** (tách hẳn khỏi màu chủ đạo)

| Mức | Màu chữ/nét | Nền nhạt | Ký hiệu |
|---|---|---|---|
| Nghiêm trọng (`critical`) | `oklch(0.43 0.15 22)` | `…0.94 0.035 22` | Hình thoi đặc |
| Cao (`high`) | `oklch(0.52 0.165 40)` | `…0.95 0.035 45` | Tam giác đặc |
| Trung bình (`medium`) | `oklch(0.51 0.1 68)`, nét tô `medium-mark` `…0.66 0.13 75` | `…0.955 0.045 88` | Tròn đặc |
| Thấp (`low`) | `oklch(0.5 0.075 150)` | `…0.955 0.03 150` | Tròn rỗng |

Quy tắc: mỗi mức luôn hiện **ký hiệu + chữ**; người mù màu vẫn phân biệt được. Dùng component `SeverityTag` và `SeverityGlyph` ([risk.tsx](src/components/risk.tsx)), không tự vẽ lại.

## 4. Bố cục và khoảng cách

- **Màn chính (workspace):** sidebar trái (232px, thu gọn 56px) → trang hợp đồng ở giữa → panel phải 384px (rủi ro và hỏi đáp). Panel phải thành ngăn kéo (`min(400px, 100vw)`) ở màn hẹp.
- **Trang hợp đồng:** rộng `--paper-w: 680px`, nền `paper` trên nền `canvas`.
- **Điểm rủi ro:** thước 0–100 (`RiskRuler`) có 4 vùng ngưỡng 0–30, 30–55, 55–85, 85–100, vùng hiện hành tô đặc. Không dùng donut.
- Bố cục ưu tiên laptop 14–15" (văn phòng, ban ngày). Phải dùng được ở màn hẹp, không cuộn ngang.
- **Lớp xếp chồng:** `--z-sticky` 10, `--z-drawer` 20, `--z-popover` 30, `--z-toast` 50. Không dùng z-index tùy ý.

## 5. Component

| Thành phần | Quy ước |
|---|---|
| Nút | Cao 36px (`md`, chữ 13.5px) hoặc 32px (`sm`, 13px); bo `rounded-md`. Biến thể `primary`, `secondary`, `ghost`, `danger`. Dùng `Button` hoặc `buttonClass` |
| Ô nhập, chọn, textarea | Cao 36px, viền `line-strong`; focus đổi viền `primary` kèm vòng `ring-[3px]` 15%; lỗi đổi viền `critical`. Dùng `Field` để gắn label, gợi ý, lỗi qua `aria-describedby` |
| Tab | `TabList`, gạch chân 2px bên dưới tab đang chọn |
| Avatar | `Avatar`, tròn, chữ cái đầu |
| Toast | Nền `ink`, chữ trắng, `rounded-md`, đổ bóng `shadow-lg`, ở lớp `--z-toast` |
| Bóng đổ | Gần như phẳng; chỉ dùng cho toast và ngăn kéo (`shadow-2xl`). Mục đang chọn ở sidebar dùng viền 1px `line` thay cho bóng |
| Trạng thái đang tải | Khung xương `.skeleton` (shimmer), không dùng spinner chặn cả trang |
| Biểu tượng | `lucide-react` |

Quy tắc chung: bo góc nhỏ (`rounded-md`, 4–6px), viền mảnh, không gradient, không blur kính.

## 6. Chuyển động

- Xuất hiện: `.enter`, mờ dần kèm trượt 4px, 200ms, easing `--ease-out-quart`.
- Đổi trạng thái control: 150ms.
- Ngăn kéo trượt vào: 200ms.
- Bắt buộc có `prefers-reduced-motion: reduce`: mọi animation và transition rút còn 1ms.

## 7. Truy cập (WCAG 2.2 AA)

- Tương phản chữ ≥ 4.5:1, kể cả placeholder.
- Focus nhìn thấy: viền 2px `primary`, lệch 2px, ở mọi phần tử (`:focus-visible`).
- Dùng được hoàn toàn bằng bàn phím; có landmark (`aside` có `aria-label`) và label cho mọi control.
- Biểu tượng trang trí có `aria-hidden`.
- Severity không phụ thuộc màu (mục 3).

## 8. Khi thêm giao diện mới

1. Dùng token và component có sẵn trước; chỉ thêm token vào `@theme` khi thật sự cần, và ghi vào đây.
2. Chữ hợp đồng và trích dẫn dùng `font-serif`; mọi thứ còn lại dùng `font-sans`.
3. Rủi ro hiển thị qua `SeverityTag`/`SeverityGlyph`, kèm đường dẫn tới điều khoản.
4. Soát lại với mục "Tránh" ở §1 trước khi gộp.

## 9. Việc cần làm khi chuyển sang React Router framework mode

- `index.css` và các token giữ nguyên; import trong `app/root.tsx`.
- Thêm landing và about (công khai) dùng cùng token; chưa có thiết kế riêng, cần bổ sung vào file này khi làm.
- Chưa có chế độ tối. Nếu cần thì phải định nghĩa lại toàn bộ token, không đảo ngược tự động.
