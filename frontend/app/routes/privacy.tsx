import type { ReactNode } from "react";
import { Link } from "react-router";

import { Logo } from "~/components/Logo";

export function meta() {
  return [
    { title: "Chính sách quyền riêng tư · ContractGuard" },
    { name: "description", content: "ContractGuard thu thập dữ liệu gì, dùng vào việc gì, lưu ở đâu và xóa thế nào." },
  ];
}

// Public page, linked from the landing footer; Google's OAuth consent screen needs its URL to publish the app.
// Content follows SRS §9.9 (retention) and §11.8 (privacy); change both together.
const contact = "contractguard.info@gmail.com";

export default function Privacy() {
  return (
    <div className="mx-auto w-full max-w-[72ch] px-4 py-10 text-[16px] leading-[1.6] sm:px-6 sm:py-16">
      <Link to="/" className="rounded-sm">
        <Logo />
      </Link>

      <h1 className="mt-12 font-contract text-[32px] leading-[1.2] font-normal sm:text-[40px]">Chính sách quyền riêng tư</h1>
      <p className="mt-2 text-ink-muted">Cập nhật ngày 22/11/2026</p>

      <p className="mt-8 text-ink-secondary">
        ContractGuard là sản phẩm của một nhóm thực tập, đang ở giai đoạn thử nghiệm. Trang này nói rõ chúng tôi thu thập dữ liệu gì, dùng vào việc gì và
        xóa thế nào.
      </p>

      <Section title="Dữ liệu chúng tôi thu thập">
        <ul className="list-disc space-y-2 pl-5">
          <li>
            <strong>Thông tin tài khoản:</strong> email, tên hiển thị và mật khẩu (chỉ lưu dạng băm). Khi bạn đăng nhập bằng Google, chúng tôi chỉ nhận email,
            trạng thái xác thực email và tên từ Google, không nhận mật khẩu Google hay quyền truy cập dữ liệu khác trong tài khoản Google của bạn.
          </li>
          <li>
            <strong>Hợp đồng bạn tải lên:</strong> file gốc, nội dung trích xuất, ngữ cảnh bạn khai báo, kết quả phân tích và lịch sử trò chuyện với trợ lý.
          </li>
          <li>
            <strong>Nhật ký:</strong> nhật ký hoạt động trên từng hợp đồng (ai xem, chia sẻ, sửa), và nhật ký kỹ thuật (địa chỉ IP, thời điểm, lỗi) để vận hành và
            bảo mật hệ thống.
          </li>
        </ul>
      </Section>

      <Section title="Chúng tôi dùng dữ liệu vào việc gì">
        <p>
          Chỉ để cung cấp các chức năng của ContractGuard: đăng nhập, phân tích rủi ro hợp đồng, trả lời câu hỏi về hợp đồng, chia sẻ trong tổ chức và gửi
          email xác thực, đặt lại mật khẩu, lời mời. Chúng tôi không bán dữ liệu, không dùng dữ liệu để quảng cáo và không dùng hợp đồng của bạn cho mục đích
          nào khác.
        </p>
        <p>Các model AI phân tích hợp đồng do nhóm tự vận hành trên máy chủ của mình, không gửi hợp đồng tới dịch vụ AI của bên thứ ba.</p>
      </Section>

      <Section title="Dữ liệu được lưu ở đâu">
        <p>
          Dữ liệu được lưu và xử lý trên hạ tầng của Amazon Web Services (Singapore), Supabase (cơ sở dữ liệu, Tokyo, Nhật Bản) và Railway (máy chủ ứng
          dụng). Email được gửi qua Gmail hoặc Amazon SES. Mọi kết nối đều được mã hóa bằng HTTPS.
        </p>
      </Section>

      <Section title="Thời gian lưu và xóa dữ liệu">
        <ul className="list-disc space-y-2 pl-5">
          <li>Hợp đồng và mọi dữ liệu liên quan được lưu cho tới khi bạn xóa. Sau khi xóa, dữ liệu biến mất khỏi giao diện ngay và bị xóa hẳn trong vòng 24 giờ.</li>
          <li>Nhật ký kỹ thuật được tự động xóa sau 30 ngày.</li>
          <li>Lời mời vào tổ chức hết hạn sau tối đa 7 ngày.</li>
          <li>Báo cáo bạn xuất ra không được lưu trên hệ thống.</li>
          <li>
            Tài khoản được lưu cho tới khi bạn yêu cầu xóa. Hãy gửi yêu cầu tới <a href={`mailto:${contact}`} className="underline underline-offset-4">{contact}</a>;
            yêu cầu được xử lý thủ công.
          </li>
        </ul>
      </Section>

      <Section title="Lưu ý">
        <p>ContractGuard chỉ ra rủi ro tiềm ẩn để bạn xem xét, không thay thế tư vấn pháp lý. Vì đây là bản thử nghiệm, vui lòng không tải lên hợp đồng chứa thông tin mật mà bạn không muốn lưu trên hệ thống.</p>
      </Section>

      <Section title="Liên hệ">
        <p>
          Mọi câu hỏi về quyền riêng tư, xin gửi tới <a href={`mailto:${contact}`} className="underline underline-offset-4">{contact}</a>.
        </p>
      </Section>
    </div>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="mt-10 space-y-3">
      <h2 className="font-contract text-[22px] leading-[1.3] font-normal">{title}</h2>
      {children}
    </section>
  );
}
