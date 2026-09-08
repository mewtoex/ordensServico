import { http, queryString, type IHttpClient } from '@/lib/http-client'
import type {
    Audit,
    Order,
    OrderFilter,
    OrderStatus,
    Page,
} from '@/lib/contracts'
export interface OrderInput {
    customerId: string
    technicianId: string
    description: string
}
export interface IOrderService {
    list(filter: OrderFilter, signal?: AbortSignal): Promise<Page<Order>>
    get(id: string, signal?: AbortSignal): Promise<Order>
    history(id: string, signal?: AbortSignal): Promise<Audit[]>
    create(input: OrderInput): Promise<Order>
    status(id: string, status: OrderStatus): Promise<Order>
    assign(id: string, technicianId: string): Promise<Order>
    addItem(id: string, catalogItemId: string, quantity: number): Promise<Order>
    quantity(id: string, itemId: string, quantity: number): Promise<Order>
    removeItem(id: string, itemId: string): Promise<void>
    download(id: string, type: 'pdf' | 'summary'): Promise<void>
}
export class OrderService implements IOrderService {
    constructor(private client: IHttpClient) {}
    list(filter: OrderFilter, signal?: AbortSignal) {
        return this.client.request<Page<Order>>(
            '/api/orders?' + queryString(filter),
            { signal },
        )
    }
    get(id: string, signal?: AbortSignal) {
        return this.client.request<Order>('/api/orders/' + id, { signal })
    }
    history(id: string, signal?: AbortSignal) {
        return this.client.request<Audit[]>(`/api/orders/${id}/history`, {
            signal,
        })
    }
    create(input: OrderInput) {
        return this.client.request<Order>('/api/orders', {
            method: 'POST',
            body: input,
        })
    }
    status(id: string, status: OrderStatus) {
        return this.client.request<Order>(`/api/orders/${id}/status`, {
            method: 'PATCH',
            body: { status },
        })
    }
    assign(id: string, technicianId: string) {
        return this.client.request<Order>(`/api/orders/${id}/technician`, {
            method: 'PATCH',
            body: { technicianId },
        })
    }
    addItem(id: string, catalogItemId: string, quantity: number) {
        return this.client.request<Order>(`/api/orders/${id}/items`, {
            method: 'POST',
            body: { catalogItemId, quantity },
        })
    }
    quantity(id: string, itemId: string, quantity: number) {
        return this.client.request<Order>(
            `/api/orders/${id}/items/${itemId}/quantity`,
            { method: 'PATCH', body: { quantity } },
        )
    }
    removeItem(id: string, itemId: string) {
        return this.client.request<void>(`/api/orders/${id}/items/${itemId}`, {
            method: 'DELETE',
        })
    }
    download(id: string, type: 'pdf' | 'summary') {
        return this.client.download(
            `/api/orders/${id}/${type}`,
            `os-${id}.${type === 'pdf' ? 'pdf' : 'txt'}`,
        )
    }
}
export const orderService: IOrderService = new OrderService(http)
