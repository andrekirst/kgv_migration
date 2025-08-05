/**
 * Zod Schema Definitions für KGV Management System
 * 
 * Comprehensive validation schemas with German error messages
 * for all entities in the KGV system.
 */

import { z } from 'zod'
import { ParzellenStatus, AntragStatus, VerlaufArt, Anrede } from '@/types/api'

// =============================================================================
// UTILITY SCHEMAS
// =============================================================================

/**
 * Deutsche Telefonnummer Validation
 * Unterstützt: +49 123 456789, 0123 456789, 0123/456789, etc.
 */
const telefonnummerSchema = z
  .string()
  .optional()
  .refine(
    (val) => {
      if (!val) return true
      const phoneRegex = /^(\+49|0)[1-9][0-9\s\-\/]{7,14}$/
      return phoneRegex.test(val.replace(/\s/g, ''))
    },
    { message: 'Bitte geben Sie eine gültige deutsche Telefonnummer ein' }
  )

/**
 * Deutsche PLZ Validation
 */
const plzSchema = z
  .string()
  .optional()
  .refine(
    (val) => {
      if (!val) return true
      return /^[0-9]{5}$/.test(val)
    },
    { message: 'PLZ muss aus 5 Ziffern bestehen' }
  )

/**
 * Email Schema mit deutschen Fehlermeldungen
 */
const emailSchema = z
  .string()
  .optional()
  .refine(
    (val) => {
      if (!val) return true
      return z.string().email().safeParse(val).success
    },
    { message: 'Bitte geben Sie eine gültige E-Mail-Adresse ein' }
  )

/**
 * Adresse Schema
 */
const adresseSchema = z.object({
  strasse: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.trim().length >= 2,
      { message: 'Straße muss mindestens 2 Zeichen haben' }
    ),
  hausnummer: z
    .string()
    .optional()
    .refine(
      (val) => !val || /^[0-9]+[a-zA-Z]?$/.test(val),
      { message: 'Ungültige Hausnummer' }
    ),
  plz: plzSchema,
  ort: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.trim().length >= 2,
      { message: 'Ort muss mindestens 2 Zeichen haben' }
    )
})

/**
 * Name Validation (für Personen)
 */
const nameSchema = z
  .string()
  .min(1, 'Dieses Feld ist erforderlich')
  .min(2, 'Name muss mindestens 2 Zeichen haben')
  .max(50, 'Name darf maximal 50 Zeichen haben')
  .regex(
    /^[a-zA-ZäöüÄÖÜß\s\-']+$/,
    'Name darf nur Buchstaben, Leerzeichen, Bindestriche und Apostrophe enthalten'
  )

/**
 * Optionaler Name (für zweite Person bei Anträgen)
 */
const optionalNameSchema = z
  .string()
  .optional()
  .refine(
    (val) => {
      if (!val) return true
      return nameSchema.safeParse(val).success
    },
    { message: 'Name muss mindestens 2 Zeichen haben und darf nur Buchstaben enthalten' }
  )

/**
 * Datum Schema (ISO String oder Date)
 */
const datumSchema = z
  .string()
  .optional()
  .refine(
    (val) => {
      if (!val) return true
      return !isNaN(Date.parse(val))
    },
    { message: 'Ungültiges Datum' }
  )

// =============================================================================
// BEZIRK SCHEMAS
// =============================================================================

/**
 * Bezirk Erstellung Schema
 */
export const bezirkCreateSchema = z.object({
  name: z
    .string()
    .min(1, 'Bezirksname ist erforderlich')
    .min(2, 'Bezirksname muss mindestens 2 Zeichen haben')
    .max(100, 'Bezirksname darf maximal 100 Zeichen haben')
    .regex(
      /^[a-zA-ZäöüÄÖÜß0-9\s\-().]+$/,
      'Bezirksname darf nur Buchstaben, Zahlen, Leerzeichen und Sonderzeichen (-.()) enthalten'
    ),
  beschreibung: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 500,
      { message: 'Beschreibung darf maximal 500 Zeichen haben' }
    ),
  bezirksleiter: optionalNameSchema,
  telefon: telefonnummerSchema,
  email: emailSchema,
  adresse: adresseSchema.optional()
})

/**
 * Bezirk Update Schema
 */
export const bezirkUpdateSchema = bezirkCreateSchema.partial().extend({
  aktiv: z.boolean().optional()
})

/**
 * Bezirk Typ Inferenz
 */
export type BezirkCreateFormData = z.infer<typeof bezirkCreateSchema>
export type BezirkUpdateFormData = z.infer<typeof bezirkUpdateSchema>

// =============================================================================
// PARZELLE SCHEMAS
// =============================================================================

/**
 * Parzellen Nummer Schema
 */
