/**
 * Form Validation Utilities für KGV Management System
 * 
 * Deutsche Lokalisierung, Custom Validators und Utility Functions
 * für React Hook Form und Zod Integration
 */

import { z } from 'zod'
import { FieldError, FieldErrors, Path, FieldValues } from 'react-hook-form'

// =============================================================================
// DEUTSCHE FEHLERMELDUNGEN KONFIGURATION
// =============================================================================

/**
 * Deutsche Fehlermeldungen für Zod
 */
export const deutscheZodMessages = {
  required_error: 'Dieses Feld ist erforderlich',
  invalid_type_error: 'Ungültiger Wert',
  too_small: 'Wert ist zu klein',
  too_big: 'Wert ist zu groß',
  invalid_string: 'Ungültige Zeichenkette',
  invalid_number: 'Ungültige Zahl',
  invalid_date: 'Ungültiges Datum',
  invalid_email: 'Ungültige E-Mail-Adresse',
  invalid_url: 'Ungültige URL',
  invalid_enum_value: 'Ungültiger Wert für Auswahl'
}

/**
 * Setzt deutsche Fehlermeldungen für Zod global
 */
export function setGermanZodMessages() {
  z.setErrorMap((issue, ctx) => {
    switch (issue.code) {
      case z.ZodIssueCode.invalid_type:
        if (issue.expected === 'string') {
          return { message: 'Text erforderlich' }
        }
        if (issue.expected === 'number') {
          return { message: 'Zahl erforderlich' }
        }
        if (issue.expected === 'boolean') {
          return { message: 'Ja/Nein-Wert erforderlich' }
        }
        return { message: 'Ungültiger Typ' }

      case z.ZodIssueCode.too_small:
        if (issue.type === 'string') {
          if (issue.minimum === 1) {
            return { message: 'Dieses Feld ist erforderlich' }
          }
          return { message: `Mindestens ${issue.minimum} Zeichen erforderlich` }
        }
        if (issue.type === 'number') {
          return { message: `Mindestens ${issue.minimum} erforderlich` }
        }
        if (issue.type === 'array') {
          return { message: `Mindestens ${issue.minimum} Element(e) erforderlich` }
        }
        return { message: 'Wert zu klein' }

      case z.ZodIssueCode.too_big:
        if (issue.type === 'string') {
          return { message: `Maximal ${issue.maximum} Zeichen erlaubt` }
        }
        if (issue.type === 'number') {
          return { message: `Maximal ${issue.maximum} erlaubt` }
        }
        if (issue.type === 'array') {
          return { message: `Maximal ${issue.maximum} Element(e) erlaubt` }
        }
        return { message: 'Wert zu groß' }

      case z.ZodIssueCode.invalid_string:
        if (issue.validation === 'email') {
          return { message: 'Ungültige E-Mail-Adresse' }
        }
        if (issue.validation === 'url') {
          return { message: 'Ungültige URL' }
        }
        if (issue.validation === 'regex') {
          return { message: 'Ungültiges Format' }
        }
        return { message: 'Ungültige Zeichenkette' }

      case z.ZodIssueCode.invalid_date:
        return { message: 'Ungültiges Datum' }

      case z.ZodIssueCode.invalid_enum_value:
        return { message: 'Bitte wählen Sie eine gültige Option' }

      case z.ZodIssueCode.unrecognized_keys:
        return { message: 'Unbekannte Felder gefunden' }

      case z.ZodIssueCode.invalid_arguments:
        return { message: 'Ungültige Argumente' }

      case z.ZodIssueCode.invalid_return_type:
        return { message: 'Ungültiger Rückgabetyp' }

      case z.ZodIssueCode.invalid_union:
        return { message: 'Ungültiger Wert' }

      case z.ZodIssueCode.invalid_union_discriminator:
        return { message: 'Unterscheidungsmerkmal ungültig' }

      case z.ZodIssueCode.invalid_literal:
        return { message: `Wert muss "${issue.expected}" sein` }

      case z.ZodIssueCode.custom:
        return { message: issue.message || 'Ungültiger Wert' }

      default:
        return { message: ctx.defaultError }
    }
  })
}

