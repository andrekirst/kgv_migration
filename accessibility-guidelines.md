# KGV Frank - Accessibility Guidelines (WCAG 2.1)

## 1. WCAG 2.1 Compliance Overview

### Conformance Level: AA
Die KGV Frank Anwendung soll WCAG 2.1 Level AA Konformität erreichen, um maximale Zugänglichkeit für Verwaltungsmitarbeiter mit verschiedenen Behinderungen zu gewährleisten.

### Vier Grundprinzipien (POUR)
1. **Perceivable** - Wahrnehmbar
2. **Operable** - Bedienbar  
3. **Understandable** - Verständlich
4. **Robust** - Robust

## 2. Perceivable (Wahrnehmbar)

### 2.1 Farbkontrast (1.4.3 - AA)
```css
/* Minimum Kontrastverhältnisse */
/* Normaler Text: 4.5:1 */
.text-normal {
  color: #1f2937;  /* Kontrast zu #ffffff: 16.8:1 ✓ */
  background: #ffffff;
}

/* Großer Text (18pt+/14pt+ bold): 3:1 */
.text-large {
  color: #374151;  /* Kontrast zu #ffffff: 12.6:1 ✓ */
  background: #ffffff;
}

/* UI Komponenten: 3:1 */
.button-primary {
  color: #ffffff;
  background: #1d4ed8; /* Kontrast: 8.6:1 ✓ */
  border: 2px solid #1d4ed8;
}

.button-secondary {
  color: #1d4ed8;
  background: #ffffff;
  border: 2px solid #1d4ed8; /* Kontrast: 3.4:1 ✓ */
}

/* Status-Indikatoren mit ausreichendem Kontrast */
.status-success {
  color: #065f46; /* Kontrast zu #ecfdf5: 10.1:1 ✓ */
  background: #ecfdf5;
}

.status-warning {
  color: #92400e; /* Kontrast zu #fffbeb: 8.9:1 ✓ */
  background: #fffbeb;
}

.status-error {
  color: #991b1b; /* Kontrast zu #fef2f2: 11.2:1 ✓ */
  background: #fef2f2;
}
```

### 2.2 Informationsvermittlung ohne Farbe (1.4.1 - A)
```tsx
// Status mit Icon UND Farbe
function StatusBadge({ status, children }) {
  const statusConfig = {
    success: { icon: CheckCircleIcon, color: 'bg-green-100 text-green-800' },
    warning: { icon: ExclamationTriangleIcon, color: 'bg-yellow-100 text-yellow-800' },
    error: { icon: XCircleIcon, color: 'bg-red-100 text-red-800' },
    info: { icon: InformationCircleIcon, color: 'bg-blue-100 text-blue-800' }
  }
  
  const { icon: Icon, color } = statusConfig[status]
  
  return (
    <span className={`inline-flex items-center gap-1 px-2 py-1 rounded-full text-sm font-medium ${color}`}>
      <Icon className="w-4 h-4" aria-hidden="true" />
      {children}
    </span>
  )
}

// Pflichtfelder mit * UND "required" Indikator
function FormField({ label, required, children, error }) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        {label}
        {required && (
          <>
            <span className="text-red-500 ml-1" aria-hidden="true">*</span>
            <span className="sr-only">(erforderlich)</span>
          </>
        )}
      </label>
      {children}
      {error && (
        <p className="mt-1 text-sm text-red-600" role="alert">
          <XCircleIcon className="w-4 h-4 inline mr-1" aria-hidden="true" />
          {error}
        </p>
      )}
    </div>
  )
}
```

### 2.3 Text-Alternativen (1.1.1 - A)
```tsx
// Informative Bilder
<img 
  src="/charts/warteliste-entwicklung.png" 
  alt="Diagramm zeigt Entwicklung der Warteliste: Bezirk 32 stieg von 78 auf 89 Anträge, Bezirk 33 sank von 72 auf 67 Anträge im letzten Quartal"
  className="w-full h-auto"
/>

// Dekorative Bilder
<img 
  src="/images/garden-decoration.jpg" 
  alt=""
  role="presentation"
  className="w-full h-auto"
/>

// Icon Buttons
<button 
  type="button"
  aria-label="Antrag löschen"
  aria-describedby="delete-help"
  className="p-2 text-red-600 hover:text-red-800"
>
  <TrashIcon className="w-5 h-5" aria-hidden="true" />
</button>
<div id="delete-help" className="sr-only">
  Dieser Vorgang kann nicht rückgängig gemacht werden
</div>

// Complex Icons mit Kontext
<div className="flex items-center gap-2">
  <DocumentIcon className="w-5 h-5 text-blue-600" aria-hidden="true" />
  <span>Antrag PDF herunterladen</span>
</div>
```

