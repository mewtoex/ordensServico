import type { ReactNode } from 'react'
export function PageHeader({
    eyebrow,
    title,
    description,
    children,
}: {
    eyebrow?: string
    title: string
    description?: string
    children?: ReactNode
}) {
    return (
        <div className="page-header">
            <div>
                {eyebrow && <p className="eyebrow">{eyebrow}</p>}
                <h1>{title}</h1>
                {description && (
                    <p className="mt-2 text-muted-foreground">{description}</p>
                )}
            </div>
            <div className="flex flex-wrap items-center gap-2">{children}</div>
        </div>
    )
}
