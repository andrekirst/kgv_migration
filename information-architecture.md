# KGV Frank - Informationsarchitektur

## 1. Hauptnavigation & Sitemap

### Navigation Hierarchie

```
KGV Frank (Logo/Home)
├── Dashboard
│   ├── Übersicht & KPIs
│   ├── Letzte Aktivitäten
│   └── Schnellaktionen
│
├── Antragsverwaltung
│   ├── Alle Anträge (Liste)
│   ├── Neuer Antrag
│   ├── Antragssuche
│   └── Warteliste
│       ├── Bezirk 32
│       ├── Bezirk 33  
│       └── Rangberechnung
│
├── Angebotsverwaltung
│   ├── Aktuelle Angebote
│   ├── Angebot erstellen
│   ├── Angebotsverlauf
│   └── Ablehnungen
│
├── Verwaltung
│   ├── Bezirke
│   ├── Katasterbezirke
│   ├── Eingangsnummern
│   └── Aktenzeichen
│
├── Berichtswesen
│   ├── Statistiken
│   ├── Export-Center
│   └── Druckvorlagen
│
├── Administration
│   ├── Benutzerverwaltung
│   ├── Berechtigungen
│   ├── Systemkonfiguration
│   └── Datenbank-Tools
│
└── Benutzer (Dropdown)
    ├── Profil
    ├── Einstellungen
    └── Abmelden
```

## 2. Navigation Pattern

### Sidebar Navigation (Desktop)
- Fixierte linke Sidebar (256px breit)
- Kollapsible Menügruppen
- Icons + Text Labels
- Aktiver Zustand visuell hervorgehoben
- Breadcrumb-Navigation im Header

### Mobile Navigation
- Hamburger Menu
- Full-Screen Overlay
- Touch-optimierte Buttons
- Swipe-Gesten für Navigation

### Navigation States
```tsx
interface NavigationState {
  current: string           // Aktuelle Seite
  breadcrumbs: Breadcrumb[] // Breadcrumb-Pfad
  sidebarCollapsed: boolean // Sidebar-Zustand
  activeGroup: string       // Aktive Menügruppe
}

interface Breadcrumb {
  label: string
  href: string
  icon?: React.ComponentType
}
```

## 3. Seiten-Architektur

### Dashboard (/)
**Zweck:** Übersicht und Schnellzugriff auf wichtige Funktionen

**Layout:**
```
[Header mit Breadcrumbs]
[KPI Cards - 4 spaltig]
[Hauptinhalt - 2 spaltig]
├── Linke Spalte (2/3)
│   ├── Letzte Anträge (Tabelle)
│   └── Anstehende Aufgaben
└── Rechte Spalte (1/3)
    ├── Wartelisten-Status
    ├── Schnellaktionen
    └── Systembenachrichtigungen
```

**Inhalte:**
- KPIs: Neue Anträge (Monat), Warteliste gesamt, Offene Angebote, Abschlüsse
- Quick Actions: Neuer Antrag, Angebot erstellen, Suche
- Recent Activity Feed
- Wartelisten-Status nach Bezirken

### Antragsverwaltung (/antraege)
**Zweck:** Zentrale Verwaltung aller Anträge

**Layout:**
```
[Header mit Suchleiste und Filter]
[Action Bar mit Buttons]
[Haupttabelle mit Pagination]
[Ausgewählte Aktionen Bar - conditional]
```

**Tabellen-Spalten:**
- Aktenzeichen
- Name (Antragsteller)
- Bewerbungsdatum
- Status (Badge)
- Wartelisten-Nr.
- Bezirk
- Letztes Update
- Aktionen (View, Edit, Delete)

**Filter & Suche:**
- Volltext-Suche
- Status-Filter
- Bezirks-Filter
- Datums-Filter
- Erweiterte Filter (Sidebar)

### Antrag Detail/Bearbeitung (/antraege/:id)
**Zweck:** Detailansicht und Bearbeitung einzelner Anträge

**Layout - Tab-basiert:**
```
[Header mit Antragstitel und Status]
[Tab Navigation]
├── Grunddaten
├── Kontaktdaten  
├── Verlauf
├── Dokumente
└── Angebote
```

**Grunddaten Tab:**
- Persönliche Daten (Antragsteller + Partner)
- Kontaktinformationen
- Bewerbungsdaten
- Wünsche und Vermerke
- Status-Management

**Verlauf Tab:**
- Chronologische Timeline
- Aktionen/Ereignisse
- Kommentare
- Systemeinträge

### Warteliste (/warteliste)
**Zweck:** Verwaltung und Visualisierung der Wartelisten

**Layout:**
```
[Bezirks-Tabs (32/33)]
[Filter und Rangberechnungs-Tools]
[Wartelisten-Tabelle mit Drag&Drop]
[Rangberechnung Sidebar]
```

