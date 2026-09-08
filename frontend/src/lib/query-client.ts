import { QueryCache, QueryClient } from '@tanstack/react-query'
import { notifyError } from './notifications'
import { sessionStore } from './session'
export const queryClient = new QueryClient({
    queryCache: new QueryCache({ onError: notifyError }),
    defaultOptions: {
        queries: {
            retry: false,
            staleTime: 30_000,
            refetchOnWindowFocus: false,
        },
        mutations: { retry: false },
    },
})
let userId = sessionStore.get()?.user.id
sessionStore.subscribe(() => {
    const next = sessionStore.get()?.user.id
    if (next !== userId) {
        userId = next
        queryClient.clear()
    }
})
