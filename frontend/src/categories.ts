// Vaste kostenpost-categorieën — moet exact overeenkomen met de enum in de
// AI-service (ai-service/app/extraction.py) en de prompt.
export const INVOICE_CATEGORIES = [
  'Geneesmiddelen',
  'Medisch materiaal',
  'Parafarmacie',
  'Kantoor & administratie',
  'IT & software',
  'Onderhoud & energie',
  'Diensten',
  'Overige',
] as const

export type InvoiceCategory = (typeof INVOICE_CATEGORIES)[number]
