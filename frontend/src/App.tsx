import { Navigate, Route, Routes } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { DashboardPage } from './pages/DashboardPage'
import { DocumentsPage } from './pages/DocumentsPage'
import { DocumentDetailPage } from './pages/DocumentDetailPage'
import { AssistantPage } from './pages/AssistantPage'
import { UsersPage } from './pages/UsersPage'
import { OrganizationsPage } from './pages/OrganizationsPage'
import { AppLayout } from './components/AppLayout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AdminRoute } from './components/AdminRoute'
import { SystemAdminRoute } from './components/SystemAdminRoute'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/documents" element={<DocumentsPage />} />
        <Route path="/documents/:id" element={<DocumentDetailPage />} />
        <Route path="/assistant" element={<AssistantPage />} />
        <Route
          path="/users"
          element={
            <AdminRoute>
              <UsersPage />
            </AdminRoute>
          }
        />
        <Route
          path="/organizations"
          element={
            <SystemAdminRoute>
              <OrganizationsPage />
            </SystemAdminRoute>
          }
        />
      </Route>

      <Route path="*" element={<Navigate to="/documents" replace />} />
    </Routes>
  )
}
