import type { Order, Session, User } from '@/lib/contracts'
export const admin: User = {
    id: '11111111-1111-4111-8111-111111111111',
    name: 'Ana Oliveira',
    email: 'ana@example.com',
    role: 'Admin',
    active: true,
}
export const session: Session = {
    accessToken: 'access-original',
    refreshToken: 'refresh-original',
    expiresAt: '2030-01-01T00:00:00Z',
    refreshExpiresAt: '2030-01-07T00:00:00Z',
    user: admin,
}
export const order: Order = {
    id: '22222222-2222-4222-8222-222222222222',
    customerId: '33333333-3333-4333-8333-333333333333',
    customerName: 'Cliente de teste',
    technicianId: '44444444-4444-4444-8444-444444444444',
    description: 'Manutenção de equipamento',
    status: 'EmAndamento',
    createdAt: '2026-09-08T14:00:00Z',
    closedAt: null,
    total: 50,
    items: [
        {
            id: '55555555-5555-4555-8555-555555555555',
            catalogItemId: '66666666-6666-4666-8666-666666666666',
            name: 'Diagnóstico',
            kind: 'Servico',
            quantity: 2,
            unitPrice: 25,
            subtotal: 50,
        },
    ],
}
export function json(value: unknown, status = 200) {
    return new Response(status === 204 ? null : JSON.stringify(value), {
        status,
        headers: { 'Content-Type': 'application/json' },
    })
}
