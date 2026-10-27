---
name: ContractGuard
description: Trợ lý AI thẩm định rủi ro hợp đồng cho SME Việt Nam; hợp đồng ở trung tâm, phân tích nằm ở lề.
colors:
  review-ink: "oklch(0.45 0.074 200)"
  review-ink-hover: "oklch(0.395 0.072 200)"
  review-ink-pressed: "oklch(0.36 0.07 200)"
  review-ink-wash: "oklch(0.94 0.026 200)"
  desk: "oklch(0.958 0.006 200)"
  sheet: "oklch(1 0 0)"
  rail: "oklch(0.937 0.008 200)"
  rule: "oklch(0.89 0.008 200)"
  rule-strong: "oklch(0.8 0.01 205)"
  ink: "oklch(0.235 0.014 225)"
  ink-secondary: "oklch(0.4 0.014 225)"
  ink-muted: "oklch(0.505 0.012 225)"
  severity-critical: "oklch(0.43 0.15 22)"
  severity-critical-wash: "oklch(0.94 0.035 22)"
  severity-high: "oklch(0.52 0.165 40)"
  severity-high-wash: "oklch(0.95 0.035 45)"
  severity-medium: "oklch(0.51 0.1 68)"
  severity-medium-mark: "oklch(0.66 0.13 75)"
  severity-medium-wash: "oklch(0.955 0.045 88)"
  severity-low: "oklch(0.5 0.075 150)"
  severity-low-wash: "oklch(0.955 0.03 150)"
typography:
  headline:
    fontFamily: "Be Vietnam Pro, ui-sans-serif, system-ui, sans-serif"
    fontSize: "22px"
    fontWeight: 600
    lineHeight: 1.3
  title:
    fontFamily: "Be Vietnam Pro, ui-sans-serif, system-ui, sans-serif"
    fontSize: "16px"
    fontWeight: 600
    lineHeight: 1.4
  body:
    fontFamily: "Be Vietnam Pro, ui-sans-serif, system-ui, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.5
  button:
    fontFamily: "Be Vietnam Pro, ui-sans-serif, system-ui, sans-serif"
    fontSize: "14px"
    fontWeight: 600
    lineHeight: 1
  label:
    fontFamily: "Be Vietnam Pro, ui-sans-serif, system-ui, sans-serif"
    fontSize: "13px"
    fontWeight: 500
    lineHeight: 1.4
  caption:
    fontFamily: "Be Vietnam Pro, ui-sans-serif, system-ui, sans-serif"
    fontSize: "12.5px"
    fontWeight: 400
    lineHeight: 1.45
  contract-body:
    fontFamily: "Source Serif 4 Variable, Georgia, serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.75
  contract-heading:
    fontFamily: "Source Serif 4 Variable, Georgia, serif"
    fontSize: "16px"
    fontWeight: 600
    lineHeight: 1.5
rounded:
  sheet: "3px"
  sm: "4px"
  md: "6px"
  full: "9999px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "12px"
  lg: "16px"
  xl: "24px"
  2xl: "32px"
components:
  button-primary:
    backgroundColor: "{colors.review-ink}"
    textColor: "{colors.sheet}"
    typography: "{typography.button}"
    rounded: "{rounded.md}"
    height: "40px"
    padding: "0 16px"
  button-primary-hover:
    backgroundColor: "{colors.review-ink-hover}"
  button-primary-active:
    backgroundColor: "{colors.review-ink-pressed}"
  button-secondary:
    backgroundColor: "{colors.sheet}"
    textColor: "{colors.ink}"
    typography: "{typography.button}"
    rounded: "{rounded.md}"
    height: "40px"
    padding: "0 16px"
  button-secondary-hover:
    backgroundColor: "{colors.desk}"
  button-ghost:
    textColor: "{colors.ink-secondary}"
    typography: "{typography.button}"
    rounded: "{rounded.md}"
    height: "40px"
    padding: "0 12px"
  button-danger:
    textColor: "{colors.severity-critical}"
    typography: "{typography.button}"
    rounded: "{rounded.md}"
    height: "40px"
    padding: "0 12px"
  button-danger-hover:
    backgroundColor: "{colors.severity-critical-wash}"
  input:
    backgroundColor: "{colors.sheet}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    height: "40px"
    padding: "0 12px"
  tab:
    textColor: "{colors.ink-muted}"
    typography: "{typography.button}"
    height: "44px"
    padding: "0 12px"
  tab-selected:
    textColor: "{colors.ink}"
  nav-item:
    textColor: "{colors.ink-secondary}"
    typography: "{typography.button}"
    rounded: "{rounded.md}"
    height: "40px"
    padding: "0 10px"
  nav-item-active:
    backgroundColor: "{colors.sheet}"
    textColor: "{colors.ink}"
  toast:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.sheet}"
    typography: "{typography.label}"
    rounded: "{rounded.md}"
    padding: "10px 14px"
  contract-sheet:
    backgroundColor: "{colors.sheet}"
    textColor: "{colors.ink}"
    typography: "{typography.contract-body}"
    rounded: "{rounded.sheet}"
    width: "680px"
  margin-note:
    textColor: "{colors.ink}"
    typography: "{typography.label}"
    rounded: "{rounded.sm}"
    width: "244px"
    padding: "6px 8px"
