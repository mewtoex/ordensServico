import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Plus, Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { PageHeader } from '@/components/shared/page-header'
import { DataTable, type Column } from '@/components/shared/data-table'
import { Pagination } from '@/components/shared/pagination'
import { FormDialog } from '@/components/shared/form-dialog'
import { ConfirmDialog } from '@/components/shared/confirm-dialog'
import { ErrorState, LoadingState } from '@/components/shared/resource-state'
import { customerSchema } from '@/lib/validation'
import type { Customer } from '@/lib/contracts'
import { useAuth } from '@/features/auth/auth-context'
import { customerService } from './customer-service'
const fields = [
    { name: 'name', label: 'Nome', autoComplete: 'name' },
    { name: 'email', label: 'E-mail', type: 'email', autoComplete: 'email' },
    { name: 'phone', label: 'Telefone', type: 'tel', autoComplete: 'tel' },
]
function CustomerEditor({ customer }: { customer?: Customer }) {
    return (
        <FormDialog
            title={customer ? 'Editar cliente' : 'Novo cliente'}
            description="Dados de contato do cliente."
            trigger={
                <Button
                    variant={customer ? 'ghost' : 'default'}
                    size={customer ? 'sm' : 'default'}
                >
                    {!customer && <Plus size={16} />}{' '}
                    {customer ? 'Editar' : 'Novo cliente'}
                </Button>
            }
            fields={fields}
            schema={customerSchema}
            initialValues={{
                name: customer?.name ?? '',
                email: customer?.email ?? '',
                phone: customer?.phone ?? '',
            }}
            submit={(values) =>
                customerService.save(
                    {
                        name: values.name,
                        email: values.email,
                        phone: values.phone,
                    },
                    customer?.id,
                )
            }
            success={customer ? 'Cliente atualizado.' : 'Cliente cadastrado.'}
        />
    )
}
export default function CustomersPage() {
    const { isAdmin } = useAuth()
    const [page, setPage] = useState(1)
    const [search, setSearch] = useState('')
    const query = useQuery({
        queryKey: ['customers', page, search],
        queryFn: ({ signal }) => customerService.list(page, search, signal),
    })
    const columns: Column<Customer>[] = [
        {
            title: 'Cliente',
            render: (row) => <span className="font-medium">{row.name}</span>,
        },
        { title: 'E-mail', render: (row) => row.email },
        { title: 'Telefone', render: (row) => row.phone },
    ]
    if (isAdmin)
        columns.push({
            title: 'Ações',
            className: 'text-right',
            render: (row) => (
                <div className="flex justify-end gap-1">
                    <CustomerEditor customer={row} />
                    <ConfirmDialog
                        label="Excluir"
                        title={`Excluir ${row.name}?`}
                        description="O cadastro sairá das listas. As ordens e o histórico serão preservados."
                        action={() => customerService.remove(row.id)}
                        success="Cliente excluído."
                    />
                </div>
            ),
        })
    return (
        <>
            <PageHeader
                eyebrow="RELACIONAMENTO"
                title="Clientes"
                description={
                    isAdmin
                        ? 'Mantenha os contatos sempre por perto.'
                        : 'Clientes relacionados aos seus atendimentos.'
                }
            >
                {isAdmin && <CustomerEditor />}
            </PageHeader>
            <div className="toolbar">
                <Search size={18} className="text-muted-foreground" />
                <Input
                    className="max-w-sm"
                    aria-label="Buscar clientes"
                    placeholder="Buscar cliente por nome…"
                    value={search}
                    onChange={(e) => {
                        setSearch(e.target.value)
                        setPage(1)
                    }}
                />
            </div>
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
