import { useSyncExternalStore } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { sessionStore } from '@/lib/session'
import { authService } from './auth-service'
import { AuthContext } from './auth-context'
import { ErrorState, LoadingState } from '@/components/shared/resource-state'
export function ProtectedRoute() {
    const session = useSyncExternalStore(
        sessionStore.subscribe,
        sessionStore.get,
    )
    const location = useLocation()
    const query = useQuery({
        queryKey: ['me', session?.user.id],
        queryFn: ({ signal }) => authService.me(signal),
        enabled: !!session,
    })
    if (!session)
        return (
            <Navigate to="/login" state={{ from: location.pathname }} replace />
        )
    if (query.isPending)
        return (
            <main className="p-10">
                <LoadingState />
            </main>
        )
    if (query.isError)
        return (
            <main className="p-10">
                <ErrorState retry={() => void query.refetch()} />
            </main>
        )
    return (
        <AuthContext.Provider value={query.data}>
            <Outlet />
        </AuthContext.Provider>
    )
}