const parzellenNummerSchema = z
  .string()
  .min(1, 'Parzellennummer ist erforderlich')
  .max(20, 'Parzellennummer darf maximal 20 Zeichen haben')
  .regex(
    /^[a-zA-Z0-9\-\/]+$/,
    'Parzellennummer darf nur Buchstaben, Zahlen, Bindestriche und Schrägstriche enthalten'
  )

/**
 * Parzelle Erstellung Schema
 */
export const parzelleCreateSchema = z.object({
  nummer: parzellenNummerSchema,
  bezirkId: z
    .number()
    .positive('Bitte wählen Sie einen gültigen Bezirk'),
  groesse: z
    .number()
    .positive('Größe muss eine positive Zahl sein')
    .min(1, 'Mindestgröße ist 1 m²')
    .max(10000, 'Maximalgröße ist 10.000 m²'),
  beschreibung: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 1000,
      { message: 'Beschreibung darf maximal 1000 Zeichen haben' }
    ),
  ausstattung: z
    .array(z.string())
    .default([])
    .refine(
      (arr) => arr.every(item => item.length <= 100),
      { message: 'Jede Ausstattung darf maximal 100 Zeichen haben' }
    ),
  monatlichePacht: z
    .number()
    .nonnegative('Pacht darf nicht negativ sein')
    .max(10000, 'Maximale Pacht ist 10.000 €'),
  kaution: z
    .number()
    .nonnegative('Kaution darf nicht negativ sein')
    .max(50000, 'Maximale Kaution ist 50.000 €')
    .optional(),
  kuendigungsfrist: z
    .number()
    .int('Kündigungsfrist muss eine ganze Zahl sein')
    .min(1, 'Mindeste Kündigungsfrist ist 1 Monat')
    .max(24, 'Maximale Kündigungsfrist ist 24 Monate'),
  adresse: adresseSchema.optional(),
  bemerkungen: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 1000,
      { message: 'Bemerkungen dürfen maximal 1000 Zeichen haben' }
    )
})

/**
 * Parzelle Update Schema
 */
export const parzelleUpdateSchema = parzelleCreateSchema.partial().extend({
  status: z.nativeEnum(ParzellenStatus).optional(),
  aktiv: z.boolean().optional()
})

/**
 * Parzellen Assignment Schema
 */
export const parzellenAssignmentSchema = z.object({
  parzelleId: z.number().positive(),
  mieterId: z.number().positive(),
  mieterVorname: nameSchema,
  mieterNachname: nameSchema,
  mieterEmail: emailSchema,
  mieterTelefon: telefonnummerSchema,
  mietbeginn: z
    .string()
    .min(1, 'Mietbeginn ist erforderlich')
    .refine(
      (val) => !isNaN(Date.parse(val)),
      { message: 'Ungültiges Datum für Mietbeginn' }
    ),
  mietende: datumSchema,
  monatlichePacht: z
    .number()
    .nonnegative('Pacht darf nicht negativ sein'),
  kaution: z
    .number()
    .nonnegative('Kaution darf nicht negativ sein')
    .optional(),
  bemerkungen: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 1000,
      { message: 'Bemerkungen dürfen maximal 1000 Zeichen haben' }
    )
})

/**
 * Parzelle Typ Inferenz
 */
export type ParzelleCreateFormData = z.infer<typeof parzelleCreateSchema>
export type ParzelleUpdateFormData = z.infer<typeof parzelleUpdateSchema>
export type ParzellenAssignmentFormData = z.infer<typeof parzellenAssignmentSchema>

// =============================================================================
// ANTRAG SCHEMAS
// =============================================================================

/**
 * Antrag Erstellung Schema
 */
export const antragCreateSchema = z.object({
  // Erste Person (erforderlich)
  anrede: z.nativeEnum(Anrede).optional(),
  titel: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 20,
      { message: 'Titel darf maximal 20 Zeichen haben' }
    ),
  vorname: nameSchema,
  nachname: nameSchema,
  
  // Zweite Person (optional)
  anrede2: z.nativeEnum(Anrede).optional(),
  titel2: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 20,
      { message: 'Titel darf maximal 20 Zeichen haben' }
    ),
  vorname2: optionalNameSchema,
  nachname2: optionalNameSchema,
  
  // Kontaktdaten
  briefanrede: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 100,
      { message: 'Briefanrede darf maximal 100 Zeichen haben' }
    ),
  strasse: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.trim().length >= 3,
      { message: 'Straße muss mindestens 3 Zeichen haben' }
    ),
  plz: plzSchema,
  ort: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.trim().length >= 2,
      { message: 'Ort muss mindestens 2 Zeichen haben' }
    ),
  
  // Telefonnummern
  telefon: telefonnummerSchema,
  mobilTelefon: telefonnummerSchema,
  geschTelefon: telefonnummerSchema,
  mobilTelefon2: telefonnummerSchema,
  
  // E-Mail
  eMail: emailSchema,
  
  // Antrags-spezifische Felder
  bewerbungsdatum: datumSchema,
  wunsch: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 500,
      { message: 'Wunsch darf maximal 500 Zeichen haben' }
    ),
  vermerk: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 1000,
      { message: 'Vermerk darf maximal 1000 Zeichen haben' }
    ),
  
  // Geburtsdaten
  geburtstag: datumSchema,
  geburtstag2: datumSchema
})

