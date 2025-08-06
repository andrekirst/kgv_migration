/**
 * Form Components Index für KGV Management System
 * 
 * Zentraler Export aller Form-Komponenten, Hooks und Utilities
 */

// =============================================================================
// FORM PROVIDER & UTILITIES
// =============================================================================

export {
  KGVFormProvider,
  FormField,
  useKGVForm,
  useFormContext,
  useFormStatus,
  useFormActions,
  useUnsavedChangesWarning,
  type FormContextValue,
  type KGVFormProviderProps,
  type UseKGVFormProps,
  type FormFieldProps
} from './form-provider'

// =============================================================================
// FORM COMPONENTS
// =============================================================================

export { default as BezirkForm, type BezirkFormProps } from './bezirk-form'

export { default as ParzelleForm, type ParzelleFormProps } from './parzelle-form'

export { default as AntragForm, type AntragFormProps } from './antrag-form'

// =============================================================================
// FORM HOOKS
// =============================================================================

export {
  useBezirkCreateForm,
  useBezirkEditForm,
  useParzelleCreateForm,
  useParzelleEditForm,
  useAntragCreateForm,
  useAntragEditForm,
  useAsyncValidation,
  useBezirkNameValidation,
  useParzellenNummerValidation,
  useEmailValidation,
  useOptimisticFormUpdate,
  useFormResetConfirmation,
  type UseBezirkCreateFormOptions,
  type UseBezirkEditFormOptions,
  type UseParzelleCreateFormOptions,
  type UseParzelleEditFormOptions,
  type UseAntragCreateFormOptions,
  type UseAntragEditFormOptions,
  type UseAsyncValidationOptions
} from '../../hooks/forms/use-form-mutations'

// =============================================================================
// VALIDATION SCHEMAS & UTILITIES
// =============================================================================

export {
  // Schemas
  bezirkCreateSchema,
  bezirkUpdateSchema,
  parzelleCreateSchema,
  parzelleUpdateSchema,
  parzellenAssignmentSchema,
  antragCreateSchema,
  antragUpdateSchema,
  verlaufCreateSchema,
  bezirkeFilterSchema,
  parzellenFilterSchema,
  antraegeFilterSchema,
  bezirkSearchSchema,
  generalSearchSchema,
  bulkParzelleOperationSchema,
  exportRequestSchema,
  
  // Types
  type BezirkCreateFormData,
  type BezirkUpdateFormData,
  type ParzelleCreateFormData,
  type ParzelleUpdateFormData,
  type ParzellenAssignmentFormData,
  type AntragCreateFormData,
  type AntragUpdateFormData,
  type VerlaufCreateFormData,
  type BezirkeFilterFormData,
  type ParzellenFilterFormData,
  type AntraegeFilterFormData,
  type BezirkSearchFormData,
  type GeneralSearchFormData,
  type BulkParzelleOperationFormData,
  type ExportRequestFormData
} from '../../lib/validation/schemas'

export {
  // Utilities
  setGermanZodMessages,
  getFieldErrorMessage,
  getAllErrorMessages,
  hasFieldError,
  getErrorCount,
  formatValidationErrorsForToast,
  
  // Validators
  validateGermanPostalCode,
  validateGermanPhoneNumber,
  validateGermanName,
  validateParzellenNummer,
  validateEuroAmount,
  validateSquareMeters,
  validatePastDate,
  validateFutureDate,
  validateAge,
  
  // Async Validators
  validateUniqueBezirkName,
  validateUniqueParzellenNummer,
  validateUniqueEmail,
  
  // Transformation Utilities
  transformFormDataForApi,
  cleanEmptyStrings,
  cleanNullValues,
  
  // Formatting
  formatPhoneNumberForDisplay,
  formatPostalCodeForDisplay,
  formatCurrencyForDisplay,
  formatSquareMetersForDisplay,
  
  // Focus Utilities
  focusFirstErrorField,
  scrollToField,
  
  // Form State Utilities
  isFormDirty,
  areFieldsDirty,
  getDirtyFieldCount
} from '../../lib/validation/form-utils'

// =============================================================================
// RE-EXPORTS (für Convenience)
// =============================================================================

// React Hook Form
export {
  useForm,
  useFormContext as useRHFFormContext,
  useWatch,
  useController,
  Controller,
  FormProvider,
  type UseFormProps,
  type UseFormReturn,
  type FieldValues,
  type FieldError,
  type FieldErrors,
  type Control,
  type RegisterOptions
} from 'react-hook-form'

// Zod
export { z } from 'zod'
export { zodResolver } from '@hookform/resolvers/zod'

// =============================================================================
// CONSTANTS & CONFIGURATIONS
// =============================================================================

