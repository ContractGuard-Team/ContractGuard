import { Fragment, useState, type ReactNode } from "react";
import { Link, redirect } from "react-router";

import { buttonClass } from "~/components/Button";
import { NoteButton, severityWash } from "~/components/ContractSheet";
import { Logo } from "~/components/Logo";
import { RiskRuler } from "~/components/RiskRuler";
import { SeverityTag, type Severity } from "~/components/SeverityTag";
import { apiFetch, type Schemas } from "~/lib/api";
import { riskDomainLabels } from "~/lib/labels";

export function meta() {
  return [
    { title: "ContractGuard · Thẩm định rủi ro hợp đồng" },
    { name: "description", content: "ContractGuard đọc hợp đồng, tách từng điều khoản và ghi chú rủi ro tiềm ẩn ngay bên lề, kèm lý do và hướng xử lý." },
  ];
}

// A signed-in visitor goes straight to the library. No session, or no API at all: the page still shows.
export async function clientLoader() {
  const me = await apiFetch<Schemas["MeResponse"]>("/me").catch(() => null);
  if (me) {
    throw redirect("/contracts");
  }

  return null;
}

// DESIGN §7: the landing page is the one place with display type. Sizes step at breakpoints (no clamp);
// line height stays at 1.14 or more so stacked Vietnamese marks (ệ, ấ) are not clipped.
const container = "mx-auto w-full max-w-[1280px] px-6 lg:px-12";
const display = "font-contract text-[40px] leading-[1.14] font-normal tracking-[-0.02em] sm:text-[56px] lg:text-[72px]";
const statement = "font-contract text-[28px] leading-[1.2] font-normal tracking-[-0.015em] sm:text-[36px] lg:text-[44px]";
const lead = "max-w-[52ch] text-[18px] leading-[1.6] text-ink-secondary";
const textLink = "rounded-sm py-2 font-medium text-ink underline decoration-rule-strong underline-offset-4 transition-colors duration-150 hover:decoration-ink";

interface SampleClause {
  id: string;
  type: string;
  page: number;
  heading: string;
  text: string;
  note?: { severity: Severity; title: string; why: string; recommendation: string };
}

// An invented excerpt, read from Bên B's side. Wording follows PRODUCT.md: potential risk, never "illegal".
const sampleClauses: SampleClause[] = [
  {
    id: "5.1",
    type: "Phạm vi công việc",
    page: 3,
    heading: "5.1. Phạm vi dịch vụ",
    text: "Bên B cung cấp, cài đặt và bảo trì phần mềm quản lý bán hàng theo Phụ lục 01 trong suốt thời hạn hợp đồng.",
  },
  {
    id: "6.2",
    type: "Phạt và bồi thường",
    page: 4,
    heading: "6.2. Phạt vi phạm",
    text: "Nếu Bên B chậm tiến độ quá 03 ngày, Bên B chịu phạt 1% giá trị hợp đồng cho mỗi ngày chậm trễ và bồi thường toàn bộ thiệt hại gián tiếp phát sinh, không áp dụng mức giới hạn tối đa.",
    note: {
      severity: "critical",
      title: "Mức phạt cộng dồn, không có trần",
      why: "Mức phạt tính theo ngày và không có giới hạn, nên có thể vượt mức 8% giá trị phần nghĩa vụ bị vi phạm nêu tại Điều 301 Luật Thương mại 2005. Thiệt hại gián tiếp cũng khó ước lượng trước.",
      recommendation: "Đề nghị ghi rõ tổng mức phạt tối đa và loại trừ thiệt hại gián tiếp, hoặc giới hạn bồi thường theo giá trị hợp đồng.",
    },
  },
  {
    id: "7.1",
    type: "Chấm dứt hợp đồng",
    page: 5,
    heading: "7.1. Đơn phương chấm dứt",
    text: "Bên A có quyền đơn phương chấm dứt hợp đồng ngay lập tức bằng thông báo miệng hoặc văn bản nếu đánh giá chất lượng dịch vụ không đạt yêu cầu nội bộ.",
    note: {
      severity: "high",
      title: "Chấm dứt ngay, không có thời hạn khắc phục",
      why: "Bên A có thể chấm dứt chỉ dựa trên đánh giá nội bộ, không báo trước và không cho Bên B thời gian sửa lỗi. Chi phí đã bỏ ra có nguy cơ không thu hồi được.",
      recommendation: "Đề nghị thêm thời hạn báo trước bằng văn bản và một khoảng thời gian để khắc phục trước khi quyền chấm dứt có hiệu lực.",
    },
  },
  {
    id: "8.3",
    type: "Thanh toán",
    page: 6,
    heading: "8.3. Thời hạn thanh toán",
    text: "Bên A thanh toán trong vòng 60 ngày làm việc kể từ ngày phòng Tài chính Bên A phê duyệt biên bản nghiệm thu và nhận đủ chứng từ hợp lệ.",
    note: {
      severity: "medium",
      title: "Hạn thanh toán dài, tính từ mốc nội bộ của Bên A",
      why: "60 ngày làm việc tương đương gần ba tháng, lại tính từ một mốc do Bên A tự quyết định. Dòng tiền của Bên B phụ thuộc vào tiến độ phê duyệt của đối tác.",
      recommendation: "Đề nghị tính hạn từ ngày ký biên bản nghiệm thu và rút ngắn thời hạn; có thể đề xuất tạm ứng một phần khi ký.",
    },
  },
];

