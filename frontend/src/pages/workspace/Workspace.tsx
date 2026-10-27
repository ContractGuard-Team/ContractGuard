import { MessageSquareText, ShieldAlert, Users, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router'
import { AppShell, EmptyState } from '../../components/AppShell'
import { StatusBadge } from '../../components/risk'
import { Avatar, Button, Select, TabList, buttonClass, cx } from '../../components/ui'
import { useStore } from '../../demo/store'
import { jumpToClause, useMediaQuery } from '../../hooks'
import { MANUAL_STATUSES, STATUS_LABEL, formatDate, type Domain, type ReviewStatus } from '../../model'
import { AnalysisFailed, AnalysisProgress } from './AnalysisProgress'
import { ChatPanel } from './ChatPanel'
import { Checklist } from './Checklist'
import { DocumentView } from './DocumentView'
import { ExportMenu } from './ExportMenu'
import { FindingsPanel } from './FindingsPanel'
import { History } from './History'
import { Overview } from './Overview'
import { ShareDialog } from './ShareDialog'

type Tab = 'van-ban' | 'tong-quan' | 'checklist' | 'lich-su'
type Side = 'rui-ro' | 'hoi-dap'

export default function Workspace() {
  const { id = '' } = useParams()
  const { getContract, setStatus, findingEntry, members, share, toast } = useStore()
  const c = getContract(id)
  const [params, setParams] = useSearchParams()
  const wide = useMediaQuery('(min-width: 1280px)')
  const [side, setSide] = useState<Side>('rui-ro')
  const [drawer, setDrawer] = useState(false)
  const [shareOpen, setShareOpen] = useState(false)
  const [domain, setDomain] = useState<Domain | ''>('')

  useEffect(() => {
    if (!drawer) return
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setDrawer(false)
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [drawer])

  if (!c)
    return (
      <AppShell compact>
        <EmptyState
          title="Không tìm thấy hợp đồng"
          action={
            <Link to="/hop-dong" className={buttonClass('secondary')}>
              Về thư viện hợp đồng
            </Link>
          }
        >
          Hợp đồng có thể đã bị xóa hoặc bạn chưa được chia sẻ.
        </EmptyState>
      </AppShell>
    )

  const detail = c.detail
  const tab = (params.get('tab') as Tab | null) ?? (detail ? 'van-ban' : 'tong-quan')
  const selectedId = params.get('f')
  const ready = c.status !== 'analyzing' && c.status !== 'failed' && c.result

  const update = (fn: (p: URLSearchParams) => void) =>
    setParams(
      (p) => {
        fn(p)
        return p
      },
      { replace: true },
    )
  const setTab = (t: Tab) => update((p) => p.set('tab', t))
  const selectFinding = (fid: string | null) => {
    update((p) => {
      p.set('tab', 'van-ban')
      if (fid) p.set('f', fid)
      else p.delete('f')
    })
    setSide('rui-ro')
    if (fid && !wide) setDrawer(true)
  }
  /** Cuộn tới điều khoản; đóng ngăn kéo trên màn hình hẹp để thấy văn bản. */
  const showClause = (clauseId: string, closeDrawer = true) => {
    if (tab !== 'van-ban') setTab('van-ban')
    if (closeDrawer) setDrawer(false)
    setTimeout(() => jumpToClause(clauseId), tab === 'van-ban' ? 0 : 80)
  }
  /** Mở chi tiết một rủi ro từ tab khác và đưa văn bản tới đúng điều khoản. */
  const openFinding = (fid: string) => {
    selectFinding(fid)
    const clauseId = detail?.findings.find((f) => f.id === fid)?.clauseId
    if (clauseId) showClause(clauseId, false)
  }

  const s = share(c.id)
  const people = [c.ownerId, ...s.memberIds].map((mid) => members.find((m) => m.id === mid)).filter((m) => m !== undefined)
  const owner = members.find((m) => m.id === c.ownerId)

  const sidePanel = detail && (
    <>
      <div className="flex shrink-0 items-center justify-between border-b border-line px-2">
        <TabList
          label="Bảng bên phải"
          idPrefix="side"
          value={side}
          onChange={setSide}
          tabs={[
            {
              id: 'rui-ro',
              label: (
                <>
                  <ShieldAlert aria-hidden className="size-4" />
                  Rủi ro <span className="text-ink-3 tabular-nums">{detail.findings.length}</span>
                </>
              ),
            },
            {
              id: 'hoi-dap',
              label: (
                <>
                  <MessageSquareText aria-hidden className="size-4" />
                  Hỏi đáp
                </>
              ),
            },
          ]}
        />
        {!wide && (
          <Button variant="ghost" size="sm" className="size-8 px-0" aria-label="Đóng bảng" onClick={() => setDrawer(false)}>
            <X aria-hidden className="size-4" />
          </Button>
        )}
      </div>
      <div id="side-panel-rui-ro" role="tabpanel" aria-labelledby="side-tab-rui-ro" className={cx('min-h-0 flex-1 flex-col', side === 'rui-ro' ? 'flex' : 'hidden')}>
        <FindingsPanel
          contractId={c.id}
          findings={detail.findings}
          selectedId={selectedId}
          onSelect={selectFinding}
          onJump={(cid) => showClause(cid, false)}
          onShowClause={(cid) => showClause(cid)}
          domain={domain}
          onDomain={setDomain}
        />
      </div>
      <div id="side-panel-hoi-dap" role="tabpanel" aria-labelledby="side-tab-hoi-dap" className={cx('min-h-0 flex-1 flex-col', side === 'hoi-dap' ? 'flex' : 'hidden')}>
        <ChatPanel detail={detail} onJump={(cid) => showClause(cid)} />
      </div>
    </>
  )

  const noDetail = (
    // shortcut: chỉ 2 hợp đồng mẫu có văn bản đầy đủ; xóa khi dữ liệu lấy từ API.
    <EmptyState
      title="Bản demo chưa có văn bản của hợp đồng này"
      action={
        <span className="flex flex-wrap justify-center gap-2">
          <Link to="/hop-dong/hd-cloud" className={buttonClass('secondary')}>
            Mở hợp đồng cloud mẫu
          </Link>
          <Link to="/hop-dong/hd-noi-that" className={buttonClass('secondary')}>
            Mở hợp đồng nội thất mẫu
          </Link>
        </span>
      }
    >
      Chỉ hai hợp đồng mẫu có văn bản đầy đủ. Tab Tổng quan và Lịch sử vẫn xem được.
    </EmptyState>
  )

  return (
    <AppShell compact>
      <div className="flex h-[calc(100dvh-48px)] flex-col md:h-dvh">
        <header className="shrink-0 border-b border-line bg-paper px-4 sm:px-6">
          <div className="flex flex-wrap items-start gap-x-4 gap-y-2 pt-3">
            <div className="min-w-0 flex-1 basis-[320px]">
              <nav aria-label="Breadcrumb" className="text-[12.5px] text-ink-3">
                <Link to="/hop-dong" className="hover:text-ink hover:underline">
                  Hợp đồng
                </Link>
              </nav>
              <h1 className="truncate text-[18px] leading-snug font-semibold" title={c.title}>
                {c.title}
              </h1>
              <p className="truncate text-[12.5px] text-ink-2">
                {c.partner} · {c.type} · V{c.versions[0].v} · cập nhật {formatDate(c.updatedAt)}
                {owner && ` · phụ trách ${owner.name}`}
              </p>
            </div>
            <div className="flex items-center gap-2">
              {ready ? (
                <Select
                  aria-label="Trạng thái duyệt"
                  value={c.status}
                  onChange={(e) => {
                    setStatus(c.id, e.target.value as ReviewStatus)
                    toast(`Đã chuyển sang ${STATUS_LABEL[e.target.value as ReviewStatus]}`)
                  }}
                  className="w-[152px] [&_select]:h-9 [&_select]:font-medium"
                >
                  {MANUAL_STATUSES.map((st) => (
                    <option key={st} value={st}>
                      {STATUS_LABEL[st]}
                    </option>
                  ))}
                </Select>
              ) : (
                <StatusBadge status={c.status} />
              )}
              <Button onClick={() => setShareOpen(true)}>
                <span className="flex -space-x-1.5" aria-hidden>
                  {people.slice(0, 3).map((m) => (
                    <Avatar key={m.id} name={m.name} size={20} className="ring-2 ring-paper" />
                  ))}
                </span>
                {s.orgWide ? <Users aria-hidden className="size-4 text-ink-3" /> : null}
                <span className="max-sm:sr-only">Chia sẻ</span>
              </Button>
              <ExportMenu contract={c} />
            </div>
          </div>

          {ready ? (
            <div className="mt-1 flex items-end justify-between gap-2">
              <TabList
                label="Phần của hợp đồng"
                idPrefix="ws"
                className="-mx-2.5 [scrollbar-width:none] overflow-x-auto overflow-y-hidden"
                value={tab}
                onChange={setTab}
                tabs={[
                  { id: 'van-ban', label: 'Văn bản' },
                  { id: 'tong-quan', label: 'Tổng quan' },
                  { id: 'checklist', label: 'Checklist bảo mật' },
                  { id: 'lich-su', label: 'Lịch sử' },
                ]}
              />
              {tab === 'van-ban' && detail && !wide && (
                <div className="flex shrink-0 gap-1 pb-1.5">
                  <Button
                    size="sm"
                    variant="secondary"
                    onClick={() => {
                      setSide('rui-ro')
                      setDrawer(true)
                    }}
                  >
                    <ShieldAlert aria-hidden className="size-4" />
                    <span className="max-sm:sr-only">Rủi ro</span>
                    <span className="tabular-nums text-ink-3">{detail.findings.length}</span>
                  </Button>
                  <Button
                    size="sm"
                    variant="secondary"
                    onClick={() => {
                      setSide('hoi-dap')
                      setDrawer(true)
                    }}
                  >
                    <MessageSquareText aria-hidden className="size-4" />
                    <span className="max-sm:sr-only">Hỏi đáp</span>
                  </Button>
                </div>
              )}
            </div>
          ) : (
            <div className="h-3" />
          )}
        </header>

        {/* shortcut: hợp đồng tải lên trong demo hiển thị kết quả của hợp đồng mẫu; xóa khi có backend. */}
        {c.id.startsWith('hd-moi-') && ready && (
          <p className="shrink-0 border-b border-line bg-primary-soft px-4 py-2 text-[12.5px] text-primary-ink sm:px-6">
            Bản demo: kết quả bên dưới lấy từ hợp đồng mẫu, không phải từ tệp bạn vừa tải lên.
          </p>
        )}

        <div className="min-h-0 flex-1">
          {c.status === 'analyzing' && <AnalysisProgress contract={c} />}
          {c.status === 'failed' && <AnalysisFailed contract={c} />}
          {ready && (
            <div id={`ws-panel-${tab}`} role="tabpanel" aria-labelledby={`ws-tab-${tab}`} className="h-full">
              {tab === 'van-ban' &&
                (detail ? (
                  <div className="flex h-full">
                    <div className="min-w-0 flex-1 overflow-y-auto">
                      <DocumentView detail={detail} selectedId={selectedId} onSelect={selectFinding} stateOf={(fid) => findingEntry(c.id, fid).state} />
                    </div>
                    {wide ? (
                      <aside aria-label="Rủi ro và hỏi đáp" className="flex w-[384px] shrink-0 flex-col border-l border-line bg-paper">
                        {sidePanel}
                      </aside>
                    ) : (
                      <>
                        {drawer && (
                          <div aria-hidden onClick={() => setDrawer(false)} className="fixed inset-0 bg-ink/25" style={{ zIndex: 'var(--z-drawer)' }} />
                        )}
                        <aside
                          aria-label="Rủi ro và hỏi đáp"
                          inert={!drawer}
                          className={cx(
                            'fixed inset-y-0 right-0 flex w-[min(400px,100vw)] flex-col bg-paper shadow-2xl transition-transform duration-200 ease-out-quart',
                            drawer ? 'translate-x-0' : 'translate-x-full',
                          )}
                          style={{ zIndex: 'var(--z-drawer)' }}
                        >
                          {sidePanel}
                        </aside>
                      </>
                    )}
                  </div>
                ) : (
                  noDetail
                ))}
              {tab !== 'van-ban' && (
                <div className="h-full overflow-y-auto">
                  {tab === 'tong-quan' && (
                    <Overview
                      contract={c}
                      onOpenFinding={openFinding}
                      onOpenDomain={(d) => {
                        setDomain(d)
                        selectFinding(null)
                        if (!wide) setDrawer(true)
                      }}
                    />
                  )}
                  {tab === 'checklist' && (detail ? <Checklist contract={c} onJump={(cid) => showClause(cid)} onOpenFinding={openFinding} /> : noDetail)}
                  {tab === 'lich-su' && <History contract={c} />}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
      <ShareDialog contract={c} open={shareOpen} onClose={() => setShareOpen(false)} />
    </AppShell>
  )
}
