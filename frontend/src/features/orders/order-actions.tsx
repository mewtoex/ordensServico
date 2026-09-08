import { Download, FileText } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/components/shared/confirm-dialog'
import { useAction } from '@/hooks/use-action'
import type { Order, OrderStatus } from '@/lib/contracts'
import { transitions, statusLabels } from './order-status'
import { orderService } from './order-service'
export function OrderDownloads({ id }: { id: string }) {
    const download = useAction(
        (type: 'pdf' | 'summary') => orderService.download(id, type),
        'Comprovante preparado para download.',
    )
    return (
        <>
            <Button
                variant="outline"
                disabled={download.isPending}
                onClick={() => download.mutate('summary')}
            >
                <FileText size={16} />
                Resumo TXT
            </Button>
            <Button
                variant="outline"
                disabled={download.isPending}
                onClick={() => download.mutate('pdf')}
            >
                <Download size={16} />
                Baixar PDF
            </Button>
        </>
    )
}
export function OrderActions({ order }: { order: Order }) {
    const mutation = useAction(
        (status: OrderStatus) => orderService.status(order.id, status),
        'Situação da ordem atualizada.',
    )
    const next = transitions[order.status]
    if (!next.length)
        return (
            <p className="text-sm text-muted-foreground">
                Este atendimento foi encerrado. Os registros estão disponíveis
                para consulta.
            </p>
        )
    return (
        <div className="flex flex-wrap gap-2">
            {next.map((status) =>
                status === 'Cancelada' || status === 'Concluida' ? (
                    <ConfirmDialog
                        key={status}
                        label={
                            status === 'Cancelada'
                                ? 'Cancelar ordem'
                                : 'Concluir atendimento'
                        }
                        title={
                            status === 'Cancelada'
                                ? 'Cancelar esta ordem?'
                                : 'Concluir este atendimento?'
                        }
                        description="Após encerrar, não será possível alterar itens, técnico ou situação desta ordem."
                        destructive={status === 'Cancelada'}
                        action={() => orderService.status(order.id, status)}
                        success="Ordem encerrada."
                    />
                ) : (
                    <Button
                        key={status}
                        size="sm"
                        disabled={mutation.isPending}
                        onClick={() => mutation.mutate(status)}
                    >
                        {status === 'EmAndamento'
                            ? 'Iniciar / retomar atendimento'
                            : statusLabels[status]}
                    </Button>
                ),
            )}
        </div>
    )
}
