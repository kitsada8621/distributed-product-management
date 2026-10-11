import TextField, { type TextFieldProps } from '@mui/material/TextField'

type FormFieldProps = Omit<TextFieldProps, 'error'> & { error?: string }

export function FormField({ error, helperText, ...rest }: FormFieldProps) {
  return <TextField fullWidth margin="normal" {...rest} error={Boolean(error)} helperText={error ?? helperText} />
}
