export function formatDate(isoUtc: string, timeZone?: string): string {
  return new Intl.DateTimeFormat('en-GB', {
    dateStyle: 'medium',
    timeStyle: 'short',
    hour12: false,
    timeZone,
  }).format(new Date(isoUtc))
}
