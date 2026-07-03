export interface AuthResponse {
  token: string
  email: string
  expiresAt: string
}

export interface DocumentSummary {
  id: string
  fileName: string
  status: string
  uploadedAt: string
  supplierName: string | null
  invoiceNumber: string | null
  invoiceDate: string | null
  totalAmount: number | null
  currency: string | null
}
