/**
 * Antrag Form Component für KGV Management System
 * 
 * Vollständiges Formular für Antrag-Erstellung und -Bearbeitung
 * mit Zod Validation, deutscher Lokalisierung und React Query Integration
 */

'use client'

import React from 'react'
import { Controller } from 'react-hook-form'
import { 
  Loader2, Save, X, User, Mail, Phone, Calendar,
  Users, MapPin, FileText, Clock
} from 'lucide-react'
import { 
  useKGVForm, 
  KGVFormProvider, 
  FormField,
  useFormStatus,
  useFormActions 
} from './form-provider'
import { 
  antragCreateSchema, 
  antragUpdateSchema,
  type AntragCreateFormData,
  type AntragUpdateFormData 
} from '@/lib/validation/schemas'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'  
import { Select } from '@/components/ui/select'
import { useCreateAntrag, useUpdateAntrag } from '@/hooks/api/use-antraege'
import { Anrede, AntragStatus, type AntragDto } from '@/types/api'

// =============================================================================
// TYPES
// =============================================================================

interface AntragFormProps {
  initialData?: Partial<AntragDto>
  mode: 'create' | 'edit'
  onSuccess?: (antrag: AntragDto) => void
  onCancel?: () => void
  className?: string
}

interface AntragFormFieldsProps {
  mode: 'create' | 'edit'
}

// =============================================================================
// FORM FIELDS COMPONENT
// =============================================================================

