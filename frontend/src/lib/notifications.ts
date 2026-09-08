import { toast } from 'sonner'
import { ApiError } from './api-error'
export function notifyError(error: unknown) {
    if (error instanceof Error && error.name === 'AbortError') return
    const message =
        error instanceof Error ? error.message : 'Ocorreu um erro inesperado.'
    const details =
        error instanceof ApiError ? Object.values(error.fields).join(' ') : ''
    toast.error(message, {
        id: message,
        description: details || undefined,
        duration: 6500,
    })
}
