import { AlertCircle, Inbox } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
export function LoadingState() {
    return (
        <div className="space-y-4" role="status" aria-label="Carregando">
            <Skeleton className="h-14 w-full" />
            <Skeleton className="h-28 w-full" />
            <Skeleton className="h-28 w-full" />
        </div>
    )
}
export function ErrorState({ retry }: { retry: () => void }) {
    return (
        <div className="empty-state" role="alert">
            <AlertCircle />
            <h2>Não foi possível carregar os dados</h2>
            <p>Verifique sua conexão ou tente novamente.</p>
            <Button variant="outline" onClick={retry}>
                Tentar novamente
            </Button>
        </div>
    )
}
export function EmptyState({
    title = 'Nenhum registro encontrado',
    description = 'Os registros aparecerão aqui assim que forem cadastrados.',
}: {
    title?: string
    description?: string
}) {
    return (
        <div className="empty-state">
            <Inbox />
            <h2>{title}</h2>
            <p>{description}</p>
        </div>
    )
}
