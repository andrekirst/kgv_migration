# KGV Frank - Design System

## 1. Design Tokens & Tailwind CSS Konfiguration

### Farbpalette
```css
/* Primary Colors - Verwaltungsblau */
--primary-50: #eff6ff
--primary-100: #dbeafe
--primary-500: #3b82f6   /* Hauptfarbe */
--primary-600: #2563eb
--primary-700: #1d4ed8
--primary-900: #1e293b

/* Secondary Colors - Akzentgrün */
--secondary-50: #f0fdf4
--secondary-100: #dcfce7
--secondary-500: #22c55e
--secondary-600: #16a34a

/* Status Colors */
--success: #10b981
--warning: #f59e0b
--error: #ef4444
--info: #3b82f6

/* Neutral Grays */
--gray-50: #f9fafb
--gray-100: #f3f4f6
--gray-200: #e5e7eb
--gray-300: #d1d5db
--gray-400: #9ca3af
--gray-500: #6b7280
--gray-600: #4b5563
--gray-700: #374151
--gray-800: #1f2937
--gray-900: #111827
```

### Typografie
```css
/* Font Families */
--font-sans: 'Inter', system-ui, sans-serif
--font-mono: 'JetBrains Mono', monospace

/* Font Sizes */
--text-xs: 0.75rem     /* 12px */
--text-sm: 0.875rem    /* 14px */
--text-base: 1rem      /* 16px */
--text-lg: 1.125rem    /* 18px */
--text-xl: 1.25rem     /* 20px */
--text-2xl: 1.5rem     /* 24px */
--text-3xl: 1.875rem   /* 30px */
--text-4xl: 2.25rem    /* 36px */

/* Line Heights */
--leading-tight: 1.25
--leading-normal: 1.5
--leading-relaxed: 1.75
```

### Spacing & Layout
```css
/* Spacing Scale */
--space-1: 0.25rem     /* 4px */
--space-2: 0.5rem      /* 8px */
--space-3: 0.75rem     /* 12px */
--space-4: 1rem        /* 16px */
--space-6: 1.5rem      /* 24px */
--space-8: 2rem        /* 32px */
--space-12: 3rem       /* 48px */
--space-16: 4rem       /* 64px */

/* Border Radius */
--radius-sm: 0.125rem  /* 2px */
--radius: 0.25rem      /* 4px */
--radius-md: 0.375rem  /* 6px */
--radius-lg: 0.5rem    /* 8px */
--radius-xl: 0.75rem   /* 12px */

/* Shadows */
--shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05)
--shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)
--shadow-md: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)
--shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)
```

## 2. Komponentenbibliothek

### Basis-Komponenten

#### Button
```tsx
// Varianten: primary, secondary, outline, ghost
// Größen: sm, md, lg
// Zustände: default, hover, active, disabled, loading

<Button variant="primary" size="md">
  Antrag speichern
</Button>

<Button variant="outline" size="sm" icon={<PlusIcon />}>
  Hinzufügen
</Button>
```

#### Input & Form Elements
```tsx
// Text Input mit Label und Validierung
<FormField>
  <Label required>Vorname</Label>
  <Input 
    type="text" 
    placeholder="Max" 
    error={errors.vorname}
    helperText="Bitte geben Sie den Vornamen ein"
  />
  <ErrorMessage>{errors.vorname}</ErrorMessage>
</FormField>

// Select/Dropdown
<Select 
  label="Bezirk" 
  options={bezirke}
  placeholder="Bezirk auswählen"
  searchable
  clearable
/>

// Date Picker
<DatePicker 
  label="Bewerbungsdatum"
  value={bewerbungsdatum}
  onChange={setBewerbungsdatum}
  maxDate={new Date()}
/>
```

#### Data Display
```tsx
// Card für Datengruppierung
<Card>
  <CardHeader>
    <CardTitle>Antragsdaten</CardTitle>
    <CardDescription>Grundlegende Informationen zum Antrag</CardDescription>
  </CardHeader>
  <CardContent>
    {/* Content */}
  </CardContent>
</Card>

// Table für Listen
<DataTable 
  columns={antragColumns}
  data={antraege}
  sortable
  filterable
  pagination
  selectable
  actions={tableActions}
/>

// Badge für Status
<Badge variant="success">Aktiv</Badge>
<Badge variant="warning">Wartend</Badge>
<Badge variant="error">Abgelehnt</Badge>
```

