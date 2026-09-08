export const money = (value: number) =>
    new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL',
    }).format(value)
export const dateTime = (value: string) =>
    new Intl.DateTimeFormat('pt-BR', {
        dateStyle: 'short',
        timeStyle: 'short',
    }).format(new Date(value))
export const shortId = (id: string) => id.slice(0, 8).toUpperCase()
export function dateBoundary(value: string, exclusive = false) {
    if (!value) return undefined
    const date = new Date(value + 'T00:00:00Z')
    if (exclusive) date.setUTCDate(date.getUTCDate() + 1)
    return date.toISOString()
}
