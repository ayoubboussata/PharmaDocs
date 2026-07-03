export interface AuthResponse {
  token: string
  email: string
  expiresAt: string
}

export type DocumentStatus = 'Pending' | 'Processed' | 'Failed'

export interface DocumentSummary {
  id: string
  fileName: string
  status: DocumentStatus
  uploadedAt: string
  supplierName: string | null
  invoiceNumber: string | null
  invoiceDate: string | null
  totalAmount: number | null
  currency: string | null
}

export interface InvoiceLineItem {
  id: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface ExtractedInvoice {
  id: string
  supplierName: string
  invoiceNumber: string
  invoiceDate: string | null
  totalAmount: number
  currency: string
  lineItems: InvoiceLineItem[]
}

export interface DocumentDetail {
  id: string
  fileName: string
  contentType: string
  fileSizeBytes: number
  status: DocumentStatus
  uploadedAt: string
  errorMessage: string | null
  extractedInvoice: ExtractedInvoice | null
}
