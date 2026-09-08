import { useQuery } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { Navigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { PageHeader } from '@/components/shared/page-header'
import { DataTable } from '@/components/shared/data-table'
import { FormDialog } from '@/components/shared/form-dialog'
import { ConfirmDialog } from '@/components/shared/confirm-dialog'
import { ErrorState, LoadingState } from '@/components/shared/resource-state'
import { userSchema, createUserSchema } from '@/lib/validation'
import type { Role, User } from '@/lib/contracts'
import { useAuth } from '@/features/auth/auth-context'
import { userService } from './user-service'
function UserEditor({ user }: { user?: User }) {
    return (
        <FormDialog
            title={user ? 'Editar usuário' : 'Novo usuário'}
            description="Defina o perfil e os dados de acesso da pessoa."
            trigger={
                <Button
                    variant={user ? 'ghost' : 'default'}
                    size={user ? 'sm' : 'default'}
                >
                    {!user && <Plus size={16} />}{' '}
                    {user ? 'Editar' : 'Novo usuário'}
                </Button>
            }
            fields={[
                { name: 'name', label: 'Nome' },
                { name: 'email', label: 'E-mail', type: 'email' },
                {
                    name: 'role',
                    label: 'Perfil',
                    options: [
                        { value: 'Tecnico', label: 'Técnico' },
                        { value: 'Admin', label: 'Administrador' },
                    ],
                },
                ...(!user
                    ? [
                          {
                              name: 'password',
                              label: 'Senha inicial',
                              type: 'password',
                              autoComplete: 'new-password',
                              hint: 'Pelo menos 12 caracteres.',
                          },
                      ]
                    : []),
            ]}
            schema={user ? userSchema : createUserSchema}
            initialValues={{
                name: user?.name ?? '',
                email: user?.email ?? '',
                role: user?.role ?? 'Tecnico',
                password: '',
            }}
            submit={(values) =>
                userService.save(
                    {
                        name: values.name,
                        email: values.email,
                        role: values.role as Role,
                        ...(!user ? { password: values.password } : {}),
                    },
                    user?.id,
                )
            }
            success={user ? 'Usuário atualizado.' : 'Usuário cadastrado.'}
        />
    )
}
export default function UsersPage() {
    const { user, isAdmin } = useAuth()
    const query = useQuery({
        queryKey: ['users'],
        queryFn: ({ signal }) => userService.list(signal),
        enabled: isAdmin,
    })
    if (!isAdmin) return <Navigate to="/" replace />
    return (
        <>
            <PageHeader
                eyebrow="EQUIPE"
                title="Usuários"
                description="Organize a equipe e os níveis de acesso."
            >
                <UserEditor />
            </PageHeader>
            {query.isPending ? (
                <LoadingState />
            ) : query.isError ? (
                <ErrorState retry={() => void query.refetch()} />
            ) : (
                <DataTable
                    rows={query.data}
                    columns={[
                        {
                            title: 'Nome',
                            render: (row) => (
                                <span className="font-medium">
                                    {row.name}
                                    {row.id === user.id && (
                                        <span className="ml-2 text-xs text-muted-foreground">
                                            Você
                                        </span>
                                    )}
                                </span>
                            ),
                        },
                        { title: 'E-mail', render: (row) => row.email },
                        {
                            title: 'Perfil',
                            render: (row) =>
                                row.role === 'Admin'
                                    ? 'Administrador'
                                    : 'Técnico',
                        },
                        {
                            title: 'Acesso',
                            render: (row) => (
                                <span
                                    className={`status-badge ${row.active ? 'bg-emerald-50 text-emerald-800' : 'bg-slate-100 text-slate-600'}`}
                                >
                                    {row.active ? 'Ativo' : 'Desativado'}
                                </span>
                            ),
                        },
                        {
                            title: 'Ações',
                            className: 'text-right',
                            render: (row) => (
                                <div className="flex justify-end gap-1">
                                    <UserEditor user={row} />
                                    <ConfirmDialog
                                        disabled={row.id === user.id}
                                        label={
                                            row.active ? 'Desativar' : 'Ativar'
                                        }
                                        title={`${row.active ? 'Desativar' : 'Ativar'} ${row.name}?`}
                                        description={
                                            row.active
                                                ? 'O usuário perderá acesso. Seus registros de atendimento serão preservados.'
                                                : 'O usuário poderá entrar novamente com sua senha.'
                                        }
                                        destructive={row.active}
                                        action={() =>
                                            userService.setActive(
                                                row.id,
                                                !row.active,
                                            )
                                        }
                                        success="Acesso atualizado."
                                    />
                                </div>
                            ),
                        },
                    ]}
                />
            )}
        </>
    )
}
