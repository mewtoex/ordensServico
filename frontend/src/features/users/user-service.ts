import { http, type IHttpClient } from '@/lib/http-client'
import type { User } from '@/lib/contracts'
export type UserInput = Pick<User, 'name' | 'email' | 'role'> & {
    password?: string
}
export interface IUserService {
    list(signal?: AbortSignal): Promise<User[]>
    save(input: UserInput, id?: string): Promise<User>
    setActive(id: string, active: boolean): Promise<void>
}
export class UserService implements IUserService {
    constructor(private client: IHttpClient) {}
    list(signal?: AbortSignal) {
        return this.client.request<User[]>('/api/users', { signal })
    }
    save(input: UserInput, id?: string) {
        return this.client.request<User>('/api/users' + (id ? '/' + id : ''), {
            method: id ? 'PUT' : 'POST',
            body: input,
        })
    }
    setActive(id: string, active: boolean) {
        return this.client.request<void>(
            `/api/users/${id}/active?active=${active}`,
            { method: 'PATCH' },
        )
    }
}
export const userService: IUserService = new UserService(http)