// =============================================================================
// FORM UTILITY FUNCTIONS
// =============================================================================

/**
 * Extrahiert die erste Fehlermeldung aus einem FieldError
 */
export function getFieldErrorMessage(error: FieldError | undefined): string | undefined {
  if (!error) return undefined
  return error.message
}

/**
 * Extrahiert alle Fehlermeldungen aus FieldErrors
 */
export function getAllErrorMessages<T extends FieldValues>(
  errors: FieldErrors<T>
): Record<string, string> {
  const messages: Record<string, string> = {}
  
  Object.keys(errors).forEach((key) => {
    const error = errors[key as Path<T>]
    if (error) {
      messages[key] = getFieldErrorMessage(error) || 'Ungültiger Wert'
    }
  })
  
  return messages
}

/**
 * Überprüft, ob ein Feld einen Fehler hat
 */
export function hasFieldError<T extends FieldValues>(
  errors: FieldErrors<T>,
  fieldName: Path<T>
): boolean {
  return !!errors[fieldName]
}

/**
 * Zählt die Gesamtzahl der Fehler
 */
export function getErrorCount<T extends FieldValues>(errors: FieldErrors<T>): number {
  return Object.keys(errors).length
}

/**
 * Formatiert Validation Errors für Toast Nachrichten
 */
export function formatValidationErrorsForToast<T extends FieldValues>(
  errors: FieldErrors<T>
): string {
  const errorMessages = getAllErrorMessages(errors)
  const messages = Object.values(errorMessages)
  
  if (messages.length === 1) {
    return messages[0]
  }
  
  if (messages.length <= 3) {
    return messages.join(', ')
  }
  
  return `${messages.slice(0, 2).join(', ')} und ${messages.length - 2} weitere Fehler`
}

// =============================================================================
// CUSTOM VALIDATORS
// =============================================================================

/**
 * Validator für deutsche PLZ
 */
export const validateGermanPostalCode = (value: string): boolean => {
  if (!value) return true // Optional field
  return /^[0-9]{5}$/.test(value)
}

/**
 * Validator für deutsche Telefonnummern
 */
export const validateGermanPhoneNumber = (value: string): boolean => {
  if (!value) return true // Optional field
  const cleanedValue = value.replace(/[\s\-\/()]/g, '')
  return /^(\+49|0)[1-9][0-9]{6,14}$/.test(cleanedValue)
}

/**
 * Validator für deutsche Namen (mit Umlauten)
 */
export const validateGermanName = (value: string): boolean => {
  if (!value) return false
  return /^[a-zA-ZäöüÄÖÜß\-\s']+$/.test(value.trim())
}

/**
 * Validator für Parzellennummern
 */
export const validateParzellenNummer = (value: string): boolean => {
  if (!value) return false
  return /^[a-zA-Z0-9\-\/]{1,20}$/.test(value.trim())
}

/**
 * Validator für Geldbeträge (Euro)
 */
export const validateEuroAmount = (value: number): boolean => {
  return value >= 0 && value <= 999999.99
}

/**
 * Validator für Flächenangaben (Quadratmeter)
 */
export const validateSquareMeters = (value: number): boolean => {
  return value > 0 && value <= 100000
}

/**
 * Validator für Datum (nicht in der Zukunft)
 */
export const validatePastDate = (value: string): boolean => {
  if (!value) return true
  const date = new Date(value)
  const today = new Date()
  today.setHours(23, 59, 59, 999) // Ende des heutigen Tages
  return date <= today
}

/**
 * Validator für Datum (nicht in der Vergangenheit)
 */
export const validateFutureDate = (value: string): boolean => {
  if (!value) return true
  const date = new Date(value)
  const today = new Date()
  today.setHours(0, 0, 0, 0) // Beginn des heutigen Tages
  return date >= today
}

/**
 * Validator für Altersangaben (18-120 Jahre)
 */
export const validateAge = (birthDate: string): boolean => {
  if (!birthDate) return true
  const birth = new Date(birthDate)
  const today = new Date()
  const age = today.getFullYear() - birth.getFullYear()
  const monthDiff = today.getMonth() - birth.getMonth()
  
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) {
    return age - 1 >= 18 && age - 1 <= 120
  }
  
  return age >= 18 && age <= 120
}

