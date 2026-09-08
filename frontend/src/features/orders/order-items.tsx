import { z } from 'zod'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { FormDialog } from '@/components/shared/form-dialog'
import { ConfirmDialog } from '@/components/shared/confirm-dialog'
import { PagedLookup } from '@/components/shared/paged-lookup'
import { DataTable } from '@/components/shared/data-table'
import { catalogService } from '@/features/catalog/catalog-service'
import type { Order } from '@/lib/contracts'
import { money } from '@/lib/format'
import { quantityField } from '@/lib/validation'
import { orderService } from './order-service'
export function OrderItems({
    order,
    editable,
}: {
    order: Order
    editable: boolean
}) {
    return (
        <section className="panel">
            <div className="section-heading">
                <div>
                    <h2>Serviços e peças</h2>
                    <p>Valores registrados neste atendimento.</p>
                </div>
                {editable && (
                    <FormDialog
                        title="Adicionar item"
                        description="Escolha um serviço ou peça do catálogo."
                        trigger={
                            <Button variant="outline" size="sm">
                                <Plus size={16} />
                                Adicionar item
                            </Button>
                        }
                        schema={z.object({
                            catalogItemId: z
                                .string()
                                .uuid('Selecione um item.'),
                            quantity: quantityField,
                        })}
                        initialValues={{ catalogItemId: '', quantity: '1' }}
                        fields={[
                            {
                                name: 'catalogItemId',
                                label: 'Item do catálogo',
                                render: (props) => (
                                    <PagedLookup
                                        {...props}
                                        queryKey="catalog"
                                        fetchPage={async (
                                            page,
                                            _search,
                                            signal,
                                        ) => {
                                            const result =
                                                await catalogService.list(
                                                    page,
                                                    signal,
                                                )
                                            return {
                                                ...result,
                                                data: result.data.map(
                                                    (item) => ({
                                                        id: item.id,
                                                        label: `${item.name} · ${money(item.price)}`,
                                                    }),
                                                ),
                                            }
                                        }}
                                    />
                                ),
                            },
                            {
                                name: 'quantity',
                                label: 'Quantidade',
                                type: 'number',
                            },
                        ]}
                        submit={(values) =>
                            orderService.addItem(
                                order.id,
                                values.catalogItemId,
                                Number(values.quantity),
                            )
                        }
                        success="Item adicionado à ordem."
                    />
                )}
            </div>
            <DataTable
                rows={order.items}
                columns={[
                    {
                        title: 'Item',
                        render: (item) => (
                            <div>
                                <span className="font-medium">{item.name}</span>
                                <p className="text-sm text-muted-foreground">
                                    {item.kind === 'Servico'
                                        ? 'Serviço'
                                        : 'Peça'}
                                </p>
                            </div>
                        ),
                    },
                    { title: 'Qtd.', render: (item) => item.quantity },
                    {
                        title: 'Valor unitário',
                        render: (item) => money(item.unitPrice),
                    },
                    {
                        title: 'Subtotal',
                        render: (item) => money(item.subtotal),
                    },
                    ...(editable
                        ? [
                              {
                                  title: 'Ações',
                                  className: 'text-right',
                                  render: (item: Order['items'][number]) => (
                                      <div className="flex gap-1 justify-end">
                                          <FormDialog
                                              title="Alterar quantidade"
                                              description={`${item.name}. O preço unitário registrado será preservado.`}
                                              trigger={
                                                  <Button
                                                      variant="ghost"
                                                      size="sm"
                                                  >
                                                      Quantidade
                                                  </Button>
                                              }
                                              schema={z.object({
                                                  quantity: quantityField,
                                              })}
                                              initialValues={{
                                                  quantity: String(
                                                      item.quantity,
                                                  ),
                                              }}
                                              fields={[
                                                  {
                                                      name: 'quantity',
                                                      label: 'Quantidade',
                                                      type: 'number',
                                                  },
                                              ]}
                                              submit={(values) =>
                                                  orderService.quantity(
                                                      order.id,
                                                      item.id,
                                                      Number(values.quantity),
                                                  )
                                              }
                                              success="Quantidade atualizada."
                                          />
                                          <ConfirmDialog
                                              label="Remover"
                                              title="Remover item?"
                                              description={`${item.name} deixará de compor o total. O histórico será preservado.`}
                                              action={() =>
                                                  orderService.removeItem(
                                                      order.id,
                                                      item.id,
                                                  )
                                              }
                                              success="Item removido."
                                          />
                                      </div>
                                  ),
                              },
                          ]
                        : []),
                ]}
            />
            <div className="order-total">
                <span>Total da ordem</span>
                <strong>{money(order.total)}</strong>
            </div>
        </section>
    )
}
