import { EmptyState } from '../../components/AppShell'
import { CheckStatusTag } from '../../components/risk'
import { CHECK_STATUS_LABEL, clauseLabel, type CheckStatus, type Contract } from '../../model'

const GROUPS = [
  { prefix: 'CY', title: 'An ninh mạng' },
  { prefix: 'PR', title: 'Dữ liệu và quyền riêng tư' },
  { prefix: 'TP', title: 'Bên thứ ba' },
]

export function Checklist({ contract: c, onJump, onOpenFinding }: { contract: Contract; onJump: (id: string) => void; onOpenFinding: (id: string) => void }) {
  const checks = c.detail?.checks ?? []
  if (!checks.length)
    return (
      <EmptyState title="Checklist bảo mật không áp dụng cho hợp đồng này">
        {c.result?.naReason ?? 'Đối tác không tiếp nhận hay xử lý dữ liệu của bên mình.'} Nếu thực tế có, hãy sửa ngữ cảnh trong tab Tổng quan và phân tích lại.
      </EmptyState>
    )

  const tally = (['pass', 'partial', 'missing'] as CheckStatus[]).map((s) => [s, checks.filter((x) => x.status === s).length] as const)

  return (
    <div className="mx-auto max-w-[1000px] px-4 py-6 sm:px-8 sm:py-8">
      <h2 className="text-[17px] font-semibold">20 điểm kiểm tra bảo mật trong hợp đồng</h2>
      <p className="mt-1 max-w-[70ch] text-[13.5px] text-ink-2">
        Một hợp đồng an toàn về dữ liệu cần quy định đủ các điểm dưới đây. Điểm chưa đạt sinh ra rủi ro; nhiều điểm thiếu cùng nguyên nhân được gộp vào một rủi ro.
      </p>
      <p className="mt-3 flex flex-wrap gap-x-5 gap-y-1 text-[13.5px]">
        {tally.map(([s, n]) => (
          <span key={s} className="inline-flex items-center gap-1.5">
            <CheckStatusTag status={s} />
            <span className="font-semibold tabular-nums">{n}</span>
            <span className="sr-only">{CHECK_STATUS_LABEL[s]}</span>
          </span>
        ))}
      </p>

      {GROUPS.map((g) => {
        const rows = checks.filter((x) => x.id.startsWith(g.prefix))
        return (
          <section key={g.prefix} aria-labelledby={`grp-${g.prefix}`} className="mt-7">
            <h3 id={`grp-${g.prefix}`} className="text-[13.5px] font-semibold text-ink-2">
              {g.title} <span className="font-normal text-ink-3 tabular-nums">· {rows.length}</span>
            </h3>
            <div className="mt-2 overflow-x-auto rounded-lg border border-line bg-paper">
              <table className="w-full min-w-[620px] text-left text-[13.5px]">
                <thead className="border-b border-line bg-canvas/60 text-[12.5px] text-ink-3">
                  <tr>
                    <th scope="col" className="w-20 py-2 pr-2 pl-4 font-medium">Mã</th>
                    <th scope="col" className="py-2 pr-3 font-medium">Kiểm tra</th>
                    <th scope="col" className="w-32 py-2 pr-3 font-medium">Kết quả</th>
                    <th scope="col" className="w-28 py-2 pr-3 font-medium">Điều khoản</th>
                    <th scope="col" className="w-36 py-2 pr-4 font-medium">Rủi ro</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-line">
                  {rows.map((x) => (
                    <tr key={x.id}>
                      <td className="py-2.5 pr-2 pl-4 text-[12.5px] text-ink-3 tabular-nums">{x.id}</td>
                      <td className="py-2.5 pr-3">{x.name}</td>
                      <td className="py-2.5 pr-3 text-[13px]">
                        <CheckStatusTag status={x.status} />
                      </td>
                      <td className="py-2.5 pr-3 text-[13px]">
                        {x.clauseId ? (
                          <button type="button" onClick={() => onJump(x.clauseId!)} className="font-medium text-primary hover:underline">
                            {clauseLabel(x.clauseId)}
                          </button>
                        ) : (
                          <span className="text-ink-3">Không có</span>
                        )}
                      </td>
                      <td className="py-2.5 pr-4 text-[13px]">
                        {x.findingId ? (
                          <button type="button" onClick={() => onOpenFinding(x.findingId!)} className="font-medium text-primary hover:underline">
                            {x.note ?? `Xem ${x.findingId}`}
                          </button>
                        ) : (
                          <span className="text-ink-3">—</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        )
      })}
    </div>
  )
}