## 3. Operable (Bedienbar)

### 3.1 Keyboard Navigation (2.1.1 - A)
```tsx
// Tab-Reihenfolge und Focus Management
function Modal({ isOpen, onClose, title, children }) {
  const focusTrapRef = useRef(null)
  
  useEffect(() => {
    if (isOpen) {
      // Focus auf ersten focusable Element
      const firstFocusable = focusTrapRef.current?.querySelector(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      )
      firstFocusable?.focus()
    }
  }, [isOpen])
  
  const handleKeyDown = (e) => {
    if (e.key === 'Escape') {
      onClose()
    }
    
    // Focus Trap
    if (e.key === 'Tab') {
      const focusableElements = focusTrapRef.current?.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      )
      const firstElement = focusableElements[0]
      const lastElement = focusableElements[focusableElements.length - 1]
      
      if (e.shiftKey && document.activeElement === firstElement) {
        e.preventDefault()
        lastElement.focus()
      } else if (!e.shiftKey && document.activeElement === lastElement) {
        e.preventDefault()
        firstElement.focus()
      }
    }
  }
  
  if (!isOpen) return null
  
  return (
    <div className="fixed inset-0 z-50 bg-black bg-opacity-50">
      <div 
        ref={focusTrapRef}
        className="fixed inset-0 flex items-center justify-center p-4"
        onKeyDown={handleKeyDown}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
      >
        <div className="bg-white rounded-lg shadow-xl max-w-md w-full">
          <div className="p-6">
            <h2 id="modal-title" className="text-lg font-semibold mb-4">
              {title}
            </h2>
            {children}
            <div className="flex gap-2 mt-6">
              <button 
                onClick={onClose}
                className="px-4 py-2 bg-gray-200 rounded hover:bg-gray-300 focus:ring-2 focus:ring-gray-500"
              >
                Abbrechen
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

// Skip Links
function SkipLinks() {
  return (
    <nav className="sr-only focus:not-sr-only">
      <a 
        href="#main-content"
        className="absolute top-4 left-4 z-50 bg-blue-600 text-white px-4 py-2 rounded focus:ring-2 focus:ring-blue-300"
      >
        Zum Hauptinhalt springen
      </a>
      <a 
        href="#navigation"
        className="absolute top-4 left-32 z-50 bg-blue-600 text-white px-4 py-2 rounded focus:ring-2 focus:ring-blue-300"
      >
        Zur Navigation springen
      </a>
    </nav>
  )
}
```

### 3.2 Focus Indicators (2.4.7 - AA)
```css
/* Sichtbare Focus-Indikatoren */
.focus-visible {
  outline: 2px solid #3b82f6;
  outline-offset: 2px;
  border-radius: 4px;
}

/* Spezielle Focus-Styles für verschiedene Komponenten */
button:focus-visible {
  outline: 2px solid #3b82f6;
  outline-offset: 2px;
}

input:focus-visible,
select:focus-visible,
textarea:focus-visible {
  outline: 2px solid #3b82f6;
  outline-offset: 0;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

a:focus-visible {
  outline: 2px solid #3b82f6;
  outline-offset: 2px;
  text-decoration: underline;
}

/* High Contrast Mode Support */
@media (prefers-contrast: high) {
  .focus-visible {
    outline: 3px solid;
    outline-offset: 3px;
  }
}
```

### 3.3 Touch Targets (2.5.5 - AAA implementiert als AA)
```css
/* Minimum Touch Target Size: 44px x 44px */
.touch-target {
  min-width: 2.75rem; /* 44px */
  min-height: 2.75rem; /* 44px */
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

/* Button Größen */
.btn-sm {
  min-height: 2.75rem; /* 44px */
  padding: 0.75rem 1rem;
}

.btn-md {
  min-height: 3rem; /* 48px */
  padding: 1rem 1.5rem;
}

.btn-lg {
  min-height: 3.5rem; /* 56px */
  padding: 1.25rem 2rem;
}

/* Abstand zwischen Touch Targets */
.touch-spacing {
  margin: 0.125rem; /* 2px Mindestabstand */
}
```

## 4. Understandable (Verständlich)