const steps = [
  {
    title: "Tải hợp đồng lên",
    body: "PDF, DOCX hoặc bản scan. Bạn cho biết mình là bên nào trong hợp đồng, để phân tích đứng về phía bạn.",
  },
  {
    title: "Đọc ghi chú ở lề",
    body: "Từng điều khoản được tách ra và phân loại. Rủi ro hiện ngay bên cạnh, xếp theo bốn mức, kèm lý do và hướng xử lý.",
  },
  {
    title: "Hỏi tiếp, rồi mang đi đàm phán",
    body: "Hỏi trợ lý về bất kỳ điều khoản nào; câu trả lời luôn dẫn về đúng đoạn văn bản. Xuất báo cáo để gửi cho đối tác hoặc đồng nghiệp.",
  },
];

const domainNotes: Record<Schemas["RiskDomain"], string> = {
  legal: "Điều khoản có thể vô hiệu, luật áp dụng, cách giải quyết tranh chấp.",
  financial: "Mức phạt, bồi thường, thời hạn và điều kiện thanh toán.",
  operational: "Tiến độ, nghiệm thu, quyền đơn phương chấm dứt.",
  compliance: "Giấy phép, tiêu chuẩn và nghĩa vụ báo cáo theo quy định.",
  cybersecurity: "Trách nhiệm bảo vệ hệ thống và thông báo khi có sự cố.",
  data_privacy: "Cách thu thập, xử lý và chuyển giao dữ liệu cá nhân.",
  third_party: "Nhà thầu phụ, chuyển nhượng hợp đồng, sở hữu trí tuệ.",
};

