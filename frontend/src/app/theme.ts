import { createTheme } from '@mui/material/styles'

declare module '@mui/material/styles' {
  interface TypeBackground {
    surface2: string
    hero: string
  }
}

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#0C8276', dark: '#0A6A60', light: '#D9F0EB', contrastText: '#fff' },
    secondary: { main: '#F2A33A', contrastText: '#3A2400' },
    success: { main: '#2E9E6F' },
    warning: { main: '#E39A1F' },
    error: { main: '#D0453B' },
    info: { main: '#3B82A6' },
    background: { default: '#F6F9F7', paper: '#FFFFFF', surface2: '#EAF3F0', hero: '#0B5F57' },
    text: { primary: '#1F2D2B', secondary: '#587270' },
    divider: '#DFE9E5',
  },
  shape: { borderRadius: 12 },
  typography: {
    fontFamily: '"IBM Plex Sans Thai","Inter",system-ui,sans-serif',
    h4: { fontSize: 28, fontWeight: 600, letterSpacing: '-0.01em' },
    h6: { fontSize: 18, fontWeight: 600 },
    body1: { fontSize: 15, lineHeight: 1.6 },
    button: { textTransform: 'none', fontWeight: 600 },
  },
  components: {
    MuiButtonBase: {
      styleOverrides: {
        root: { '&.Mui-focusVisible': { outline: '2px solid #0C8276', outlineOffset: 2 } },
      },
    },
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: { root: { borderRadius: 10, paddingInline: 20 } },
    },
    MuiIconButton: {
      styleOverrides: { root: { minWidth: 40, minHeight: 40 } },
    },
    MuiPaper: {
      defaultProps: { elevation: 0 },
      styleOverrides: { root: { border: '1px solid #DFE9E5' } },
    },
    MuiAppBar: {
      defaultProps: { elevation: 0, color: 'inherit' },
      styleOverrides: {
        root: { backgroundColor: '#fff', borderBottom: '1px solid #DFE9E5' },
      },
    },
    MuiTableHead: {
      styleOverrides: {
        root: {
          '& .MuiTableCell-head': { backgroundColor: '#EAF3F0', fontWeight: 600, color: '#587270' },
        },
      },
    },
    MuiTableRow: {
      styleOverrides: { root: { '&:hover': { backgroundColor: '#F6F9F7' } } },
    },
    MuiOutlinedInput: {
      styleOverrides: { root: { backgroundColor: '#fff', borderRadius: 10 } },
    },
    MuiChip: {
      styleOverrides: { root: { fontWeight: 600 } },
    },
  },
})
