import { useQuery } from '@tanstack/react-query'
import type { FieldControl } from '@/components/shared/data-form'
import { userService } from './user-service'
import { Button } from '@/components/ui/button'
export function TechnicianSelect({
    id,
    value,
    onChange,
    disabled,
}: FieldControl) {
    const query = useQuery({
        queryKey: ['users'],
        queryFn: ({ signal }) => userService.list(signal),
    })
    return (
        <>
            <select
                id={id}
                className="select-control"
                disabled={disabled || query.isPending}
                value={value}
                onChange={(e) => onChange(e.target.value)}
            >
                <option value="">
                    {query.isPending
                        ? 'Carregando equipe…'
                        : 'Selecione o técnico'}
                </option>
                {query.data
                    ?.filter((user) => user.active && user.role === 'Tecnico')
                    .map((user) => (
                        <option key={user.id} value={user.id}>
                            {user.name}
                        </option>
                    ))}
            </select>
            {query.isError && (
                <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => void query.refetch()}
                >
                    Tentar carregar equipe
                </Button>
            )}
        </>
    )
}
