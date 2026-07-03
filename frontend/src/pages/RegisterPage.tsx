import { useNavigate } from 'react-router-dom'
import { AuthForm } from '../components/AuthForm'
import { useAuth } from '../auth/AuthContext'

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()

  return (
    <AuthForm
      title="Maak een nieuw account"
      submitLabel="Registreren"
      onSubmit={async (email, password) => {
        await register(email, password)
        navigate('/documents')
      }}
      footer={{ text: 'Al een account?', linkText: 'Log in', to: '/login' }}
    />
  )
}
