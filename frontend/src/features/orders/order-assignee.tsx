import { useQuery } from '@tanstack/react-query'
import { UserRound } from 'lucide-react'
import { useAuth } from '@/features/auth/auth-context'
import { userService } from '@/features/users/user-service'
export function OrderAssignee({ technicianId }: { technicianId: string }) {
    const { user, isAdmin } = useAuth()
    const query = useQuery({
        queryKey: ['users'],
        queryFn: ({ signal }) => userService.list(signal),
        enabled: isAdmin,
    })
    const name = isAdmin
        ? (query.data?.find((person) => person.id === technicianId)?.name ??
          (query.isPending ? 'Carregando…' : 'Não disponível'))
        : user.name
    return (
        <span className="flex items-center gap-2 text-sm text-muted-foreground">
            <UserRound size={16} />
            Responsável: {name}
        </span>
    )
}
