import { http, type IHttpClient } from '@/lib/http-client'
import type { Session, User } from '@/lib/contracts'
import { sessionStore } from '@/lib/session'
export interface IAuthService {
    login(email: string, password: string): Promise<Session>
    me(signal?: AbortSignal): Promise<User>
    logout(): Promise<void>
    changePassword(currentPassword: string, newPassword: string): Promise<void>
}
export class AuthService implements IAuthService {
    constructor(private client: IHttpClient) {}
    async login(email: string, password: string) {
        const session = await this.client.request<Session>('/api/auth/login', {
            method: 'POST',
            body: { email, password },
            anonymous: true,
        })
        sessionStore.set(session)
        return session
    }
    me(signal?: AbortSignal) {
        return this.client.request<User>('/api/auth/me', { signal })
    }
    async logout() {
        await this.client.request('/api/auth/logout', { method: 'POST' })
        sessionStore.set(null)
    }
    async changePassword(currentPassword: string, newPassword: string) {
        await this.client.request('/api/auth/change-password', {
            method: 'POST',
            body: { currentPassword, newPassword },
        })
        sessionStore.set(null)
    }
}
export const authService: IAuthService = new AuthService(http)
