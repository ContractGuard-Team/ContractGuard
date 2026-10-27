import { X } from 'lucide-react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { Avatar, Button, Input, Select } from '../../components/ui'
import { ORG_SHORT } from '../../demo/mock'
import { useStore } from '../../demo/store'
import type { Contract } from '../../model'

/** Chia sẻ theo email kiểu Google Drive: Owner hợp đồng thêm/bớt Member, hoặc mở cho cả tổ chức (ACC-06..08). */
export function ShareDialog({ contract: c, open, onClose }: { contract: Contract; open: boolean; onClose: () => void }) {
  const ref = useRef<HTMLDialogElement>(null)
  const { members, me, share, updateShare, toast } = useStore()
  const s = share(c.id)
  const owner = members.find((m) => m.id === c.ownerId)
  const isOwner = c.ownerId === me.id
  const [email, setEmail] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    const d = ref.current
    if (!d) return
    if (open && !d.open) d.showModal()
    if (!open && d.open) d.close()
  }, [open])

  const add = (e: FormEvent) => {
    e.preventDefault()
    const value = email.trim().toLowerCase()
    const m = members.find((x) => x.email === value)
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) return setError('Nhập một địa chỉ email hợp lệ.')
    if (!m) return setError('notmember')
    if (m.id === c.ownerId || s.memberIds.includes(m.id)) return setError(`${m.name} đã có quyền truy cập.`)
    updateShare(c.id, { memberIds: [...s.memberIds, m.id] })
    toast(`Đã chia sẻ với ${m.name}`)
    setEmail('')
    setError('')
  }

  return (
    <dialog
      ref={ref}
      onClose={onClose}
      onClick={(e) => e.target === ref.current && onClose()}
      aria-labelledby="share-title"
      className="m-auto w-[min(520px,calc(100vw-32px))] rounded-lg bg-paper p-0 text-ink shadow-2xl"
    >
      <div className="px-6 pt-5 pb-6">
        <div className="flex items-start gap-3">
          <h2 id="share-title" className="flex-1 text-[16px] leading-snug font-semibold">
            Chia sẻ “{c.title}”
          </h2>
          <Button variant="ghost" size="sm" className="-mt-1 -mr-2 size-8 px-0" aria-label="Đóng" onClick={onClose}>
            <X aria-hidden className="size-4" />
          </Button>
        </div>

        {isOwner ? (
          <form onSubmit={add} noValidate className="mt-4">
            <label htmlFor="share-email" className="text-[13px] font-medium">
              Thêm người bằng email
            </label>
            <div className="mt-1.5 flex gap-2">
              <Input
                id="share-email"
                type="email"
                list="org-emails"
                placeholder="ten@lamsonviet.vn"
                value={email}
                aria-invalid={error ? true : undefined}
                aria-describedby={error ? 'share-err' : undefined}
                onChange={(e) => {
                  setEmail(e.target.value)
                  setError('')
                }}
              />
              <datalist id="org-emails">
                {members.filter((m) => m.id !== c.ownerId && !s.memberIds.includes(m.id)).map((m) => (
                  <option key={m.id} value={m.email}>
                    {m.name}
                  </option>
                ))}
              </datalist>
              <Button type="submit" variant="primary">
                Thêm
              </Button>
            </div>
            {error && (
              <p id="share-err" className="mt-1.5 text-[12.5px] text-critical">
                {error === 'notmember' ? (
                  <>
                    Email này chưa thuộc {ORG_SHORT}. Owner tổ chức cần{' '}
                    <Link to="/to-chuc" className="font-medium underline">
                      mời người này vào tổ chức
                    </Link>{' '}
                    trước.
                  </>
                ) : (
                  error
                )}
              </p>
            )}
          </form>
        ) : (
          <p className="mt-3 rounded-md bg-canvas px-3 py-2 text-[13px] text-ink-2">Chỉ Owner của hợp đồng ({owner?.name}) mới thêm hoặc bớt người được.</p>
        )}

        <h3 className="mt-6 text-[13px] font-semibold">Người có quyền truy cập</h3>
        <ul className="mt-2 flex flex-col">
          {[c.ownerId, ...s.memberIds].map((id) => {
            const m = members.find((x) => x.id === id)
            if (!m) return null
            return (
              <li key={id} className="flex items-center gap-3 py-2">
                <Avatar name={m.name} size={32} />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-[13.5px] font-medium">
                    {m.name}
                    {m.id === me.id && <span className="font-normal text-ink-3"> (bạn)</span>}
                  </p>
                  <p className="truncate text-[12.5px] text-ink-3">{m.email}</p>
                </div>
                {id === c.ownerId ? (
                  <span className="text-[12.5px] text-ink-2">Owner</span>
                ) : (
                  <span className="flex items-center gap-1">
                    <span className="text-[12.5px] text-ink-2">Member</span>
                    {isOwner && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="size-8 px-0"
                        aria-label={`Thu hồi quyền của ${m.name}`}
                        onClick={() => {
                          updateShare(c.id, { memberIds: s.memberIds.filter((x) => x !== id) })
                          toast(`Đã thu hồi quyền của ${m.name}`)
                        }}
                      >
                        <X aria-hidden className="size-4" />
                      </Button>
                    )}
                  </span>
                )}
              </li>
            )
          })}
        </ul>

        <div className="mt-5 border-t border-line pt-4">
          <label htmlFor="share-general" className="text-[13px] font-semibold">
            Quyền truy cập chung
          </label>
          <Select
            id="share-general"
            className="mt-1.5"
            disabled={!isOwner}
            value={s.orgWide ? 'org' : 'restricted'}
            onChange={(e) => {
              updateShare(c.id, { orgWide: e.target.value === 'org' })
              toast(e.target.value === 'org' ? `Mọi người trong ${ORG_SHORT} đều xem được` : 'Chỉ những người được thêm mới xem được')
            }}
          >
            <option value="restricted">Chỉ những người được thêm</option>
            <option value="org">Mọi người trong {ORG_SHORT}</option>
          </Select>
          <p className="mt-1.5 text-[12.5px] text-ink-3">Member xem, hỏi đáp, xuất báo cáo, tải phiên bản mới và cập nhật trạng thái. Chỉ Owner chia sẻ và xóa được hợp đồng.</p>
        </div>

        <div className="mt-5 flex justify-end">
          <Button variant="primary" onClick={onClose}>
            Xong
          </Button>
        </div>
      </div>
    </dialog>
  )
}
