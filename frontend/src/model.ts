// Kiểu dữ liệu và nhãn hiển thị. Bám theo docs/01-product-spec.md (mục 6, 7).

export type Severity = 'critical' | 'high' | 'medium' | 'low'
export const SEVERITIES: Severity[] = ['critical', 'high', 'medium', 'low']
export const SEVERITY_LABEL: Record<Severity, string> = {
  critical: 'Nghiêm trọng',
  high: 'Cao',
  medium: 'Trung bình',
  low: 'Thấp',
}

/** Ngưỡng mức rủi ro theo spec mục 7.6. */
export function levelOf(score: number): Severity {
  if (score >= 85) return 'critical'
  if (score >= 55) return 'high'
  if (score >= 30) return 'medium'
  return 'low'
}

export type Domain = 'legal' | 'financial' | 'operational' | 'compliance' | 'cyber' | 'privacy' | 'thirdparty'
export const DOMAINS: { id: Domain; label: string; question: string }[] = [
  { id: 'legal', label: 'Pháp lý', question: 'Khi có chuyện, mình chịu trách nhiệm đến đâu và giải quyết ở đâu?' },
  { id: 'financial', label: 'Tài chính', question: 'Mình trả bao nhiêu, khi nào, và có thể mất thêm tiền ở đâu?' },
  { id: 'operational', label: 'Vận hành', question: 'Đối tác có giao đúng thứ mình cần, đúng hạn, đúng chất lượng?' },
  { id: 'compliance', label: 'Tuân thủ', question: 'Hợp đồng có khiến mình vi phạm nghĩa vụ với cơ quan nhà nước?' },
  { id: 'cyber', label: 'An ninh mạng', question: 'Hệ thống và dữ liệu của mình có được bảo vệ khi đối tác bị tấn công?' },
  { id: 'privacy', label: 'Dữ liệu & quyền riêng tư', question: 'Dữ liệu giao cho đối tác thuộc về ai, dùng vào việc gì, đi đâu?' },
  { id: 'thirdparty', label: 'Bên thứ ba', question: 'Ngoài đối tác, còn ai khác chạm vào dịch vụ hoặc dữ liệu?' },
]
export const DOMAIN_LABEL = Object.fromEntries(DOMAINS.map((d) => [d.id, d.label])) as Record<Domain, string>

export type ReviewStatus = 'analyzing' | 'review' | 'negotiating' | 'approved' | 'rejected' | 'failed'
export const STATUS_LABEL: Record<ReviewStatus, string> = {
  analyzing: 'Đang phân tích',
  review: 'Chờ review',
  negotiating: 'Đang đàm phán',
  approved: 'Đã duyệt',
  rejected: 'Từ chối',
  failed: 'Lỗi phân tích',
}
/** Trạng thái người dùng được chọn tay (CON-05). */
export const MANUAL_STATUSES: ReviewStatus[] = ['review', 'negotiating', 'approved', 'rejected']

export type FindingState = 'open' | 'negotiate' | 'accepted' | 'dismissed' | 'resolved'
export const FINDING_STATE_LABEL: Record<FindingState, string> = {
  open: 'Chưa xử lý',
  negotiate: 'Cần đàm phán',
  accepted: 'Chấp nhận rủi ro',
  dismissed: 'Không đúng',
  resolved: 'Đã xử lý',
}

export type CheckStatus = 'pass' | 'partial' | 'missing' | 'na'
export const CHECK_STATUS_LABEL: Record<CheckStatus, string> = {
  pass: 'Đạt',
  partial: 'Một phần',
  missing: 'Thiếu',
  na: 'Không áp dụng',
}

export interface Clause {
  id: string // "9.1" hoặc "8" khi điều chỉ có một khoản
  text: string
}

export interface Article {
  number: string
  title: string
  page: number
  clauses: Clause[]
}

export interface Finding {
  id: string
  title: string
  kind: 'issue' | 'missing'
  domain: Domain
  secondary?: Domain[]
  severity: Severity
  clauseId?: string
  /** Đoạn trích nguyên văn trong điều khoản, dùng để tô sáng. */
  quote?: string
  /** Với finding loại "thiếu điều khoản": đã tìm gì mà không thấy. */
  missingWhat?: string
  why: string
  impact: string
  recommendation: string
  reference?: string
}

export interface Check {
  id: string
  name: string
  status: CheckStatus
  clauseId?: string
  findingId?: string
  note?: string
}

export interface ChatScript {
  q: string
  keywords: string[]
  /** Trích dẫn dạng [[9.1]] sẽ hiển thị thành nút nhảy tới điều khoản. */
  a: string
}

export interface Party {
  label: string
  name: string
  rep: string
}

export interface ContractDetail {
  heading: string
  number: string
  preamble: string
  partyA: Party
  partyB: Party
  articles: Article[]
  findings: Finding[]
  checks: Check[]
  chat: ChatScript[]
  chatFallback: string
}

export interface ContractContext {
  ourSide: 'buyer' | 'seller'
  provides: string
  dataAccess: 'none' | 'normal' | 'personal'
  criticality: 'low' | 'medium' | 'high'
}

export interface AnalysisResult {
  score: number
  domainScores: Record<Domain, number | null> // null = không áp dụng
  naReason?: string
  counts: Record<Severity, number>
  clauseCount: number
}

export interface Version {
  v: number
  date: string
  by: string
  score?: number
  note?: string
}

export interface Activity {
  at: string
  by: string
  text: string
}

export interface Contract {
  id: string
  title: string
  partner: string
  type: string
  value?: string
  pages: number
  updatedAt: string
  ownerId: string
  status: ReviewStatus
  context: ContractContext
  result?: AnalysisResult
  error?: string
  detail?: ContractDetail
  versions: Version[]
  activity: Activity[]
}

export interface Member {
  id: string
  name: string
  email: string
  role: 'owner' | 'member'
  title: string
  joined: string
}

export interface Invite {
  email: string
  role: 'owner' | 'member'
  sentAt: string
}

export interface Share {
  memberIds: string[]
  orgWide: boolean
}

const dateFmt = new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
const timeFmt = new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit' })
export const formatDate = (iso: string) => dateFmt.format(new Date(iso))
export const formatDateTime = (iso: string) => `${timeFmt.format(new Date(iso))}, ${dateFmt.format(new Date(iso))}`

/** Chữ cái đầu của họ và tên: "Phạm Đức Anh" -> "PA". */
export const initials = (name: string) => {
  const w = name.trim().split(/\s+/)
  return (w[0][0] + (w.length > 1 ? w[w.length - 1][0] : '')).toUpperCase()
}

/** "9.1" -> "Điều 9.1" */
export const clauseLabel = (id: string) => `Điều ${id}`

/** Câu khuyến nghị tổng (VIEW-01). */
export function verdict(counts: Record<Severity, number>): string {
  const parts = []
  if (counts.critical) parts.push(`${counts.critical} rủi ro nghiêm trọng`)
  if (counts.high) parts.push(`${counts.high} rủi ro cao`)
  if (!parts.length) return 'Không có rủi ro nghiêm trọng hoặc cao. Có thể duyệt sau khi xem qua các điểm còn lại.'
  return `Không nên duyệt khi chưa đàm phán lại ${parts.join(' và ')}.`
}
