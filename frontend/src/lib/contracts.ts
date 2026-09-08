export type Role = 'Admin' | 'Tecnico'
export type OrderStatus =
    'Aberta' | 'EmAndamento' | 'AguardandoPeca' | 'Concluida' | 'Cancelada'
export type ItemKind = 'Servico' | 'Peca'
export interface User {
    id: string
    name: string
    email: string
    role: Role
    active: boolean
}
export interface Session {
    accessToken: string
    expiresAt: string
    refreshToken: string
    refreshExpiresAt: string
    user: User
}
export interface Customer {
    id: string
    name: string
    email: string
    phone: string
}
export interface CatalogItem {
    id: string
    name: string
    kind: ItemKind
    price: number
}
export interface OrderItem {
    id: string
    catalogItemId: string
    name: string
    kind: ItemKind
    quantity: number
    unitPrice: number
    subtotal: number
}
export interface Order {
    id: string
    customerId: string
    customerName: string
    technicianId: string
    description: string
    status: OrderStatus
    createdAt: string
    closedAt: string | null
    total: number
    items: OrderItem[]
}
export interface Audit {
    id: string
    actorId: string
    actorName: string
    action: string
    detail: string
    at: string
}
export interface MonthlyReport {
    year: number
    month: number
    created: number
    completed: number
    revenue: number
}
export interface Page<T> {
    total: number
    page: number
    pageSize: number
    data: T[]
}
export interface OrderFilter {
    page?: number
    pageSize?: number
    search?: string
    customerId?: string
    status?: string
    from?: string
    to?: string
}
