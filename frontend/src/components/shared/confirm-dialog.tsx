import { useState } from 'react'
import {
    AlertDialog,
    AlertDialogTrigger,
    AlertDialogContent,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogCancel,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { useAction } from '@/hooks/use-action'
export function ConfirmDialog({
    label,
    title,
    description,
    action,
    success,
    destructive = true,
    disabled = false,
}: {
    label: string
    title: string
    description: string
    action: () => Promise<unknown>
    success: string
    destructive?: boolean
    disabled?: boolean
}) {
    const [open, setOpen] = useState(false)
    const mutation = useAction(action, success, () => setOpen(false))
    return (
        <AlertDialog
            open={open}
            onOpenChange={(value) => {
                if (!mutation.isPending) setOpen(value)
            }}
        >
            <AlertDialogTrigger asChild>
                <Button
                    variant="ghost"
                    size="sm"
                    disabled={disabled}
                    className={destructive ? 'text-destructive' : ''}
                >
                    {label}
                </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
                <AlertDialogHeader>
                    <AlertDialogTitle>{title}</AlertDialogTitle>
                    <AlertDialogDescription>
                        {description}
                    </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                    <AlertDialogCancel disabled={mutation.isPending}>
                        Voltar
                    </AlertDialogCancel>
                    <Button
                        variant={destructive ? 'destructive' : 'default'}
                        disabled={mutation.isPending}
                        onClick={() => mutation.mutate()}
                    >
                        {mutation.isPending ? 'Aguarde…' : 'Confirmar'}
                    </Button>
                </AlertDialogFooter>
            </AlertDialogContent>
        </AlertDialog>
    )
}
