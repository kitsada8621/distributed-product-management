export function isValidQuantity(quantity: number): boolean {
  return Number.isInteger(quantity) && quantity > 0
}
