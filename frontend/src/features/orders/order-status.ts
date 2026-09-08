import type { OrderStatus } from '@/lib/contracts'
export const statusLabels: Record<OrderStatus, string> = {
    Aberta: 'Aberta',
    EmAndamento: 'Em andamento',
    AguardandoPeca: 'Aguardando peça',
    Concluida: 'Concluída',
    Cancelada: 'Cancelada',
}
export const statusClasses: Record<OrderStatus, string> = {
    Aberta: 'bg-sky-50 text-sky-800 ring-sky-200',
    EmAndamento: 'bg-indigo-50 text-indigo-800 ring-indigo-200',
    AguardandoPeca: 'bg-amber-50 text-amber-900 ring-amber-200',
    Concluida: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
    Cancelada: 'bg-slate-100 text-slate-600 ring-slate-200',
}
export const transitions: Record<OrderStatus, OrderStatus[]> = {
    Aberta: ['EmAndamento', 'Cancelada'],
    EmAndamento: ['AguardandoPeca', 'Concluida', 'Cancelada'],
    AguardandoPeca: ['EmAndamento', 'Cancelada'],
    Concluida: [],
    Cancelada: [],
}
