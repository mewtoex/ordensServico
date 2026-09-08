import { z } from 'zod'
import { PageHeader } from '@/components/shared/page-header'
import { DataForm } from '@/components/shared/data-form'
import { passwordField } from '@/lib/validation'
import { authService } from './auth-service'
import { useAuth } from './auth-context'
const schema = z
    .object({
        currentPassword: z.string().min(1, 'Informe a senha atual.'),
        newPassword: passwordField,
        confirmation: z.string(),
    })
    .refine((values) => values.newPassword === values.confirmation, {
        path: ['confirmation'],
        message: 'As senhas não coincidem.',
    })
export default function AccountPage() {
    const { user } = useAuth()
    return (
        <>
            <PageHeader
                eyebrow="PREFERÊNCIAS"
                title="Minha conta"
                description={`${user.name} · ${user.email}`}
            />
            <section className="panel max-w-xl">
                <h2 className="mb-2 text-lg font-semibold">Alterar senha</h2>
                <p className="text-sm text-muted-foreground mb-6">
                    Após salvar, entre novamente em todos os dispositivos.
                </p>
                <DataForm
                    fields={[
                        {
                            name: 'currentPassword',
                            label: 'Senha atual',
                            type: 'password',
                            autoComplete: 'current-password',
                        },
                        {
                            name: 'newPassword',
                            label: 'Nova senha',
                            type: 'password',
                            autoComplete: 'new-password',
                            hint: 'Use pelo menos 12 caracteres.',
                        },
                        {
                            name: 'confirmation',
                            label: 'Confirmar nova senha',
                            type: 'password',
                            autoComplete: 'new-password',
                        },
                    ]}
                    schema={schema}
                    initialValues={{
                        currentPassword: '',
                        newPassword: '',
                        confirmation: '',
                    }}
                    submit={(values) =>
                        authService.changePassword(
                            values.currentPassword,
                            values.newPassword,
                        )
                    }
                    success="Senha alterada. Entre novamente."
                />
            </section>
        </>
    )
}
