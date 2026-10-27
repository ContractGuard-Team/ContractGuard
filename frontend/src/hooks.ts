import { useSyncExternalStore } from 'react'

export function useMediaQuery(query: string) {
  return useSyncExternalStore(
    (cb) => {
      const m = matchMedia(query)
      m.addEventListener('change', cb)
      return () => m.removeEventListener('change', cb)
    },
    () => matchMedia(query).matches,
  )
}

export const prefersReducedMotion = () => matchMedia('(prefers-reduced-motion: reduce)').matches

/** Cuộn tới điều khoản và nháy nền một lần để mắt bắt được vị trí. */
export function jumpToClause(clauseId: string) {
  const el = document.getElementById(`clause-${clauseId}`)
  if (!el) return
  const reduced = prefersReducedMotion()
  el.scrollIntoView({ block: 'center', behavior: reduced ? 'auto' : 'smooth' })
  if (!reduced)
    el.animate([{ backgroundColor: 'oklch(0.9 0.05 200)' }, { backgroundColor: 'transparent' }], {
      duration: 1400,
      delay: 250,
      easing: 'cubic-bezier(0.25, 1, 0.5, 1)',
    })
}
