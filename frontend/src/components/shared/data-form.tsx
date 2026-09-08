import { useState, useEffect, type ReactNode } from 'react'
import type { z } from 'zod'
import { LoaderCircle } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/api-error'
import { useAction } from '@/hooks/use-action'
export interface FieldControl {
    id: string
    value: string
    onChange: (value: string) => void
    disabled: boolean
}
export interface FormField {
    name: string
    label: string
    type?: string
    autoComplete?: string
    hint?: string
    options?: { value: string; label: string }[]
    render?: (props: FieldControl) => ReactNode
}
export interface DataFormProps {
    fields: FormField[]
    initialValues: Record<string, string>
    schema: z.ZodType<Record<string, string>>
    submit: (values: Record<string, string>) => Promise<unknown>
    success: string
    onSuccess?: () => void
    submitLabel?: string
    onPendingChange?: (pending: boolean) => void
}
export function DataForm({
    fields,
    initialValues,
    schema,
    submit,
    success,
    onSuccess,
    submitLabel = 'Salvar',
    onPendingChange,
}: DataFormProps) {
    const [values, setValues] = useState(initialValues)
    const [errors, setErrors] = useState<Record<string, string>>({})
    const mutation = useAction(submit, success, onSuccess)
    useEffect(() => {
        onPendingChange?.(mutation.isPending)
    }, [mutation.isPending, onPendingChange])
    const backendErrors =
        mutation.error instanceof ApiError ? mutation.error.fields : {}
    return (
        <form
            noValidate
            onSubmit={(event) => {
                event.preventDefault()
                const parsed = schema.safeParse(values)
                if (!parsed.success) {
                    setErrors(
                        Object.fromEntries(
                            parsed.error.issues.map((issue) => [
                                String(issue.path[0]),
                                issue.message,
                            ]),
                        ),
                    )
                    return
                }
                setErrors({})
                mutation.mutate(parsed.data)
            }}
            className="space-y-5"
        >
            <fieldset disabled={mutation.isPending} className="space-y-5">
                {fields.map((field) => {
                    const error =
                        errors[field.name] || backendErrors[field.name]
                    const id = `field-${field.name}`
                    const change = (value: string) => {
                        setValues((previous) => ({
                            ...previous,
                            [field.name]: value,
                        }))
                        setErrors((previous) => ({
                            ...previous,
                            [field.name]: '',
                        }))
                        if (mutation.isError) mutation.reset()
                    }
                    const props = {
                        id,
                        value: values[field.name] ?? '',
                        disabled: mutation.isPending,
                        onChange: change,
                    }
                    return (
                        <div className="space-y-2" key={field.name}>
                            <Label htmlFor={id}>{field.label}</Label>
                            {field.render ? (
                                field.render(props)
                            ) : field.options ? (
                                <select
                                    id={id}
                                    className="select-control"
                                    value={props.value}
                                    onChange={(e) => change(e.target.value)}
                                    aria-invalid={!!error}
                                    aria-describedby={
                                        error ? `${id}-error` : undefined
                                    }
                                >
                                    {field.options.map((option) => (
                                        <option
                                            key={option.value}
                                            value={option.value}
                                        >
                                            {option.label}
                                        </option>
                                    ))}
                                </select>
                            ) : field.type === 'textarea' ? (
                                <Textarea
                                    id={id}
                                    value={props.value}
                                    onChange={(e) => change(e.target.value)}
                                    rows={4}
                                    aria-invalid={!!error}
                                    aria-describedby={
                                        error ? `${id}-error` : undefined
                                    }
                                />
                            ) : (
                                <Input
                                    id={id}
                                    type={field.type ?? 'text'}
                                    step={
                                        field.type === 'number'
                                            ? 'any'
                                            : undefined
                                    }
                                    value={props.value}
                                    onChange={(e) => change(e.target.value)}
                                    autoComplete={field.autoComplete}
                                    aria-invalid={!!error}
                                    aria-describedby={
                                        error ? `${id}-error` : undefined
                                    }
                                />
                            )}
                            {field.hint && (
                                <p className="text-sm text-muted-foreground">
                                    {field.hint}
                                </p>
                            )}
                            {error && (
                                <p
                                    id={`${id}-error`}
                                    role="alert"
                                    className="text-sm text-destructive"
                                >
                                    {error}
                                </p>
                            )}
                        </div>
                    )
                })}
            </fieldset>
            <Button
                className="w-full"
                type="submit"
                disabled={mutation.isPending}
            >
                {mutation.isPending && (
                    <LoaderCircle className="animate-spin" size={16} />
                )}{' '}
                {mutation.isPending ? 'Aguarde…' : submitLabel}
            </Button>
        </form>
    )
}
