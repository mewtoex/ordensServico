import { Link } from 'react-router-dom'
import { ArrowUpRight } from 'lucide-react'
import type { Column } from '@/components/shared/data-table'
import { StatusBadge } from '@/components/shared/status-badge'
import type { Order } from '@/lib/contracts'
import { dateTime, money, shortId } from '@/lib/format'
export const orderColumns: Column<Order>[] = [
    {
        title: 'Ordem / cliente',
        render: (row) => (
            <Link to={'/orders/' + row.id} className="block group">
                <span className="font-semibold group-hover:text-primary">
                    {row.customerName}
                </span>
                <span className="block mt-1 text-xs font-mono text-muted-foreground">
                    OS #{shortId(row.id)}
                </span>
            </Link>
        ),
    },
    { title: 'Situação', render: (row) => <StatusBadge status={row.status} /> },
    {
        title: 'Abertura',
        render: (row) => (
            <span className="text-muted-foreground">
                {dateTime(row.createdAt)}
            </span>
        ),
    },
    {
        title: 'Total',
        render: (row) => (
            <span className="font-medium tabular-nums">{money(row.total)}</span>
        ),
    },
    {
        title: 'Detalhes',
        className: 'text-right',
        render: (row) => (
            <Link
                className="inline-flex items-center gap-1 text-primary font-medium"
                to={'/orders/' + row.id}
            >
                Abrir
                <ArrowUpRight size={16} />
            </Link>
        ),
    },
]
