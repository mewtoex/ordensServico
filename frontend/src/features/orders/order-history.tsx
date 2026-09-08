import { useQuery } from '@tanstack/react-query'
import { History } from 'lucide-react'
import { dateTime } from '@/lib/format'
import { ErrorState, LoadingState } from '@/components/shared/resource-state'
import { orderService } from './order-service'
const actions: Record<string, string> = {
    Criacao: 'Ordem criada',
    Status: 'Situação alterada',
    ItemAdicionado: 'Item adicionado',
    ItemRemovido: 'Item removido',
    QuantidadeAlterada: 'Quantidade alterada',
    TecnicoAlterado: 'Técnico reatribuído',
}
export function OrderHistory({ id }: { id: string }) {
    const query = useQuery({
        queryKey: ['history', id],
        queryFn: ({ signal }) => orderService.history(id, signal),
    })
    return (
        <section className="panel">
            <div className="section-heading">
                <h2 className="flex items-center gap-2">
                    <History size={18} />
                    Histórico do atendimento
                </h2>
            </div>
            {query.isPending ? (
                <LoadingState />
            ) : query.isError ? (
                <ErrorState retry={() => void query.refetch()} />
            ) : (
                <ol className="timeline">
                    {query.data.map((entry) => (
                        <li key={entry.id}>
                            <span className="timeline-dot" />
                            <div className="flex flex-wrap justify-between gap-2">
                                <strong>
                                    {actions[entry.action] ?? entry.action}
                                </strong>
                                <time className="text-sm text-muted-foreground">
                                    {dateTime(entry.at)}
                                </time>
                            </div>
                            <p className="mt-1 text-sm text-muted-foreground break-words">
                                {entry.detail}
                            </p>
                            <p className="mt-2 text-sm">{entry.actorName}</p>
                        </li>
                    ))}
                </ol>
            )}
        </section>
    )
}
