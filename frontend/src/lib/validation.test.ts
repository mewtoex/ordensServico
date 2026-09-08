import { describe, expect, it } from 'vitest'
import { dateBoundary } from './format'
import { quantityField, catalogSchema, orderSchema } from './validation'
import { transitions } from '@/features/orders/order-status'
describe('contratos e regras da interface', () => {
    it('converte a data final para limite exclusivo UTC, incluindo virada do mês', () => {
        expect(dateBoundary('2026-09-30', true)).toBe(
            '2026-10-01T00:00:00.000Z',
        )
        expect(dateBoundary('')).toBeUndefined()
    })
    it.each(['0', '-1', '1.5', '10001', 'abc', ''])(
        'rejeita quantidade inválida %s',
        (value) => expect(quantityField.safeParse(value).success).toBe(false),
    )
    it('não permite ações sobre ordens encerradas', () => {
        expect(transitions.Concluida).toEqual([])
        expect(transitions.Cancelada).toEqual([])
    })
    it('valida preço e vínculos antes de criar registros', () => {
        expect(
            catalogSchema.safeParse({ name: 'Peça', kind: 'Peca', price: '0' })
                .success,
        ).toBe(false)
        expect(
            orderSchema.safeParse({
                customerId: '',
                technicianId: '',
                description: 'Teste',
            }).success,
        ).toBe(false)
    })
})
