import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { toast } from 'sonner'
import App from '@/App'
import { sessionStore } from '@/lib/session'
import { queryClient } from '@/lib/query-client'
import { admin, json, order, session } from './fixtures'
import type { Customer, User } from '@/lib/contracts'
let customers: Customer[]
let identity: User
let currentOrder = structuredClone(order)
let customerFailure = false
beforeEach(() => {
    sessionStore.set(null)
    queryClient.clear()
    customers = []
    identity = admin
    currentOrder = structuredClone(order)
    customerFailure = false
    window.history.replaceState({}, '', '/login')
    vi.stubGlobal(
        'matchMedia',
        vi.fn().mockReturnValue({
            matches: false,
            addListener: vi.fn(),
            removeListener: vi.fn(),
            addEventListener: vi.fn(),
            removeEventListener: vi.fn(),
        }),
    )
    vi.stubGlobal(
        'fetch',
        vi.fn<typeof fetch>(async (url, options) => {
            const path = new URL(String(url)).pathname
            if (path === '/api/auth/login')
                return json({ ...session, user: identity })
            if (path === '/api/auth/me') return json(identity)
            if (path === '/api/users')
                return json([
                    admin,
                    { ...admin, id: order.technicianId, role: 'Tecnico' },
                ])
            if (path === `/api/orders/${order.id}`) return json(currentOrder)
            if (path === `/api/orders/${order.id}/history`) return json([])
            if (
                path ===
                `/api/orders/${order.id}/items/${order.items[0].id}/quantity`
            ) {
                const quantity = (
                    JSON.parse(String(options?.body)) as { quantity: number }
                ).quantity
                currentOrder.items[0].quantity = quantity
                currentOrder.items[0].subtotal =
                    quantity * currentOrder.items[0].unitPrice
                currentOrder.total = currentOrder.items[0].subtotal
                return json(currentOrder)
            }
            if (path === `/api/orders/${order.id}/status`) {
                currentOrder.status = (
                    JSON.parse(String(options?.body)) as {
                        status: typeof order.status
                    }
                ).status
                return json(currentOrder)
            }
            if (path === '/api/orders')
                return json({ total: 1, page: 1, pageSize: 6, data: [order] })
            if (path === '/api/reports/monthly')
                return json({
                    year: 2026,
                    month: 9,
                    created: 1,
                    completed: 0,
                    revenue: 0,
                })
            if (path === '/api/customers') {
                if (options?.method === 'POST') {
                    if (customerFailure)
                        return json(
                            {
                                title: 'Não foi possível salvar cliente.',
                                errors: { Name: ['Nome inválido.'] },
                            },
                            400,
                        )
                    const values = JSON.parse(String(options.body)) as Omit<
                        Customer,
                        'id'
                    >
                    customers.push({ ...values, id: 'new-customer' })
                    return json(customers.at(-1), 201)
                }
                return json({
                    total: customers.length,
                    page: 1,
                    pageSize: 10,
                    data: customers,
                })
            }
            if (path === '/api/auth/logout') return json(null, 204)
            return json({}, 404)
        }),
    )
})
afterEach(() => {
    toast.dismiss()
    sessionStore.set(null)
    queryClient.clear()
    vi.unstubAllGlobals()
})
async function login() {
    const user = userEvent.setup()
    render(<App />)
    await user.type(await screen.findByLabelText('E-mail'), 'ana@example.com')
    await user.type(screen.getByLabelText('Senha'), 'Valid-password123!')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))
    await screen.findByRole('heading', { name: 'Olá, Ana' })
    return user
}
describe('fluxos da interface com API simulada', () => {
    it('faz login, cadastra um cliente e apresenta feedback de sucesso', async () => {
        const user = await login()
        await user.click(screen.getByRole('link', { name: 'Clientes' }))
        await user.click(
            await screen.findByRole('button', { name: 'Novo cliente' }),
        )
        const dialog = await screen.findByRole('dialog')
        await user.type(within(dialog).getByLabelText('Nome'), 'Marina Lima')
        await user.type(
            within(dialog).getByLabelText('E-mail'),
            'marina@example.com',
        )
        await user.type(
            within(dialog).getByLabelText('Telefone'),
            '11999999999',
        )
        await user.click(within(dialog).getByRole('button', { name: 'Salvar' }))
        await screen.findByText('Marina Lima')
        await screen.findByText('Cliente cadastrado.')
        expect(customers[0].email).toBe('marina@example.com')
    })
    it('restringe criação e administração para o técnico', async () => {
        identity = { ...admin, role: 'Tecnico' }
        const user = await login()
        expect(
            screen.queryByRole('link', { name: 'Usuários' }),
        ).not.toBeInTheDocument()
        expect(
            screen.queryByRole('button', { name: 'Nova ordem' }),
        ).not.toBeInTheDocument()
        await user.click(screen.getByRole('link', { name: 'Clientes' }))
        await screen.findByRole('heading', { name: 'Clientes' })
        expect(
            screen.queryByRole('button', { name: 'Novo cliente' }),
        ).not.toBeInTheDocument()
    })
    it('valida campos sem enviar uma requisição inválida', async () => {
        const user = userEvent.setup()
        render(<App />)
        await user.click(await screen.findByRole('button', { name: 'Entrar' }))
        await screen.findByText('Informe sua senha.')
        expect(fetch).not.toHaveBeenCalled()
    })
    it('encerra a sessão após confirmar logout', async () => {
        const user = await login()
        await user.click(screen.getByRole('button', { name: 'Sair da conta' }))
        await user.click(
            await screen.findByRole('button', { name: 'Confirmar' }),
        )
        await waitFor(() => expect(sessionStore.get()).toBeNull())
        await screen.findByRole('heading', { name: 'Entre na sua conta' })
    })
})

