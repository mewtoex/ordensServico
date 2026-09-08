import { http, queryString, type IHttpClient } from '@/lib/http-client'
import type { MonthlyReport } from '@/lib/contracts'
export interface IReportService {
    monthly(
        year: number,
        month: number,
        signal?: AbortSignal,
    ): Promise<MonthlyReport>
}
export class ReportService implements IReportService {
    constructor(private client: IHttpClient) {}
    monthly(year: number, month: number, signal?: AbortSignal) {
        return this.client.request<MonthlyReport>(
            '/api/reports/monthly?' + queryString({ year, month }),
            { signal },
        )
    }
}
export const reportService: IReportService = new ReportService(http)
