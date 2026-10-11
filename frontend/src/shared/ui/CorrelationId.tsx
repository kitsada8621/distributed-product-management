import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import Box from '@mui/material/Box'
import IconButton from '@mui/material/IconButton'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'

export function CorrelationId({ id }: { id: string }) {
  return (
    <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
      <Typography variant="caption">รหัสอ้างอิง:</Typography>
      <Box component="code" sx={{ fontFamily: 'monospace', fontSize: 12 }}>
        {id}
      </Box>
      <IconButton size="small" aria-label="คัดลอกรหัสอ้างอิง" onClick={() => void navigator.clipboard.writeText(id)}>
        <ContentCopyIcon fontSize="inherit" />
      </IconButton>
    </Stack>
  )
}