export const FORM_CONFIG = {
  // Default Form Settings
  DEFAULT_MODE: 'onBlur' as const,
  DEFAULT_REVALIDATE_MODE: 'onChange' as const,
  DEFAULT_CRITERIA_MODE: 'all' as const,
  
  // Debounce Settings
  VALIDATION_DEBOUNCE_MS: 300,
  ASYNC_VALIDATION_DEBOUNCE_MS: 500,
  
  // Toast Settings
  SUCCESS_TOAST_DURATION: 3000,
  ERROR_TOAST_DURATION: 5000,
  WARNING_TOAST_DURATION: 4000,
  
  // Field Length Limits
  MAX_NAME_LENGTH: 50,
  MAX_DESCRIPTION_LENGTH: 500,
  MAX_COMMENT_LENGTH: 1000,
  MAX_PHONE_LENGTH: 20,
  MAX_EMAIL_LENGTH: 100,
  MAX_ADDRESS_LENGTH: 100,
  
  // Numeric Limits
  MIN_SQUARE_METERS: 1,
  MAX_SQUARE_METERS: 10000,
  MIN_MONTHLY_RENT: 0,
  MAX_MONTHLY_RENT: 10000,
  MIN_DEPOSIT: 0,
  MAX_DEPOSIT: 50000,
  MIN_NOTICE_PERIOD: 1,
  MAX_NOTICE_PERIOD: 24,
  
  // Age Limits
  MIN_AGE: 18,
  MAX_AGE: 120
} as const

export const VALIDATION_MESSAGES = {
  REQUIRED: 'Dieses Feld ist erforderlich',
  INVALID_EMAIL: 'Ungültige E-Mail-Adresse',
  INVALID_PHONE: 'Ungültige Telefonnummer',
  INVALID_POSTAL_CODE: 'Ungültige PLZ (5 Ziffern erforderlich)',
  INVALID_NAME: 'Name darf nur Buchstaben, Leerzeichen und Bindestriche enthalten',
  INVALID_NUMBER: 'Ungültige Zahl',
  INVALID_DATE: 'Ungültiges Datum',
  DATE_IN_FUTURE: 'Datum darf nicht in der Zukunft liegen',
  DATE_IN_PAST: 'Datum darf nicht in der Vergangenheit liegen',
  VALUE_TOO_SMALL: 'Wert ist zu klein',
  VALUE_TOO_LARGE: 'Wert ist zu groß',
  TEXT_TOO_SHORT: 'Text ist zu kurz',
  TEXT_TOO_LONG: 'Text ist zu lang',
  NOT_UNIQUE: 'Dieser Wert ist bereits vergeben',
  PASSWORDS_DONT_MATCH: 'Passwörter stimmen nicht überein',
  TERMS_NOT_ACCEPTED: 'Bitte akzeptieren Sie die Nutzungsbedingungen'
} as const

// =============================================================================
// HELPER FUNCTIONS
// =============================================================================

/**
 * Erstellt ein vollständiges Formular-Setup für eine Entität
 */
export function createEntityFormSetup<TCreateData, TUpdateData, TEntity>(config: {
  createSchema: z.ZodType<TCreateData>
  updateSchema: z.ZodType<TUpdateData>
  createDefaults: TCreateData
  entityName: string
  queryKeys: {
    lists: () => string[]
    detail: (id: string | number) => string[]
    dropdown?: () => string[]
    statistics?: () => string[]
  }
}) {
  return {
    schemas: {
      create: config.createSchema,
      update: config.updateSchema
    },
    defaults: {
      create: config.createDefaults
    },
    queryKeys: config.queryKeys,
    entityName: config.entityName
  }
}

/**
 * Standardisierte Erfolgs-Toast Nachricht
 */
export function showSuccessToast(action: 'erstellt' | 'aktualisiert' | 'gelöscht', entityName: string) {
  toast.success(`${entityName} erfolgreich ${action}`, {
    duration: FORM_CONFIG.SUCCESS_TOAST_DURATION,
    position: 'top-right'
  })
}

/**
 * Standardisierte Fehler-Toast Nachricht
 */
export function showErrorToast(action: 'erstellen' | 'aktualisieren' | 'löschen', entityName: string, error?: string) {
  const errorMessage = error || 'Ein unbekannter Fehler ist aufgetreten'
  toast.error(`Fehler beim ${action} von ${entityName}: ${errorMessage}`, {
    duration: FORM_CONFIG.ERROR_TOAST_DURATION,
    position: 'top-right'
  })
}

/**
 * Standardisierte Validierungs-Toast Nachricht
 */
export function showValidationToast(errors: Record<string, string>) {
  const message = formatValidationErrorsForToast(errors)
  toast.error(`Validierungsfehler: ${message}`, {
    duration: FORM_CONFIG.ERROR_TOAST_DURATION,
    position: 'top-right'
  })
}

// Toast import für Helper Functions
import toast from 'react-hot-toast'