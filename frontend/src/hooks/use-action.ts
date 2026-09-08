import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { notifyError } from '@/lib/notifications'
export function useAction<T, V>(
    action: (values: V) => Promise<T>,
    message: string,
    onSuccess?: (data: T) => void,
) {
    const client = useQueryClient()
    return useMutation({
        mutationFn: action,
        onError: notifyError,
        onSuccess: (data) => {
            toast.success(message)
            void client.invalidateQueries()
            onSuccess?.(data)
        },
    })
}