---

# Design System: ContractGuard

## 1. Overview

**Creative North Star: "Lề trang có chú thích" (The Annotated Margin)**

Màn hình ContractGuard giống một tờ hợp đồng trắng đặt trên mặt bàn lạnh màu, có người thẩm định cẩn thận ghi chú bằng bút chì ở lề. Văn bản hợp đồng luôn ở giữa và luôn đọc được như một văn bản thật: chữ có chân, dòng thoáng, khổ giấy cố định. Mọi phân tích (rủi ro, trích dẫn, câu trả lời chat) nằm sát lề, nối về đúng điều khoản bằng nét đứt mảnh, và bấm vào là tới. Công cụ lùi về sau: thanh bên và panel dùng nền xám xanh nhạt, chữ giao diện là sans gọn, màu chủ đạo chỉ xuất hiện khi có hành động.

Người dùng là giám đốc và nhân viên sales của SME Việt Nam, đọc hợp đồng 20–50 trang trên laptop 14–15" vào ban ngày, tập trung và hơi lo vì sợ bỏ sót "bẫy" pháp lý. Vì thế giao diện chắc tay và dễ bấm: nút và ô nhập cao 40px, chữ nút 14px đậm vừa, tương phản rõ. Không có gì nhấp nháy, không có gì dọa. Rủi ro nghiêm trọng nổi bật bằng hình và chữ, không bằng màu chói.

Hệ thống này từ chối:
- Dashboard SaaS kiểu "AI làm": donut gauge điểm số, hero metric to kèm gradient, lưới card giống hệt nhau, gradient tím xanh, glassmorphism.
- Nền kem/be "ấm áp" mặc định, eyebrow chữ in hoa giãn chữ trên mỗi section.
- Chatbot là trung tâm màn hình.
- Thẩm mỹ "legal tech" xanh navy và vàng kim, con dấu, búa phán quyết.

**Key Characteristics:**
- Một tờ hợp đồng trắng rộng 680px làm trung tâm; ghi chú lề rộng 244px bên phải.
- Bảng màu tiết chế: neutral ngả xanh lạnh (hue 200–225), một màu hành động, bốn màu severity luôn kèm hình và chữ.
- Hai họ chữ đối lập có chủ đích: Source Serif 4 cho văn bản hợp đồng, Be Vietnam Pro cho mọi thứ còn lại.
- Phẳng; chiều sâu bằng tầng nền, chỉ tờ hợp đồng, ngăn kéo và toast có bóng.
- Chuyển động ngắn (150–200ms, ease-out-quart), chỉ để báo trạng thái.
- Bố cục ưu tiên desktop; ở màn hẹp panel phải thành ngăn kéo, thanh bên thu về 56px, không bao giờ cuộn ngang.

## 2. Colors

Bảng màu lạnh, tiết chế: các tầng xám ngả xanh lục lam làm nền và chữ, một màu mực xanh cho hành động, bốn màu severity tách hẳn khỏi màu chủ đạo.

### Primary
- **Xanh Mực Thẩm Định** (`review-ink`): màu duy nhất cho hành động chính, mục đang chọn, viền focus và gạch chân tab đang mở. Hover đậm hơn (`review-ink-hover`), lúc nhấn đậm nữa (`review-ink-pressed`). Nền nhạt `review-ink-wash` dùng cho vùng được chọn hoặc tô nhẹ liên kết đang trỏ tới.

