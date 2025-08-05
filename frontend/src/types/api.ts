// API Types for KGV Frontend Application
export interface ApiResponse<T> {
  data: T
  success: boolean
  message?: string
  errors?: string[]
}

export interface PaginatedResponse<T> {
  data: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface ApiError {
  message: string
  status: number
  details?: string[]
}

// Authentication Types
export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  refreshToken: string
  expiresAt: string
  user: User
}

export interface User {
  id: string
  email: string
  name: string
  roles: string[]
}

// Antrag (Application) Types
export interface AntragDto {
  id: string
  aktenzeichen?: string
  wartelistenNr32?: string
  wartelistenNr33?: string
  anrede?: string
  titel?: string
  vorname?: string
  nachname?: string
  anrede2?: string
  titel2?: string
  vorname2?: string
  nachname2?: string
  briefanrede?: string
  strasse?: string
  plz?: string
  ort?: string
  telefon?: string
  mobilTelefon?: string
  geschTelefon?: string
  mobilTelefon2?: string
  eMail?: string
  bewerbungsdatum?: string
  bestaetigungsdatum?: string
  aktuellesAngebot?: string
  loeschdatum?: string
  wunsch?: string
  vermerk?: string
  aktiv: boolean
  deaktiviertAm?: string
  geburtstag?: string
  geburtstag2?: string
  status: AntragStatus
  statusBeschreibung: string
  vollName: string
  vollName2?: string
  vollAdresse?: string
  createdAt: string
  updatedAt?: string
  createdBy?: string
  updatedBy?: string
  verlauf: VerlaufDto[]
}

export interface AntragListDto {
  id: string
  aktenzeichen?: string
  vollName: string
  bewerbungsdatum?: string
  status: AntragStatus
  statusBeschreibung: string
  ort?: string
  aktiv: boolean
  updatedAt?: string
}

export interface CreateAntragRequest {
  anrede?: string
  titel?: string
  vorname: string
  nachname: string
  anrede2?: string
  titel2?: string
  vorname2?: string
  nachname2?: string
  briefanrede?: string
  strasse?: string
  plz?: string
  ort?: string
  telefon?: string
  mobilTelefon?: string
  geschTelefon?: string
  mobilTelefon2?: string
  eMail?: string
  bewerbungsdatum?: string
  wunsch?: string
  vermerk?: string
  geburtstag?: string
  geburtstag2?: string
}

export interface UpdateAntragRequest extends Partial<CreateAntragRequest> {
  id: string
}

// Bezirk (District) Types
export interface BezirkDto {
  id: string
  name: string
  katasterbezirke: KatasterbezirkDto[]
  createdAt: string
  updatedAt?: string
}

export interface KatasterbezirkDto {
  id: string
  bezirkId: string
  katasterbezirk: string
  katasterbezirkName: string
  createdAt: string
  updatedAt?: string
}

// Verlauf (History) Types
export interface VerlaufDto {
  id: string
  antragId: string
  art: VerlaufArt
  datum: string
  gemarkung?: string
  flur?: string
  parzelle?: string
  groesse?: string
  sachbearbeiter?: string
  hinweis?: string
  kommentar?: string
  createdAt: string
  createdBy?: string
}

// Enums
export enum AntragStatus {
  Neu = 0,
  InBearbeitung = 1,
  Wartend = 2,
  Genehmigt = 3,
  Abgelehnt = 4,
  Archiviert = 5
}

export enum VerlaufArt {
  Antrag = 'ANTR',
  Angebot = 'ANGE',
  Zusage = 'ZUSA',
  Absage = 'ABSA',
  Archiv = 'ARCH'
}

export enum Anrede {
  Herr = 'Herr',
  Frau = 'Frau',
  Divers = 'Divers'
}

// Filter and Sort Types
export interface AntragFilter {
  search?: string
  status?: AntragStatus[]
  bezirk?: string[]
  aktiv?: boolean
  bewerbungsdatumVon?: string
  bewerbungsdatumBis?: string
  ort?: string[]
}

export interface SortOption {
  field: string
  direction: 'asc' | 'desc'
}

export interface PaginationParams {
  pageNumber: number
  pageSize: number
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

// Dashboard Types
export interface DashboardStats {
  totalAntraege: number
  activeAntraege: number
  pendingAntraege: number
  approvedThisMonth: number
  averageProcessingTime: number
  statusDistribution: StatusDistribution[]
  monthlyTrends: MonthlyTrend[]
  bezirkStatistics: BezirkStatistic[]
}

export interface StatusDistribution {
  status: AntragStatus
  count: number
  percentage: number
}

export interface MonthlyTrend {
  month: string
  antraege: number
  genehmigt: number
  abgelehnt: number
}

export interface BezirkStatistic {
  bezirkName: string
  totalAntraege: number
  pendingAntraege: number
  averageProcessingTime: number
}

// Export/Report Types
export interface ExportRequest {
  format: 'excel' | 'pdf' | 'csv'
  filter?: AntragFilter
  includeHistory?: boolean
  dateRange?: {
    from: string
    to: string
  }
}

export interface ReportData {
  fileName: string
  contentType: string
  data: Blob
}