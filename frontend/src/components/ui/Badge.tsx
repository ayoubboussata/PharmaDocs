import type { ReactNode } from 'react'

type Tone = 'neutral' | 'success' | 'danger' | 'warning' | 'accent'

const tones: Record<Tone, string> = {
  neutral: 'bg-elevated text-muted',
  success: 'bg-success-soft text-success',
  danger: 'bg-danger-soft text-danger',
  warning: 'bg-warning-soft text-warning',
  accent: 'bg-accent-soft text-accent-text',
}

export function Badge({
  tone = 'neutral',
  children,
  className = '',
}: {
  tone?: Tone
  children: ReactNode
  className?: string
}) {
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium ${tones[tone]} ${className}`}
    >
      {children}
    </span>
  )
}
