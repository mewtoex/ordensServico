import type { OrderStatus } from '@/lib/contracts'
import { statusClasses, statusLabels } from '@/features/orders/order-status'
export function StatusBadge({ status }: { status: OrderStatus }) {
    return (
        <span className={`status-badge ${statusClasses[status]}`}>
            <span className="size-1.5 rounded-full bg-current" />
            {statusLabels[status]}
        </span>
    )
}