**Features:**
- Drag & Drop Neuordnung
- Automatische Rangberechnung
- Filtermöglichkeiten
- Export-Funktionen
- Historische Entwicklung

### Angebotsverwaltung (/angebote)
**Zweck:** Erstellung und Verwaltung von Parzellen-Angeboten

**Layout:**
```
[Status-Overview Cards]
[Filter und Suche]
[Angebots-Tabelle]
[Bulk Actions]
```

**Angebot erstellen Workflow:**
1. Parzellen-Details eingeben
2. Antragsteller aus Warteliste auswählen
3. Angebot generieren
4. Versendung und Tracking

## 4. Content-Gruppierung

### Formular-Strukturierung
**Persönliche Daten:**
- Antragsteller (Primär)
- Partner/Ehepartner (Sekundär)
- Kontaktdaten gruppiert

**Administrative Daten:**
- Aktenzeichen & Nummern
- Bezirks-Zuordnung
- Status & Termine

**Zusatzinformationen:**
- Wünsche & Präferenzen
- Interne Vermerke
- Verlaufsdaten

### Daten-Priorisierung
**Primäre Informationen (immer sichtbar):**
- Name, Aktenzeichen, Status
- Bewerbungsdatum, Bezirk
- Kontaktdaten (Telefon, E-Mail)

**Sekundäre Informationen (auf Anfrage):**
- Detaillierte Adressdaten
- Partner-Informationen
- Historische Daten

**Meta-Informationen (Admin-Bereich):**
- Systemdaten, Logs
- Berechtigungen
- Konfigurationen

## 5. Search & Filter Architecture

### Globale Suche (Header)
```tsx
interface GlobalSearch {
  query: string
  filters: {
    type: 'antraege' | 'personen' | 'angebote' | 'all'
    dateRange?: DateRange
    status?: string[]
  }
  results: SearchResult[]
}
```

### Erweiterte Filter (Sidebar)
```tsx
interface AdvancedFilters {
  // Basis-Filter
  status: string[]
  bezirk: string[]
  dateRange: DateRange
  
  // Spezifische Filter
  antragsteller: string
  wartelistenNr: string
  aktenzeichen: string
  
  // Boolean Filter
  aktiv: boolean
  mitPartner: boolean
  hatAngebot: boolean
}
```

### Saved Searches & Favorites
- Benutzer können häufige Suchen speichern
- Schnelle Filter-Presets
- Personalisierte Dashboards

## 6. Data Flow & State Management

### Page State Structure
```tsx
interface PageState {
  // Loading States
  loading: boolean
  error: string | null
  
  // Data
  data: any[]
  selectedItems: string[]
  
  // UI State
  filters: FilterState
  sorting: SortState
  pagination: PaginationState
  
  // Modal/Dialog States
  showModal: boolean
  modalType: 'create' | 'edit' | 'delete'
  modalData: any
}
```

### Navigation Context
```tsx
interface NavigationContext {
  currentPage: string
  breadcrumbs: Breadcrumb[]
  sidebarState: {
    collapsed: boolean
    activeGroup: string
  }
  
  // Functions
  navigate: (path: string) => void
  setBreadcrumbs: (crumbs: Breadcrumb[]) => void
  toggleSidebar: () => void
}
```

## 7. Responsive Behavior

### Desktop (1024px+)
- Vollständige Sidebar-Navigation
- Multi-Column Layouts
- Detaillierte Tabellen
- Erweiterte Filter-Optionen

### Tablet (768px - 1023px)
- Kollapsible Sidebar
- 2-Column Layouts
- Simplified Tables
- Tab-Navigation optimiert

### Mobile (< 768px)
- Hamburger Navigation
- Single-Column Layouts
- Card-based Data Display
- Touch-optimierte Interaktionen

### Navigation Adaptation
```css
/* Desktop */
.nav-desktop {
  @apply hidden lg:flex lg:flex-col lg:fixed lg:left-0 lg:w-64;
}

/* Mobile */
.nav-mobile {
  @apply lg:hidden;
}

/* Content adjustment */
.main-content {
  @apply ml-0 lg:ml-64;
}
```

## 8. Error Handling & Empty States

### Error States
- Network Errors
- Validation Errors
- Permission Errors
- System Errors

### Empty States
- Keine Suchergebnisse
- Leere Listen
- Fehlende Daten
- Neue Benutzer Onboarding

### Loading States
- Page Loading
- Data Fetching
- Form Submission
- Background Operations

## 9. Accessibility Navigation

### Keyboard Navigation
- Tab-Order klar definiert
- Skip-Links zu Hauptinhalten
- Tastatur-Shortcuts für häufige Aktionen

### Screen Reader Support
- Semantic HTML Structure
- ARIA Labels und Descriptions
- Live Regions für Updates

### Focus Management
- Visible Focus Indicators
- Focus Trapping in Modals
- Logical Focus Flow