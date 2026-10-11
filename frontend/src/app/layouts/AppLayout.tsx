import MenuIcon from '@mui/icons-material/Menu'
import Box from '@mui/material/Box'
import IconButton from '@mui/material/IconButton'
import useMediaQuery from '@mui/material/useMediaQuery'
import { useTheme } from '@mui/material/styles'
import { useState } from 'react'
import { Outlet, useLocation } from 'react-router'
import { AdminDrawer } from './AdminDrawer'
import { AppHeader } from './AppHeader'

export function AppLayout() {
  const isAdminRoute = useLocation().pathname.startsWith('/admin')
  const desktop = useMediaQuery(useTheme().breakpoints.up('md'))
  const [menuOpen, setMenuOpen] = useState(false)

  if (!isAdminRoute) {
    return (
      <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
        <AppHeader />
        <Outlet />
      </Box>
    )
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppHeader />
      <Box sx={{ display: 'flex' }}>
        <AdminDrawer
          variant={desktop ? 'permanent' : 'temporary'}
          open={desktop || menuOpen}
          onClose={() => setMenuOpen(false)}
        />
        <Box component="main" sx={{ flexGrow: 1, minWidth: 0 }}>
          {desktop ? null : (
            <IconButton aria-label="เปิดเมนูผู้ดูแลระบบ" onClick={() => setMenuOpen(true)} sx={{ ml: 1, mt: 1 }}>
              <MenuIcon />
            </IconButton>
          )}
          <Outlet />
        </Box>
      </Box>
    </Box>
  )
}
