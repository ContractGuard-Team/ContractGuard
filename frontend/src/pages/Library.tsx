import { Plus, Search } from 'lucide-react'
import { useMemo } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { AppShell, EmptyState } from '../components/AppShell'
import { SeverityGlyph, StatusBadge, SEV_TEXT } from '../components/risk'
import { Avatar, Button, Input, Select, buttonClass, cx } from '../components/ui'
import { useStore } from '../demo/store'
import { SEVERITIES, SEVERITY_LABEL, STATUS_LABEL, formatDate, levelOf, type Contract, type ReviewStatus } from '../model'

const SORTS = {
  moi: { label: 'Mới cập nhật', fn: (a: Contract, b: Contract) => b.updatedAt.localeCompare(a.updatedAt) },
  'rui-ro': { label: 'Rủi ro cao nhất', fn: (a: Contract, b: Contract) => (b.result?.score ?? -1) - (a.result?.score ?? -1) },
  ten: { label: 'Tên A–Z', fn: (a: Contract, b: Contract) => a.title.localeCompare(b.title, 'vi') },
}

export default function Library() {
  const { contracts, members } = useStore()
  const [params, setParams] = useSearchParams()
  const navigate = useNavigate()
  const q = params.get('q') ?? ''
  const level = params.get('muc') ?? ''
  const status = params.get('trang-thai') ?? ''
  const type = params.get('loai') ?? ''
  const sort = (params.get('sap-xep') ?? 'moi') as keyof typeof SORTS
  const filtered = Boolean(q || level || status || type)

  const set = (key: string, value: string) =>
    setParams(
      (p) => {
        if (value) p.set(key, value)
        else p.delete(key)
        return p
      },
      { replace: true },
    )

  const types = useMemo(() => [...new Set(contracts.map((c) => c.type))].sort((a, b) => a.localeCompare(b, 'vi')), [contracts])

  const rows = useMemo(() => {
    const needle = q.trim().toLocaleLowerCase('vi')
    return contracts
      .filter(
        (c) =>
          (!needle || `${c.title} ${c.partner}`.toLocaleLowerCase('vi').includes(needle)) &&
          (!level || (c.result && levelOf(c.result.score) === level)) &&
          (!status || c.status === status) &&
          (!type || c.type === type),
      )
      .sort(SORTS[sort]?.fn ?? SORTS.moi.fn)
  }, [contracts, q, level, status, type, sort])

  const waiting = contracts.filter((c) => c.status === 'review').length
  const urgent = contracts.filter((c) => c.result && c.result.counts.critical > 0 && ['review', 'negotiating'].includes(c.status)).length
  const running = contracts.filter((c) => c.status === 'analyzing').length

  return (
    <AppShell>
      <div className="mx-auto max-w-[1200px] px-4 py-6 sm:px-8 sm:py-8">
        <header className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="text-[22px] font-semibold tracking-[-0.01em]">Hợp đồng</h1>
            <p className="mt-1 text-[13.5px] text-ink-2">
              {contracts.length} hợp đồng.{' '}
              {waiting > 0 && (
                <>
                  <button type="button" className="font-medium text-primary hover:underline" onClick={() => set('trang-thai', 'review')}>
                    {waiting} đang chờ review
                  </button>
                  .{' '}
                </>
              )}
              {urgent > 0 && <>{urgent} hợp đồng đang xử lý còn rủi ro nghiêm trọng. </>}
              {running > 0 && <>{running} đang được phân tích.</>}
            </p>
          </div>
          <Link to="/hop-dong/moi" className={cx(buttonClass('primary'), 'md:hidden')}>
            <Plus aria-hidden className="size-4" />
            Tải lên
          </Link>
        </header>

        <div role="search" className="mt-5 flex flex-wrap items-center gap-2">
          <div className="relative w-full sm:w-72">
            <Search aria-hidden className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-ink-3" />
            <Input
              type="search"
              aria-label="Tìm theo tên hợp đồng hoặc đối tác"
              placeholder="Tìm theo tên hoặc đối tác"
              value={q}
              onChange={(e) => set('q', e.target.value)}
              className="pl-9"
            />
          </div>
          <Select aria-label="Lọc theo mức rủi ro" value={level} onChange={(e) => set('muc', e.target.value)} className="w-[calc(50%-4px)] sm:w-40">
            <option value="">Mọi mức rủi ro</option>
            {SEVERITIES.map((s) => (
              <option key={s} value={s}>
                {SEVERITY_LABEL[s]}
              </option>
            ))}
          </Select>
          <Select aria-label="Lọc theo trạng thái" value={status} onChange={(e) => set('trang-thai', e.target.value)} className="w-[calc(50%-4px)] sm:w-40">
            <option value="">Mọi trạng thái</option>
            {(Object.keys(STATUS_LABEL) as ReviewStatus[]).map((s) => (
              <option key={s} value={s}>
                {STATUS_LABEL[s]}
              </option>
            ))}
          </Select>
          <Select aria-label="Lọc theo loại hợp đồng" value={type} onChange={(e) => set('loai', e.target.value)} className="w-[calc(50%-4px)] sm:w-40">
            <option value="">Mọi loại</option>
            {types.map((t) => (
              <option key={t}>{t}</option>
            ))}
          </Select>
          <Select aria-label="Sắp xếp theo" value={sort} onChange={(e) => set('sap-xep', e.target.value === 'moi' ? '' : e.target.value)} className="w-[calc(50%-4px)] sm:ml-auto sm:w-56">
            {Object.entries(SORTS).map(([k, v]) => (
              <option key={k} value={k}>
                {v.label}
              </option>
            ))}
          </Select>
          {filtered && (
            <Button variant="ghost" size="sm" onClick={() => setParams({}, { replace: true })}>
              Xóa bộ lọc
            </Button>
          )}
        </div>

        <div className="mt-4 overflow-hidden rounded-lg border border-line bg-paper">
          {rows.length === 0 ? (
            <EmptyState
              title="Không có hợp đồng nào khớp bộ lọc"
              action={
                <Button onClick={() => setParams({}, { replace: true })} variant="secondary">
                  Xóa bộ lọc
                </Button>
              }
            >
              Thử bỏ bớt điều kiện lọc hoặc tìm bằng tên đối tác.
            </EmptyState>
          ) : (
            <table className="w-full text-left text-[13.5px]">
              <caption className="sr-only">Danh sách hợp đồng</caption>
              <thead className="border-b border-line bg-canvas/60 text-[12.5px] whitespace-nowrap text-ink-3">
                <tr>
                  <th scope="col" className="py-2.5 pr-3 pl-4 font-medium">
                    Hợp đồng
                  </th>
                  <th scope="col" className="hidden px-3 py-2.5 font-medium lg:table-cell">
                    Loại
                  </th>
                  <th scope="col" className="px-3 py-2.5 font-medium">
                    Rủi ro
                  </th>
                  <th scope="col" className="hidden px-3 py-2.5 font-medium xl:table-cell">
                    Cần chú ý
                  </th>
                  <th scope="col" className="hidden px-3 py-2.5 font-medium sm:table-cell">
                    Trạng thái
                  </th>
                  <th scope="col" className="hidden px-3 py-2.5 font-medium md:table-cell">
                    Cập nhật
                  </th>
                  <th scope="col" className="hidden py-2.5 pr-4 pl-3 font-medium lg:table-cell">
                    Phụ trách
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {rows.map((c) => {
                  const owner = members.find((m) => m.id === c.ownerId)
                  return (
                    <tr
                      key={c.id}
                      onClick={(e) => {
                        if (!(e.target as HTMLElement).closest('a')) navigate(`/hop-dong/${c.id}`)
                      }}
                      className="cursor-pointer transition-colors duration-150 hover:bg-canvas/70"
                    >
                      <td className="max-w-0 py-3 pr-3 pl-4">
                        <Link to={`/hop-dong/${c.id}`} className="line-clamp-2 rounded font-medium text-ink hover:underline sm:block sm:truncate">
                          {c.title}
                        </Link>
                        <p className="mt-0.5 truncate text-[12.5px] text-ink-3">
                          {c.partner}
                          {c.value && <span className="hidden sm:inline"> · {c.value}</span>}
                        </p>
                        <StatusBadge status={c.status} className="mt-1.5 sm:hidden" />
                      </td>
                      <td className="hidden px-3 py-3 whitespace-nowrap text-ink-2 lg:table-cell">{c.type}</td>
                      <td className="px-3 py-3 whitespace-nowrap">
                        <RiskCell contract={c} />
                      </td>
                      <td className="hidden px-3 py-3 xl:table-cell">
                        {c.result && c.result.counts.critical + c.result.counts.high > 0 ? (
                          <span className="flex items-center gap-3 text-[12.5px] whitespace-nowrap text-ink-2 tabular-nums">
                            {(['critical', 'high'] as const)
                              .filter((s) => c.result!.counts[s] > 0)
                              .map((s) => (
                                <span key={s} className="inline-flex items-center gap-1">
                                  <SeverityGlyph severity={s} />
                                  {c.result!.counts[s]}
                                  <span className="sr-only">{SEVERITY_LABEL[s].toLowerCase()}</span>
                                </span>
                              ))}
                          </span>
                        ) : (
                          <span className="text-ink-3">—</span>
                        )}
                      </td>
                      <td className="hidden px-3 py-3 sm:table-cell">
                        <StatusBadge status={c.status} />
                      </td>
                      <td className="hidden px-3 py-3 text-ink-2 tabular-nums md:table-cell">{formatDate(c.updatedAt)}</td>
                      <td className="hidden py-3 pr-4 pl-3 lg:table-cell">
                        {owner && (
                          <span className="flex items-center gap-2 text-ink-2">
                            <Avatar name={owner.name} size={22} />
                            <span className="truncate">{owner.name}</span>
                          </span>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </AppShell>
  )
}

function RiskCell({ contract: c }: { contract: Contract }) {
  if (c.status === 'analyzing')
    return (
      <span className="flex flex-col gap-1.5 text-[12.5px] text-ink-2">
        Đang phân tích
        <span className="block h-1 w-20 overflow-hidden rounded-full bg-line">
          <span className="skeleton block h-full w-full bg-primary/40" />
        </span>
      </span>
    )
  if (c.status === 'failed' || !c.result) return <span className="text-[12.5px] text-ink-3">Chưa có kết quả</span>
  const lvl = levelOf(c.result.score)
  return (
    <span className="inline-flex items-center gap-2">
      <SeverityGlyph severity={lvl} />
      <span className="font-semibold tabular-nums">{c.result.score}</span>
      <span className={cx('text-[12.5px] max-sm:sr-only', SEV_TEXT[lvl])}>{SEVERITY_LABEL[lvl]}</span>
    </span>
  )
}
