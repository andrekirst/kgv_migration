// Comprehensive error handling utilities for React Query and API operations
import toast from 'react-hot-toast'
import type { ApiError } from '@/types/api'

// German error messages for common scenarios
export const GERMAN_ERROR_MESSAGES = {
  // Network errors
  NETWORK_ERROR: 'Netzwerkfehler. Bitte überprüfen Sie Ihre Internetverbindung.',
  TIMEOUT_ERROR: 'Die Anfrage dauerte zu lange. Bitte versuchen Sie es erneut.',
  CONNECTION_ERROR: 'Verbindung zum Server fehlgeschlagen.',
  
  // Authentication errors
  UNAUTHORIZED: 'Sie sind nicht angemeldet. Bitte melden Sie sich an.',
  FORBIDDEN: 'Sie haben nicht die erforderlichen Berechtigungen für diese Aktion.',
  TOKEN_EXPIRED: 'Ihre Sitzung ist abgelaufen. Bitte melden Sie sich erneut an.',
  
  // Client errors
  BAD_REQUEST: 'Ungültige Anfrage. Bitte überprüfen Sie Ihre Eingaben.',
  NOT_FOUND: 'Die angeforderte Ressource wurde nicht gefunden.',
  CONFLICT: 'Die Aktion kann nicht ausgeführt werden, da ein Konflikt vorliegt.',
  VALIDATION_ERROR: 'Eingabevalidierung fehlgeschlagen. Bitte korrigieren Sie Ihre Eingaben.',
  
  // Server errors
  INTERNAL_SERVER_ERROR: 'Ein Serverfehler ist aufgetreten. Bitte versuchen Sie es später erneut.',
  SERVICE_UNAVAILABLE: 'Der Service ist momentan nicht verfügbar. Bitte versuchen Sie es später.',
  GATEWAY_TIMEOUT: 'Server-Timeout. Bitte versuchen Sie es erneut.',
  
  // Rate limiting
  TOO_MANY_REQUESTS: 'Zu viele Anfragen. Bitte warten Sie einen Moment.',
  
  // Generic fallbacks
  UNKNOWN_ERROR: 'Ein unbekannter Fehler ist aufgetreten.',
  OPERATION_FAILED: 'Die Aktion konnte nicht ausgeführt werden.',
} as const

// Error severity levels
export enum ErrorSeverity {
  LOW = 'low',
  MEDIUM = 'medium',
  HIGH = 'high',
  CRITICAL = 'critical'
}

// Enhanced error type with context
export interface EnhancedError extends ApiError {
  severity: ErrorSeverity
  context?: string
  timestamp: Date
  userMessage: string
  technicalMessage?: string
}

/**
 * Maps HTTP status codes to German error messages and severity levels
 */
export function mapStatusToError(status: number, context?: string): {
  message: string
  severity: ErrorSeverity
} {
  const errorMap: Record<number, { message: string; severity: ErrorSeverity }> = {
    // 4xx Client Errors
    400: { message: GERMAN_ERROR_MESSAGES.BAD_REQUEST, severity: ErrorSeverity.MEDIUM },
    401: { message: GERMAN_ERROR_MESSAGES.UNAUTHORIZED, severity: ErrorSeverity.HIGH },
    403: { message: GERMAN_ERROR_MESSAGES.FORBIDDEN, severity: ErrorSeverity.HIGH },
    404: { message: GERMAN_ERROR_MESSAGES.NOT_FOUND, severity: ErrorSeverity.LOW },
    408: { message: GERMAN_ERROR_MESSAGES.TIMEOUT_ERROR, severity: ErrorSeverity.MEDIUM },
    409: { message: GERMAN_ERROR_MESSAGES.CONFLICT, severity: ErrorSeverity.MEDIUM },
    422: { message: GERMAN_ERROR_MESSAGES.VALIDATION_ERROR, severity: ErrorSeverity.MEDIUM },
    429: { message: GERMAN_ERROR_MESSAGES.TOO_MANY_REQUESTS, severity: ErrorSeverity.MEDIUM },
    
    // 5xx Server Errors
    500: { message: GERMAN_ERROR_MESSAGES.INTERNAL_SERVER_ERROR, severity: ErrorSeverity.HIGH },
    502: { message: GERMAN_ERROR_MESSAGES.SERVICE_UNAVAILABLE, severity: ErrorSeverity.HIGH },
    503: { message: GERMAN_ERROR_MESSAGES.SERVICE_UNAVAILABLE, severity: ErrorSeverity.HIGH },
    504: { message: GERMAN_ERROR_MESSAGES.GATEWAY_TIMEOUT, severity: ErrorSeverity.HIGH },
  }

  return errorMap[status] || {
    message: GERMAN_ERROR_MESSAGES.UNKNOWN_ERROR,
    severity: ErrorSeverity.MEDIUM
  }
}

