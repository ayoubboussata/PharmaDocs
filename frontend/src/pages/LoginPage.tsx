import { useNavigate } from 'react-router-dom'
import { AuthForm } from '../components/AuthForm'
import { useAuth } from '../auth/AuthContext'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()

  return (
    <AuthForm
      title="Log in op je account"
      submitLabel="Inloggen"
      onSubmit={async (email, password) => {
        await login(email, password)
        navigate('/documents')
      }}
      note="Account nodig? Vraag een beheerder om er een aan te maken."
    />
  )
}
