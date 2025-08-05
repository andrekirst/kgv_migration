// Utility functions for the KGV Frontend Application
import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'
import { format, parseISO, formatDistanceToNow, isValid } from 'date-fns'
import { de } from 'date-fns/locale'

/**
 * Combine classes with tailwind-merge
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/**
 * Format date for German locale
 */
export function formatDate(date: string | Date | null | undefined, formatStr: string = 'dd.MM.yyyy'): string {
  if (!date) return '-'
  
  try {
    const dateObj = typeof date === 'string' ? parseISO(date) : date
    if (!isValid(dateObj)) return '-'
    return format(dateObj, formatStr, { locale: de })
  } catch {
    return '-'
  }
}

/**
 * Format date with time for German locale
 */
export function formatDateTime(date: string | Date | null | undefined): string {
  return formatDate(date, 'dd.MM.yyyy HH:mm')
}

/**
 * Format relative time for German locale
 */
export function formatRelativeTime(date: string | Date | null | undefined): string {
  if (!date) return '-'
  
  try {
    const dateObj = typeof date === 'string' ? parseISO(date) : date
    if (!isValid(dateObj)) return '-'
    return formatDistanceToNow(dateObj, { addSuffix: true, locale: de })
  } catch {
    return '-'
  }
}

/**
 * Format German phone number
 */
export function formatPhoneNumber(phone: string | null | undefined): string {
  if (!phone) return '-'
  
  // Remove all non-digits
  const cleaned = phone.replace(/\D/g, '')
  
  // German mobile format
  if (cleaned.startsWith('49') && cleaned.length === 12) {
    return `+49 ${cleaned.slice(2, 5)} ${cleaned.slice(5, 8)} ${cleaned.slice(8)}`
  }
  
  // German landline format
  if (cleaned.startsWith('49') && cleaned.length === 11) {
    return `+49 ${cleaned.slice(2, 5)} ${cleaned.slice(5)}`
  }
  
  // Local format
  if (cleaned.length === 11) {
    return `0${cleaned.slice(1, 4)} ${cleaned.slice(4)}`
  }
  
  return phone
}

/**
 * Format German postal code
 */
export function formatPostalCode(plz: string | null | undefined): string {
  if (!plz) return '-'
  return plz.padStart(5, '0')
}

/**
 * Format full German address
 */
export function formatAddress(strasse?: string, plz?: string, ort?: string): string {
  const parts = []
  if (strasse) parts.push(strasse)
  if (plz || ort) {
    const location = [formatPostalCode(plz), ort].filter(Boolean).join(' ')
    if (location.trim()) parts.push(location)
  }
  return parts.join(', ') || '-'
}

/**
 * Format full name
 */
export function formatFullName(
  anrede?: string,
  titel?: string,
  vorname?: string,
  nachname?: string
): string {
  const parts = []
  if (anrede) parts.push(anrede)
  if (titel) parts.push(titel)
  if (vorname) parts.push(vorname)
  if (nachname) parts.push(nachname)
  return parts.join(' ') || '-'
}

/**
 * Get initials from name
 */
export function getInitials(name: string): string {
  return name
    .split(' ')
    .map(word => word.charAt(0).toUpperCase())
    .slice(0, 2)
    .join('')
}

/**
 * Validate German email
 */
export function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

/**
 * Validate German phone number
 */
export function isValidPhoneNumber(phone: string): boolean {
  const cleaned = phone.replace(/\D/g, '')
  return cleaned.length >= 10 && cleaned.length <= 15
}

/**
 * Validate German postal code
 */
export function isValidPostalCode(plz: string): boolean {
  return /^\d{5}$/.test(plz)
}

/**
 * Truncate text with ellipsis
 */
export function truncate(text: string | null | undefined, maxLength: number): string {
  if (!text) return '-'
  if (text.length <= maxLength) return text
  return text.slice(0, maxLength) + '...'
}

/**
 * Sleep utility for testing
 */
export function sleep(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms))
}

/**
 * Debounce function
 */
export function debounce<T extends (...args: unknown[]) => void>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout: NodeJS.Timeout | null = null
  
  return (...args: Parameters<T>) => {
    if (timeout) clearTimeout(timeout)
    timeout = setTimeout(() => func(...args), wait)
  }
}

/**
 * Generate random ID
 */
export function generateId(): string {
  return Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15)
}

/**
 * Convert bytes to human readable format
 */
export function formatBytes(bytes: number, decimals: number = 2): string {
  if (bytes === 0) return '0 Bytes'

  const k = 1024
  const dm = decimals < 0 ? 0 : decimals
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB']

  const i = Math.floor(Math.log(bytes) / Math.log(k))

  return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i]
}

/**
 * Check if string is empty or whitespace
 */
export function isEmpty(value: string | null | undefined): boolean {
  return !value || value.trim().length === 0
}

/**
 * Capitalize first letter
 */
export function capitalize(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1).toLowerCase()
}

/**
 * Convert camelCase to kebab-case
 */
export function camelToKebab(str: string): string {
  return str.replace(/([a-z0-9]|(?=[A-Z]))([A-Z])/g, '$1-$2').toLowerCase()
}

/**
 * Deep clone object
 */
export function deepClone<T>(obj: T): T {
  if (obj === null || typeof obj !== 'object') return obj
  if (obj instanceof Date) return new Date(obj.getTime()) as unknown as T
  if (obj instanceof Array) return obj.map(item => deepClone(item)) as unknown as T
  if (typeof obj === 'object') {
    const clonedObj = {} as T
    for (const key in obj) {
      if (obj.hasOwnProperty(key)) {
        clonedObj[key] = deepClone(obj[key])
      }
    }
    return clonedObj
  }
  return obj
}

/**
 * Compare two objects for equality
 */
export function isEqual(obj1: unknown, obj2: unknown): boolean {
  if (obj1 === obj2) return true
  if (obj1 == null || obj2 == null) return false
  if (typeof obj1 !== typeof obj2) return false
  
  if (typeof obj1 === 'object') {
    const keys1 = Object.keys(obj1 as object)
    const keys2 = Object.keys(obj2 as object)
    
    if (keys1.length !== keys2.length) return false
    
    for (const key of keys1) {
      if (!keys2.includes(key)) return false
      if (!isEqual((obj1 as Record<string, unknown>)[key], (obj2 as Record<string, unknown>)[key])) return false
    }
    
    return true
  }
  
  return false
}