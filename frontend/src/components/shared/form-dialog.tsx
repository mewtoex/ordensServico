import { useState, type ReactNode } from 'react'
import {
    Dialog,
    DialogTrigger,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogDescription,
} from '@/components/ui/dialog'
import { DataForm, type DataFormProps } from './data-form'
export function FormDialog({
    trigger,
    title,
    description,
    ...props
}: DataFormProps & { trigger: ReactNode; title: string; description: string }) {
    const [open, setOpen] = useState(false)
    const [pending, setPending] = useState(false)
    return (
        <Dialog
            open={open}
            onOpenChange={(value) => {
                if (!pending) setOpen(value)
            }}
        >
            <DialogTrigger asChild>{trigger}</DialogTrigger>
            <DialogContent className="max-h-[90dvh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>{title}</DialogTitle>
                    <DialogDescription>{description}</DialogDescription>
                </DialogHeader>
                {open && (
                    <DataForm
                        {...props}
                        onPendingChange={setPending}
                        onSuccess={() => {
                            setOpen(false)
                            props.onSuccess?.()
                        }}
                    />
                )}
            </DialogContent>
        </Dialog>
    )
}