export default function Landing() {
  return (
    // isolate + -z-10 on the art: the patterns sit under every text, inside this page only. clip, not hidden, so no scroll box.
    <div className="relative isolate flex min-h-screen flex-col overflow-x-clip text-[16px] leading-[1.6]">
      <DecoSunburst />
      <header className={`${container} flex h-20 items-center justify-between`}>
        <Link to="/" className="rounded-sm">
          <Logo />
        </Link>
        <nav aria-label="Tài khoản" className="flex shrink-0 items-center gap-2 whitespace-nowrap">
          <Link to="/login" className={buttonClass("ghost", "sm")}>
            Đăng nhập
          </Link>
          {/* On a phone the hero button right below does this job; the header keeps only the login. */}
          <Link to="/register" className={`${buttonClass("secondary", "sm")} max-sm:hidden`}>
            Tạo tài khoản
          </Link>
        </nav>
      </header>

      <main className="flex-1">
        <section>
          <div className={`${container} relative pt-16 pb-20 sm:pt-24 lg:pt-32 lg:pb-28`}>
          {/* Non-breaking spaces keep compound words ("vấn đề", "rủi ro") on one line in the headings. */}
          <h1 className={display}>
            <span className="md:block">Biết hợp đồng có vấn&nbsp;đề ở&nbsp;đâu,</span> trước khi&nbsp;ký.
          </h1>
          <p className={`${lead} mt-8`}>
            ContractGuard đọc hợp đồng đối tác gửi sang, tách từng điều khoản và ghi chú rủi ro tiềm ẩn ngay bên lề: nằm ở đâu, vì sao cần xem xét, nên đàm phán lại điều gì.
          </p>
          <div className="mt-10 flex flex-wrap items-center gap-x-6 gap-y-3">
            <Link to="/register" className={buttonClass()}>
              Thử với hợp đồng của bạn
            </Link>
            <a href="#vi-du" className={textLink}>
              Xem một ví dụ
            </a>
          </div>
          </div>
        </section>

        <section id="vi-du" aria-labelledby="vi-du-title" className="border-t border-rule">
          <div className={`${container} py-20 lg:py-28`}>
            <h2 id="vi-du-title" className={statement}>
              Hợp đồng ở giữa. Ghi chú ở lề.
            </h2>
            <p className={`${lead} mt-5`}>
              Phân tích không tách thành một bản báo cáo riêng. Mỗi rủi ro nằm cạnh đúng điều khoản sinh ra nó. Bấm vào một ghi chú để xem vì sao.
            </p>
            <SampleSheet />
          </div>
        </section>

        <section aria-labelledby="cach-lam-title" className="border-t border-rule">
          <div className={`${container} py-20 lg:py-28`}>
            <h2 id="cach-lam-title" className={statement}>
              Từ file đến bàn đàm phán.
            </h2>
            <ol className="mt-12 lg:mt-16">
              {steps.map((step, index) => (
                <li key={step.title} className="grid gap-x-10 gap-y-2 border-t border-rule py-7 sm:grid-cols-[3rem_1fr] lg:grid-cols-[5rem_1fr_1.2fr] lg:py-9">
                  <span aria-hidden="true" className="font-contract text-[22px] leading-[1.3] text-ink-muted tabular-nums lg:text-[24px]">
                    {index + 1}
                  </span>
                  <h3 className="font-contract text-[22px] leading-[1.3] font-normal lg:text-[24px]">{step.title}</h3>
                  <p className="max-w-[52ch] text-ink-secondary sm:col-start-2 lg:col-start-3">{step.body}</p>
                </li>
              ))}
            </ol>
          </div>
        </section>

        <section aria-labelledby="nhom-rui-ro-title" className="border-t border-rule">
          <div className={`${container} grid gap-x-16 gap-y-10 py-20 lg:grid-cols-[1fr_1.4fr] lg:py-28`}>
            <div>
              <h2 id="nhom-rui-ro-title" className={statement}>
                Bảy nhóm rủi&nbsp;ro, một lần đọc.
              </h2>
              <p className={`${lead} mt-5`}>Mỗi điều khoản được xem xét trên cả bảy nhóm, không chỉ phần pháp lý.</p>
            </div>
            <dl>
              {Object.entries(riskDomainLabels).map(([domain, name]) => (
                <div key={domain} className="grid gap-x-8 gap-y-1 border-t border-rule py-5 sm:grid-cols-[15rem_1fr] sm:items-baseline">
                  <dt className="font-contract text-[22px] leading-[1.3]">{name}</dt>
                  <dd className="text-ink-secondary">{domainNotes[domain as Schemas["RiskDomain"]]}</dd>
                </div>
              ))}
            </dl>
          </div>
        </section>

        <section aria-labelledby="bat-dau-title" className="relative overflow-hidden border-t border-rule">
          <div className={`${container} relative pt-24 pb-48 lg:pt-36 lg:pb-64`}>
            <h2 id="bat-dau-title" className={`${display} max-w-[900px]`}>
              <span className="md:block">Hợp đồng tiếp theo,</span> đừng đọc một&nbsp;mình.
            </h2>
            <div className="mt-10 flex flex-wrap items-center gap-x-6 gap-y-3">
              <Link to="/register" className={buttonClass()}>
                Tạo tài khoản
              </Link>
              <Link to="/login" className={textLink}>
                Đăng nhập
              </Link>
            </div>
          </div>
          <DecoFrieze />
        </section>
      </main>

      <footer className="border-t border-rule">
        <div className={`${container} flex flex-col gap-4 py-10 text-body text-ink-muted sm:flex-row sm:items-baseline sm:justify-between`}>
          <Logo />
          <p className="max-w-[60ch]">ContractGuard chỉ ra rủi ro tiềm ẩn để bạn xem xét, không thay thế tư vấn pháp lý. © 2026 ContractGuard.</p>
        </div>
      </footer>
    </div>
  );
}