// =============================================================================
// ASYNC VALIDATORS
// =============================================================================

/**
 * Async Validator für eindeutige Bezirksnamen
 */
export const validateUniqueBezirkName = async (
  name: string,
  currentId?: number,
  apiClient?: any
): Promise<boolean> => {
  if (!name || !apiClient) return true
  
  try {
    const response = await apiClient.get(`/bezirke/check-name?name=${encodeURIComponent(name)}&excludeId=${currentId || ''}`)
    return response.data?.isUnique !== false
  } catch (error) {
    console.warn('Eindeutigkeitsprüfung fehlgeschlagen:', error)
    return true // Bei Fehlern erlauben wir die Eingabe
  }
}

/**
 * Async Validator für eindeutige Parzellennummern
 */
export const validateUniqueParzellenNummer = async (
  nummer: string,
  bezirkId: number,
  currentId?: number,
  apiClient?: any
): Promise<boolean> => {
  if (!nummer || !bezirkId || !apiClient) return true
  
  try {
    const response = await apiClient.get(
      `/parzellen/check-nummer?nummer=${encodeURIComponent(nummer)}&bezirkId=${bezirkId}&excludeId=${currentId || ''}`
    )
    return response.data?.isUnique !== false
  } catch (error) {
    console.warn('Eindeutigkeitsprüfung fehlgeschlagen:', error)
    return true
  }
}

/**
 * Async Validator für E-Mail-Adressen (Duplikatsprüfung)
 */
export const validateUniqueEmail = async (
  email: string,
  currentId?: string,
  apiClient?: any
): Promise<boolean> => {
  if (!email || !apiClient) return true
  
  try {
    const response = await apiClient.get(
      `/antraege/check-email?email=${encodeURIComponent(email)}&excludeId=${currentId || ''}`
    )
    return response.data?.isUnique !== false
  } catch (error) {
    console.warn('E-Mail-Eindeutigkeitsprüfung fehlgeschlagen:', error)
    return true
  }
}

// =============================================================================
// FORM TRANSFORMATION UTILITIES
// =============================================================================

/**
 * Transformiert Form-Daten für API-Request
 */
export function transformFormDataForApi<T extends Record<string, any>>(
  data: T,
  transformations?: Partial<Record<keyof T, (value: any) => any>>
): T {
  const transformed = { ...data }
  
  if (transformations) {
    Object.keys(transformations).forEach((key) => {
      const transformer = transformations[key as keyof T]
      if (transformer && key in transformed) {
        transformed[key as keyof T] = transformer(transformed[key as keyof T])
      }
    })
  }
  
  return transformed
}

/**
 * Bereinigt leere Strings zu undefined
 */
export function cleanEmptyStrings<T extends Record<string, any>>(data: T): T {
  const cleaned = { ...data }
  
  Object.keys(cleaned).forEach((key) => {
    const value = cleaned[key as keyof T]
    if (typeof value === 'string' && value.trim() === '') {
      cleaned[key as keyof T] = undefined as any
    }
  })
  
  return cleaned
}

/**
 * Bereinigt null-Werte zu undefined
 */
export function cleanNullValues<T extends Record<string, any>>(data: T): T {
  const cleaned = { ...data }
  
  Object.keys(cleaned).forEach((key) => {
    if (cleaned[key as keyof T] === null) {
      cleaned[key as keyof T] = undefined as any
    }
  })
  
  return cleaned
}

/**
 * Formatiert Telefonnummern für die Anzeige
 */
