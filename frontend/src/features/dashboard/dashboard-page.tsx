import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
    ArrowRight,
    ClipboardList,
    CircleCheck,
    Wallet,
    Clock3,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { PageHeader } from '@/components/shared/page-header'
import { DataTable } from '@/components/shared/data-table'
import { ErrorState, LoadingState } from '@/components/shared/resource-state'
import { useAuth } from '@/features/auth/auth-context'
import { orderService } from '@/features/orders/order-service'
import { orderColumns } from '@/features/orders/order-columns'
import { CreateOrderDialog } from '@/features/orders/create-order-dialog'
import { money } from '@/lib/format'
import { reportService } from './report-service'
export default function DashboardPage() {
    const { user, isAdmin } = useAuth()
    const [month, setMonth] = useState(new Date().toISOString().slice(0, 7))
    const [year, monthNumber] = month.split('-').map(Number)
    const report = useQuery({
        queryKey: ['report', month],
        queryFn: ({ signal }) =>
            reportService.monthly(year, monthNumber, signal),
        enabled: isAdmin && !!month,
    })
    const recent = useQuery({
        queryKey: ['recent-orders'],
        queryFn: ({ signal }) =>
            orderService.list({ page: 1, pageSize: 6 }, signal),
    })
    const ongoing = useQuery({
        queryKey: ['orders-count', 'EmAndamento'],
        queryFn: ({ signal }) =>
            orderService.list(
                { page: 1, pageSize: 1, status: 'EmAndamento' },
                signal,
            ),
    })
    const pending = useQuery({
        queryKey: ['orders-count', 'AguardandoPeca'],
        queryFn: ({ signal }) =>
            orderService.list(
                { page: 1, pageSize: 1, status: 'AguardandoPeca' },
                signal,
            ),
        enabled: !isAdmin,
    })
    const metrics = isAdmin
        ? [
              {
                  label: 'Ordens abertas no mês',
                  value: report.data?.created,
                  icon: ClipboardList,
                  note: 'Novos atendimentos',
              },
              {
                  label: 'Concluídas no mês',
                  value: report.data?.completed,
                  icon: CircleCheck,
                  note: 'Atendimentos entregues',
              },
              {
                  label: 'Receita do mês',
                  value: report.data ? money(report.data.revenue) : undefined,
                  icon: Wallet,
                  note: 'Ordens concluídas · UTC',
              },
              {
                  label: 'Em andamento',
                  value: ongoing.data?.total,
                  icon: Clock3,
                  note: 'Em toda a operação, agora',
              },
          ]
        : [
              {
                  label: 'Meus atendimentos',
                  value: recent.data?.total,
                  icon: ClipboardList,
                  note: 'Todas as suas ordens',
              },
              {
                  label: 'Em andamento',
                  value: ongoing.data?.total,
                  icon: Clock3,
                  note: 'Acompanhamento atual',
              },
              {
                  label: 'Aguardando peça',
                  value: pending.data?.total,
                  icon: Wallet,
                  note: 'Pendentes de material',
              },
          ]
    return (
        <>
            <PageHeader
                eyebrow="VISÃO GERAL"
                title={`Olá, ${user.name.split(' ')[0]}`}
                description="Veja como estão os atendimentos da sua equipe."
            >
                {isAdmin && <CreateOrderDialog />}
            </PageHeader>
            <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
                <h2 className="font-semibold">Resumo da operação</h2>
                {isAdmin && (
                    <label className="flex items-center gap-2 text-sm text-muted-foreground">
                        Período
                        <Input
                            className="w-44"
                            type="month"
                            aria-label="Mês do relatório"
                            value={month}
                            min="2000-01"
                            max="2100-12"
                            onChange={(e) => {
                                if (e.target.value) setMonth(e.target.value)
                            }}
                        />
                    </label>
                )}
            </div>
            <div className={`metrics-grid ${!isAdmin ? 'metrics-three' : ''}`}>
                {metrics.map((metric) => (
                    <article className="metric-card" key={metric.label}>
                        <div className="flex justify-between gap-2">
                            <span className="text-sm text-muted-foreground">
                                {metric.label}
                            </span>
                            <metric.icon size={18} className="text-primary" />
                        </div>
                        <strong>{metric.value ?? '—'}</strong>
                        <p>{metric.note}</p>
                    </article>
                ))}
            </div>
            {(report.isError || ongoing.isError || pending.isError) && (
                <div className="mb-6 text-sm text-destructive">
                    Alguns indicadores não puderam ser carregados.{' '}
                    <button
                        className="underline"
                        onClick={() => {
                            if (isAdmin) void report.refetch()
                            else void pending.refetch()
                            void ongoing.refetch()
                        }}
                    >
                        Tentar novamente
                    </button>
                </div>
            )}
            <section className="panel">
                <div className="section-heading">
                    <div>
                        <h2>Últimas ordens</h2>
                        <p>Os atendimentos mais recentes, em um só lugar.</p>
                    </div>
                    <Button variant="ghost" asChild>
                        <Link to="/orders">
                            Ver todas
                            <ArrowRight size={16} />
                        </Link>
                    </Button>
                </div>
                {recent.isPending ? (
                    <LoadingState />
                ) : recent.isError ? (
                    <ErrorState retry={() => void recent.refetch()} />
                ) : (
                    <DataTable rows={recent.data.data} columns={orderColumns} />
                )}
            </section>
        </>
    )
}
