import Box from "@mui/material/Box";
import Paper, { type PaperProps } from "@mui/material/Paper";
import Typography from "@mui/material/Typography";
import type { ReactNode } from "react";

interface EmptyStateProps extends PaperProps {
  title: string;
  description?: string;
  icon?: ReactNode;
  action?: ReactNode;
}

export function EmptyState({
  title,
  description,
  icon,
  action,
}: EmptyStateProps) {
  return (
    <Paper
      variant="outlined"
      sx={{
        textAlign: "center",
        py: 6,
        px: 3,
        borderStyle: "dashed",
        mb: "10px",
      }}
    >
      {icon ? (
        <Box sx={{ color: "text.secondary", fontSize: 48 }}>{icon}</Box>
      ) : null}
      <Typography variant="h6">{title}</Typography>
      {description ? (
        <Typography color="text.secondary" sx={{ mt: 1 }}>
          {description}
        </Typography>
      ) : null}
      {action ? <Box sx={{ mt: 2 }}>{action}</Box> : null}
    </Paper>
  );
}
