# KGV Form Validation System

Ein umfassendes Form Validation System für das KGV (Kleingartenverein) Management System mit React Hook Form, Zod Schema Validation und deutscher Lokalisierung.

## 📋 Inhaltsverzeichnis

- [Überblick](#überblick)
- [Features](#features)
- [Installation & Setup](#installation--setup)
- [Verwendung](#verwendung)
- [Komponenten](#komponenten)
- [Hooks](#hooks)
- [Validation Schemas](#validation-schemas)
- [Beispiele](#beispiele)
- [Best Practices](#best-practices)
- [Migration Guide](#migration-guide)

---

## Überblick

Das KGV Form Validation System bietet eine vollständige, typsichere und benutzerfreundliche Lösung für alle Formulare im KGV Management System. Es kombiniert die Macht von React Hook Form mit Zod Schema Validation und bietet deutsche Lokalisierung, optimistische Updates und nahtlose React Query Integration.

### Hauptmerkmale

- ✅ **Typsicherheit**: Vollständige TypeScript-Integration mit statischer Typisierung
- ✅ **Deutsche Lokalisierung**: Alle Fehlermeldungen und Validierungen auf Deutsch
- ✅ **Performance**: Optimierte Re-Rendering und Debounced Validation
- ✅ **Benutzerfreundlichkeit**: Intuitive Fehlerbehandlung und -anzeige
- ✅ **Accessibility**: WCAG-konforme Implementierung
- ✅ **React Query Integration**: Nahtlose API-Integration mit Caching
- ✅ **Async Validation**: Eindeutigkeitsprüfungen und Server-Validierung
- ✅ **Optimistic Updates**: Sofortiges Feedback für bessere UX

---

## Features

### 🔧 Core Features

- **Zod Schema Validation**: Typsichere Schema-Definition mit deutschen Fehlermeldungen
- **React Hook Form**: Performante Form-Verwaltung mit minimalen Re-Renders
- **Form Provider**: Zentralisierte Form-Logik mit Context API
- **Custom Hooks**: Spezialisierte Hooks für verschiedene Entitäten
- **Async Validation**: Server-seitige Validierung (Eindeutigkeit, etc.)
- **Error Handling**: Umfassende Fehlerbehandlung und -anzeige
- **Toast Notifications**: Benutzerfreundliche Erfolgs- und Fehlermeldungen

### 🌍 Deutsche Lokalisierung

- Alle Validierungsmeldungen auf Deutsch
- Deutsche Formatvalidierung (PLZ, Telefonnummern, Namen)
- Kulturspezifische Validierung (Umlaute, deutsche Addressformate)
- Deutsche Toast-Nachrichten und UI-Texte

### 🚀 Performance Optimierungen  

- Debounced Validation für Async-Aufrufe
- Optimistische Updates für bessere UX
- Intelligentes Caching mit React Query
- Minimale Re-Renders durch React Hook Form

### ♿ Accessibility

- ARIA-Labels und -Beschreibungen
- Keyboard Navigation Support
- Screen Reader kompatibel
- Focus Management bei Fehlern

---

## Installation & Setup

### 1. Dependencies

Die benötigten Dependencies sind bereits installiert:

```json
{
  "react-hook-form": "^7.52.1",
  "zod": "^3.23.8",
  "@hookform/resolvers": "^3.9.0",
  "@tanstack/react-query": "^5.51.1",
  "react-hot-toast": "^2.4.1"
}
```

### 2. Setup in der Anwendung

```tsx
// In deiner App-Komponente oder Layout
import { setGermanZodMessages } from '@/lib/validation/form-utils'

// Deutsche Zod-Nachrichten aktivieren (wird automatisch beim Import gemacht)
setGermanZodMessages()
```

### 3. Provider Setup

Der `KGVFormProvider` wird automatisch in den Form-Komponenten verwendet. Keine zusätzliche Konfiguration erforderlich.

---

## Verwendung

### Basis-Verwendung

```tsx
import { BezirkForm } from '@/components/forms'

function CreateBezirkPage() {
  const router = useRouter()

  const handleSuccess = (bezirk: Bezirk) => {
    router.push(`/bezirke/${bezirk.id}`)
  }

  return (
    <BezirkForm
      mode="create"
      onSuccess={handleSuccess}
      onCancel={() => router.back()}
    />
  )
}
```

### Erweiterte Verwendung mit Custom Hooks

```tsx
import { useBezirkCreateForm } from '@/hooks/forms/use-form-mutations'
import { KGVFormProvider } from '@/components/forms'

function CustomBezirkForm() {
  const {
    form,
    handleSubmit,
    isSubmitting,
    canSubmit
  } = useBezirkCreateForm({
    onSuccess: (bezirk) => {
      console.log('Bezirk erstellt:', bezirk)
    }
  })

  return (
    <KGVFormProvider form={form} onSubmit={handleSubmit}>
      {/* Deine Custom Form Felder */}
    </KGVFormProvider>
  )
}
```

---

## Komponenten

### BezirkForm

Vollständige Form-Komponente für Bezirks-Verwaltung.

```tsx
<BezirkForm
  mode="create" | "edit"
  initialData={bezirk} // nur bei mode="edit"
  onSuccess={(bezirk) => {}}
  onCancel={() => {}}
  className="custom-class"
/>
```

**Props:**
- `mode`: 'create' | 'edit' - Bestimmt das Verhalten der Form
- `initialData?`: Partial<Bezirk> - Anfangsdaten für Edit-Modus
- `onSuccess?`: (bezirk: Bezirk) => void - Erfolgs-Callback
- `onCancel?`: () => void - Abbruch-Callback
- `className?`: string - Zusätzliche CSS-Klassen

### ParzelleForm

```tsx
<ParzelleForm
  mode="create" | "edit"
  initialData={parzelle}
  preselectedBezirkId={123} // für create-Modus
  onSuccess={(parzelle) => {}}
  onCancel={() => {}}
/>
```

### AntragForm

```tsx
<AntragForm
  mode="create" | "edit"
  initialData={antrag}
  onSuccess={(antrag) => {}}
  onCancel={() => {}}
/>
```

### KGVFormProvider

Basis-Provider für alle Formulare.

```tsx
<KGVFormProvider
  form={form}
  onSubmit={handleSubmit}
  formRef={formRef}
  preventLeave={true}
  showErrorToasts={true}
>
  {children}
</KGVFormProvider>
```

### FormField

Wrapper-Komponente für individuelle Felder.

```tsx
<FormField
  name="fieldName"
  label="Label"
  description="Beschreibung"
  required={true}
>
  <Input {...field} />
</FormField>
```

---

## Hooks

### Form Hooks

#### useBezirkCreateForm / useBezirkEditForm

```tsx
const {
  form,
  mutation,
  handleSubmit,
  handleSubmitError,
  isSubmitting,
  isSuccess,
  reset
} = useBezirkCreateForm({
  onSuccess: (bezirk) => {},
  onError: (error) => {}
})
```

#### useKGVForm

Basis-Hook für Custom Forms.

```tsx
const form = useKGVForm({
  schema: bezirkCreateSchema,
  defaultValues: {},
  mode: 'onBlur',
  reValidateMode: 'onChange'
})
```

### Validation Hooks

#### useAsyncValidation

Für asynchrone Validierung (z.B. Eindeutigkeitsprüfung).

```tsx
const {
  setValue,
  isValidating,
  isValid,
  validate
} = useAsyncValidation({
  queryKey: ['bezirke', 'validate', 'name'],
  validationFn: async (name) => {
    // API-Call zur Validierung
    return isUnique
  }
})
```

#### Spezielle Validation Hooks

```tsx
// Bezirksname-Eindeutigkeit
const nameValidation = useBezirkNameValidation(currentId)

// Parzellennummer-Eindeutigkeit  
const nummerValidation = useParzellenNummerValidation(bezirkId, currentId)

// E-Mail-Eindeutigkeit
const emailValidation = useEmailValidation(currentId)
```

### Utility Hooks

#### useFormStatus

```tsx
const {
  isSubmitting,
  hasErrors,
  errorCount,
  isDirty,
  canSubmit
} = useFormStatus()
```

#### useFormActions

```tsx
const {
  showValidationErrors,
  clearErrors,
  focusFirstError
} = useFormActions()
```

#### useUnsavedChangesWarning

```tsx
useUnsavedChangesWarning(enabled) // Warnt vor ungespeicherten Änderungen
```

---

## Validation Schemas

### Bezirk Schemas

```tsx
import { bezirkCreateSchema, bezirkUpdateSchema } from '@/lib/validation/schemas'

// Typen ableiten
type BezirkCreateData = z.infer<typeof bezirkCreateSchema>
type BezirkUpdateData = z.infer<typeof bezirkUpdateSchema>
```

### Parzelle Schemas

```tsx
import { 
  parzelleCreateSchema, 
  parzelleUpdateSchema,
  parzellenAssignmentSchema 
} from '@/lib/validation/schemas'
```

### Antrag Schemas

```tsx
import { 
  antragCreateSchema, 
  antragUpdateSchema,
  verlaufCreateSchema 
} from '@/lib/validation/schemas'
```

### Filter Schemas

```tsx
import { 
  bezirkeFilterSchema,
  parzellenFilterSchema,
  antraegeFilterSchema 
} from '@/lib/validation/schemas'
```

### Custom Schema Beispiel

```tsx
import { z } from 'zod'

const customSchema = z.object({
  name: z
    .string()
    .min(1, 'Name ist erforderlich')
    .max(50, 'Name ist zu lang'),
  email: z
    .string()
    .email('Ungültige E-Mail-Adresse')
    .optional(),
  plz: z
    .string()
    .regex(/^\d{5}$/, 'PLZ muss 5 Ziffern haben')
    .optional()
})
```

---

## Beispiele

### Einfache Form Migration

**Vorher (ohne Form System):**

```tsx
function OldForm() {
  const [data, setData] = useState({})
  const [errors, setErrors] = useState({})
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    // Manuelle Validierung...
    // API-Call...
  }

  return (
    <form onSubmit={handleSubmit}>
      {/* Viel Boilerplate Code... */}
    </form>
  )
}
```

**Nachher (mit Form System):**

```tsx
function NewForm() {
  const handleSuccess = (result) => {
    // Erfolgs-Handling
  }

  return (
    <BezirkForm
      mode="create"
      onSuccess={handleSuccess}
      onCancel={() => router.back()}
    />
  )
}
```

### Custom Form Field

```tsx
import { Controller } from 'react-hook-form'
import { FormField } from '@/components/forms'

function CustomPhoneField({ name, label }) {
  return (
    <FormField name={name} label={label} required>
      <Controller
        name={name}
        render={({ field, fieldState }) => (
          <PhoneInput
            {...field}
            country="DE"
            error={!!fieldState.error}
            helperText={fieldState.error?.message}
          />
        )}
      />
    </FormField>
  )
}
```

### Async Validation Beispiel

```tsx
function BezirkNameField() {
  const nameValidation = useBezirkNameValidation()
  
  return (
    <FormField name="name" label="Bezirksname" required>
      <Controller
        name="name"
        render={({ field, fieldState }) => (
          <div>
            <Input
              {...field}
              onChange={(e) => {
                field.onChange(e.target.value)
                nameValidation.validate(e.target.value)
              }}
            />
            {nameValidation.isValidating && (
              <p className="text-sm text-gray-500">Prüfe Verfügbarkeit...</p>
            )}
            {!nameValidation.isValid && (
              <p className="text-sm text-red-600">Name bereits vergeben</p>
            )}
          </div>
        )}
      />
    </FormField>
  )
}
```

---

## Best Practices

### 1. Schema Design

- **Verwende aussagekräftige Fehlermeldungen** auf Deutsch
- **Nutze optionale Felder** für nicht erforderliche Eingaben
- **Implementiere Custom Validators** für spezielle Anforderungen

```tsx
const schema = z.object({
  name: z
    .string()
    .min(1, 'Bezirksname ist erforderlich')
    .min(2, 'Bezirksname muss mindestens 2 Zeichen haben')
    .max(100, 'Bezirksname darf maximal 100 Zeichen haben'),
  plz: z
    .string()
    .optional()
    .refine(
      (val) => !val || /^\d{5}$/.test(val),
      'PLZ muss aus 5 Ziffern bestehen'
    )
})
```

### 2. Form Performance

- **Verwende `mode: 'onBlur'`** für bessere Performance
- **Nutze `reValidateMode: 'onChange'`** für sofortiges Feedback
- **Implementiere Debouncing** für Async-Validierung

```tsx
const form = useKGVForm({
  schema,
  mode: 'onBlur', // Validierung beim Verlassen des Feldes
  reValidateMode: 'onChange', // Re-Validierung bei Änderungen
  delayError: 300 // Verzögerung für Fehlermeldungen
})
```

### 3. Error Handling

- **Zeige spezifische Fehlermeldungen** für jedes Feld
- **Verwende Toast-Nachrichten** für globale Meldungen
- **Implementiere Retry-Logik** für API-Fehler

```tsx
const handleSubmitError = (errors) => {
  // Fokussiere erstes Fehlerfeld
  focusFirstErrorField(errors, formRef)
  
  // Zeige Validierungsfehler
  showValidationToast(errors)
}
```

### 4. Accessibility

- **Verwende semantische HTML-Elemente**
- **Implementiere korrekte ARIA-Labels**
- **Sorge für Keyboard-Navigation**

```tsx
<FormField
  name="name"
  label="Bezirksname"
  required
  description="Eindeutiger Name für den Bezirk"
>
  <Input
    {...field}
    aria-describedby="name-description name-error"
    aria-required="true"
    aria-invalid={!!fieldState.error}
  />
</FormField>
```

### 5. Testing

- **Teste Validierungslogik** isoliert
- **Mocke API-Calls** für Async-Validierung
- **Teste Accessibility** mit Screen Readers

```tsx
// Beispiel Unit Test
describe('bezirkCreateSchema', () => {
  it('should validate required name', () => {
    const result = bezirkCreateSchema.safeParse({ name: '' })
    expect(result.success).toBe(false)
    expect(result.error.issues[0].message).toBe('Bezirksname ist erforderlich')
  })
})
```

---

## Migration Guide

### Von manueller Validierung zu Zod Schema

**Vorher:**
```tsx
const validateForm = () => {
  const errors = {}
  if (!formData.name.trim()) {
    errors.name = 'Name ist erforderlich'
  }
  if (formData.email && !isValidEmail(formData.email)) {
    errors.email = 'Ungültige E-Mail'
  }
  return errors
}
```

**Nachher:**
```tsx
const schema = z.object({
  name: z.string().min(1, 'Name ist erforderlich'),
  email: z.string().email('Ungültige E-Mail').optional()
})
```

### Von useState zu React Hook Form

**Vorher:**
```tsx
const [formData, setFormData] = useState({})
const [errors, setErrors] = useState({})

const handleChange = (field, value) => {
  setFormData(prev => ({ ...prev, [field]: value }))
}
```

**Nachher:**
```tsx
const form = useKGVForm({
  schema,
  defaultValues: {}
})

// React Hook Form verwaltet State automatisch
```

### Schritt-für-Schritt Migration

1. **Schema definieren**
   ```tsx
   const schema = z.object({
     // Deine Validierungsregeln
   })
   ```

2. **Form Hook setup**
   ```tsx
   const form = useKGVForm({ schema, defaultValues })
   ```

3. **FormProvider hinzufügen**
   ```tsx
   <KGVFormProvider form={form} onSubmit={handleSubmit}>
   ```

4. **Felder mit Controller wrappen**
   ```tsx
   <Controller
     name="fieldName"
     render={({ field, fieldState }) => (
       <Input {...field} error={!!fieldState.error} />
     )}
   />
   ```

5. **Fehlermeldungen anpassen**
   ```tsx
   {fieldState.error && (
     <p className="text-red-600">{fieldState.error.message}</p>
   )}
   ```

### Häufige Fallstricke

1. **Vergessen von Controller bei Custom Components**
   ```tsx
   // ❌ Falsch
   <CustomInput {...register('name')} />
   
   // ✅ Richtig
   <Controller
     name="name"
     render={({ field }) => <CustomInput {...field} />}
   />
   ```

2. **Fehlende Typ-Definitionen**
   ```tsx
   // ❌ Falsch
   const form = useForm()
   
   // ✅ Richtig
   const form = useKGVForm<BezirkCreateFormData>({ schema })
   ```

3. **Manuelle Error-Behandlung**
   ```tsx
   // ❌ Falsch - manuell
   if (hasErrors) {
     setErrors(apiErrors)
   }
   
   // ✅ Richtig - automatisch durch FormProvider
   // Fehler werden automatisch vom Provider behandelt
   ```

---

## 🛠 Troubleshooting

### Häufige Probleme und Lösungen

**Problem: "Form wird nicht validiert"**
```tsx
// Lösung: Stelle sicher, dass zodResolver verwendet wird
const form = useForm({
  resolver: zodResolver(schema) // ← Wichtig!
})
```

**Problem: "Async Validierung funktioniert nicht"**
```tsx
// Lösung: Verwende enabled-Flag richtig
const validation = useAsyncValidation({
  validationFn,
  enabled: !!value && value.length >= 2 // ← Bedingung
})
```

**Problem: "Deutsche Umlaute werden nicht erkannt"**
```tsx
// Lösung: Regex für deutsche Namen anpassen
const nameRegex = /^[a-zA-ZäöüÄÖÜß\s\-']+$/
```

---

## 📈 Performance Tipps

1. **Nutze debouncing** für teure Validierungen
2. **Vermeide zu häufige Re-Renders** mit `mode: 'onBlur'`
3. **Implementiere optimistische Updates** für bessere UX
4. **Cache Validierungsergebnisse** mit React Query
5. **Verwende `React.memo`** für komplexe Form-Komponenten

---

## 🔮 Roadmap

- [ ] **Multi-Step Forms**: Support für mehrstufige Formulare
- [ ] **Conditional Fields**: Dynamische Feldanzeige basierend auf anderen Feldern
- [ ] **File Upload Integration**: Drag & Drop File Upload mit Validierung
- [ ] **Rich Text Editor**: Integration von Rich Text Editoren
- [ ] **Advanced Date Handling**: Erweiterte Datums-/Zeitvalidierung
- [ ] **Form Templates**: Wiederverwendbare Form-Templates
- [ ] **Analytics Integration**: Form-Interaktion Analytics

---

## 📚 Weiterführende Ressourcen

- [React Hook Form Dokumentation](https://react-hook-form.com/)
- [Zod Dokumentation](https://zod.dev/)
- [React Query Dokumentation](https://tanstack.com/query/latest)
- [Accessibility Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)

---

**Entwickelt für das KGV Management System mit ❤️ und deutscher Gründlichkeit.**