import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import type { Page } from '@/lib/contracts'
import type { FieldControl } from './data-form'
export interface LookupOption {
    id: string
    label: string
}
export function PagedLookup({
    id,
    value,
    onChange,
    disabled,
    queryKey,
    fetchPage,
    searchable = false,
}: FieldControl & {
    queryKey: string
    fetchPage: (
        page: number,
        search: string,
        signal: AbortSignal,
    ) => Promise<Page<LookupOption>>
    searchable?: boolean
}) {
    const [page, setPage] = useState(1)
    const [search, setSearch] = useState('')
    const [selected, setSelected] = useState<LookupOption | null>(null)
    const query = useQuery({
        queryKey: ['lookup', queryKey, page, search],
        queryFn: ({ signal }) => fetchPage(page, search, signal),
    })
    const options = query.data?.data ?? []
    return (
        <div className="space-y-2">
            {searchable && (
                <Input
                    aria-label="Buscar opções por nome"
                    placeholder="Buscar por nome…"
                    value={search}
                    onChange={(e) => {
                        setSearch(e.target.value)
                        setPage(1)
                    }}
                />
            )}
            <select
                id={id}
                className="select-control"
                value={value}
                disabled={disabled || query.isPending}
                onChange={(e) => {
                    setSelected(
                        options.find(
                            (option) => option.id === e.target.value,
                        ) ?? null,
                    )
                    onChange(e.target.value)
                }}
            >
                <option value="">
                    {query.isPending ? 'Carregando…' : 'Selecione uma opção'}
                </option>
                {selected &&
                    !options.some((option) => option.id === selected.id) && (
                        <option value={selected.id}>{selected.label}</option>
                    )}
                {options.map((option) => (
                    <option key={option.id} value={option.id}>
                        {option.label}
                    </option>
                ))}
            </select>
            {query.isError ? (
                <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => void query.refetch()}
                >
                    Tentar carregar novamente
                </Button>
            ) : (
                <div className="flex items-center justify-between text-sm text-muted-foreground">
                    <span>
                        {query.data?.total ?? 0} opções · Página {page}
                    </span>
                    <div className="flex gap-1">
                        <Button
                            type="button"
                            size="sm"
                            variant="ghost"
                            disabled={page === 1 || query.isFetching}
                            onClick={() => setPage(page - 1)}
                        >
                            Anterior
                        </Button>
                        <Button
                            type="button"
                            size="sm"
                            variant="ghost"
                            disabled={
                                !query.data ||
                                page * query.data.pageSize >=
                                    query.data.total ||
                                query.isFetching
                            }
                            onClick={() => setPage(page + 1)}
                        >
                            Próxima
                        </Button>
                    </div>
                </div>
            )}
        </div>
    )
}