#### Navigation
```tsx
// Sidebar Navigation
<Sidebar>
  <SidebarHeader>
    <Logo />
    <UserProfile />
  </SidebarHeader>
  <SidebarContent>
    <NavGroup title="Antragsverwaltung">
      <NavItem icon={<ApplicationIcon />} href="/antraege">
        Anträge
      </NavItem>
      <NavItem icon={<WaitlistIcon />} href="/warteliste">
        Warteliste
      </NavItem>
    </NavGroup>
  </SidebarContent>
</Sidebar>

// Breadcrumbs
<Breadcrumbs>
  <BreadcrumbItem href="/dashboard">Dashboard</BreadcrumbItem>
  <BreadcrumbItem href="/antraege">Anträge</BreadcrumbItem>
  <BreadcrumbItem current>Antrag bearbeiten</BreadcrumbItem>
</Breadcrumbs>

// Tabs
<Tabs defaultValue="grunddaten">
  <TabsList>
    <TabsTrigger value="grunddaten">Grunddaten</TabsTrigger>
    <TabsTrigger value="kontakt">Kontaktdaten</TabsTrigger>
    <TabsTrigger value="verlauf">Verlauf</TabsTrigger>
  </TabsList>
  <TabsContent value="grunddaten">
    {/* Grunddaten Form */}
  </TabsContent>
</Tabs>
```

### Layout-Komponenten

#### Page Layout
```tsx
<PageLayout>
  <PageHeader>
    <PageTitle>Antragsverwaltung</PageTitle>
    <PageActions>
      <Button variant="primary">Neuer Antrag</Button>
    </PageActions>
  </PageHeader>
  <PageContent>
    {/* Main content */}
  </PageContent>
</PageLayout>
```

#### Grid System
```tsx
// Responsive Grid mit Tailwind
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
  <Card>...</Card>
  <Card>...</Card>
  <Card>...</Card>
</div>

// Form Grid
<div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
  <FormField>...</FormField>
  <FormField>...</FormField>
</div>
```

## 3. Responsive Design Breakpoints

```css
/* Mobile First Approach */
/* xs: 0px */
/* sm: 640px */
/* md: 768px */
/* lg: 1024px */
/* xl: 1280px */
/* 2xl: 1536px */

/* Layout Anpassungen */
.sidebar {
  @apply hidden lg:block lg:w-64;
}

.mobile-nav {
  @apply block lg:hidden;
}

.table-responsive {
  @apply block md:table;
}

.form-grid {
  @apply grid-cols-1 md:grid-cols-2 lg:grid-cols-3;
}
```

## 4. Accessibility (WCAG 2.1)

### Farbkontrast
- Normale Texte: Mindestens 4.5:1
- Große Texte (18pt+): Mindestens 3:1
- UI-Elemente: Mindestens 3:1

### Keyboard Navigation
```tsx
// Focus Management
const FocusTrap = ({ children }) => {
  // Implementierung für Focus-Steuerung in Modals
}

// Skip Links
<SkipLink href="#main-content">
  Zum Hauptinhalt springen
</SkipLink>

// Aria Labels
<Button aria-label="Antrag löschen" aria-describedby="delete-help">
  <TrashIcon />
</Button>
<div id="delete-help" className="sr-only">
  Dieser Vorgang kann nicht rückgängig gemacht werden
</div>
```

### Semantic HTML
```tsx
// Proper heading hierarchy
<main>
  <h1>Antragsverwaltung</h1>
  <section>
    <h2>Aktuelle Anträge</h2>
    <article>
      <h3>Antrag #{antragNummer}</h3>
    </article>
  </section>
</main>

// Form labels and descriptions
<fieldset>
  <legend>Persönliche Daten</legend>
  <FormField>
    <Label htmlFor="vorname">Vorname</Label>
    <Input id="vorname" aria-describedby="vorname-help" />
    <div id="vorname-help">Bitte geben Sie Ihren Vornamen ein</div>
  </FormField>
</fieldset>
```

## 5. Icon System

Verwendung von Lucide React Icons für Konsistenz:

```tsx
import { 
  User, 
  FileText, 
  Calendar, 
  Search, 
  Plus, 
  Edit, 
  Trash2, 
  Download,
  Filter,
  MoreHorizontal
} from 'lucide-react'

// Icon Größen
<Icon className="w-4 h-4" />  // sm
<Icon className="w-5 h-5" />  // md (default)
<Icon className="w-6 h-6" />  // lg
```

## 6. Animation & Transitions

```css
/* Subtile Transitions für bessere UX */
.transition-base {
  @apply transition-colors duration-200 ease-in-out;
}

.transition-transform {
  @apply transition-transform duration-300 ease-out;
}

/* Loading States */
@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

.loading-pulse {
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

/* Slide in animations */
@keyframes slideIn {
  from { transform: translateX(-100%); }
  to { transform: translateX(0); }
}

.slide-in {
  animation: slideIn 0.3s ease-out;
}
```

## 7. Dark Mode Support (Optional)

```css
/* CSS Variables für Theme Switching */
:root {
  --bg-primary: theme('colors.white');
  --bg-secondary: theme('colors.gray.50');
  --text-primary: theme('colors.gray.900');
  --text-secondary: theme('colors.gray.600');
}

[data-theme="dark"] {
  --bg-primary: theme('colors.gray.900');
  --bg-secondary: theme('colors.gray.800');
  --text-primary: theme('colors.gray.100');
  --text-secondary: theme('colors.gray.400');
}
```