it('mostra a quantidade e o total retornados pelo backend', async () => {
    const user = await login()
    await user.click(await screen.findByRole('link', { name: 'Abrir' }))
    await user.click(await screen.findByRole('button', { name: 'Quantidade' }))
    const dialog = await screen.findByRole('dialog')
    await user.clear(within(dialog).getByLabelText('Quantidade'))
    await user.type(within(dialog).getByLabelText('Quantidade'), '3')
    await user.click(within(dialog).getByRole('button', { name: 'Salvar' }))
    await waitFor(() =>
        expect(
            screen.getByText('Total da ordem').parentElement,
        ).toHaveTextContent('75,00'),
    )
    expect(currentOrder.items[0].id).toBe(order.items[0].id)
    expect(currentOrder.items[0].unitPrice).toBe(25)
})

it('oculta edição quando o backend encerra a ordem', async () => {
    const user = await login()
    await user.click(await screen.findByRole('link', { name: 'Abrir' }))
    await user.click(
        await screen.findByRole('button', { name: 'Cancelar ordem' }),
    )
    await user.click(await screen.findByRole('button', { name: 'Confirmar' }))
    await screen.findByText(
        'Este atendimento foi encerrado. Os registros estão disponíveis para consulta.',
    )
    expect(
        screen.queryByRole('button', { name: 'Adicionar item' }),
    ).not.toBeInTheDocument()
    expect(
        screen.queryByRole('button', { name: 'Quantidade' }),
    ).not.toBeInTheDocument()
})

it('apresenta os erros do backend no toast e junto ao campo', async () => {
    customerFailure = true
    const user = await login()
    await user.click(screen.getByRole('link', { name: 'Clientes' }))
    await user.click(
        await screen.findByRole('button', { name: 'Novo cliente' }),
    )
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText('Nome'), 'Marina Lima')
    await user.type(
        within(dialog).getByLabelText('E-mail'),
        'marina@example.com',
    )
    await user.type(within(dialog).getByLabelText('Telefone'), '11999999999')
    await user.click(within(dialog).getByRole('button', { name: 'Salvar' }))
    await screen.findByText('Não foi possível salvar cliente.')
    expect(within(dialog).getByRole('alert')).toHaveTextContent(
        'Nome inválido.',
    )
    expect(customers).toHaveLength(0)
})
