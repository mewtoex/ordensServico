import { z } from 'zod'
export const nameField = z
    .string()
    .trim()
    .min(1, 'Informe o nome.')
    .max(160, 'Use até 160 caracteres.')
export const emailField = z
    .string()
    .trim()
    .email('Informe um e-mail válido.')
    .max(254)
export const passwordField = z
    .string()
    .min(12, 'Use pelo menos 12 caracteres.')
    .max(128, 'Use até 128 caracteres.')
export const quantityField = z
    .string()
    .refine(
        (value) =>
            Number.isInteger(Number(value)) &&
            Number(value) >= 1 &&
            Number(value) <= 10000,
        'Informe uma quantidade inteira entre 1 e 10000.',
    )
export const customerSchema = z.object({
    name: nameField,
    email: emailField,
    phone: z.string().trim().min(1, 'Informe o telefone.').max(30),
})
export const catalogSchema = z.object({
    name: nameField,
    kind: z.enum(['Servico', 'Peca']),
    price: z
        .string()
        .refine(
            (value) =>
                Number.isFinite(Number(value)) &&
                Number(value) >= 0.01 &&
                Number(value) <= 99999999,
            'Informe um valor entre R$ 0,01 e R$ 99.999.999,00.',
        ),
})
export const userSchema = z.object({
    name: nameField,
    email: emailField,
    role: z.enum(['Admin', 'Tecnico']),
})
export const createUserSchema = userSchema.extend({ password: passwordField })
export const orderSchema = z.object({
    customerId: z.string().uuid('Selecione um cliente.'),
    technicianId: z.string().uuid('Selecione um técnico.'),
    description: z
        .string()
        .trim()
        .min(1, 'Descreva o atendimento.')
        .max(4000, 'Use até 4000 caracteres.'),
})
