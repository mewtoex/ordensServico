import { useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import {
    ClipboardCheck,
    LayoutDashboard,
    ClipboardList,
    UsersRound,
    Package,
    ShieldCheck,
    Settings,
    Menu,
    X,
} from 'lucide-react'
import { useAuth } from '@/features/auth/auth-context'
import { authService } from '@/features/auth/auth-service'
import { ConfirmDialog } from '@/components/shared/confirm-dialog'
import { Button } from '@/components/ui/button'
const links = [
    { to: '/', label: 'Visão geral', icon: LayoutDashboard },
    { to: '/orders', label: 'Ordens de serviço', icon: ClipboardList },
    { to: '/customers', label: 'Clientes', icon: UsersRound },
    { to: '/catalog', label: 'Serviços e peças', icon: Package },
    { to: '/users', label: 'Usuários', icon: ShieldCheck, admin: true },
]
export function AppLayout() {
    const { user, isAdmin } = useAuth()
    const [menuOpen, setMenuOpen] = useState(false)
    return (
        <div
            className="app-shell"
            onKeyDown={(event) => {
                if (event.key === 'Escape') setMenuOpen(false)
            }}
        >
            <a href="#main-content" className="skip-link">
                Ir para o conteúdo
            </a>
            {menuOpen && (
                <button
                    className="sidebar-overlay"
                    aria-label="Fechar navegação"
                    onClick={() => setMenuOpen(false)}
                />
            )}
            <aside className={`sidebar ${menuOpen ? 'is-open' : ''}`}>
                <div className="flex justify-between items-center">
                    <NavLink
                        to="/"
                        className="brand"
                        onClick={() => setMenuOpen(false)}
                    >
                        <ClipboardCheck size={27} />
                        <span>
                            ordem<span className="text-emerald-400">.</span>
                        </span>
                    </NavLink>
                    <Button
                        className="mobile-only text-white"
                        variant="ghost"
                        size="icon"
                        aria-label="Fechar menu"
                        onClick={() => setMenuOpen(false)}
                    >
                        <X />
                    </Button>
                </div>
                <p className="sidebar-caption">ESPAÇO DE TRABALHO</p>
                <nav
                    id="main-navigation"
                    aria-label="Navegação principal"
                    className="space-y-1"
                >
                    {links
                        .filter((link) => !link.admin || isAdmin)
                        .map((link) => (
                            <NavLink
                                end={link.to === '/'}
                                to={link.to}
                                key={link.to}
                                onClick={() => setMenuOpen(false)}
                                className={({ isActive }) =>
                                    `nav-link ${isActive ? 'active' : ''}`
                                }
                            >
                                <link.icon size={19} />
                                {link.label}
                            </NavLink>
                        ))}
                </nav>
                <div className="mt-auto pt-8">
                    <NavLink
                        to="/account"
                        className="nav-link"
                        onClick={() => setMenuOpen(false)}
                    >
                        <Settings size={19} />
                        Minha conta
                    </NavLink>
                    <div className="sidebar-user">
                        <span className="avatar">
                            {user.name.slice(0, 2).toUpperCase()}
                        </span>
                        <div className="min-w-0">
                            <p className="truncate text-sm text-white font-medium">
                                {user.name}
                            </p>
                            <p className="text-xs text-slate-400">
                                {isAdmin ? 'Administrador' : 'Técnico'}
                            </p>
                        </div>
                    </div>
                    <ConfirmDialog
                        label="Sair da conta"
                        destructive={false}
                        title="Encerrar sua sessão?"
                        description="Você sairá da conta em todos os dispositivos."
                        action={() => authService.logout()}
                        success="Sessão encerrada."
                    />
                </div>
            </aside>
            <div className="workspace">
                <header className="topbar">
                    <div className="flex gap-3 items-center">
                        <Button
                            className="mobile-only"
                            variant="ghost"
                            size="icon"
                            aria-label="Abrir menu"
                            aria-expanded={menuOpen}
                            aria-controls="main-navigation"
                            onClick={() => setMenuOpen(true)}
                        >
                            <Menu />
                        </Button>
                        <span className="text-sm font-medium">
                            Gestão de atendimentos
                        </span>
                    </div>
                    <span className="text-sm text-muted-foreground hidden sm:block">
                        {new Intl.DateTimeFormat('pt-BR', {
                            day: 'numeric',
                            month: 'long',
                            year: 'numeric',
                        }).format(new Date())}
                    </span>
                </header>
                <main id="main-content" className="main-content" tabIndex={-1}>
                    <Outlet />
                </main>
                <footer className="app-footer">
                    ordem. <span>Organização em cada atendimento.</span>
                </footer>
            </div>
        </div>
    )
}
