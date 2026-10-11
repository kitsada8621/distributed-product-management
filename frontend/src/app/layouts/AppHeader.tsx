import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import {
  Link as RouterLink,
  NavLink,
  useLocation,
  useNavigate,
} from "react-router";
import { useAuth } from "@/features/auth";
import { PATHS } from "@/shared/config/paths";

const activeNavSx = {
  "&.active": { bgcolor: "primary.light", color: "primary.dark" },
};

export function AppHeader() {
  const { status, user, logout } = useAuth();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const inAdminArea = pathname.startsWith("/admin");

  async function handleLogout() {
    await logout();
    navigate(PATHS.products);
  }

  return (
    <AppBar position="sticky">
      <Toolbar>
        <Typography
          variant="h6"
          component={RouterLink}
          to={PATHS.products}
          sx={{
            color: "primary.dark",
            fontWeight: 700,
            textDecoration: "none",
            mr: 3,
          }}
        >
          สมาร์ทสต็อก
        </Typography>
        <Button
          color="inherit"
          component={NavLink}
          to={PATHS.products}
          end
          sx={activeNavSx}
        >
          สินค้า
        </Button>
        {user ? (
          <Button
            color="inherit"
            component={NavLink}
            to={PATHS.orders}
            sx={activeNavSx}
          >
            คำสั่งซื้อของฉัน
          </Button>
        ) : null}
        {user?.role === "admin" ? (
          <Button
            color="inherit"
            component={RouterLink}
            to={PATHS.adminProducts}
            className={inAdminArea ? "active" : undefined}
            sx={activeNavSx}
          >
            ผู้ดูแลระบบ
          </Button>
        ) : null}
        <Box sx={{ flexGrow: 1 }} />
        {user ? (
          <>
            <Typography variant="body2" color="text.secondary" sx={{ mr: 2 }}>
              {user.name}
            </Typography>
            <Button color="inherit" variant="outlined" onClick={handleLogout}>
              ออกจากระบบ
            </Button>
          </>
        ) : null}
        {status === "guest" ? (
          <>
            <Button
              color="inherit"
              component={NavLink}
              to={PATHS.login}
              sx={activeNavSx}
            >
              เข้าสู่ระบบ
            </Button>
            <Button
              variant="contained"
              component={RouterLink}
              to={PATHS.register}
              sx={{ ml: 1 }}
            >
              สมัครสมาชิก
            </Button>
          </>
        ) : null}
      </Toolbar>
    </AppBar>
  );
}
