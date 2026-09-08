import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
export function Pagination({
    page,
    total,
    pageSize = 10,
    onChange,
}: {
    page: number
    total: number
    pageSize?: number
    onChange: (page: number) => void
}) {
    const pages = Math.max(1, Math.ceil(total / pageSize))
    return (
        <nav aria-label="Paginação" className="pagination">
            <span>
                {total} registro{total !== 1 ? 's' : ''} · Página {page} de{' '}
                {pages}
            </span>
            <div className="flex gap-2">
                <Button
                    variant="outline"
                    size="sm"
                    disabled={page <= 1}
                    onClick={() => onChange(page - 1)}
                    aria-label="Página anterior"
                >
                    <ChevronLeft size={16} />
                    Anterior
                </Button>
                <Button
                    variant="outline"
                    size="sm"
                    disabled={page >= pages}
                    onClick={() => onChange(page + 1)}
                    aria-label="Próxima página"
                >
                    Próxima
                    <ChevronRight size={16} />
                </Button>
            </div>
        </nav>
    )
}
