// TypeScript types for Bezirke (Districts) and Parzellen (Plots) entities
// Enhanced for React Query API integration
export interface Bezirk {
  id: string // Backend uses UUID strings
  name: string
  displayName?: string
  description?: string // Backend field name
  isActive: boolean // Backend field name
  sortOrder: number
  flaeche?: number
  anzahlParzellen: number
  status: string
  createdAt: string // Backend field name
  updatedAt?: string // Backend field name
  
  // Legacy fields for compatibility
  beschreibung?: string
  bezirksleiter?: string
  telefon?: string
  email?: string
  adresse?: {
    strasse?: string
    hausnummer?: string
    plz?: string
    ort?: string
  }
  statistiken?: {
    gesamtParzellen: number
    belegteParzellen: number
    freieParzellen: number
    warteliste: number
  }
  erstelltAm?: string
  aktualisiertAm?: string
  aktiv?: boolean
}

export interface BezirkCreateRequest {
  name: string
  beschreibung?: string
  bezirksleiter?: string
  telefon?: string
  email?: string
  adresse?: {
    strasse?: string
    hausnummer?: string
    plz?: string
    ort?: string
  }
}

export interface BezirkUpdateRequest extends Partial<BezirkCreateRequest> {
  aktiv?: boolean
}

export interface Parzelle {
  id: number
  nummer: string
  bezirkId: number
  bezirkName: string
  groesse: number // in Quadratmetern
  status: ParzellenStatus
  mieter?: {
    id: number
    vorname: string
    nachname: string
    email?: string
    telefon?: string
  }
  adresse?: {
    strasse?: string
    hausnummer?: string
    plz?: string
    ort?: string
  }
  beschreibung?: string
  ausstattung: string[]
  monatlichePacht: number
  kaution?: number
  mietbeginn?: string
  mietende?: string
  kuendigungsfrist: number // in Monaten
  bemerkungen?: string
  erstelltAm: string
  aktualisiertAm: string
  aktiv: boolean
}

export enum ParzellenStatus {
  FREI = 'frei',
  BELEGT = 'belegt',
  RESERVIERT = 'reserviert',
  WARTUNG = 'wartung',
  GESPERRT = 'gesperrt'
}

export interface ParzelleCreateRequest {
  nummer: string
  bezirkId: number
  groesse: number
  beschreibung?: string
  ausstattung: string[]
  monatlichePacht: number
  kaution?: number
  kuendigungsfrist: number
  adresse?: {
    strasse?: string
    hausnummer?: string
    plz?: string
    ort?: string
  }
  bemerkungen?: string
}

export interface ParzelleUpdateRequest extends Partial<ParzelleCreateRequest> {
  status?: ParzellenStatus
  aktiv?: boolean
}

export interface ParzellenAssignment {
  parzelleId: number
  mieterId: number
  mieterVorname: string
  mieterNachname: string
  mieterEmail?: string
  mieterTelefon?: string
  mietbeginn: string
  mietende?: string
  monatlichePacht: number
  kaution?: number
  bemerkungen?: string
}

export interface ParzellenHistory {
  id: number
  parzelleId: number
  aktion: 'erstellt' | 'zugewiesen' | 'freigegeben' | 'aktualisiert' | 'deaktiviert'
  beschreibung: string
  durchgefuehrtVon: string
  zeitpunkt: string
  details?: Record<string, unknown>
}

// API Response types
export interface BezirkeListResponse {
  bezirke: Bezirk[]
  pagination: {
    page: number
    limit: number
    total: number
    totalPages: number
  }
  filters?: BezirkeFilter
}

// List DTO for table/list displays
export interface BezirkListDto {
  id: number
  name: string
  beschreibung?: string
  bezirksleiter?: string
  statistiken: {
    gesamtParzellen: number
    belegteParzellen: number
    freieParzellen: number
    warteliste: number
  }
  erstelltAm: string
  aktiv: boolean
}

export interface ParzellenListResponse {
  parzellen: Parzelle[]
  pagination: {
    page: number
    limit: number
    total: number
    totalPages: number
  }
  filters?: ParzellenFilter
}

// List DTO for table/list displays
export interface ParzelleListDto {
  id: number
  nummer: string
  bezirkId: number
  bezirkName: string
  groesse: number
  status: ParzellenStatus
  mieter?: {
    id: number
    vorname: string
    nachname: string
  }
  monatlichePacht: number
  erstelltAm: string
  aktiv: boolean
}

// Filter and search types
export interface BezirkeFilter {
  search?: string
  aktiv?: boolean
  hasApplications?: boolean
  hasCadastralAreas?: boolean
  page?: number
  limit?: number
  sortBy?: 'name' | 'erstelltAm' | 'gesamtParzellen' | 'aktiveParzellen' | 'freieParzellen'
  sortOrder?: 'asc' | 'desc'
}

// Search parameters for API
export interface BezirkSearchParameters {
  query: string
  limit?: number
  activeOnly?: boolean
  fuzzyMatch?: boolean
}

export interface ParzellenFilter {
  search?: string
  bezirkId?: number
  status?: ParzellenStatus[]
  aktiv?: boolean
  groesseMin?: number
  groesseMax?: number
  pachtMin?: number
  pachtMax?: number
  page?: number
  limit?: number
  sortBy?: 'nummer' | 'groesse' | 'monatlichePacht' | 'erstelltAm'
  sortOrder?: 'asc' | 'desc'
}

// Statistics types
export interface BezirkStatistiken {
  bezirkId: number
  bezirkName: string
  gesamtParzellen: number
  belegteParzellen: number
  freieParzellen: number
  reservierteParzellen: number
  wartungParzellen: number
  gesperrteParzellen: number
  durchschnittsPacht: number
  gesamtEinnahmen: number
  warteliste: number
  auslastung: number // Prozent
  letzteAktualisierung: string
}

// Search result types
export interface BezirkSearchResult {
  id: number
  name: string
  beschreibung?: string
  aktiv: boolean
}

export interface ParzelleSearchResult {
  id: number
  nummer: string
  bezirkId: number
  bezirkName: string
  status: ParzellenStatus
  groesse: number
  monatlichePacht: number
  aktiv: boolean
}

export interface GesamtStatistiken {
  gesamtBezirke: number
  aktiveBezirke: number
  gesamtParzellen: number
  belegteParzellen: number
  freieParzellen: number
  reservierteParzellen: number
  wartungParzellen: number
  gesperrteParzellen: number
  durchschnittsPacht: number
  gesamtEinnahmen: number
  auslastung: number
  trends: {
    neueAntraege: number
    kuendigungen: number
    neueParzellen: number
    zeitraum: string
  }
  letzteAktualisierung: string
}

// Error handling types
export interface ValidationError {
  field: string
  message: string
  value?: any
}

export interface BezirkApiError {
  message: string
  status: number
  details?: ValidationError[]
  timestamp: string
}

// Operation result types
export interface BezirkOperationResult<T = any> {
  success: boolean
  data?: T
  message?: string
  errors?: ValidationError[]
}

// Bulk operation types
export interface BulkParzelleOperation {
  parzelleIds: number[]
  operation: 'activate' | 'deactivate' | 'delete' | 'changeStatus'
  newStatus?: ParzellenStatus
  reason?: string
}

export interface BulkOperationResult {
  total: number
  successful: number
  failed: number
  errors: Array<{
    id: number
    error: string
  }>
}