function AntragFormFields({ mode }: AntragFormFieldsProps) {
  const { isSubmitting } = useFormStatus()

  const anredeOptions = Object.values(Anrede).map(anrede => ({
    value: anrede,
    label: anrede
  }))

  const statusOptions = Object.entries(AntragStatus)
    .filter(([key]) => isNaN(Number(key)))
    .map(([key, value]) => ({
      value: value.toString(),
      label: {
        Neu: 'Neu',
        InBearbeitung: 'In Bearbeitung',
        Wartend: 'Wartend',
        Genehmigt: 'Genehmigt',
        Abgelehnt: 'Abgelehnt',
        Archiviert: 'Archiviert'
      }[key] || key
    }))

  return (
    <div className="space-y-6">
      {/* Erste Person (Hauptantragsteller) */}
      <Card className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <User className="h-5 w-5 text-primary-600" />
          <h3 className="text-lg font-semibold text-gray-900">
            Hauptantragsteller/in
          </h3>
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <FormField
            name="anrede"
            label="Anrede"
          >
            <Controller
              name="anrede"
              render={({ field, fieldState }) => (
                <div>
                  <Select
                    value={field.value || ''}
                    onValueChange={field.onChange}
                    disabled={isSubmitting}
                    placeholder="Anrede"
                  >
                    {anredeOptions.map(option => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </Select>
                  {fieldState.error && (
                    <p className="mt-1 text-sm text-red-600">
                      {fieldState.error.message}
                    </p>
                  )}
                </div>
              )}
            />
          </FormField>

          <FormField
            name="titel"
            label="Titel"
          >
            <Controller
              name="titel"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="titel"
                  placeholder="Dr., Prof., etc."
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  value={field.value || ''}
                />
              )}
            />
          </FormField>

          <FormField
            name="vorname"
            label="Vorname"
            required
          >
            <Controller
              name="vorname"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="vorname"
                  placeholder="Max"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  autoComplete="given-name"
                />
              )}
            />
          </FormField>

          <FormField
            name="nachname"
            label="Nachname"
            required
          >
            <Controller
              name="nachname"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="nachname"
                  placeholder="Mustermann"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  autoComplete="family-name"
                />
              )}
            />
          </FormField>

          <FormField
            name="geburtstag"
            label="Geburtsdatum"
            className="md:col-span-2"
          >
            <Controller
              name="geburtstag"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="geburtstag"
                  type="date"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  autoComplete="bday"
                  value={field.value ? field.value.split('T')[0] : ''}
                  onChange={(e) => field.onChange(e.target.value ? new Date(e.target.value).toISOString() : '')}
                />
              )}
            />
          </FormField>
        </div>
      </Card>

      {/* Zweite Person (Partner/in) */}
      <Card className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <Users className="h-5 w-5 text-primary-600" />
          <h3 className="text-lg font-semibold text-gray-900">
            Partner/in (optional)
          </h3>
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <FormField
            name="anrede2"
            label="Anrede"
          >
            <Controller
              name="anrede2"
              render={({ field, fieldState }) => (
                <div>
                  <Select
                    value={field.value || ''}
                    onValueChange={field.onChange}
                    disabled={isSubmitting}
                    placeholder="Anrede"
                  >
                    {anredeOptions.map(option => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </Select>
                  {fieldState.error && (
                    <p className="mt-1 text-sm text-red-600">
                      {fieldState.error.message}
                    </p>
                  )}
                </div>
              )}
            />
          </FormField>

          <FormField
            name="titel2"
            label="Titel"
          >
            <Controller
              name="titel2"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="titel2"
                  placeholder="Dr., Prof., etc."
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  value={field.value || ''}
                />
              )}
            />
          </FormField>

          <FormField
            name="vorname2"
            label="Vorname"
          >
            <Controller
              name="vorname2"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="vorname2"
                  placeholder="Maria"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  value={field.value || ''}
                />
              )}
            />
          </FormField>

          <FormField
            name="nachname2"
            label="Nachname"
          >
            <Controller
              name="nachname2"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="nachname2"
                  placeholder="Mustermann"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  value={field.value || ''}
                />
              )}
            />
          </FormField>

          <FormField
            name="geburtstag2"
            label="Geburtsdatum"
            className="md:col-span-2"
          >
            <Controller
              name="geburtstag2"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="geburtstag2"
                  type="date"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  value={field.value ? field.value.split('T')[0] : ''}
                  onChange={(e) => field.onChange(e.target.value ? new Date(e.target.value).toISOString() : '')}
                />
              )}
            />
          </FormField>

          <FormField
            name="briefanrede"
            label="Briefanrede"
            description="Anrede für offizielle Korrespondenz"
            className="md:col-span-4"
          >
            <Controller
              name="briefanrede"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="briefanrede"
                  placeholder="z.B. Sehr geehrte Familie Mustermann"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  value={field.value || ''}
                />
              )}
            />
          </FormField>
        </div>
      </Card>

      {/* Adresse */}
      <Card className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <MapPin className="h-5 w-5 text-primary-600" />
          <h3 className="text-lg font-semibold text-gray-900">
            Adresse
          </h3>
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <FormField
            name="strasse"
            label="Straße"
            className="md:col-span-3"
          >
            <Controller
              name="strasse"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="strasse"
                  placeholder="Musterstraße 123"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  autoComplete="street-address"
                  value={field.value || ''}
                />
              )}
            />
          </FormField>

          <FormField
            name="plz"
            label="PLZ"
          >
            <Controller
              name="plz"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="plz"
                  placeholder="12345"
                  maxLength={5}
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  autoComplete="postal-code"
                  value={field.value || ''}
                />
              )}
            />
          </FormField>

          <FormField
            name="ort"
            label="Ort"
            className="md:col-span-4"
          >
            <Controller
              name="ort"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="ort"
                  placeholder="Musterstadt"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  autoComplete="address-level2"
                  value={field.value || ''}
                />
              )}
            />
          </FormField>
        </div>
      </Card>

      {/* Kontaktdaten */}
      <Card className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <Phone className="h-5 w-5 text-primary-600" />
          <h3 className="text-lg font-semibold text-gray-900">
            Kontaktdaten
          </h3>
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <FormField
            name="telefon"
            label="Telefon (privat)"
          >
            <Controller
              name="telefon"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="telefon"
                  type="tel"
                  placeholder="0123 456789"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  autoComplete="tel-national"
                  value={field.value || ''}
                />
              )}
            />
          </FormField>

          <FormField
            name="mobilTelefon"
            label="Mobiltelefon"
          >
            <Controller
              name="mobilTelefon"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="mobilTelefon"
                  type="tel"
                  placeholder="0171 123456"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  autoComplete="tel"
                  value={field.value || ''}
                />
              )}
            />
          </FormField>

          <FormField
            name="geschTelefon"
            label="Telefon (geschäftlich)"
          >
            <Controller
              name="geschTelefon"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="geschTelefon"
                  type="tel"
                  placeholder="0123 987654"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  autoComplete="work tel"
                  value={field.value || ''}
                />
              )}
            />
          </FormField>

          <FormField
            name="mobilTelefon2"
            label="Mobiltelefon (Partner/in)"
          >
            <Controller
              name="mobilTelefon2"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="mobilTelefon2"
                  type="tel"
                  placeholder="0171 654321"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  value={field.value || ''}
                />
              )}
            />
          </FormField>

          <FormField
            name="eMail"
            label="E-Mail"
            className="md:col-span-2"
          >
            <Controller
              name="eMail"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="eMail"
                  type="email"
                  placeholder="max.mustermann@beispiel.de"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  autoComplete="email"
                  value={field.value || ''}
                />
              )}
            />
          </FormField>
        </div>
      </Card>

      {/* Antrags-Informationen */}
      <Card className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <FileText className="h-5 w-5 text-primary-600" />
          <h3 className="text-lg font-semibold text-gray-900">
            Antrags-Informationen
          </h3>
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <FormField
            name="bewerbungsdatum"
            label="Bewerbungsdatum"
          >
            <Controller
              name="bewerbungsdatum"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="bewerbungsdatum"
                  type="date"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  value={field.value ? field.value.split('T')[0] : ''}
                  onChange={(e) => field.onChange(e.target.value ? new Date(e.target.value).toISOString() : '')}
                />
              )}
            />
          </FormField>

          {mode === 'edit' && (
            <FormField
              name="status"
              label="Status"
              description="Aktueller Bearbeitungsstatus"
            >
              <Controller
                name="status"
                render={({ field, fieldState }) => (
                  <div>
                    <Select
                      value={field.value?.toString() || ''}
                      onValueChange={(value) => field.onChange(parseInt(value))}
                      disabled={isSubmitting}
                      placeholder="Status auswählen"
                    >
                      {statusOptions.map(option => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </Select>
                    {fieldState.error && (
                      <p className="mt-1 text-sm text-red-600">
                        {fieldState.error.message}
                      </p>
                    )}
                  </div>
                )}
              />
            </FormField>
          )}

          <FormField
            name="wunsch"
            label="Wunsch / Anmerkungen"
            description="Besondere Wünsche oder Anmerkungen"
            className="md:col-span-2"
          >
            <Controller
              name="wunsch"
              render={({ field, fieldState }) => (
                <div>
                  <textarea
                    {...field}
                    id="wunsch"
                    rows={3}
                    placeholder="Besondere Wünsche bezüglich Lage, Größe, etc."
                    className={`flex w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent disabled:cursor-not-allowed disabled:opacity-50 transition-colors duration-200 ${
                      fieldState.error ? 'border-red-500 focus:ring-red-500' : ''
                    }`}
                    disabled={isSubmitting}
                    value={field.value || ''}
                  />
                  {fieldState.error && (
                    <p className="mt-1 text-sm text-red-600">
                      {fieldState.error.message}
                    </p>
                  )}
                </div>
              )}
            />
          </FormField>

          <FormField
            name="vermerk"
            label="Interne Vermerke"
            description="Interne Anmerkungen (nur für Verwaltung sichtbar)"
            className="md:col-span-2"
          >
            <Controller
              name="vermerk"
              render={({ field, fieldState }) => (
                <div>
                  <textarea
                    {...field}
                    id="vermerk"
                    rows={3}
                    placeholder="Interne Vermerke, Notizen zur Bearbeitung, etc."
                    className={`flex w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent disabled:cursor-not-allowed disabled:opacity-50 transition-colors duration-200 ${
                      fieldState.error ? 'border-red-500 focus:ring-red-500' : ''
                    }`}
                    disabled={isSubmitting}
                    value={field.value || ''}
                  />
                  {fieldState.error && (
                    <p className="mt-1 text-sm text-red-600">
                      {fieldState.error.message}
                    </p>
                  )}
                </div>
              )}
            />
          </FormField>
        </div>
      </Card>
    </div>
  )
}

