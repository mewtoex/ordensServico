import type { Session } from './contracts'
const key = 'ordem.session'
function read(): Session | null {
    try {
        const value = JSON.parse(
            sessionStorage.getItem(key) ?? 'null',
        ) as Session | null
        return value?.accessToken && value.refreshToken && value.user?.id
            ? value
            : null
    } catch {
        return null
    }
}
let current = read()
const listeners = new Set<() => void>()
export const sessionStore = {
    get: () => current,
    subscribe: (listener: () => void) => {
        listeners.add(listener)
        return () => {
            listeners.delete(listener)
        }
    },
    set: (session: Session | null) => {
        current = session
        try {
            if (session) sessionStorage.setItem(key, JSON.stringify(session))
            else sessionStorage.removeItem(key)
        } catch {
            /* In-memory sessions still work when storage is blocked. */
        }
        listeners.forEach((listener) => listener())
    },
}
