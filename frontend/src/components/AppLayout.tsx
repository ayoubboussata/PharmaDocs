import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'

/** App-shell: vaste zijbalk links, scrollbare inhoud rechts. */
export function AppLayout() {
  return (
    <div className="flex min-h-screen bg-canvas text-fg">
      <Sidebar />
      <main className="min-w-0 flex-1">
        <Outlet />
      </main>
    </div>
  )
}