// The one picture of the page: the app's own contract sheet and margin notes (DESIGN §5), with the
// same grid and tokens as ContractSheet, on invented clauses. Under 948px the notes move into the sheet.
function SampleSheet() {
  const [openId, setOpenId] = useState<string | null>("6.2");

  return (
    <div className="@container mt-12 lg:mt-16">
      <RiskRuler score={68} level="high" label="Điểm rủi ro tổng của ví dụ" />

      <div className="relative mt-8 grid max-w-sheet grid-cols-1 text-body @min-[948px]:max-w-[948px] @min-[948px]:grid-cols-[var(--spacing-sheet)_var(--spacing-margin-note)] @min-[948px]:gap-x-6">
        <div aria-hidden="true" className="absolute inset-y-0 left-0 w-full rounded-sheet bg-sheet shadow-sheet @min-[948px]:w-sheet" />

        <div className="relative px-8 pt-8 pb-6">
          <p className="font-contract text-contract font-semibold">Hợp đồng cung cấp dịch vụ phần mềm</p>
          <p className="text-caption text-ink-muted">Trích đoạn minh họa, không phải hợp đồng thật.</p>
        </div>
        <span aria-hidden="true" className="hidden @min-[948px]:block" />

        {sampleClauses.map((clause) => {
          const { note } = clause;
          const open = clause.id === openId;
          const noteButton = (margin: boolean) =>
            note && <NoteButton finding={{ findingId: clause.id, severity: note.severity, title: note.title }} selected={open} onSelect={setOpenId} margin={margin} />;

          return (
            <Fragment key={clause.id}>
              <article
                aria-label={clause.heading}
                className={`relative border-t border-rule px-8 py-6 transition-colors duration-200 ${note ? `cursor-pointer ${severityWash[note.severity]}` : ""}`}
                onClick={note ? () => setOpenId(open ? null : clause.id) : undefined}
              >
                <p className="mb-2 flex flex-wrap items-baseline justify-between gap-x-4 text-label">
                  <span className="text-ink">{clause.type}</span>
                  <span className="flex items-center gap-3 font-normal text-ink-muted">
                    {note && <SeverityTag severity={note.severity} />}
                    Trang {clause.page}
                  </span>
                </p>

                {note && <div className="mb-2 flex flex-col @min-[948px]:hidden">{noteButton(false)}</div>}

                <h3 className="mb-1 font-contract text-contract font-semibold">{clause.heading}</h3>
                <p className="font-contract text-contract">{clause.text}</p>

                {note && open && (
                  // Reading or selecting the explanation must not close it.
                  <div className="mt-4 flex cursor-auto flex-col gap-3 rounded-md border border-rule bg-sheet p-4" onClick={(event) => event.stopPropagation()}>
                    <p className="font-semibold">{note.title}</p>
                    <Part title="Vì sao là rủi ro">{note.why}</Part>
                    <Part title="Hướng xử lý">{note.recommendation}</Part>
                  </div>
                )}
              </article>

              {note ? (
                <aside aria-label={`Ghi chú cho ${clause.heading}`} className="hidden flex-col py-6 @min-[948px]:flex">
                  {noteButton(true)}
                </aside>
              ) : (
                <span aria-hidden="true" className="hidden @min-[948px]:block" />
              )}
            </Fragment>
          );
        })}
      </div>
    </div>
  );
}

