import { useEffect, useState, type FormEvent } from 'react'
import { AppShell } from '../components/AppShell'
import { Avatar, Button, Input, Select, cx } from '../components/ui'
import { ORG_NAME } from '../demo/mock'
import { useStore } from '../demo/store'
import { formatDate, type Member } from '../model'

const ROLE = { owner: 'Owner', member: 'Member' }

export default function Members() {
  const { members, invites, me, invite, revokeInvite, setRole, removeMember, toast } = useStore()
  const isOwner = me.role === 'owner'
  const owners = members.filter((m) => m.role === 'owner').length
  const [email, setEmail] = useState('')
  const [role, setNewRole] = useState<Member['role']>('member')
  const [error, setError] = useState('')
  const [confirming, setConfirming] = useState<string | null>(null)
  const [roleError, setRoleError] = useState<{ id: string; text: string } | null>(null)

  useEffect(() => {
    if (!confirming) return
    const t = setTimeout(() => setConfirming(null), 4000)
    return () => clearTimeout(t)
  }, [confirming])

  const onInvite = (e: FormEvent) => {
    e.preventDefault()
    const value = email.trim().toLowerCase()
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) return setError('Nhập một địa chỉ email hợp lệ.')
    if (members.some((m) => m.email === value)) return setError('Người này đã là thành viên của tổ chức.')
    if (invites.some((i) => i.email === value)) return setError('Đã có lời mời đang chờ cho email này.')
    invite(value, role)
    toast(`Đã gửi lời mời tới ${value}`)
    setEmail('')
    setError('')
  }

  return (
    <AppShell>
      <div className="mx-auto max-w-[960px] px-4 py-6 sm:px-8 sm:py-8">
        <h1 className="text-[22px] font-semibold tracking-[-0.01em]">Tổ chức</h1>
        <p className="mt-1 text-[13.5px] text-ink-2">{ORG_NAME}</p>

        {isOwner && (
          <section aria-labelledby="invite-title" className="mt-7">
            <h2 id="invite-title" className="text-[15px] font-semibold">
              Mời thành viên
            </h2>
            <form onSubmit={onInvite} noValidate className="mt-2.5 flex flex-wrap items-start gap-2">
              <div className="min-w-0 flex-1 basis-64">
                <label htmlFor="invite-email" className="sr-only">
                  Email người được mời
                </label>
                <Input
                  id="invite-email"
                  type="email"
                  placeholder="ten@lamsonviet.vn"
                  value={email}
                  aria-invalid={error ? true : undefined}
                  aria-describedby={error ? 'invite-err' : 'invite-hint'}
                  onChange={(e) => {
                    setEmail(e.target.value)
                    setError('')
                  }}
                />
                {error ? (
                  <p id="invite-err" className="mt-1.5 text-[12.5px] text-critical">
                    {error}
                  </p>
                ) : (
                  <p id="invite-hint" className="mt-1.5 text-[12.5px] text-ink-3">
                    Người được mời nhận email kèm link tạo tài khoản.
                  </p>
                )}
              </div>
              <Select aria-label="Vai trò" value={role} onChange={(e) => setNewRole(e.target.value as Member['role'])} className="w-32">
                <option value="member">Member</option>
                <option value="owner">Owner</option>
              </Select>
              <Button type="submit" variant="primary">
                Gửi lời mời
              </Button>
            </form>
          </section>
        )}

        <section aria-labelledby="members-title" className="mt-8">
          <h2 id="members-title" className="text-[15px] font-semibold">
            Thành viên <span className="font-normal text-ink-3 tabular-nums">· {members.length}</span>
          </h2>
          <div className="mt-2.5 overflow-hidden rounded-lg border border-line bg-paper">
            <table className="w-full text-left text-[13.5px]">
              <thead className="border-b border-line bg-canvas/60 text-[12.5px] text-ink-3">
                <tr>
                  <th scope="col" className="w-[42%] py-2.5 pr-3 pl-4 font-medium">
                    Người
                  </th>
                  <th scope="col" className="hidden px-3 py-2.5 font-medium md:table-cell">
                    Chức danh
                  </th>
                  <th scope="col" className="px-3 py-2.5 font-medium">
                    Vai trò
                  </th>
                  <th scope="col" className="hidden px-3 py-2.5 font-medium sm:table-cell">
                    Tham gia
                  </th>
                  <th scope="col" className="py-2.5 pr-4 pl-3">
                    <span className="sr-only">Thao tác</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {members.map((m) => (
                  <tr key={m.id}>
                    <td className="max-w-0 py-3 pr-3 pl-4">
                      <div className="flex items-center gap-3">
                        <Avatar name={m.name} size={32} />
                        <div className="min-w-0">
                          <p className="truncate font-medium">
                            {m.name}
                            {m.id === me.id && <span className="font-normal text-ink-3"> (bạn)</span>}
                          </p>
                          <p className="truncate text-[12.5px] text-ink-3">{m.email}</p>
                        </div>
                      </div>
                    </td>
                    <td className="hidden px-3 py-3 text-ink-2 md:table-cell">{m.title}</td>
                    <td className="px-3 py-3">
                      {isOwner ? (
                        <>
                          <Select
                            aria-label={`Vai trò của ${m.name}`}
                            value={m.role}
                            onChange={(e) => {
                              const next = e.target.value as Member['role']
                              if (m.role === 'owner' && next === 'member' && owners === 1)
                                return setRoleError({ id: m.id, text: 'Tổ chức cần ít nhất một Owner.' })
                              setRoleError(null)
                              setRole(m.id, next)
                              toast(`${m.name} giờ là ${ROLE[next]}`)
                            }}
                            className="w-28 [&_select]:h-8 [&_select]:text-[13px]"
                          >
                            <option value="owner">Owner</option>
                            <option value="member">Member</option>
                          </Select>
                          {roleError?.id === m.id && <p className="mt-1 text-[12px] text-critical">{roleError.text}</p>}
                        </>
                      ) : (
                        ROLE[m.role]
                      )}
                    </td>
                    <td className="hidden px-3 py-3 text-ink-2 tabular-nums sm:table-cell">{formatDate(m.joined)}</td>
                    <td className="py-3 pr-4 pl-3 text-right">
                      {isOwner && m.id !== me.id && (
                        <Button
                          size="sm"
                          variant={confirming === m.id ? 'danger' : 'ghost'}
                          className={cx(confirming === m.id && 'bg-critical-tint')}
                          onClick={() => {
                            if (confirming !== m.id) return setConfirming(m.id)
                            removeMember(m.id)
                            setConfirming(null)
                            toast(`Đã xóa ${m.name} khỏi tổ chức`)
                          }}
                        >
                          {confirming === m.id ? 'Bấm lần nữa để xóa' : 'Xóa'}
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        {invites.length > 0 && (
          <section aria-labelledby="pending-title" className="mt-8">
            <h2 id="pending-title" className="text-[15px] font-semibold">
              Lời mời đang chờ <span className="font-normal text-ink-3 tabular-nums">· {invites.length}</span>
            </h2>
            <ul className="mt-2.5 divide-y divide-line rounded-lg border border-line bg-paper">
              {invites.map((i) => (
                <li key={i.email} className="flex flex-wrap items-center gap-x-4 gap-y-1 px-4 py-3">
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium">{i.email}</p>
                    <p className="text-[12.5px] text-ink-3">
                      {ROLE[i.role]} · gửi ngày {formatDate(i.sentAt)}
                    </p>
                  </div>
                  {isOwner && (
                    <span className="flex gap-1">
                      <Button size="sm" variant="ghost" onClick={() => toast(`Đã gửi lại lời mời tới ${i.email}`)}>
                        Gửi lại
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => {
                          revokeInvite(i.email)
                          toast(`Đã thu hồi lời mời của ${i.email}`)
                        }}
                      >
                        Thu hồi
                      </Button>
                    </span>
                  )}
                </li>
              ))}
            </ul>
          </section>
        )}

        <section aria-labelledby="roles-title" className="mt-8 max-w-[70ch] text-[13px] text-ink-2">
          <h2 id="roles-title" className="text-[13.5px] font-semibold text-ink">
            Vai trò trong tổ chức
          </h2>
          <p className="mt-1.5">
            <strong className="font-medium text-ink">Owner</strong> mời và xóa thành viên, đổi vai trò, sửa cài đặt tổ chức. Tổ chức có thể có nhiều Owner nhưng luôn cần ít nhất một.
          </p>
          <p className="mt-1">
            <strong className="font-medium text-ink">Member</strong> tải hợp đồng lên và làm việc với các hợp đồng được chia sẻ. Hợp đồng mặc định chỉ người tạo thấy, giống Google Drive.
          </p>
        </section>
      </div>
    </AppShell>
  )
}