/**
 * Antrag Update Schema
 */
export const antragUpdateSchema = antragCreateSchema.partial().extend({
  id: z.string().min(1, 'ID ist erforderlich'),
  status: z.nativeEnum(AntragStatus).optional(),
  aktuellesAngebot: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 500,
      { message: 'Aktuelles Angebot darf maximal 500 Zeichen haben' }
    ),
  loeschdatum: datumSchema,
  bestaetigungsdatum: datumSchema
})

/**
 * Verlauf Erstellung Schema
 */
export const verlaufCreateSchema = z.object({
  antragId: z.string().min(1, 'Antrag-ID ist erforderlich'),
  art: z.nativeEnum(VerlaufArt),
  datum: z
    .string()
    .min(1, 'Datum ist erforderlich')
    .refine(
      (val) => !isNaN(Date.parse(val)),
      { message: 'Ungültiges Datum' }
    ),
  gemarkung: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 100,
      { message: 'Gemarkung darf maximal 100 Zeichen haben' }
    ),
  flur: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 50,
      { message: 'Flur darf maximal 50 Zeichen haben' }
    ),
  parzelle: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 50,
      { message: 'Parzelle darf maximal 50 Zeichen haben' }
    ),
  groesse: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 50,
      { message: 'Größe darf maximal 50 Zeichen haben' }
    ),
  sachbearbeiter: optionalNameSchema,
  hinweis: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 500,
      { message: 'Hinweis darf maximal 500 Zeichen haben' }
    ),
  kommentar: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 1000,
      { message: 'Kommentar darf maximal 1000 Zeichen haben' }
    )
})

/**
 * Antrag Typ Inferenz
 */
export type AntragCreateFormData = z.infer<typeof antragCreateSchema>
export type AntragUpdateFormData = z.infer<typeof antragUpdateSchema>
export type VerlaufCreateFormData = z.infer<typeof verlaufCreateSchema>

// =============================================================================
// FILTER SCHEMAS
// =============================================================================

/**
 * Bezirke Filter Schema
 */
export const bezirkeFilterSchema = z.object({
  search: z.string().optional(),
  aktiv: z.boolean().optional(),
  hasApplications: z.boolean().optional(),
  hasCadastralAreas: z.boolean().optional(),
  page: z.number().positive().optional(),
  limit: z.number().positive().max(1000).optional(),
  sortBy: z.enum(['name', 'erstelltAm', 'gesamtParzellen', 'aktiveParzellen', 'freieParzellen']).optional(),
  sortOrder: z.enum(['asc', 'desc']).optional()
})

/**
 * Parzellen Filter Schema
 */
export const parzellenFilterSchema = z.object({
  search: z.string().optional(),
  bezirkId: z.number().positive().optional(),
  status: z.array(z.nativeEnum(ParzellenStatus)).optional(),
  aktiv: z.boolean().optional(),
  groesseMin: z.number().nonnegative().optional(),
  groesseMax: z.number().positive().optional(),
  pachtMin: z.number().nonnegative().optional(),
  pachtMax: z.number().positive().optional(),
  page: z.number().positive().optional(),
  limit: z.number().positive().max(1000).optional(),
  sortBy: z.enum(['nummer', 'groesse', 'monatlichePacht', 'erstelltAm']).optional(),
  sortOrder: z.enum(['asc', 'desc']).optional()
}).refine(
  (data) => {
    if (data.groesseMin && data.groesseMax) {
      return data.groesseMin <= data.groesseMax
    }
    return true
  },
  {
    message: 'Mindestgröße muss kleiner oder gleich der Maximalgröße sein',
    path: ['groesseMax']
  }
).refine(
  (data) => {
    if (data.pachtMin && data.pachtMax) {
      return data.pachtMin <= data.pachtMax
    }
    return true
  },
  {
    message: 'Mindestpacht muss kleiner oder gleich der Maximalpacht sein',
    path: ['pachtMax']
  }
)

/**
 * Anträge Filter Schema
 */
