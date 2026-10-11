export interface Product {
  id: string
  name: string
  description: string
  price: number
  categoryId: string | null
  categoryName: string | null
  inStock: boolean
  stock: number | null
}

export interface ProductListQuery {
  q: string
  categoryId: string
  page: number
}