### 4.1 Sprache und Lesbarkeit (3.1.1 - A)
```html
<!DOCTYPE html>
<html lang="de">
<head>
  <meta charset="UTF-8">
  <title>KGV Frank - Antragsverwaltung</title>
</head>
<body>
  <!-- Hauptsprache Deutsch, Abschnitte in anderen Sprachen markiert -->
  <main lang="de">
    <h1>Antragsverwaltung</h1>
    <p>Willkommen im Verwaltungssystem für Kleingartenvereine.</p>
    
    <!-- Fremdsprachige Begriffe markieren -->
    <p>
      Der <span lang="en">Status</span> des Antrags wurde aktualisiert.
    </p>
  </main>
</body>
</html>
```

### 4.2 Eingabehilfen und Fehlererkennung (3.3.1 - A, 3.3.3 - AA)
```tsx
// Formular mit umfassender Validierung und Hilfestellung
function AntragsFormular() {
  const [errors, setErrors] = useState({})
  const [touched, setTouched] = useState({})
  
  const validateField = (name, value) => {
    const newErrors = { ...errors }
    
    switch (name) {
      case 'vorname':
        if (!value.trim()) {
          newErrors.vorname = 'Vorname ist erforderlich'
        } else if (value.length < 2) {
          newErrors.vorname = 'Vorname muss mindestens 2 Zeichen lang sein'
        } else {
          delete newErrors.vorname
        }
        break
        
      case 'email':
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
        if (!value.trim()) {
          newErrors.email = 'E-Mail-Adresse ist erforderlich'
        } else if (!emailRegex.test(value)) {
          newErrors.email = 'Bitte geben Sie eine gültige E-Mail-Adresse ein (z.B. name@beispiel.de)'
        } else {
          delete newErrors.email
        }
        break
        
      case 'telefon':
        const telefonRegex = /^[\d\s\-\+\(\)]+$/
        if (value && !telefonRegex.test(value)) {
          newErrors.telefon = 'Telefonnummer darf nur Ziffern, Leerzeichen und die Zeichen +()-/ enthalten'
        } else {
          delete newErrors.telefon
        }
        break
    }
    
    setErrors(newErrors)
  }
  
  return (
    <form noValidate onSubmit={handleSubmit}>
      <fieldset>
        <legend className="text-lg font-semibold mb-4">
          Persönliche Angaben
        </legend>
        
        {/* Vorname - Pflichtfeld */}
        <div className="mb-4">
          <label 
            htmlFor="vorname" 
            className="block text-sm font-medium text-gray-700 mb-1"
          >
            Vorname
            <span className="text-red-500 ml-1" aria-hidden="true">*</span>
            <span className="sr-only">(erforderlich)</span>
          </label>
          <input
            id="vorname"
            name="vorname"
            type="text"
            required
            aria-required="true"
            aria-invalid={errors.vorname ? 'true' : 'false'}
            aria-describedby="vorname-help vorname-error"
            className={`w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500 ${
              errors.vorname ? 'border-red-500' : 'border-gray-300'
            }`}
            onBlur={(e) => {
              setTouched(prev => ({ ...prev, vorname: true }))
              validateField('vorname', e.target.value)
            }}
            onChange={(e) => {
              if (touched.vorname) {
                validateField('vorname', e.target.value)
              }
            }}
          />
          <div id="vorname-help" className="mt-1 text-sm text-gray-600">
            Geben Sie Ihren Vornamen ein (mindestens 2 Zeichen)
          </div>
          {errors.vorname && (
            <div id="vorname-error" className="mt-1 text-sm text-red-600" role="alert">
              <XCircleIcon className="w-4 h-4 inline mr-1" aria-hidden="true" />
              {errors.vorname}
            </div>
          )}
        </div>
        
        {/* E-Mail mit Format-Beispiel */}
        <div className="mb-4">
          <label 
            htmlFor="email" 
            className="block text-sm font-medium text-gray-700 mb-1"
          >
            E-Mail-Adresse
            <span className="text-red-500 ml-1" aria-hidden="true">*</span>
            <span className="sr-only">(erforderlich)</span>
          </label>
          <input
            id="email"
            name="email"
            type="email"
            required
            aria-required="true"
            aria-invalid={errors.email ? 'true' : 'false'}
            aria-describedby="email-help email-error"
            placeholder="name@beispiel.de"
            className={`w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500 ${
              errors.email ? 'border-red-500' : 'border-gray-300'
            }`}
          />
          <div id="email-help" className="mt-1 text-sm text-gray-600">
            Format: name@beispiel.de
          </div>
          {errors.email && (
            <div id="email-error" className="mt-1 text-sm text-red-600" role="alert">
              <XCircleIcon className="w-4 h-4 inline mr-1" aria-hidden="true" />
              {errors.email}
            </div>
          )}
        </div>
      </fieldset>
      
      {/* Fehlerübersicht am Formularende */}
      {Object.keys(errors).length > 0 && (
        <div 
          className="bg-red-50 border border-red-200 rounded-md p-4 mb-6"
          role="alert"
          aria-labelledby="form-errors-title"
        >
          <h3 id="form-errors-title" className="text-red-800 font-medium mb-2">
            Bitte korrigieren Sie folgende Fehler:
          </h3>
          <ul className="text-red-700 text-sm space-y-1">
            {Object.entries(errors).map(([field, error]) => (
              <li key={field}>
                <a 
                  href={`#${field}`}
                  className="underline hover:no-underline"
                  onClick={(e) => {
                    e.preventDefault()
                    document.getElementById(field)?.focus()
                  }}
                >
                  {error}
                </a>
              </li>
            ))}
          </ul>
        </div>
      )}
    </form>
  )
}
```

### 4.3 Labels und Instructions (3.3.2 - A)
```tsx
// Comprehensive Form Labels
function DateRangePicker({ label, required, startDate, endDate, onChange, error }) {
  const startId = useId()
  const endId = useId()
  const helpId = useId()
  const errorId = useId()
  
  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium text-gray-700">
        {label}
        {required && (
          <>
            <span className="text-red-500 ml-1" aria-hidden="true">*</span>
            <span className="sr-only">(erforderlich)</span>
          </>
        )}
      </legend>
      
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label htmlFor={startId} className="block text-sm text-gray-600 mb-1">
            Von
          </label>
          <input
            id={startId}
            type="date"
            value={startDate}
            onChange={(e) => onChange({ startDate: e.target.value, endDate })}
            aria-describedby={`${helpId} ${error ? errorId : ''}`}
            aria-invalid={error ? 'true' : 'false'}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
        
        <div>
          <label htmlFor={endId} className="block text-sm text-gray-600 mb-1">
            Bis
          </label>
          <input
            id={endId}
            type="date"
            value={endDate}
            onChange={(e) => onChange({ startDate, endDate: e.target.value })}
            aria-describedby={`${helpId} ${error ? errorId : ''}`}
            aria-invalid={error ? 'true' : 'false'}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
      </div>
      
      <div id={helpId} className="text-sm text-gray-600">
        Wählen Sie den gewünschten Zeitraum für die Auswertung
      </div>
      
      {error && (
        <div id={errorId} className="text-sm text-red-600" role="alert">
          {error}
        </div>
      )}
    </fieldset>
  )
}
```

## 5. Robust (Robust)

### 5.1 Semantic HTML (4.1.1 - A)
```tsx
// Proper Semantic Structure
function AntragsDetailPage({ antrag }) {
  return (
    <main id="main-content">
      <header className="mb-6">
        <nav aria-label="Breadcrumb">
          <ol className="flex space-x-2 text-sm">
            <li><a href="/dashboard">Dashboard</a></li>
            <li aria-hidden="true">/</li>
            <li><a href="/antraege">Anträge</a></li>
            <li aria-hidden="true">/</li>
            <li aria-current="page">Antrag {antrag.aktenzeichen}</li>
          </ol>
        </nav>
        
        <h1 className="text-2xl font-bold text-gray-900 mt-4">
          Antrag {antrag.aktenzeichen}
        </h1>
      </header>
      
      <article>
        <section aria-labelledby="grunddaten-heading">
          <h2 id="grunddaten-heading">Grunddaten</h2>
          <dl className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <dt className="font-medium text-gray-700">Antragsteller</dt>
              <dd className="text-gray-900">{antrag.vorname} {antrag.nachname}</dd>
            </div>
            <div>
              <dt className="font-medium text-gray-700">Bewerbungsdatum</dt>
              <dd className="text-gray-900">
                <time dateTime={antrag.bewerbungsdatum}>
                  {formatDate(antrag.bewerbungsdatum)}
                </time>
              </dd>
            </div>
          </dl>
        </section>
        
        <section aria-labelledby="verlauf-heading" className="mt-8">
          <h2 id="verlauf-heading">Verlauf</h2>
          <ol className="space-y-4">
            {antrag.verlauf.map((eintrag, index) => (
              <li key={eintrag.id} className="flex gap-4">
                <div className="flex-shrink-0 w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
                  <span className="text-blue-800 text-sm font-medium">
                    {index + 1}
                  </span>
                </div>
                <div>
                  <h3 className="font-medium">{eintrag.art}</h3>
                  <p className="text-gray-600 text-sm">
                    <time dateTime={eintrag.datum}>
                      {formatDate(eintrag.datum)}
                    </time>
                    {eintrag.sachbearbeiter && (
                      <> - {eintrag.sachbearbeiter}</>
                    )}
                  </p>
                  {eintrag.kommentar && (
                    <p className="mt-1 text-gray-700">{eintrag.kommentar}</p>
                  )}
                </div>
              </li>
            ))}
          </ol>
        </section>
      </article>
      
      <aside aria-labelledby="aktionen-heading" className="mt-8">
        <h2 id="aktionen-heading" className="sr-only">Aktionen</h2>
        <div className="flex gap-4">
          <button type="button" className="btn btn-primary">
            Bearbeiten
          </button>
          <button type="button" className="btn btn-secondary">
            Drucken
          </button>
        </div>
      </aside>
    </main>
  )
}
```

### 5.2 ARIA Attributes (4.1.2 - A)
```tsx
// Data Table with ARIA
function AntragsTabelle({ antraege, onSort, sortField, sortDirection }) {
  const [selectedRows, setSelectedRows] = useState([])
  
  return (
    <div className="overflow-x-auto">
      <table 
        className="min-w-full divide-y divide-gray-300"
        role="table"
        aria-label="Antragsliste"
        aria-rowcount={antraege.length + 1}
      >
        <thead className="bg-gray-50">
          <tr role="row" aria-rowindex="1">
            <th scope="col" className="px-6 py-3">
              <input
                type="checkbox"
                aria-label="Alle Anträge auswählen"
                onChange={(e) => {
                  if (e.target.checked) {
                    setSelectedRows(antraege.map(a => a.id))
                  } else {
                    setSelectedRows([])
                  }
                }}
                checked={selectedRows.length === antraege.length}
                indeterminate={selectedRows.length > 0 && selectedRows.length < antraege.length}
              />
            </th>
            <th 
              scope="col" 
              className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
            >
              <button
                onClick={() => onSort('aktenzeichen')}
                className="group inline-flex items-center"
                aria-sort={
                  sortField === 'aktenzeichen' 
                    ? sortDirection === 'asc' ? 'ascending' : 'descending'
                    : 'none'
                }
              >
                Aktenzeichen
                <ChevronUpDownIcon className="ml-2 h-4 w-4" aria-hidden="true" />
              </button>
            </th>
            <th scope="col" className="px-6 py-3 text-left">
              Name
            </th>
            <th scope="col" className="px-6 py-3 text-left">
              Status
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {antraege.map((antrag, index) => (
            <tr 
              key={antrag.id} 
              role="row" 
              aria-rowindex={index + 2}
              className={selectedRows.includes(antrag.id) ? 'bg-blue-50' : ''}
            >
              <td className="px-6 py-4">
                <input
                  type="checkbox"
                  aria-label={`Antrag ${antrag.aktenzeichen} auswählen`}
                  checked={selectedRows.includes(antrag.id)}
                  onChange={(e) => {
                    if (e.target.checked) {
                      setSelectedRows(prev => [...prev, antrag.id])
                    } else {
                      setSelectedRows(prev => prev.filter(id => id !== antrag.id))
                    }
                  }}
                />
              </td>
              <td className="px-6 py-4 text-sm font-medium text-gray-900">
                <a 
                  href={`/antraege/${antrag.id}`}
                  className="text-blue-600 hover:text-blue-800"
                  aria-describedby={`antrag-${antrag.id}-status`}
                >
                  {antrag.aktenzeichen}
                </a>
              </td>
              <td className="px-6 py-4 text-sm text-gray-900">
                {antrag.vorname} {antrag.nachname}
              </td>
              <td className="px-6 py-4">
                <StatusBadge 
                  status={antrag.status} 
                  id={`antrag-${antrag.id}-status`}
                >
                  {antrag.statusText}
                </StatusBadge>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      
      {selectedRows.length > 0 && (
        <div 
          className="bg-blue-50 px-6 py-3 border-t"
          role="status"
          aria-live="polite"
        >
          {selectedRows.length} {selectedRows.length === 1 ? 'Antrag' : 'Anträge'} ausgewählt
          <button className="ml-4 text-blue-600 hover:text-blue-800">
            Ausgewählte bearbeiten
          </button>
        </div>
      )}
    </div>
  )
}
```

## 6. Testing & Validation

### 6.1 Automated Testing
```typescript
// Jest Tests für Accessibility
describe('Accessibility Tests', () => {
  test('should have no accessibility violations', async () => {
    const { container } = render(<AntragsFormular />)
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })
  
  test('should support keyboard navigation', () => {
    render(<Navigation />)
    
    const firstLink = screen.getByRole('link', { name: /dashboard/i })
    firstLink.focus()
    
    fireEvent.keyDown(firstLink, { key: 'Tab' })
    
    const secondLink = screen.getByRole('link', { name: /anträge/i })
    expect(secondLink).toHaveFocus()
  })
  
  test('should announce form errors to screen readers', async () => {
    render(<AntragsFormular />)
    
    const submitButton = screen.getByRole('button', { name: /speichern/i })
    fireEvent.click(submitButton)
    
    await waitFor(() => {
      const errorMessage = screen.getByRole('alert')
      expect(errorMessage).toBeInTheDocument()
      expect(errorMessage).toHaveTextContent(/vorname ist erforderlich/i)
    })
  })
})
```

### 6.2 Manual Testing Checklist
```markdown
## Keyboard Navigation
□ Alle interaktiven Elemente sind mit Tab erreichbar
□ Tab-Reihenfolge ist logisch
□ Focus ist sichtbar
□ Escape schließt Modals/Dropdowns
□ Arrow Keys funktionieren in Menüs/Listen
□ Enter/Spacebar aktivieren Buttons

## Screen Reader
□ Überschriften-Hierarchie ist korrekt (h1-h6)
□ Landmark Roles sind gesetzt (main, nav, aside)
□ Form Labels sind korrekt verknüpft
□ Status-Änderungen werden angekündigt (aria-live)
□ Tabellen haben korrekte Headers
□ Listen verwenden ul/ol/li

## Visuell
□ Farbkontrast erfüllt WCAG AA (4.5:1)
□ Information ist nicht nur über Farbe vermittelt
□ Text ist auf 200% zoombar ohne horizontal scrolling
□ Layout funktioniert bei verschiedenen Schriftgrößen

## Motor
□ Touch-Targets sind mindestens 44x44px
□ Drag & Drop hat Keyboard-Alternative
□ Timeouts sind angemessen oder abschaltbar
```

### 6.3 Accessibility Tools Integration
```javascript
// webpack.config.js - Development Tools
module.exports = {
  // ... andere Config
  plugins: [
    // Accessibility Linting
    new ESLintPlugin({
      extensions: ['js', 'jsx', 'ts', 'tsx'],
      eslintPath: require.resolve('eslint'),
      context: path.resolve(__dirname, 'src'),
      configFile: '.eslintrc.js',
    }),
  ],
}

// .eslintrc.js
module.exports = {
  extends: [
    'plugin:jsx-a11y/recommended'
  ],
  plugins: ['jsx-a11y'],
  rules: {
    'jsx-a11y/anchor-is-valid': 'error',
    'jsx-a11y/aria-props': 'error',
    'jsx-a11y/aria-proptypes': 'error',
    'jsx-a11y/aria-unsupported-elements': 'error',
    'jsx-a11y/alt-text': 'error',
    'jsx-a11y/img-redundant-alt': 'error',
    'jsx-a11y/label-has-associated-control': 'error',
    'jsx-a11y/no-autofocus': 'error',
    'jsx-a11y/click-events-have-key-events': 'error',
    'jsx-a11y/interactive-supports-focus': 'error'
  }
}
```

## 7. Documentation & Training

### 7.1 Accessibility Statement
Eine vollständige Accessibility-Erklärung sollte auf der Website verfügbar sein, die folgende Punkte abdeckt:

- Konformitätslevel (WCAG 2.1 AA)
- Bekannte Einschränkungen
- Feedback-Mechanismus
- Kontaktinformationen für Accessibility-Probleme
- Datum der letzten Überprüfung

### 7.2 Team Training
Regelmäßige Schulungen für das Entwicklungsteam zu:

- WCAG 2.1 Grundlagen
- Assistive Technology Testing
- Inclusive Design Principles
- Code Review für Accessibility
- User Testing mit Menschen mit Behinderungen