/**
 * Enhances a basic API error with additional context and German messages
 */
export function enhanceError(error: ApiError, context?: string): EnhancedError {
  const { message: userMessage, severity } = mapStatusToError(error.status, context)
  
  return {
    ...error,
    severity,
    context,
    timestamp: new Date(),
    userMessage,
    technicalMessage: error.message !== userMessage ? error.message : undefined
  }
}

/**
 * Centralized error handler for React Query operations
 */
export class ErrorHandler {
  private static instance: ErrorHandler
  private errorLog: EnhancedError[] = []
  private maxLogSize = 100

  static getInstance(): ErrorHandler {
    if (!ErrorHandler.instance) {
      ErrorHandler.instance = new ErrorHandler()
    }
    return ErrorHandler.instance
  }

  /**
   * Handle and log an error
   */
  handleError(error: ApiError | Error, context?: string): EnhancedError {
    let enhancedError: EnhancedError

    if ('status' in error) {
      // API Error
      enhancedError = enhanceError(error, context)
    } else {
      // Generic Error
      enhancedError = {
        message: error.message,
        status: 0,
        details: [],
        severity: ErrorSeverity.MEDIUM,
        context,
        timestamp: new Date(),
        userMessage: this.getGenericErrorMessage(error.message),
        technicalMessage: error.message
      }
    }

    // Log the error
    this.logError(enhancedError)

    // Show appropriate notification
    this.showErrorNotification(enhancedError)

    return enhancedError
  }

  /**
   * Handle React Query errors specifically
   */
  handleQueryError(error: any, context?: string): void {
    console.error(`Query Error in ${context}:`, error)
    
    if (error?.status) {
      this.handleError(error as ApiError, context)
    } else {
      // Network or other errors
      const enhancedError: EnhancedError = {
        message: error?.message || 'Unknown error',
        status: 0,
        details: [],
        severity: ErrorSeverity.MEDIUM,
        context,
        timestamp: new Date(),
        userMessage: GERMAN_ERROR_MESSAGES.NETWORK_ERROR,
        technicalMessage: error?.message
      }
      
      this.logError(enhancedError)
      this.showErrorNotification(enhancedError)
    }
  }

  /**
   * Handle mutation errors with additional context
   */
  handleMutationError(error: any, operation: string, data?: any): void {
    console.error(`Mutation Error (${operation}):`, error, 'Data:', data)
    
    const context = `${operation} operation`
    this.handleQueryError(error, context)
  }

  /**
   * Show appropriate error notification based on severity
   */
  private showErrorNotification(error: EnhancedError): void {
    const duration = this.getNotificationDuration(error.severity)
    
    switch (error.severity) {
      case ErrorSeverity.CRITICAL:
        toast.error(error.userMessage, {
          duration: duration,
          position: 'top-center',
          style: {
            background: '#fee2e2',
            border: '1px solid #fecaca',
            color: '#dc2626',
            fontWeight: '600'
          }
        })
        break
        
      case ErrorSeverity.HIGH:
        toast.error(error.userMessage, {
          duration: duration,
          position: 'top-right',
          style: {
            background: '#fef2f2',
            border: '1px solid #fecaca',
            color: '#dc2626'
          }
        })
        break
        
      case ErrorSeverity.MEDIUM:
        toast.error(error.userMessage, {
          duration: duration,
          position: 'top-right'
        })
        break
        
      case ErrorSeverity.LOW:
        // For low severity errors, show a less intrusive notification
        if (process.env.NODE_ENV === 'development') {
          toast.error(error.userMessage, {
            duration: 3000,
            position: 'bottom-right'
          })
        }
        break
    }
  }

