import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, CalendarDays, UserRound } from 'lucide-react'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { PageHeader } from '@/components/shared/page-header'
import { StatusBadge } from '@/components/shared/status-badge'
import { FormDialog } from '@/components/shared/form-dialog'
import { ErrorState, LoadingState } from '@/components/shared/resource-state'
import { useAuth } from '@/features/auth/auth-context'
import { TechnicianSelect } from '@/features/users/technician-select'
import { dateTime, shortId } from '@/lib/format'
import { orderService } from './order-service'
import { OrderItems } from './order-items'
import { OrderHistory } from './order-history'
import { OrderAssignee } from './order-assignee'
import { OrderActions, OrderDownloads } from './order-actions'
import { transitions } from './order-status'
export default function OrderDetailPage() {
    const { id = '' } = useParams()
    const { isAdmin } = useAuth()
    const query = useQuery({
        queryKey: ['order', id],
        queryFn: ({ signal }) => orderService.get(id, signal),
    })
    if (query.isPending) return <LoadingState />
    if (query.isError) return <ErrorState retry={() => void query.refetch()} />
    const order = query.data
    const editable = transitions[order.status].length > 0
    return (
        <>
            <Link
                className="inline-flex items-center gap-2 text-muted-foreground text-sm mb-5 hover:text-primary"
                to="/orders"
            >
                <ArrowLeft size={16} />
                Todas as ordens
            </Link>
            <PageHeader
                eyebrow={`ORDEM #${shortId(order.id)}`}
                title={order.customerName}
            >
                <OrderDownloads id={id} />
            </PageHeader>
            <section className="panel mb-6">
                <div className="flex flex-wrap items-center gap-4 mb-5">
                    <StatusBadge status={order.status} />
                    <OrderAssignee technicianId={order.technicianId} />
                    <span className="flex items-center gap-2 text-sm text-muted-foreground">
                        <CalendarDays size={16} />
                        {dateTime(order.createdAt)}
                    </span>
                    {order.closedAt && (
                        <span className="text-sm text-muted-foreground">
                            Encerrada em {dateTime(order.closedAt)}
                        </span>
                    )}
                </div>
                <p className="whitespace-pre-wrap break-words mb-6 leading-relaxed">
                    {order.description}
                </p>
                <div className="flex flex-wrap items-center justify-between gap-3 border-t pt-4">
                    <OrderActions order={order} />
                    {isAdmin && editable && (
                        <FormDialog
                            trigger={
                                <Button size="sm" variant="outline">
                                    <UserRound size={16} />
                                    Reatribuir técnico
                                </Button>
                            }
                            title="Reatribuir atendimento"
                            description="O técnico anterior perderá acesso à ordem após a alteração."
                            schema={z.object({
                                technicianId: z
                                    .string()
                                    .uuid('Selecione um técnico.'),
                            })}
                            fields={[
                                {
                                    name: 'technicianId',
                                    label: 'Novo técnico',
                                    render: (props) => (
                                        <TechnicianSelect {...props} />
                                    ),
                                },
                            ]}
                            initialValues={{ technicianId: '' }}
                            submit={(values) =>
                                orderService.assign(id, values.technicianId)
                            }
                            success="Técnico reatribuído."
                        />
                    )}
                </div>
            </section>
            <div className="space-y-6">
                <OrderItems order={order} editable={editable} />
                <OrderHistory id={id} />
            </div>
        </>
    )
}
