import type { ReactNode } from 'react'
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table'
import { EmptyState } from './resource-state'
export interface Column<T> {
    title: string
    render: (row: T) => ReactNode
    className?: string
}
export function DataTable<T extends { id: string }>({
    rows,
    columns,
}: {
    rows: T[]
    columns: Column<T>[]
}) {
    if (!rows.length) return <EmptyState />
    return (
        <div className="rounded-xl border bg-card overflow-hidden">
            <Table>
                <TableHeader>
                    <TableRow>
                        {columns.map((column) => (
                            <TableHead
                                key={column.title}
                                className={column.className}
                            >
                                {column.title}
                            </TableHead>
                        ))}
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {rows.map((row) => (
                        <TableRow key={row.id}>
                            {columns.map((column) => (
                                <TableCell
                                    key={column.title}
                                    className={column.className}
                                >
                                    {column.render(row)}
                                </TableCell>
                            ))}
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </div>
    )
}
