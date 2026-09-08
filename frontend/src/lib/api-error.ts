export class ApiError extends Error {
    constructor(
        message: string,
        public status: number,
        public fields: Record<string, string> = {},
        public traceId?: string,
    ) {
        super(message)
        this.name = 'ApiError'
    }
}
const messages: Record<number, string> = {
    400: 'Confira os dados informados.',
    401: 'Sua sessão expirou. Entre novamente.',
    403: 'Você não tem permissão para esta ação.',
    404: 'Registro não encontrado ou indisponível.',
    409: 'Os dados foram alterados. Atualize a página e tente novamente.',
    429: 'Muitas tentativas. Aguarde um minuto e tente novamente.',
    500: 'Não foi possível concluir a solicitação.',
    503: 'O serviço está indisponível no momento.',
}
export async function parseApiError(response: Response): Promise<ApiError> {
    const problem = (await response.json().catch(() => ({}))) as {
        title?: string
        detail?: string
        errors?: Record<string, string[]>
        traceId?: string
    }
    const fields = Object.fromEntries(
        Object.entries(problem.errors ?? {}).map(([key, values]) => [
            key
                .replace(/^\$\./, '')
                .replace(/^./, (value) => value.toLowerCase()),
            values.join(' '),
        ]),
    )
    const detail =
        problem.detail ||
        (problem.title &&
        ![
            'Unauthorized',
            'Forbidden',
            'Not Found',
            'One or more validation errors occurred.',
        ].includes(problem.title)
            ? problem.title
            : undefined)
    return new ApiError(
        response.status >= 500
            ? (messages[response.status] ?? messages[500])
            : (detail ??
                  messages[response.status] ??
                  'Não foi possível concluir a solicitação.'),
        response.status,
        fields,
        problem.traceId || response.headers.get('X-Trace-Id') || undefined,
    )
}
