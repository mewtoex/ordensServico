import { useSyncExternalStore } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ArrowUpRight, ClipboardCheck, ShieldCheck } from 'lucide-react'
import { z } from 'zod'
import { DataForm } from '@/components/shared/data-form'
import { sessionStore } from '@/lib/session'
import { authService } from './auth-service'
import { emailField } from '@/lib/validation'
const schema = z.object({
    email: emailField,
    password: z.string().min(1, 'Informe sua senha.'),
})
export default function LoginPage() {
    const session = useSyncExternalStore(
        sessionStore.subscribe,
        sessionStore.get,
    )
    const navigate = useNavigate()
    const location = useLocation()
    const from = (location.state as { from?: string } | null)?.from
    const destination =
        from?.startsWith('/') && !from.startsWith('//') && from !== '/login'
            ? from
            : '/'
    if (session) return <Navigate to={destination} replace />
    return (
        <main className="login-layout">
            <section className="login-story">
                <div className="brand">
                    <ClipboardCheck size={28} />
                    <span>
                        ordem<span className="text-emerald-400">.</span>
                    </span>
                </div>
                <div>
                    <p className="eyebrow text-emerald-300">
                        GESTÃO DE SERVIÇOS
                    </p>
                    <h1>
                        Do primeiro contato
                        <br />
                        ao serviço concluído
                        <span className="text-emerald-400">.</span>
                    </h1>
                    <p className="mt-6 max-w-sm text-slate-300 leading-relaxed">
                        Clientes, equipe e atendimentos organizados em um só
                        lugar.
                    </p>
                </div>
                <div className="flex items-center gap-3 text-sm text-slate-400">
                    <ShieldCheck size={18} />
                    Acesso restrito à sua equipe
                    <ArrowUpRight size={16} className="ml-auto" />
                </div>
            </section>
            <section className="login-form">
                <div className="w-full max-w-sm">
                    <p className="eyebrow mb-3">BEM-VINDO DE VOLTA</p>
                    <h2 className="text-3xl font-semibold tracking-tight">
                        Entre na sua conta
                    </h2>
                    <p className="text-muted-foreground mt-3 mb-8">
                        Use o acesso fornecido pelo administrador.
                    </p>
                    <DataForm
                        fields={[
                            {
                                name: 'email',
                                label: 'E-mail',
                                type: 'email',
                                autoComplete: 'username',
                            },
                            {
                                name: 'password',
                                label: 'Senha',
                                type: 'password',
                                autoComplete: 'current-password',
                            },
                        ]}
                        schema={schema}
                        initialValues={{ email: '', password: '' }}
                        submit={(values) =>
                            authService.login(values.email, values.password)
                        }
                        success="Bem-vindo!"
                        submitLabel="Entrar"
                        onSuccess={() =>
                            navigate(destination, { replace: true })
                        }
                    />
                    <p className="mt-8 text-sm text-muted-foreground">
                        Problemas com o acesso? Fale com o administrador.
                    </p>
                </div>
            </section>
        </main>
    )
}
