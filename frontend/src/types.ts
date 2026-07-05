export interface AuthResponse {
  token: string
  email: string
  role: string
  expiresAt: string
}

export interface CreatedUserResponse {
  email: string
  role: string
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
  category: string | null
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
  subtotalAmount: number
  vatRate: number | null
  vatAmount: number
  totalAmount: number
  currency: string
  category: string | null
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

// --- Kennisassistent (RAG) ---

export interface KnowledgeSource {
  sourceName: string
  chunkCount: number
  indexedAt: string
}

export interface KnowledgeIngestResponse {
  sourceName: string
  chunkCount: number
}

export interface AskResponse {
  answer: string
  sources: string[]
}

// --- Dashboard ---

export interface SpendByLabel {
  label: string
  total: number
  count: number
}

export interface SpendByMonth {
  month: string
  total: number
  count: number
}

export interface DashboardSummary {
  totalSpend: number
  invoiceCount: number
  currency: string
  bySupplier: SpendByLabel[]
  byMonth: SpendByMonth[]
  byCategory: SpendByLabel[]
}
