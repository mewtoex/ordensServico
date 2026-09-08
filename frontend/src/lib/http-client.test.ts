import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { HttpClient } from './http-client'
import { sessionStore } from './session'
import { json, session } from '@/test/fixtures'
describe('HTTP client', () => {
    beforeEach(() => sessionStore.set(session))
    afterEach(() => sessionStore.set(null))
    it('renova apenas uma vez quando duas requisições recebem 401', async () => {
        let refreshes = 0
        const transport = vi.fn<typeof fetch>(async (url, options) => {
            if (String(url).endsWith('/api/auth/refresh')) {
                refreshes++
                await new Promise((resolve) => setTimeout(resolve, 10))
                return json({
                    ...session,
                    accessToken: 'access-renewed',
                    refreshToken: 'refresh-renewed',
                })
            }
            const token = new Headers(options?.headers).get('Authorization')
            return token === 'Bearer access-renewed'
                ? json({ ok: true })
                : json({}, 401)
        })
        const client = new HttpClient('https://api.example', transport)
        const results = await Promise.all([
            client.request('/one'),
            client.request('/two'),
        ])
        expect(results).toEqual([{ ok: true }, { ok: true }])
        expect(refreshes).toBe(1)
        expect(sessionStore.get()?.refreshToken).toBe('refresh-renewed')
    })
    it('limpa a sessão quando o refresh é revogado', async () => {
        const client = new HttpClient(
            '',
            vi.fn<typeof fetch>(async () => json({}, 401)),
        )
        await expect(client.request('/private')).rejects.toMatchObject({
            status: 401,
        })
        expect(sessionStore.get()).toBeNull()
    })
    it('preserva a sessão em indisponibilidade temporária na renovação', async () => {
        const client = new HttpClient(
            '',
            vi.fn<typeof fetch>(async (url) =>
                json({}, String(url).includes('refresh') ? 503 : 401),
            ),
        )
        await expect(client.request('/private')).rejects.toMatchObject({
            status: 503,
        })
        expect(sessionStore.get()).toEqual(session)
    })
    it('não repete operações em conflito e preserva erros de campo', async () => {
        const transport = vi.fn<typeof fetch>(async () =>
            json(
                {
                    title: 'Registro alterado por outra operação.',
                    errors: { Name: ['Nome obrigatório.'] },
                },
                409,
            ),
        )
        const client = new HttpClient('', transport)
        await expect(
            client.request('/record', { method: 'POST', body: {} }),
        ).rejects.toMatchObject({
            status: 409,
            fields: { name: 'Nome obrigatório.' },
        })
        expect(transport).toHaveBeenCalledTimes(1)
    })
    it('não tenta renovar login inválido', async () => {
        const transport = vi.fn<typeof fetch>(async () => json({}, 401))
        await expect(
            new HttpClient('', transport).request('/api/auth/login', {
                anonymous: true,
            }),
        ).rejects.toMatchObject({ message: 'E-mail ou senha incorretos.' })
        expect(transport).toHaveBeenCalledTimes(1)
    })
    it('trata 204 sem tentar ler JSON', async () => {
        await expect(
            new HttpClient(
                '',
                vi.fn<typeof fetch>(async () => json(null, 204)),
            ).request('/logout', { method: 'POST' }),
        ).resolves.toBeUndefined()
    })
    it('não restaura uma sessão encerrada durante a renovação', async () => {
        let release!: () => void
        const transport = vi.fn<typeof fetch>(async (url) => {
            if (String(url).includes('refresh')) {
                await new Promise<void>((resolve) => {
                    release = resolve
                })
                return json({ ...session, accessToken: 'new' })
            }
            return json({}, 401)
        })
        const pending = new HttpClient('', transport).request('/private')
        await vi.waitFor(() => expect(release).toBeDefined())
        sessionStore.set(null)
        release()
        await expect(pending).rejects.toMatchObject({ status: 401 })
        expect(sessionStore.get()).toBeNull()
    })
})
