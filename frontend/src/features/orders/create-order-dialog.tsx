import { Plus } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { FormDialog } from '@/components/shared/form-dialog'
import { PagedLookup } from '@/components/shared/paged-lookup'
import { customerService } from '@/features/customers/customer-service'
import { TechnicianSelect } from '@/features/users/technician-select'
import { orderSchema } from '@/lib/validation'
import { orderService } from './order-service'
export function CreateOrderDialog() {
    const navigate = useNavigate()
    return (
        <FormDialog
            title="Nova ordem de serviço"
            description="Selecione o cliente e a pessoa responsável pelo atendimento."
            trigger={
                <Button>
                    <Plus size={17} />
                    Nova ordem
                </Button>
            }
            schema={orderSchema}
            initialValues={{
                customerId: '',
                technicianId: '',
                description: '',
            }}
            fields={[
                {
                    name: 'customerId',
                    label: 'Cliente',
                    render: (props) => (
                        <PagedLookup
                            {...props}
                            queryKey="customers"
                            searchable
                            fetchPage={async (page, search, signal) => {
                                const result = await customerService.list(
                                    page,
                                    search,
                                    signal,
                                )
                                return {
                                    ...result,
                                    data: result.data.map((item) => ({
                                        id: item.id,
                                        label: item.name,
                                    })),
                                }
                            }}
                        />
                    ),
                },
                {
                    name: 'technicianId',
                    label: 'Técnico responsável',
                    render: (props) => <TechnicianSelect {...props} />,
                },
                {
                    name: 'description',
                    label: 'Descrição do atendimento',
                    type: 'textarea',
                },
            ]}
            submit={async (values) => {
                const result = await orderService.create({
                    customerId: values.customerId,
                    technicianId: values.technicianId,
                    description: values.description,
                })
                navigate('/orders/' + result.id)
                return result
            }}
            success="Ordem de serviço criada."
            submitLabel="Criar ordem"
        />
    )
}