### Tertiary
Bốn màu severity, chỉ dùng để báo mức rủi ro. Mỗi màu có bản chữ/nét và bản nền nhạt (`-wash`) để tô điều khoản.
- **Đỏ Gạch Nghiêm Trọng** (`severity-critical`): rủi ro nghiêm trọng; cũng là màu của nút xóa và lỗi nhập liệu.
- **Cam Đất Cao** (`severity-high`): rủi ro cao.
- **Vàng Đồng Trung Bình** (`severity-medium` cho chữ, `severity-medium-mark` cho ký hiệu và nét tô vì bản chữ quá tối để làm dấu).
- **Xanh Lá Trầm Thấp** (`severity-low`): rủi ro thấp.

### Neutral
- **Mặt Bàn** (`desk`): nền trang, nơi tờ hợp đồng đặt lên.
- **Tờ Giấy** (`sheet`): trắng tinh; tờ hợp đồng, panel, ô nhập, nút phụ.
- **Thanh Ray** (`rail`): nền thanh bên, đậm hơn mặt bàn một bậc.
- **Đường Kẻ** (`rule`) và **Đường Kẻ Đậm** (`rule-strong`): đường chia nhẹ; viền ô nhập và nút phụ.
- **Mực** (`ink`): chữ chính. **Mực Phụ** (`ink-secondary`): chữ phụ, nhãn điều hướng. **Mực Mờ** (`ink-muted`): gợi ý, placeholder, chú thích; vẫn đạt ≥ 4.5:1 trên `sheet` và `desk`.

### Named Rules
**The Action-Only Rule.** Xanh Mực Thẩm Định chỉ dành cho hành động, lựa chọn và focus. Không dùng nó để trang trí, tô tiêu đề hay làm nền section. Nếu một màn hình có hơn ~10% diện tích màu xanh, có gì đó sai.

**The Never-Alone Rule.** Màu severity không bao giờ đứng một mình: luôn đi cùng ký hiệu hình học (thoi đặc, tam giác đặc, tròn đặc, tròn rỗng) và chữ ("Nghiêm trọng", "Cao", "Trung bình", "Thấp"). Người mù màu phải phân biệt được mức rủi ro khi in trắng đen.

**The Cold Desk Rule.** Nền là xám ngả xanh lạnh, không bao giờ là kem, be hay "giấy cũ". Hơi ấm của sản phẩm đến từ chữ có chân của hợp đồng, không từ màu nền.

## 3. Typography

**Display Font:** không dùng (giao diện sản phẩm, không có hero)
**Body Font:** Be Vietnam Pro (fallback `ui-sans-serif, system-ui, sans-serif`)
**Contract Font:** Source Serif 4 Variable (fallback `Georgia, serif`)

**Character:** Be Vietnam Pro là sans gọn, đủ dấu tiếng Việt, mang toàn bộ giao diện. Source Serif 4 chỉ xuất hiện ở nơi có chữ của hợp đồng (văn bản, trích dẫn điều khoản), để người dùng luôn phân biệt "đây là hợp đồng nói" với "đây là hệ thống nói".

### Hierarchy
- **Headline** (600, 22px, 1.3): tên trang, tên hợp đồng ở đầu workspace.
- **Title** (600, 16px, 1.4): tiêu đề panel, tiêu đề finding, tiêu đề hộp thoại.
- **Body** (400, 14px, 1.5): chữ giao diện mặc định. Đoạn văn giải thích dài giới hạn 65–75ch.
- **Button** (600, 14px): chữ trên nút, tab, mục điều hướng.
- **Label** (500, 13px, 1.4): nhãn ô nhập, nhãn trường, toast, ghi chú lề.
- **Caption** (400, 12.5px, 1.45): gợi ý dưới ô nhập, thông báo lỗi, số trang, thời gian.
- **Contract Body** (400, 16px, 1.75): văn bản hợp đồng trên tờ giấy. **Contract Heading** (600, 16px): tên điều, khoản.

Thang cố định bằng px, không dùng `clamp()`. Tiêu đề dùng `text-wrap: balance`, đoạn văn dùng `text-wrap: pretty`. Số trong bảng và số trang dùng `tabular-nums`.

### Named Rules
**The Two Voices Rule.** Chữ có chân là giọng của hợp đồng; chữ không chân là giọng của hệ thống. Không bao giờ đặt nút, nhãn hay câu trả lời chat bằng Source Serif; không bao giờ hiện văn bản điều khoản bằng Be Vietnam Pro.

