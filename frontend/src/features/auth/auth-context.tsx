import { createContext, useContext } from 'react'
import type { User } from '@/lib/contracts'
export const AuthContext = createContext<User | null>(null)
export function useAuth() {
    const user = useContext(AuthContext)
    if (!user) throw new Error('Área autenticada necessária.')
    return { user, isAdmin: user.role === 'Admin' }
}
