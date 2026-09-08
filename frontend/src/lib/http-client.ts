import { ApiError, parseApiError } from './api-error'
import type { Session } from './contracts'
import { sessionStore } from './session'
export interface RequestOptions {
    method?: string
    body?: unknown
    signal?: AbortSignal
    anonymous?: boolean
}
export interface IHttpClient {
    request<T>(path: string, options?: RequestOptions): Promise<T>
    download(path: string, fallbackName: string): Promise<void>
}
export class HttpClient implements IHttpClient {
    private refreshing: Promise<void> | null = null
    constructor(
        private readonly baseUrl: string,
        private readonly transport: typeof fetch = (...args) => fetch(...args),
    ) {}
    private async refresh() {
        if (!this.refreshing) {
            const previous = sessionStore.get()
            this.refreshing = (async () => {
                if (!previous) throw new ApiError('Entre para continuar.', 401)
                const response = await this.transport(
                    this.baseUrl + '/api/auth/refresh',
                    {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            refreshToken: previous.refreshToken,
                        }),
                    },
                )
                if (!response.ok) {
                    if (
                        sessionStore.get() === previous &&
                        [400, 401, 403, 409].includes(response.status)
                    )
                        sessionStore.set(null)
                    throw await parseApiError(response)
                }
                const session = (await response.json()) as Session
                if (sessionStore.get() !== previous)
                    throw new ApiError(
                        'A sessão foi alterada. Entre novamente.',
                        401,
                    )
                sessionStore.set(session)
            })().finally(() => {
                this.refreshing = null
            })
        }
        return this.refreshing
    }
    private async send(
        path: string,
        options: RequestOptions = {},
    ): Promise<Response> {
        const send = () =>
            this.transport(this.baseUrl + path, {
                method: options.method ?? 'GET',
                signal: options.signal,
                headers: {
                    ...(options.body
                        ? { 'Content-Type': 'application/json' }
                        : {}),
                    ...(!options.anonymous && sessionStore.get()
                        ? {
                              Authorization: `Bearer ${sessionStore.get()!.accessToken}`,
                          }
                        : {}),
                },
                body: options.body ? JSON.stringify(options.body) : undefined,
            })
        try {
            const token = sessionStore.get()?.accessToken
            let response = await send()
            if (
                response.status === 401 &&
                !options.anonymous &&
                sessionStore.get()
            ) {
                if (sessionStore.get()?.accessToken === token)
                    await this.refresh()
                response = await send()
                if (response.status === 401) sessionStore.set(null)
            }
            if (
                response.status === 401 &&
                options.anonymous &&
                path === '/api/auth/login'
            )
                throw new ApiError('E-mail ou senha incorretos.', 401)
            if (!response.ok) throw await parseApiError(response)
            return response
        } catch (error) {
            if (
                error instanceof ApiError ||
                (error instanceof Error && error.name === 'AbortError')
            )
                throw error
            throw new ApiError(
                'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.',
                0,
            )
        }
    }
    async request<T>(path: string, options: RequestOptions = {}): Promise<T> {
        const response = await this.send(path, options)
        return response.status === 204
            ? (undefined as T)
            : (response.json() as Promise<T>)
    }
    async download(path: string, fallbackName: string) {
        const response = await this.send(path)
        const blob = await response.blob()
        const url = URL.createObjectURL(blob)
        const anchor = document.createElement('a')
        anchor.href = url
        anchor.download = fallbackName
        document.body.appendChild(anchor)
        anchor.click()
        anchor.remove()
        setTimeout(() => URL.revokeObjectURL(url), 1000)
    }
}
export const http = new HttpClient(
    (import.meta.env.VITE_API_URL ?? 'http://localhost:5080').replace(
        /\/$/,
        '',
    ),
)
export function queryString(values: object) {
    const params = new URLSearchParams()
    Object.entries(values).forEach(([key, value]) => {
        if (value !== undefined && value !== '') params.set(key, String(value))
    })
    return params.toString()
}
