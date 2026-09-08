import { http, queryString, type IHttpClient } from '@/lib/http-client'
import type { CatalogItem, Page } from '@/lib/contracts'
export type CatalogInput = Omit<CatalogItem, 'id'>
export interface ICatalogService {
    list(page: number, signal?: AbortSignal): Promise<Page<CatalogItem>>
    save(input: CatalogInput, id?: string): Promise<CatalogItem>
    remove(id: string): Promise<void>
}
export class CatalogService implements ICatalogService {
    constructor(private client: IHttpClient) {}
    list(page: number, signal?: AbortSignal) {
        return this.client.request<Page<CatalogItem>>(
            '/api/catalog?' + queryString({ page, pageSize: 10 }),
            { signal },
        )
    }
    save(input: CatalogInput, id?: string) {
        return this.client.request<CatalogItem>(
            '/api/catalog' + (id ? '/' + id : ''),
            { method: id ? 'PUT' : 'POST', body: input },
        )
    }
    remove(id: string) {
        return this.client.request<void>('/api/catalog/' + id, {
            method: 'DELETE',
        })
    }
}
export const catalogService: ICatalogService = new CatalogService(http)