**The No Shouting Rule.** Không chữ in hoa giãn chữ làm eyebrow, không tiêu đề lớn hơn 22px trong ứng dụng. Mức độ nghiêm trọng thể hiện bằng ký hiệu và màu severity, không bằng cỡ chữ.

## 4. Elevation

Hệ thống phẳng. Chiều sâu đến từ tầng nền: thanh ray (`rail`) → mặt bàn (`desk`) → tờ giấy (`sheet`). Chỉ ba thứ được có bóng, vì chúng thật sự nằm trên một lớp khác.

### Shadow Vocabulary
- **Tờ hợp đồng** (`box-shadow: 0 1px 2px oklch(0.2 0.02 220 / 0.06), 0 10px 28px -14px oklch(0.2 0.02 220 / 0.22)`): tờ giấy nằm trên mặt bàn. Chỉ dùng cho tờ hợp đồng.
- **Ngăn kéo** (bóng `2xl` của Tailwind): panel rủi ro và hỏi đáp khi trượt vào ở màn hẹp.
- **Toast** (bóng `lg` của Tailwind): thông báo ngắn ở góc dưới phải.

Mục đang chọn ở thanh bên không dùng bóng mà dùng nền `sheet` kèm viền 1px `rule`.

### Named Rules
**The Paper Only Rule.** Card, panel, nút và ô nhập không có bóng. Nếu thấy bóng dưới một thứ không phải tờ hợp đồng, ngăn kéo hay toast, hãy xóa nó.

## 5. Components

Cảm giác chung: **chắc tay, dễ bấm**. Vùng bấm cao 40px (tab 44px), chữ nút 14px đậm vừa, viền rõ, bo góc nhỏ 6px. Mọi component tương tác có đủ trạng thái mặc định, hover, focus, nhấn, vô hiệu, đang tải và lỗi.

### Buttons
- **Shape:** bo nhẹ (6px), cao 40px; bản nhỏ cao 36px, chữ 13px.
- **Primary:** nền Xanh Mực Thẩm Định, chữ trắng, đệm ngang 16px. Hover đậm một bậc, nhấn đậm hai bậc.
- **Secondary:** nền trắng, viền 1px `rule-strong`, chữ `ink`; hover đổi nền sang `desk`.
- **Ghost:** không nền, chữ `ink-secondary`; hover nền mực 6%, chữ đậm lên `ink`.
- **Danger:** chữ đỏ gạch, hover nền `severity-critical-wash`. Dùng cho xóa và thu hồi quyền.
- **Hover / Focus:** đổi màu nền và viền trong 150ms. Focus luôn là viền 2px Xanh Mực Thẩm Định, lệch ra 2px.
- **Disabled / Loading:** độ mờ 50%, con trỏ cấm; đang tải thì giữ nguyên kích thước nút, thay chữ bằng biểu tượng xoay nhỏ kèm chữ.

### Inputs / Fields
- **Style:** nền trắng, viền 1px `rule-strong`, bo 6px, cao 40px, chữ 14px. Nhãn 13px đậm vừa ở trên, gợi ý 12.5px `ink-muted` ở dưới.
- **Focus:** viền đổi sang Xanh Mực Thẩm Định kèm vòng sáng 3px cùng màu ở 15% độ đậm.
- **Error:** viền và vòng sáng đổi sang đỏ gạch, câu lỗi 12.5px màu đỏ gạch thay chỗ gợi ý, gắn bằng `aria-describedby`.
- **Disabled:** nền `desk`, chữ `ink-muted`.

### Tabs
- Cao 44px, chữ 14px đậm vừa. Tab đang mở: chữ `ink` và gạch chân 2px Xanh Mực Thẩm Định. Tab khác: chữ `ink-muted`, hover lên `ink`. Phím mũi tên trái/phải, Home, End chuyển tab theo WAI-ARIA.

### Navigation
- Thanh bên trái rộng 232px trên nền `rail`, thu về 56px chỉ còn biểu tượng ở màn hẹp hoặc khi đang mở workspace.
- Mục điều hướng cao 40px, biểu tượng 16px kèm chữ 14px. Mục đang chọn: nền `sheet`, viền 1px `rule`, chữ `ink`. Mục khác: chữ `ink-secondary`, hover nền mực 5%.
- Nút chính "Tải hợp đồng lên" nằm ngay dưới logo, là nút primary rộng hết thanh.
- Có liên kết "Bỏ qua tới nội dung chính" khi nhấn Tab lần đầu.

