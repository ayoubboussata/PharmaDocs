import { Component, type ErrorInfo, type ReactNode } from 'react'

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
}

/**
 * Vangt render-fouten in de component-boom op zodat één kapotte pagina niet de
 * hele app zwart maakt; toont een nette fallback met een herlaadknop.
 */
export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // In productie zou dit naar een monitoring-dienst gaan; voorlopig de console.
    console.error('Onverwachte UI-fout:', error, info.componentStack)
  }

  render() {
    if (!this.state.hasError) return this.props.children

    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-canvas px-6 text-center">
        <div>
          <h1 className="text-lg font-semibold text-fg">Er ging iets mis</h1>
          <p className="mt-1 text-sm text-muted">
            Er is een onverwachte fout opgetreden. Herlaad de pagina om verder te gaan.
          </p>
        </div>
        <button
          onClick={() => window.location.reload()}
          className="rounded-lg bg-accent px-4 py-2 text-sm text-accent-fg transition-colors hover:bg-accent-hover"
        >
          Pagina herladen
        </button>
      </div>
    )
  }
}