// =============================================================================
// FORM ACTIONS COMPONENT
// =============================================================================

interface AntragFormActionsProps {
  mode: 'create' | 'edit'
  onCancel?: () => void
}

function AntragFormActions({ mode, onCancel }: AntragFormActionsProps) {
  const { isSubmitting, canSubmit, hasErrors } = useFormStatus()
  const { showValidationErrors } = useFormActions()

  const handleShowErrors = () => {
    if (hasErrors) {
      showValidationErrors()
    }
  }

  return (
    <div className="flex items-center justify-between pt-6 border-t border-gray-200">
      <div className="flex items-center space-x-2">
        {hasErrors && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleShowErrors}
            className="text-red-600 border-red-300 hover:bg-red-50"
          >
            Fehler anzeigen
          </Button>
        )}
      </div>

      <div className="flex items-center space-x-3">
        {onCancel && (
          <Button
            type="button"
            variant="outline"
            onClick={onCancel}
            disabled={isSubmitting}
          >
            <X className="h-4 w-4 mr-2" />
            Abbrechen
          </Button>
        )}
        
        <Button
          type="submit"
          disabled={!canSubmit || isSubmitting}
          className="min-w-[120px]"
        >
          {isSubmitting ? (
            <>
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              Speichert...
            </>
          ) : (
            <>
              <Save className="h-4 w-4 mr-2" />
              {mode === 'create' ? 'Erstellen' : 'Speichern'}
            </>
          )}
        </Button>
      </div>
    </div>
  )
}

// =============================================================================
// MAIN FORM COMPONENT
// =============================================================================

/**
 * Antrag Form Hauptkomponente
 */