// Art deco: a stepped sunburst rising from the corner of the hero, and a frieze of fans at the close (DESIGN §7).
// Every line is computed. Drawn in a faint ink tint, behind the text; decoration only, hidden from screen readers.
const artClass = "pointer-events-none absolute -z-10 text-ink/[0.13]";
const n = (value: number) => value.toFixed(1);
const polar = (cx: number, cy: number, r: number, t: number) => `${n(cx + r * Math.cos(t))} ${n(cy + r * Math.sin(t))}`;
// Clockwise on screen, for spans up to half a turn.
const arc = (cx: number, cy: number, r: number, from: number, to: number) => `M${polar(cx, cy, r, from)}A${r} ${r} 0 0 1 ${polar(cx, cy, r, to)}`;

// Rays fan from left to straight up and a little past; every fourth is long and filled, every second is medium.
function sunburst() {
  const [x, y, step] = [150, 400, Math.PI / 32];
  const rays = ["", ""];
  for (let k = 0; k <= 20; k++) {
    const t = Math.PI + k * step;
    const reach = k % 4 === 0 ? 540 : k % 2 === 0 ? 470 : 400;
    rays[k % 4 === 0 ? 0 : 1] += `M${polar(x, y, 116, t)}L${polar(x, y, reach, t - 0.022)}L${polar(x, y, reach + 14, t)}L${polar(x, y, reach, t + 0.022)}Z`;
  }
  const arcs = [50, 62, 74, 98, 104, 250, 256].map((r) => arc(x, y, r, Math.PI, Math.PI + 20 * step)).join("");
  return [rays[0], rays[1] + arcs];
}

const [longRays, sunLines] = sunburst();

function DecoSunburst() {
  return (
    <svg aria-hidden="true" viewBox="-400 -400 800 800" className={`${artClass} top-[-120px] right-[-260px] size-[860px] max-md:hidden`}>
      <g stroke="currentColor" strokeWidth="0.75" strokeLinejoin="round">
        <path d={longRays} fill="currentColor" fillOpacity="0.3" vectorEffect="non-scaling-stroke" />
        <path d={sunLines} fill="none" vectorEffect="non-scaling-stroke" />
      </g>
    </svg>
  );
}

// One fan: nested half circles with spokes, and a stem topped by a diamond where it meets the next fan.
const fanPeriod = 120;
function fanPath() {
  const [cx, cy] = [fanPeriod / 2, 150];
  let d = "M0 150V88M0 70l6 9l-6 9l-6 -9Z";
  for (const r of [60, 54, 36, 18]) d += arc(cx, cy, r, Math.PI, Math.PI * 2);
  for (let k = 1; k < 8; k++) d += `M${polar(cx, cy, 18, Math.PI + (k * Math.PI) / 8)}L${polar(cx, cy, 54, Math.PI + (k * Math.PI) / 8)}`;
  return d;
}

const fan = fanPath();

function DecoFrieze() {
  // The viewBox is narrow, so the height sets the scale; overflow-visible lets the fans run to the page edges.
  return (
    <svg aria-hidden="true" viewBox="0 0 240 200" className={`${artClass} inset-x-0 bottom-8 h-32 w-full overflow-visible lg:bottom-12 lg:h-44`}>
      <g fill="none" stroke="currentColor" strokeWidth="0.75">
        {Array.from({ length: 38 }, (_, i) => (
          <path key={i} d={fan} vectorEffect="non-scaling-stroke" transform={`translate(${(i - 18) * fanPeriod} 0)`} />
        ))}
        <path d="M-2400 56H2640M-2400 62H2640M-2400 150H2640M-2400 158H2640M-2400 164H2640" vectorEffect="non-scaling-stroke" />
      </g>
    </svg>
  );
}

function Part({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div>
      <h4 className="text-label text-ink-secondary">{title}</h4>
      <p className="max-w-prose">{children}</p>
    </div>
  );
}
