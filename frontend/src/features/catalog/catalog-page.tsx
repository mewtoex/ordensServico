import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { PageHeader } from '@/components/shared/page-header'
import { DataTable, type Column } from '@/components/shared/data-table'
import { Pagination } from '@/components/shared/pagination'
import { FormDialog } from '@/components/shared/form-dialog'
import { ConfirmDialog } from '@/components/shared/confirm-dialog'
import { ErrorState, LoadingState } from '@/components/shared/resource-state'
import { catalogSchema } from '@/lib/validation'
import { money } from '@/lib/format'
import type { CatalogItem, ItemKind } from '@/lib/contracts'
import { useAuth } from '@/features/auth/auth-context'
import { catalogService } from './catalog-service'
function CatalogEditor({ item }: { item?: CatalogItem }) {
    return (
        <FormDialog
            title={item ? 'Editar item' : 'Novo serviço ou peça'}
            description="O preço atualizado será utilizado nas próximas inclusões em ordens."
            trigger={
                <Button
                    variant={item ? 'ghost' : 'default'}
                    size={item ? 'sm' : 'default'}
                >
                    {!item && <Plus size={16} />}{' '}
                    {item ? 'Editar' : 'Novo item'}
                </Button>
            }
            fields={[
                { name: 'name', label: 'Nome' },
                {
                    name: 'kind',
                    label: 'Tipo',
                    options: [
                        { value: 'Servico', label: 'Serviço / mão de obra' },
                        { value: 'Peca', label: 'Peça / material' },
                    ],
                },
                { name: 'price', label: 'Preço (R$)', type: 'number' },
            ]}
            schema={catalogSchema}
            initialValues={{
                name: item?.name ?? '',
                kind: item?.kind ?? 'Servico',
                price: item ? String(item.price) : '',
            }}
            submit={(values) =>
                catalogService.save(
                    {
                        name: values.name,
                        kind: values.kind as ItemKind,
                        price: Number(values.price),
                    },
                    item?.id,
                )
            }
            success={item ? 'Item atualizado.' : 'Item cadastrado.'}
        />
    )
}
export default function CatalogPage() {
    const { isAdmin } = useAuth()
    const [page, setPage] = useState(1)
    const query = useQuery({
        queryKey: ['catalog', page],
        queryFn: ({ signal }) => catalogService.list(page, signal),
    })
    const columns: Column<CatalogItem>[] = [
        {
            title: 'Descrição',
            render: (row) => <span className="font-medium">{row.name}</span>,
        },
        {
            title: 'Tipo',
            render: (row) => (row.kind === 'Servico' ? 'Serviço' : 'Peça'),
        },
        {
            title: 'Preço unitário',
            render: (row) => (
                <span className="tabular-nums">{money(row.price)}</span>
            ),
        },
    ]
    if (isAdmin)
        columns.push({
            title: 'Ações',
            className: 'text-right',
            render: (row) => (
                <div className="flex justify-end gap-1">
                    <CatalogEditor item={row} />
                    <ConfirmDialog
                        label="Excluir"
                        title={`Excluir ${row.name}?`}
                        description="O item sairá do catálogo. Valores e registros de ordens anteriores serão preservados."
                        action={() => catalogService.remove(row.id)}
                        success="Item excluído do catálogo."
                    />
                </div>
            ),
        })
    return (
        <>
            <PageHeader
                eyebrow="RECURSOS"
                title="Serviços e peças"
                description="O catálogo que dá suporte aos seus atendimentos."
            >
                {isAdmin && <CatalogEditor />}
            </PageHeader>
            {query.isPending ? (
                <LoadingState />
            ) : query.isError ? (
                <ErrorState retry={() => void query.refetch()} />
            ) : (
                <>
                    <DataTable rows={query.data.data} columns={columns} />
                    <Pagination
                        page={page}
                        total={query.data.total}
                        onChange={setPage}
                    />
                </>
            )}
        </>
    )
}
