import { lazy, Suspense } from 'react'
import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { Toaster } from '@/components/ui/sonner'
import { Button } from '@/components/ui/button'
import { LoadingState } from '@/components/shared/resource-state'
import { ProtectedRoute } from '@/features/auth/protected-route'
import { AppLayout } from '@/app/app-layout'
import { queryClient } from '@/lib/query-client'
const LoginPage = lazy(() => import('@/features/auth/login-page'))
const DashboardPage = lazy(() => import('@/features/dashboard/dashboard-page'))
const OrdersPage = lazy(() => import('@/features/orders/orders-page'))
const OrderDetailPage = lazy(
    () => import('@/features/orders/order-detail-page'),
)
const CustomersPage = lazy(() => import('@/features/customers/customers-page'))
const CatalogPage = lazy(() => import('@/features/catalog/catalog-page'))
const UsersPage = lazy(() => import('@/features/users/users-page'))
const AccountPage = lazy(() => import('@/features/auth/account-page'))
export default function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <BrowserRouter>
                <Suspense
                    fallback={
                        <div className="p-10">
                            <LoadingState />
                        </div>
                    }
                >
                    <Routes>
                        <Route path="/login" element={<LoginPage />} />
                        <Route element={<ProtectedRoute />}>
                            <Route element={<AppLayout />}>
                                <Route index element={<DashboardPage />} />
                                <Route path="orders" element={<OrdersPage />} />
                                <Route
                                    path="orders/:id"
                                    element={<OrderDetailPage />}
                                />
                                <Route
                                    path="customers"
                                    element={<CustomersPage />}
                                />
                                <Route
                                    path="catalog"
                                    element={<CatalogPage />}
                                />
                                <Route path="users" element={<UsersPage />} />
                                <Route
                                    path="account"
                                    element={<AccountPage />}
                                />
                                <Route
                                    path="*"
                                    element={
                                        <div className="empty-state">
                                            <h1>Página não encontrada</h1>
                                            <Button asChild>
                                                <Link to="/">
                                                    Voltar ao painel
                                                </Link>
                                            </Button>
                                        </div>
                                    }
                                />
                            </Route>
                        </Route>
                    </Routes>
                </Suspense>
            </BrowserRouter>
            <Toaster
                richColors
                closeButton
                position="top-right"
                theme="light"
            />
        </QueryClientProvider>
    )
}
