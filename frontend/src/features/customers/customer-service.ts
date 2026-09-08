import { http, queryString, type IHttpClient } from '@/lib/http-client'
import type { Customer, Page } from '@/lib/contracts'
export type CustomerInput = Omit<Customer, 'id'>
export interface ICustomerService {
    list(
        page: number,
        search?: string,
        signal?: AbortSignal,
    ): Promise<Page<Customer>>
    save(input: CustomerInput, id?: string): Promise<Customer>
    remove(id: string): Promise<void>
}
export class CustomerService implements ICustomerService {
    constructor(private client: IHttpClient) {}
    list(page: number, search = '', signal?: AbortSignal) {
        return this.client.request<Page<Customer>>(
            '/api/customers?' + queryString({ page, search, pageSize: 10 }),
            { signal },
        )
    }
    save(input: CustomerInput, id?: string) {
        return this.client.request<Customer>(
            '/api/customers' + (id ? '/' + id : ''),
            { method: id ? 'PUT' : 'POST', body: input },
        )
    }
    remove(id: string) {
        return this.client.request<void>('/api/customers/' + id, {
            method: 'DELETE',
        })
    }
}
export const customerService: ICustomerService = new CustomerService(http)