export function formatPhoneNumberForDisplay(phone: string): string {
  if (!phone) return ''
  
  const cleaned = phone.replace(/[\s\-\/()]/g, '')
  
  // Deutsche Handynummer
  if (cleaned.startsWith('+49') && cleaned.length === 14) {
    return `+49 ${cleaned.slice(3, 6)} ${cleaned.slice(6, 10)} ${cleaned.slice(10)}`
  }
  
  // Deutsche Festnetznummer mit Vorwahl
  if (cleaned.startsWith('0') && cleaned.length >= 10) {
    const areaCode = cleaned.slice(0, 4)
    const number = cleaned.slice(4)
    return `${areaCode} ${number.slice(0, 3)} ${number.slice(3)}`
  }
  
  return phone // Fallback: Original zurückgeben
}

/**
 * Formatiert PLZ für die Anzeige
 */
export function formatPostalCodeForDisplay(plz: string): string {
  if (!plz) return ''
  return plz.replace(/(\d{5})/, '$1')
}

/**
 * Formatiert Geldbeträge für die Anzeige
 */
export function formatCurrencyForDisplay(amount: number): string {
  return new Intl.NumberFormat('de-DE', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(amount)
}

/**
 * Formatiert Quadratmeter für die Anzeige
 */
export function formatSquareMetersForDisplay(sqm: number): string {
  return new Intl.NumberFormat('de-DE', {
    style: 'unit',
    unit: 'square-meter',
    minimumFractionDigits: 0,
    maximumFractionDigits: 2
  }).format(sqm)
}

// =============================================================================
// FIELD FOCUS UTILITIES
// =============================================================================

/**
 * Fokussiert das erste Feld mit einem Fehler
 */
export function focusFirstErrorField<T extends FieldValues>(
  errors: FieldErrors<T>,
  formRef: React.RefObject<HTMLFormElement>
): void {
  if (!formRef.current || Object.keys(errors).length === 0) return
  
  const firstErrorField = Object.keys(errors)[0]
  const fieldElement = formRef.current.querySelector(`[name="${firstErrorField}"]`) as HTMLElement
  
  if (fieldElement && typeof fieldElement.focus === 'function') {
    fieldElement.focus()
    
    // Scroll zu dem Feld, falls es nicht sichtbar ist
    fieldElement.scrollIntoView({
      behavior: 'smooth',
      block: 'center'
    })
  }
}

/**
 * Scrollt zu einem spezifischen Feld
 */
export function scrollToField(
  fieldName: string,
  formRef: React.RefObject<HTMLFormElement>
): void {
  if (!formRef.current) return
  
  const fieldElement = formRef.current.querySelector(`[name="${fieldName}"]`) as HTMLElement
  
  if (fieldElement) {
    fieldElement.scrollIntoView({
      behavior: 'smooth',
      block: 'center'
    })
  }
}

// =============================================================================
// FORM STATE UTILITIES
// =============================================================================

/**
 * Überprüft, ob das Formular "dirty" (verändert) ist
 */
export function isFormDirty<T extends FieldValues>(
  formState: { isDirty: boolean; dirtyFields: Partial<Record<keyof T, boolean>> }
): boolean {
  return formState.isDirty || Object.keys(formState.dirtyFields).length > 0
}

/**
 * Überprüft, ob spezifische Felder verändert wurden
 */
export function areFieldsDirty<T extends FieldValues>(
  dirtyFields: Partial<Record<keyof T, boolean>>,
  fieldsToCheck: (keyof T)[]
): boolean {
  return fieldsToCheck.some(field => dirtyFields[field])
}

/**
 * Zählt die Anzahl der veränderten Felder
 */
export function getDirtyFieldCount<T extends FieldValues>(
  dirtyFields: Partial<Record<keyof T, boolean>>
): number {
  return Object.values(dirtyFields).filter(Boolean).length
}

// Initialisiere deutsche Zod-Nachrichten beim Import
setGermanZodMessages()