### Severity Tag (signature)
Ký hiệu hình học 12px kèm chữ nhãn, cùng màu severity: thoi đặc (Nghiêm trọng), tam giác đặc (Cao), tròn đặc (Trung bình), tròn rỗng (Thấp). Đây là cách duy nhất để hiện mức rủi ro; không dùng chip màu không chữ.

### Contract Sheet và Margin Note (signature)
- **Tờ hợp đồng:** rộng 680px, nền trắng, bo 3px, bóng "Tờ hợp đồng", chữ Contract Body. Điều khoản có rủi ro được tô nền `-wash` của severity tương ứng; điều khoản đang được trỏ tới thì tô `review-ink-wash`.
- **Ghi chú lề:** cột 244px bên phải tờ giấy. Mỗi ghi chú gồm ký hiệu severity và một dòng tóm tắt, nối về điều khoản bằng nét đứt `rule-strong`. Bấm ghi chú thì cuộn tới điều khoản và ngược lại.
- **Thước rủi ro:** điểm 0–100 hiển thị trên một thanh ngang cao 8px chia bốn vùng ngưỡng (0–30, 30–55, 55–85, 85–100). Vùng hiện hành tô đặc, các vùng khác tô `-wash`. Thay hoàn toàn cho donut gauge.

### Feedback
- **Toast:** nền `ink`, chữ trắng 13px, bo 6px, góc dưới phải, `role="status"`.
- **Đang tải:** khung xương (skeleton) nhấp nháy nhẹ, không spinner chặn giữa nội dung.
- **Trống:** câu hướng dẫn việc cần làm tiếp kèm một nút, không chỉ "Không có dữ liệu".

## 6. Do's and Don'ts

### Do:
- **Do** đặt văn bản hợp đồng ở giữa, rộng 680px, chữ Source Serif 4 16px dòng 1.75, và gắn mọi phân tích vào lề hoặc tô sáng ngay trên điều khoản.
- **Do** cho mọi rủi ro và mọi câu trả lời chat một đường dẫn về điều khoản cụ thể; bấm vào là cuộn tới đúng chỗ.
- **Do** hiện severity bằng ký hiệu + chữ + màu, cả ba cùng lúc.
- **Do** giữ vùng bấm tối thiểu 40px (tab 44px), chữ nút 14px đậm vừa.
- **Do** dùng viền focus 2px Xanh Mực Thẩm Định lệch 2px trên mọi phần tử bấm được, và kiểm tra mọi luồng bằng bàn phím.
- **Do** giữ tương phản chữ ≥ 4.5:1, kể cả placeholder (dùng `ink-muted`, không nhạt hơn).
- **Do** giữ chuyển động 150–200ms với `cubic-bezier(0.25, 1, 0.5, 1)` và rút còn 1ms khi `prefers-reduced-motion: reduce`.
- **Do** dùng lớp z-index đã đặt: sticky 10, ngăn kéo 20, popover 30, toast 50.
- **Do** viết lời theo giọng "rủi ro tiềm ẩn, cần xem xét"; không kết luận "trái luật".

### Don't:
- **Don't** làm dashboard SaaS kiểu "AI làm": donut gauge điểm số, hero metric to kèm gradient, lưới card giống hệt nhau, gradient tím xanh, glassmorphism.
- **Don't** dùng nền kem/be "ấm áp", hay eyebrow chữ in hoa giãn chữ trên mỗi section.
- **Don't** đặt chatbot làm trung tâm màn hình. Chat là một tab trong panel phải, phục vụ việc đọc hợp đồng.
- **Don't** dùng thẩm mỹ "legal tech": xanh navy và vàng kim, con dấu, búa phán quyết.
- **Don't** dùng Xanh Mực Thẩm Định để trang trí, tô tiêu đề hay làm nền section.
- **Don't** hiện mức rủi ro chỉ bằng màu (chip màu không chữ, chấm màu không ký hiệu).
- **Don't** dùng viền trái hoặc phải dày hơn 1px làm vạch màu trên card, mục danh sách hay thông báo.
- **Don't** đặt bóng dưới card, panel, nút hay ô nhập; chỉ tờ hợp đồng, ngăn kéo và toast có bóng.
- **Don't** dùng Source Serif cho nút, nhãn hay câu trả lời chat; không dùng sans cho văn bản điều khoản.
- **Don't** mở modal khi có thể làm ngay tại chỗ; modal chỉ cho xác nhận xóa và chia sẻ.
- **Don't** dùng z-index tùy ý như 999 hay 9999.