  /**
   * Get notification duration based on severity
   */
  private getNotificationDuration(severity: ErrorSeverity): number {
    switch (severity) {
      case ErrorSeverity.CRITICAL:
        return 10000 // 10 seconds
      case ErrorSeverity.HIGH:
        return 8000  // 8 seconds
      case ErrorSeverity.MEDIUM:
        return 6000  // 6 seconds
      case ErrorSeverity.LOW:
        return 3000  // 3 seconds
      default:
        return 5000  // 5 seconds
    }
  }

  /**
   * Log error to internal array (could be extended to send to external service)
   */
  private logError(error: EnhancedError): void {
    this.errorLog.unshift(error)
    
    // Keep log size manageable
    if (this.errorLog.length > this.maxLogSize) {
      this.errorLog = this.errorLog.slice(0, this.maxLogSize)
    }

    // In production, you might want to send critical errors to a logging service
    if (process.env.NODE_ENV === 'production' && error.severity === ErrorSeverity.CRITICAL) {
      this.sendToLoggingService(error)
    }
  }

  /**
   * Get generic error message for unknown errors
   */
  private getGenericErrorMessage(technicalMessage: string): string {
    if (technicalMessage.toLowerCase().includes('network')) {
      return GERMAN_ERROR_MESSAGES.NETWORK_ERROR
    }
    if (technicalMessage.toLowerCase().includes('timeout')) {
      return GERMAN_ERROR_MESSAGES.TIMEOUT_ERROR
    }
    return GERMAN_ERROR_MESSAGES.UNKNOWN_ERROR
  }

  /**
   * Send critical errors to external logging service
   */
  private sendToLoggingService(error: EnhancedError): void {
    // Implementation would depend on your logging service
    // Example: Sentry, LogRocket, custom endpoint, etc.
    console.error('Critical Error:', error)
  }

  /**
   * Get recent errors for debugging
   */
  getRecentErrors(limit: number = 10): EnhancedError[] {
    return this.errorLog.slice(0, limit)
  }

  /**
   * Clear error log
   */
  clearErrorLog(): void {
    this.errorLog = []
  }

  /**
   * Get error statistics
   */
  getErrorStats(): {
    total: number
    bySeverity: Record<ErrorSeverity, number>
    byStatus: Record<number, number>
  } {
    const stats = {
      total: this.errorLog.length,
      bySeverity: {
        [ErrorSeverity.LOW]: 0,
        [ErrorSeverity.MEDIUM]: 0,
        [ErrorSeverity.HIGH]: 0,
        [ErrorSeverity.CRITICAL]: 0
      },
      byStatus: {} as Record<number, number>
    }

    this.errorLog.forEach(error => {
      stats.bySeverity[error.severity]++
      stats.byStatus[error.status] = (stats.byStatus[error.status] || 0) + 1
    })

    return stats
  }
}

// Export singleton instance
export const errorHandler = ErrorHandler.getInstance()

/**
 * React Query error handler function
 */
export const handleQueryError = (error: any, context?: string) => {
  errorHandler.handleQueryError(error, context)
}

/**
 * React Query mutation error handler function
 */
export const handleMutationError = (error: any, operation: string, data?: any) => {
  errorHandler.handleMutationError(error, operation, data)
}

/**
 * Utility to create user-friendly error messages for forms
 */
export function createFormErrorMessage(error: ApiError): {
  general?: string
  fields: Record<string, string>
} {
  const result: { general?: string; fields: Record<string, string> } = {
    fields: {}
  }

  if (error.status === 422 && error.details?.length) {
    // Validation errors - map to form fields
    error.details.forEach(detail => {
      if (typeof detail === 'string') {
        result.general = detail
      } else {
        // Assume detail has field and message properties
        const fieldError = detail as any
        if (fieldError.field && fieldError.message) {
          result.fields[fieldError.field] = fieldError.message
        }
      }
    })
  } else {
    // General error
    const { message } = mapStatusToError(error.status)
    result.general = message
  }

  return result
}

/**
 * Utility to check if an error should be retried
 */
export function shouldRetryError(error: ApiError): boolean {
  // Don't retry client errors (4xx) except rate limiting
  if (error.status >= 400 && error.status < 500 && error.status !== 429) {
    return false
  }
  
  // Retry server errors and network errors
  return true
}

/**
 * Type guard to check if error is an API error
 */
export function isApiError(error: any): error is ApiError {
  return error && typeof error.status === 'number' && typeof error.message === 'string'
}