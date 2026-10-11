import Box from '@mui/material/Box'
import Drawer from '@mui/material/Drawer'
import List from '@mui/material/List'
import ListItemButton from '@mui/material/ListItemButton'
import { NavLink } from 'react-router'
import { PATHS } from '@/shared/config/paths'

const ADMIN_LINKS = [
  { label: 'สินค้า', to: PATHS.adminProducts },
  { label: 'หมวดหมู่', to: PATHS.adminCategories },
  { label: 'คำสั่งซื้อ', to: PATHS.adminOrders },
]

const DRAWER_WIDTH = 240
const APP_BAR_HEIGHT = 64

interface AdminDrawerProps {
  variant: 'permanent' | 'temporary'
  open: boolean
  onClose: () => void
}

export function AdminDrawer({ variant, open, onClose }: AdminDrawerProps) {
  return (
    <Drawer
      variant={variant}
      open={open}
      onClose={onClose}
      sx={{ width: variant === 'permanent' ? DRAWER_WIDTH : 0, flexShrink: 0 }}
      slotProps={{
        paper: {
          sx: {
            width: DRAWER_WIDTH,
            top: variant === 'permanent' ? APP_BAR_HEIGHT : 0,
            height: variant === 'permanent' ? `calc(100% - ${APP_BAR_HEIGHT}px)` : '100%',
            bgcolor: 'background.surface2',
            borderRight: '1px solid',
            borderColor: 'divider',
          },
        },
      }}
    >
      <Box component="nav" aria-label="เมนูผู้ดูแลระบบ" sx={{ p: 1.5 }}>
        <List disablePadding>
          {ADMIN_LINKS.map((link) => (
            <ListItemButton
              key={link.to}
              component={NavLink}
              to={link.to}
              onClick={onClose}
              sx={{ borderRadius: 2, '&.active': { bgcolor: 'primary.light', color: 'primary.dark' } }}
            >
              {link.label}
            </ListItemButton>
          ))}
        </List>
      </Box>
    </Drawer>
  )
}
