import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Search, SlidersHorizontal } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { PageHeader } from '@/components/shared/page-header'
import { DataTable } from '@/components/shared/data-table'
import { Pagination } from '@/components/shared/pagination'
import { ErrorState, LoadingState } from '@/components/shared/resource-state'
import { useAuth } from '@/features/auth/auth-context'
import { dateBoundary } from '@/lib/format'
import { orderService } from './order-service'
import { CreateOrderDialog } from './create-order-dialog'
import { orderColumns } from './order-columns'
import { statusLabels } from './order-status'
export default function OrdersPage() {
    const { isAdmin } = useAuth()
    const [filter, setFilter] = useState({
        page: 1,
        search: '',
        status: '',
        from: '',
        to: '',
    })
    const change = (key: string, value: string) =>
        setFilter((previous) => ({ ...previous, [key]: value, page: 1 }))
    const query = useQuery({
        queryKey: ['orders', filter],
        queryFn: ({ signal }) =>
            orderService.list(
                {
                    ...filter,
                    pageSize: 10,
                    from: dateBoundary(filter.from),
                    to: dateBoundary(filter.to, true),
                },
                signal,
            ),
    })
    return (
        <>
            <PageHeader
                eyebrow="ATENDIMENTOS"
                title="Ordens de serviço"
                description={
                    isAdmin
                        ? 'Acompanhe cada atendimento, do início à entrega.'
                        : 'Seus atendimentos e as próximas etapas.'
                }
            >
                {isAdmin && <CreateOrderDialog />}
            </PageHeader>
            <div className="filter-panel">
                <div className="flex items-center gap-2">
                    <Search size={18} className="text-muted-foreground" />
                    <Input
                        aria-label="Buscar ordens por cliente"
                        placeholder="Buscar por cliente…"
                        value={filter.search}
                        onChange={(e) => change('search', e.target.value)}
                    />
                </div>
                <select
                    className="select-control"
                    aria-label="Filtrar por situação"
                    value={filter.status}
                    onChange={(e) => change('status', e.target.value)}
                >
                    <option value="">Todas as situações</option>
                    {Object.entries(statusLabels).map(([value, label]) => (
                        <option value={value} key={value}>
                            {label}
                        </option>
                    ))}
                </select>
                <label className="filter-date">
                    De
                    <Input
                        aria-label="Data inicial"
                        type="date"
                        value={filter.from}
                        max={filter.to || undefined}
                        onChange={(e) => change('from', e.target.value)}
                    />
                </label>
                <label className="filter-date">
                    Até
                    <Input
                        aria-label="Data final"
                        type="date"
                        value={filter.to}
                        min={filter.from || undefined}
                        onChange={(e) => change('to', e.target.value)}
                    />
                </label>
                <Button
                    variant="ghost"
                    title="Limpar filtros"
                    onClick={() =>
                        setFilter({
                            page: 1,
                            search: '',
                            status: '',
                            from: '',
                            to: '',
                        })
                    }
                >
                    <SlidersHorizontal size={16} />
                    Limpar
                </Button>
            </div>
            {query.isPending ? (
                <LoadingState />
            ) : query.isError ? (
                <ErrorState retry={() => void query.refetch()} />
            ) : (
                <>
                    <DataTable rows={query.data.data} columns={orderColumns} />
                    <Pagination
                        page={filter.page}
                        total={query.data.total}
                        onChange={(page) =>
                            setFilter((previous) => ({ ...previous, page }))
                        }
                    />
                </>
            )}
        </>
    )
}