export function AntragForm({
  initialData,
  mode,
  onSuccess,
  onCancel,
  className = ''
}: AntragFormProps) {
  const formRef = React.useRef<HTMLFormElement>(null)
  
  // Mutations (würden normalerweise existieren)
  // const createAntrag = useCreateAntrag()
  // const updateAntrag = useUpdateAntrag()

  // Schema und Defaultwerte basierend auf Modus
  const schema = mode === 'create' ? antragCreateSchema : antragUpdateSchema
  const defaultValues = React.useMemo(() => {
    if (mode === 'create') {
      return {
        anrede: undefined,
        titel: '',
        vorname: '',
        nachname: '',
        anrede2: undefined,
        titel2: '',
        vorname2: '',
        nachname2: '',
        briefanrede: '',
        strasse: '',
        plz: '',
        ort: '',
        telefon: '',
        mobilTelefon: '',
        geschTelefon: '',
        mobilTelefon2: '',
        eMail: '',
        bewerbungsdatum: '',
        wunsch: '',
        vermerk: '',
        geburtstag: '',
        geburtstag2: ''
      } as AntragCreateFormData
    } else {
      return {
        id: initialData?.id || '',
        anrede: initialData?.anrede,
        titel: initialData?.titel || '',
        vorname: initialData?.vorname || '',
        nachname: initialData?.nachname || '',
        anrede2: initialData?.anrede2,
        titel2: initialData?.titel2 || '',
        vorname2: initialData?.vorname2 || '',
        nachname2: initialData?.nachname2 || '',
        briefanrede: initialData?.briefanrede || '',
        strasse: initialData?.strasse || '',
        plz: initialData?.plz || '',
        ort: initialData?.ort || '',
        telefon: initialData?.telefon || '',
        mobilTelefon: initialData?.mobilTelefon || '',
        geschTelefon: initialData?.geschTelefon || '',
        mobilTelefon2: initialData?.mobilTelefon2 || '',
        eMail: initialData?.eMail || '',
        bewerbungsdatum: initialData?.bewerbungsdatum || '',
        wunsch: initialData?.wunsch || '',
        vermerk: initialData?.vermerk || '',
        geburtstag: initialData?.geburtstag || '',
        geburtstag2: initialData?.geburtstag2 || '',
        status: initialData?.status,
        aktuellesAngebot: initialData?.aktuellesAngebot || '',
        loeschdatum: initialData?.loeschdatum || '',
        bestaetigungsdatum: initialData?.bestaetigungsdatum || ''
      } as AntragUpdateFormData
    }
  }, [mode, initialData])

  // Form Hook
  const form = useKGVForm({
    schema,
    defaultValues,
    mode: 'onBlur',
    reValidateMode: 'onChange'
  })

  // Submit Handler
  const handleSubmit = React.useCallback(async (data: AntragCreateFormData | AntragUpdateFormData) => {
    try {
      // Hier würde normalerweise die API-Integration stehen
      console.log('Antrag Form Data:', data)
      
      // Simuliere API Call
      await new Promise(resolve => setTimeout(resolve, 1000))
      
      // Mock result
      const result = {
        id: mode === 'create' ? Date.now().toString() : initialData?.id || '',
        ...data,
        status: mode === 'create' ? AntragStatus.Neu : data.status || AntragStatus.Neu,
        statusBeschreibung: 'Neu eingegangen',
        vollName: `${data.vorname} ${data.nachname}`,
        vollName2: data.vorname2 && data.nachname2 ? `${data.vorname2} ${data.nachname2}` : undefined,
        vollAdresse: [data.strasse, data.plz, data.ort].filter(Boolean).join(', '),
        aktiv: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        verlauf: []
      } as AntragDto

      if (onSuccess) {
        onSuccess(result)
      }

      // Form zurücksetzen bei Erstellung
      if (mode === 'create') {
        form.reset()
      }

    } catch (error: any) {
      console.error('Form submission error:', error)
      throw error // Wird vom FormProvider behandelt
    }
  }, [mode, initialData?.id, onSuccess, form])

  return (
    <div className={`antrag-form ${className}`}>
      <KGVFormProvider
        form={form}
        onSubmit={handleSubmit}
        formRef={formRef}
        preventLeave={true}
        showErrorToasts={true}
      >
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-2xl font-bold text-gray-900">
                {mode === 'create' ? 'Neuen Antrag erstellen' : 'Antrag bearbeiten'}
              </h2>
              <p className="mt-1 text-sm text-gray-600">
                {mode === 'create' 
                  ? 'Erfassen Sie die Antragsdaten für eine neue Bewerbung'
                  : 'Bearbeiten Sie die Antragsdaten'
                }
              </p>
            </div>
          </div>

          <AntragFormFields mode={mode} />
          <AntragFormActions mode={mode} onCancel={onCancel} />
        </div>
      </KGVFormProvider>
    </div>
  )
}

// =============================================================================
// EXPORT
// =============================================================================

export type { AntragFormProps }
export default AntragForm