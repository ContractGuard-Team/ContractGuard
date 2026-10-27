// shortcut: state giữ trong bộ nhớ trình duyệt thay cho backend, mất khi tải lại trang. Thay bằng gọi API khi có service C#.
import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { STATUS_LABEL, type Contract, type ContractContext, type FindingState, type Invite, type Member, type ReviewStatus, type Share } from '../model'
import { CLOUD_DETAIL, CLOUD_RESULT, CONTRACTS, CURRENT_USER_ID, INITIAL_FINDING_STATES, INVITES, MEMBERS, SHARES, SOW_RESULT } from './mock'

/** Mỗi bước giả lập tiến trình phân tích kéo dài bao lâu (ms). */
export const ANALYSIS_STEP_MS = 2200
export const ANALYSIS_STEPS = [
  'Đọc tệp và nhận dạng văn bản',
  'Tách và phân loại điều khoản',
  'Đánh giá rủi ro theo 7 nhóm',
  'Chấm điểm và tổng hợp',
]

type FindingEntry = { state: FindingState; note: string }
type Toast = { id: number; text: string }

export interface NewContractInput {
  title: string
  partner: string
  type: string
  value?: string
  fileName: string
  context: ContractContext
}

interface Store {
  contracts: Contract[]
  members: Member[]
  invites: Invite[]
  me: Member
  analysisStart: Record<string, number>
  toasts: Toast[]
  getContract: (id: string) => Contract | undefined
  findingEntry: (cid: string, fid: string) => FindingEntry
  setFinding: (cid: string, fid: string, patch: Partial<FindingEntry>) => void
  setStatus: (cid: string, status: ReviewStatus) => void
  share: (cid: string) => Share
  updateShare: (cid: string, patch: Partial<Share>) => void
  createContract: (input: NewContractInput) => string
  reanalyze: (cid: string) => void
  completeAnalysis: (cid: string) => void
  invite: (email: string, role: Invite['role']) => void
  revokeInvite: (email: string) => void
  setRole: (memberId: string, role: Member['role']) => void
  removeMember: (memberId: string) => void
  toast: (text: string) => void
}

const Ctx = createContext<Store | null>(null)

const nowIso = () => new Date().toISOString()

export function StoreProvider({ children }: { children: ReactNode }) {
  const [contracts, setContracts] = useState(CONTRACTS)
  const [members, setMembers] = useState(MEMBERS)
  const [invites, setInvites] = useState(INVITES)
  const [findings, setFindings] = useState(INITIAL_FINDING_STATES)
  const [shares, setShares] = useState(SHARES)
  // Hợp đồng SOW trong thư viện bắt đầu "đang phân tích" từ lúc mở trang.
  const [analysisStart, setAnalysisStart] = useState<Record<string, number>>(() => ({ 'hd-sow-erp': Date.now() }))
  const [toasts, setToasts] = useState<Toast[]>([])
  const me = members.find((m) => m.id === CURRENT_USER_ID)!

  const toast = useCallback((text: string) => {
    const id = Date.now() + Math.random()
    setToasts((t) => [...t, { id, text }])
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 3500)
  }, [])

  const patchContract = useCallback((cid: string, fn: (c: Contract) => Contract) => {
    setContracts((list) => list.map((c) => (c.id === cid ? fn(c) : c)))
  }, [])

  const store = useMemo<Store>(
    () => ({
      contracts,
      members,
      invites,
      me,
      analysisStart,
      toasts,
      toast,
      getContract: (id) => contracts.find((c) => c.id === id),
      findingEntry: (cid, fid) => findings[`${cid}:${fid}`] ?? { state: 'open', note: '' },
      setFinding: (cid, fid, patch) =>
        setFindings((f) => {
          const key = `${cid}:${fid}`
          return { ...f, [key]: { ...(f[key] ?? { state: 'open', note: '' }), ...patch } }
        }),
      setStatus: (cid, status) =>
        patchContract(cid, (c) => ({
          ...c,
          status,
          updatedAt: nowIso(),
          activity: [{ at: nowIso(), by: me.name, text: `chuyển trạng thái sang ${STATUS_LABEL[status]}` }, ...c.activity],
        })),
      share: (cid) => shares[cid] ?? { memberIds: [], orgWide: false },
      updateShare: (cid, patch) => setShares((s) => ({ ...s, [cid]: { ...(s[cid] ?? { memberIds: [], orgWide: false }), ...patch } })),
      createContract: (input) => {
        const id = `hd-moi-${Date.now()}`
        const contract: Contract = {
          id,
          title: input.title,
          partner: input.partner,
          type: input.type,
          value: input.value,
          pages: 5,
          updatedAt: nowIso(),
          ownerId: me.id,
          status: 'analyzing',
          context: input.context,
          versions: [{ v: 1, date: nowIso(), by: me.name, note: input.fileName }],
          activity: [{ at: nowIso(), by: me.name, text: `tải lên phiên bản V1 (${input.fileName})` }],
        }
        setContracts((list) => [contract, ...list])
        setAnalysisStart((a) => ({ ...a, [id]: Date.now() }))
        return id
      },
      reanalyze: (cid) => {
        patchContract(cid, (c) => ({ ...c, status: 'analyzing' }))
        setAnalysisStart((a) => ({ ...a, [cid]: Date.now() }))
      },
      completeAnalysis: (cid) =>
        patchContract(cid, (c) => {
          if (c.status !== 'analyzing') return c
          // Hợp đồng mới tải lên trong demo dùng lại kết quả của hợp đồng mẫu.
          const isUpload = cid.startsWith('hd-moi-')
          const result = c.result ?? (isUpload ? CLOUD_RESULT : SOW_RESULT)
          return {
            ...c,
            status: 'review',
            result,
            detail: c.detail ?? (isUpload ? CLOUD_DETAIL : undefined),
            updatedAt: nowIso(),
            versions: c.versions.map((v, i) => (i === 0 ? { ...v, score: result.score } : v)),
            activity: [{ at: nowIso(), by: 'Hệ thống', text: `phân tích xong: ${result.score}/100` }, ...c.activity],
          }
        }),
      invite: (email, role) => setInvites((list) => [{ email, role, sentAt: nowIso() }, ...list]),
      revokeInvite: (email) => setInvites((list) => list.filter((i) => i.email !== email)),
      setRole: (memberId, role) => setMembers((list) => list.map((m) => (m.id === memberId ? { ...m, role } : m))),
      removeMember: (memberId) => setMembers((list) => list.filter((m) => m.id !== memberId)),
    }),
    [contracts, members, invites, me, analysisStart, toasts, toast, findings, shares, patchContract],
  )

  return <Ctx.Provider value={store}>{children}</Ctx.Provider>
}

export function useStore() {
  const s = useContext(Ctx)
  if (!s) throw new Error('useStore phải nằm trong StoreProvider')
  return s
}