export const antraegeFilterSchema = z.object({
  search: z.string().optional(),
  status: z.array(z.nativeEnum(AntragStatus)).optional(),
  bezirk: z.array(z.string()).optional(),
  aktiv: z.boolean().optional(),
  bewerbungsdatumVon: datumSchema,
  bewerbungsdatumBis: datumSchema,
  ort: z.array(z.string()).optional(),
  page: z.number().positive().optional(),
  limit: z.number().positive().max(1000).optional(),
  sortBy: z.string().optional(),
  sortOrder: z.enum(['asc', 'desc']).optional()
}).refine(
  (data) => {
    if (data.bewerbungsdatumVon && data.bewerbungsdatumBis) {
      const von = new Date(data.bewerbungsdatumVon)
      const bis = new Date(data.bewerbungsdatumBis)
      return von <= bis
    }
    return true
  },
  {
    message: 'Von-Datum muss vor oder gleich dem Bis-Datum sein',
    path: ['bewerbungsdatumBis']
  }
)

/**
 * Filter Typ Inferenz
 */
export type BezirkeFilterFormData = z.infer<typeof bezirkeFilterSchema>
export type ParzellenFilterFormData = z.infer<typeof parzellenFilterSchema>
export type AntraegeFilterFormData = z.infer<typeof antraegeFilterSchema>

// =============================================================================
// SEARCH SCHEMAS
// =============================================================================

/**
 * Bezirk Suche Schema
 */
export const bezirkSearchSchema = z.object({
  query: z
    .string()
    .min(1, 'Suchbegriff ist erforderlich')
    .min(2, 'Suchbegriff muss mindestens 2 Zeichen haben'),
  limit: z.number().positive().max(100).optional().default(20),
  activeOnly: z.boolean().optional().default(true),
  fuzzyMatch: z.boolean().optional().default(false)
})

/**
 * Allgemeine Suche Schema
 */
export const generalSearchSchema = z.object({
  query: z
    .string()
    .min(1, 'Suchbegriff ist erforderlich')
    .min(2, 'Suchbegriff muss mindestens 2 Zeichen haben')
    .max(100, 'Suchbegriff darf maximal 100 Zeichen haben'),
  type: z.enum(['bezirke', 'parzellen', 'antraege', 'all']).optional().default('all'),
  limit: z.number().positive().max(100).optional().default(20)
})

/**
 * Search Typ Inferenz
 */
export type BezirkSearchFormData = z.infer<typeof bezirkSearchSchema>
export type GeneralSearchFormData = z.infer<typeof generalSearchSchema>

// =============================================================================
// BULK OPERATION SCHEMAS
// =============================================================================

/**
 * Bulk Parzelle Operation Schema
 */
export const bulkParzelleOperationSchema = z.object({
  parzelleIds: z
    .array(z.number().positive())
    .min(1, 'Mindestens eine Parzelle muss ausgewählt werden')
    .max(100, 'Maximal 100 Parzellen können gleichzeitig bearbeitet werden'),
  operation: z.enum(['activate', 'deactivate', 'delete', 'changeStatus']),
  newStatus: z.nativeEnum(ParzellenStatus).optional(),
  reason: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 500,
      { message: 'Grund darf maximal 500 Zeichen haben' }
    )
}).refine(
  (data) => {
    if (data.operation === 'changeStatus') {
      return data.newStatus !== undefined
    }
    return true
  },
  {
    message: 'Neuer Status ist erforderlich bei Status-Änderung',
    path: ['newStatus']
  }
)

/**
 * Bulk Operation Typ Inferenz
 */
export type BulkParzelleOperationFormData = z.infer<typeof bulkParzelleOperationSchema>

// =============================================================================
// EXPORT SCHEMAS
// =============================================================================

/**
 * Export Request Schema
 */
export const exportRequestSchema = z.object({
  format: z.enum(['excel', 'pdf', 'csv']),
  filter: z.union([
    bezirkeFilterSchema,
    parzellenFilterSchema,
    antraegeFilterSchema
  ]).optional(),
  includeHistory: z.boolean().optional().default(false),
  dateRange: z.object({
    from: z
      .string()
      .refine(
        (val) => !isNaN(Date.parse(val)),
        { message: 'Ungültiges Von-Datum' }
      ),
    to: z
      .string()
      .refine(
        (val) => !isNaN(Date.parse(val)),
        { message: 'Ungültiges Bis-Datum' }
      )
  }).optional().refine(
    (data) => {
      if (data && data.from && data.to) {
        return new Date(data.from) <= new Date(data.to)
      }
      return true
    },
    {
      message: 'Von-Datum muss vor oder gleich dem Bis-Datum sein'
    }
  )
})

/**
 * Export Typ Inferenz
 */
export type ExportRequestFormData = z.infer<typeof exportRequestSchema>