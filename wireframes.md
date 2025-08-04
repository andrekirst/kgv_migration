# KGV Frank - Wireframes & Screen Designs

## 1. Dashboard Wireframe

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ HEADER                                                                      │
│ [Logo] KGV Frank    [Search Bar]           [User] [Notifications] [Menu]   │
└─────────────────────────────────────────────────────────────────────────────┘
┌───────────────┬─────────────────────────────────────────────────────────────┐
│ SIDEBAR       │ MAIN CONTENT                                                │
│               │                                                             │
│ 📊 Dashboard  │ Dashboard > Übersicht                                      │
│               │                                                             │
│ 📋 Anträge    │ ┌─────────┬─────────┬─────────┬─────────┐                  │
│ ├─ Alle       │ │  📈     │  👥     │  📋     │  ✅     │                  │
│ ├─ Neu        │ │ 24      │ 156     │ 8       │ 12      │                  │
│ └─ Suche      │ │ Neue    │ Warte-  │ Offene  │ Abschl. │                  │
│               │ │ Anträge │ liste   │ Angeb.  │ Monat   │                  │
│ 🎯 Warteliste │ └─────────┴─────────┴─────────┴─────────┘                  │
│ ├─ Bezirk 32  │                                                             │
│ └─ Bezirk 33  │ ┌─────────────────────────────┬─────────────────────────┐  │
│               │ │ LETZTE ANTRÄGE              │ SCHNELLAKTIONEN         │  │
│ 🏡 Angebote   │ │                             │                         │  │
│ ├─ Aktuell    │ │ ┌─ Müller, Hans ──────────┐ │ [+ Neuer Antrag]       │  │
│ ├─ Erstellen  │ │ │  AZ: 32/2025/001        │ │ [🎯 Warteliste]        │  │
│ └─ Verlauf    │ │ │  Status: Wartend        │ │ [🏡 Angebot erstellen] │  │
│               │ │ │  Datum: 01.08.2025      │ │ [📊 Statistiken]       │  │
│ ⚙️ Verwaltung │ │ └─────────────────────────┘ │                         │  │
│               │ │                             │ WARTELISTEN STATUS      │  │
│ 📊 Berichte   │ │ ┌─ Schmidt, Anna ─────────┐ │                         │  │
│               │ │ │  AZ: 33/2025/002        │ │ Bezirk 32: 89 Wartende │  │
│ 👤 Admin      │ │ │  Status: Angebot        │ │ Bezirk 33: 67 Wartende │  │
│               │ │ │  Datum: 28.07.2025      │ │                         │  │
│               │ │ └─────────────────────────┘ │ [📈 Details anzeigen]  │  │
│               │ │                             │                         │  │
│               │ │ [Alle Anträge anzeigen]    │                         │  │
│               │ └─────────────────────────────┴─────────────────────────┘  │
└───────────────┴─────────────────────────────────────────────────────────────┘
```

### Dashboard Komponenten Details

**KPI Cards (4x1 Grid):**
```
┌─────────────────┐
│ 📈 24           │
│ Neue Anträge    │
│ ↑ +15% vs Monat │
└─────────────────┘
```

**Letzte Anträge Liste:**
- Kompakte Kartenansicht
- Status-Badges farbkodiert
- Direkte Aktionen (Bearbeiten, Anzeigen)
- "Alle anzeigen" Link

**Schnellaktionen Panel:**
- Große, gut erkennbare Buttons
- Icon + Text Kombination
- Häufig verwendete Funktionen

## 2. Antragsliste Wireframe

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ HEADER mit Breadcrumbs                                                     │
│ Dashboard > Antragsverwaltung > Alle Anträge                              │
└─────────────────────────────────────────────────────────────────────────────┘
┌───────────────┬─────────────────────────────────────────────────────────────┐
│ SIDEBAR       │ MAIN CONTENT                                                │
│               │                                                             │
│ [Navigation]  │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ FILTER & SEARCH BAR                                     │ │
│               │ │ [🔍 Suche...] [Status ▼] [Bezirk ▼] [🗓️] [Filter ⚙️] │ │
│               │ └─────────────────────────────────────────────────────────┘ │
│               │                                                             │
│               │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ ACTION BAR                                              │ │
│               │ │ [+ Neuer Antrag] [📤 Export] [🗑️ Löschen] (2 ausgewählt)│ │
│               │ └─────────────────────────────────────────────────────────┘ │
│               │                                                             │
│               │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ ANTRAGS-TABELLE                                         │ │
│               │ │ ┌─┬─────────────┬─────────────┬────────┬────────┬───────┐│ │
│               │ │ │☐│Aktenzeichen │Name         │Datum   │Status  │Aktion ││ │
│               │ │ ├─┼─────────────┼─────────────┼────────┼────────┼───────┤│ │
│               │ │ │☐│32/2025/001  │Müller, Hans │01.08.25│🟡Warten│[⋯]   ││ │
│               │ │ │☑│33/2025/002  │Schmidt, A.  │28.07.25│🟢Aktiv │[⋯]   ││ │
│               │ │ │☐│32/2025/003  │Weber, Klaus │25.07.25│🔴Inakt.│[⋯]   ││ │
│               │ │ │☑│33/2025/004  │Meyer, Lisa  │20.07.25│🟡Warten│[⋯]   ││ │
│               │ │ │☐│32/2025/005  │König, Max   │15.07.25│🟢Aktiv │[⋯]   ││ │
│               │ │ └─┴─────────────┴─────────────┴────────┴────────┴───────┘│ │
│               │ └─────────────────────────────────────────────────────────┘ │
│               │                                                             │
│               │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ PAGINATION                                              │ │
│               │ │ Zeige 1-10 von 156 Einträgen  [◀] 1 2 3 ... 16 [▶]    │ │
│               │ └─────────────────────────────────────────────────────────┘ │
└───────────────┴─────────────────────────────────────────────────────────────┘
```

### Tabellen-Features
- **Sortierbare Spalten** (Klick auf Header)
- **Multi-Select** mit Checkbox
- **Status-Badges** farbkodiert
- **Dropdown-Aktionen** (Bearbeiten, Anzeigen, Löschen)
- **Responsive** (auf Mobile: Card-Layout)

## 3. Antrag Detail/Bearbeitung Wireframe

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ HEADER                                                                      │
│ Dashboard > Anträge > Antrag bearbeiten                                   │
└─────────────────────────────────────────────────────────────────────────────┘
┌───────────────┬─────────────────────────────────────────────────────────────┐
│ SIDEBAR       │ MAIN CONTENT                                                │
│               │                                                             │
│               │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ ANTRAG HEADER                                           │ │
│               │ │ 👤 Müller, Hans (32/2025/001)        🟡 Status: Wartend│ │
│               │ │ Bewerbung: 01.08.2025                 [Speichern] [❌] │ │
│               │ └─────────────────────────────────────────────────────────┘ │
│               │                                                             │
│               │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ TAB NAVIGATION                                          │ │
│               │ │ [●Grunddaten] [Kontakt] [Verlauf] [Dokumente] [Angebote]│ │
│               │ └─────────────────────────────────────────────────────────┘ │
│               │                                                             │
│               │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ GRUNDDATEN TAB                                          │ │
│               │ │                                                         │ │
│               │ │ ┌─── ANTRAGSTELLER ──────┬─── PARTNER/EHEPARTNER ────┐  │ │
│               │ │ │                        │                           │  │ │
│               │ │ │ Anrede: [Herr ▼]      │ Anrede: [Frau ▼]         │  │ │
│               │ │ │ Titel:  [_________]    │ Titel:  [_________]       │  │ │
│               │ │ │ Vorname:[Hans_____]    │ Vorname:[Maria____]       │  │ │
│               │ │ │ Name:   [Müller___]    │ Name:   [Müller___]       │  │ │
│               │ │ │                        │                           │  │ │
│               │ │ │ Geburtsdatum:          │ Geburtsdatum:             │  │ │
│               │ │ │ [📅 15.03.1975]       │ [📅 22.07.1978]          │  │ │
│               │ │ └────────────────────────┴───────────────────────────┘  │ │
│               │ │                                                         │ │
│               │ │ ┌─── KONTAKTDATEN ─────────────────────────────────────┐  │ │
│               │ │ │                                                     │  │ │
│               │ │ │ Straße:     [Musterstraße 123_______________]      │  │ │
│               │ │ │ PLZ/Ort:    [60314] [Frankfurt am Main______]      │  │ │
│               │ │ │ Telefon:    [069-12345678________________]          │  │ │
│               │ │ │ Mobil:      [0171-1234567________________]          │  │ │
│               │ │ │ E-Mail:     [hans.mueller@email.de_______]          │  │ │
│               │ │ └─────────────────────────────────────────────────────┘  │ │
│               │ │                                                         │ │
│               │ │ ┌─── ANTRAGSDATEN ─────────────────────────────────────┐  │ │
│               │ │ │                                                     │  │ │
│               │ │ │ Bewerbungsdatum: [📅 01.08.2025]                   │  │ │
│               │ │ │ Bezirk:          [Bezirk 32 ▼]                     │  │ │
│               │ │ │ Wartelisten-Nr.: [32-2025-001]                     │  │ │
│               │ │ │                                                     │  │ │
│               │ │ │ Wünsche/Präferenzen:                               │  │ │
│               │ │ │ [Größe mind. 300qm, sonnige Lage bevorzugt____]   │  │ │
│               │ │ │ [________________________________]                 │  │ │
│               │ │ │                                                     │  │ │
│               │ │ │ Interne Vermerke:                                  │  │ │
│               │ │ │ [Antragsunterlagen vollständig______________]      │  │ │
│               │ │ │ [________________________________]                 │  │ │
│               │ │ └─────────────────────────────────────────────────────┘  │ │
│               │ └─────────────────────────────────────────────────────────┘ │
│               │                                                             │
│               │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ FORM ACTIONS                                            │ │
│               │ │ [💾 Speichern] [↩️ Zurück] [🗑️ Löschen]                │ │
│               │ └─────────────────────────────────────────────────────────┘ │
└───────────────┴─────────────────────────────────────────────────────────────┘
```

## 4. Warteliste Wireframe

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ HEADER                                                                      │
│ Dashboard > Warteliste                                                     │
└─────────────────────────────────────────────────────────────────────────────┘
┌───────────────┬─────────────────────────────────────────────────────────────┐
│ SIDEBAR       │ MAIN CONTENT                                                │
│               │                                                             │
│               │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ BEZIRKS-TABS                                            │ │
│               │ │ [●Bezirk 32 (89)] [Bezirk 33 (67)] [📊 Statistiken]    │ │
│               │ └─────────────────────────────────────────────────────────┘ │
│               │                                                             │
│               │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ TOOLS & FILTER                                          │ │
│               │ │ [🔍 Suche] [🎯 Rangberechnung] [📤 Export] [⚙️ Config] │ │
│               │ └─────────────────────────────────────────────────────────┘ │
│               │                                                             │
│               │ ┌─────────────────────────────────────────────────────────┐ │
│               │ │ WARTELISTE BEZIRK 32                                    │ │
│               │ │                                                         │ │
│               │ │ ┌─────┬─────────────────┬─────────────┬────────────────┐ │ │
│               │ │ │Rang │Name             │Bewerbung    │Letzte Aktion   │ │ │
│               │ │ ├─────┼─────────────────┼─────────────┼────────────────┤ │ │
│               │ │ │ 1   │🟢 Weber, Klaus  │15.01.2023   │Angebot abgel.  │ │ │
│               │ │ │ 2   │🟡 Müller, Hans  │01.08.2025   │Neu eingetr.    │ │ │
│               │ │ │ 3   │🟢 König, Max    │15.07.2025   │Bestätigt       │ │ │
│               │ │ │ 4   │🟡 Bauer, Lisa   │22.06.2025   │Wartend          │ │ │
│               │ │ │ 5   │🟢 Graf, Thomas  │30.05.2025   │Aktiv           │ │ │
│               │ │ │ ... │                 │             │                │ │ │
│               │ │ └─────┴─────────────────┴─────────────┴────────────────┘ │ │
│               │ │                                                         │ │
│               │ │ ✋ Drag & Drop zum Neuordnen aktiviert                  │ │
│               │ └─────────────────────────────────────────────────────────┘ │
└───────────────┴─────────────────────────────────────────────────────────┬───┐
                                                                          │   │
                ┌─────────────────────────────────────────────────────────┘   │
                │ RANGBERECHNUNG SIDEBAR                                      │
                │                                                             │
                │ ┌─── AKTUELLE AUSWAHL ─────────────────────────────────────┐│
                │ │ ✓ Weber, Klaus (Rang 1)                                 ││
                │ │   Bewerbung: 15.01.2023                                 ││
                │ │   Letztes Angebot: 15.07.2025 (abgelehnt)               ││
                │ └─────────────────────────────────────────────────────────┘│
                │                                                             │
                │ ┌─── RANGBERECHNUNG ───────────────────────────────────────┐│
                │ │ Basis-Punkte (Bewerbungsdatum): 100                     ││
                │ │ + Wartezeit (2 Jahre): +24                              ││
                │ │ - Ablehnungen (1x): -5                                  ││
                │ │ + Soziale Kriterien: +10                                ││
                │ │ ─────────────────────────                               ││
                │ │ Gesamt: 129 Punkte                                      ││
                │ └─────────────────────────────────────────────────────────┘│
                │                                                             │
                │ ┌─── AKTIONEN ─────────────────────────────────────────────┐│
                │ │ [📝 Rang manuell setzen]                                ││
                │ │ [🎯 Angebot erstellen]                                  ││
                │ │ [📞 Kontakt aufnehmen]                                  ││
                │ │ [📋 Verlauf anzeigen]                                   ││
                │ └─────────────────────────────────────────────────────────┘│
                └─────────────────────────────────────────────────────────────┘
```

## 5. Mobile Wireframes

### Mobile Dashboard (375px)
```
┌─────────────────────────────┐
│ [☰] KGV Frank      [🔔][👤] │
├─────────────────────────────┤
│ 📊 Dashboard               │
│                             │
│ ┌───────┬───────┬───────────┐│
│ │  📈   │  👥   │     📋    ││
│ │  24   │ 156   │     8     ││
│ │ Neue  │Warte- │ Offene    ││
│ │Anträge│liste  │Angebote   ││
│ └───────┴───────┴───────────┘│
│                             │
│ ┌─ LETZTE ANTRÄGE ─────────┐ │
│ │                         │ │
│ │ ┌─ Müller, Hans ────────┐│ │
│ │ │ AZ: 32/2025/001       ││ │
│ │ │ 🟡 Wartend 01.08.2025 ││ │
│ │ │ [Bearbeiten] [Mehr ⋯] ││ │
│ │ └───────────────────────┘│ │
│ │                         │ │
│ │ ┌─ Schmidt, Anna ───────┐│ │
│ │ │ AZ: 33/2025/002       ││ │
│ │ │ 🟢 Angebot 28.07.2025 ││ │
│ │ │ [Bearbeiten] [Mehr ⋯] ││ │
│ │ └───────────────────────┘│ │
│ │                         │ │
│ │ [Alle anzeigen]         │ │
│ └─────────────────────────┘ │
│                             │
│ ┌─ SCHNELLAKTIONEN ────────┐ │
│ │ [+ Neuer Antrag]        │ │
│ │ [🎯 Warteliste]         │ │
│ │ [🏡 Angebot erstellen]  │ │
│ │ [📊 Statistiken]        │ │
│ └─────────────────────────┘ │
└─────────────────────────────┘
```

### Mobile Navigation (Hamburger Menu)
```
┌─────────────────────────────┐
│ [✕] Navigation             │
├─────────────────────────────┤
│                             │
│ 👤 Hans Mustermann         │
│    Administrator            │
│                             │
│ 📊 Dashboard               │
│                             │
│ 📋 Antragsverwaltung       │
│   ├─ Alle Anträge          │
│   ├─ Neuer Antrag          │
│   └─ Antragssuche          │
│                             │
│ 🎯 Warteliste              │
│   ├─ Bezirk 32             │
│   ├─ Bezirk 33             │
│   └─ Rangberechnung        │
│                             │
│ 🏡 Angebotsverwaltung      │
│                             │
│ ⚙️ Verwaltung              │
│                             │
│ 📊 Berichtswesen           │
│                             │
│ 👤 Administration          │
│                             │
│ ─────────────────────────   │
│                             │
│ ⚙️ Einstellungen           │
│ 🚪 Abmelden                │
│                             │
└─────────────────────────────┘
```

## 6. Form Layout Patterns

### Responsive Form Grid
```css
/* Desktop: 2-3 Column Layout */
.form-grid-desktop {
  @apply grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6;
}

/* Tablet: 2 Column Layout */
.form-grid-tablet {
  @apply grid grid-cols-1 md:grid-cols-2 gap-4;
}

/* Mobile: Single Column */
.form-grid-mobile {
  @apply grid grid-cols-1 gap-4;
}
```

### Form Field Grouping
```
┌─── GRUPPE 1: PERSÖNLICHE DATEN ────────────────────┐
│ [Anrede] [Titel_______________] [Vorname_________] │
│ [Nachname_____________________] [Geburtsdatum___] │
└────────────────────────────────────────────────────┘

┌─── GRUPPE 2: KONTAKTDATEN ─────────────────────────┐
│ [Straße_______________________]                    │
│ [PLZ___] [Ort______________] [Land______________]   │
│ [Telefon_________] [E-Mail__________________]       │
└────────────────────────────────────────────────────┘
```

## 7. Data Visualization Wireframes

### Dashboard Charts
```
┌─── ANTRÄGE ÜBER ZEIT ──────────────────────────────┐
│                                      ┌─┐           │
│    ┌─┐                    ┌─┐     ┌─┐│ │           │
│    │ │     ┌─┐           │ │     │ ││ │   ┌─┐     │
│  ┌─┐│ │  ┌─┐│ │  ┌─┐  ┌─┐│ │  ┌─┐│ ││ │┌─┐│ │     │
│  │ ││ │  │ ││ │  │ │  │ ││ │  │ ││ ││ ││ ││ │     │
│  └─┘└─┘  └─┘└─┘  └─┘  └─┘└─┘  └─┘└─┘└─┘└─┘└─┘     │
│  Jan Feb Mar Apr Mai Jun Jul Aug Sep Okt Nov       │
└────────────────────────────────────────────────────┘

┌─── WARTELISTEN STATUS ─────────────────────────────┐
│ Bezirk 32 ████████████████░░░░ 89 (75%)          │
│ Bezirk 33 ████████████░░░░░░░░░ 67 (60%)          │
│                                                    │
│ 🟢 Aktiv: 98  🟡 Wartend: 58  🔴 Inaktiv: 12     │
└────────────────────────────────────────────────────┘
```

## 8. Error & Loading States

### Loading Skeleton
```
┌─────────────────────────────────────────────────────┐
│ ████░░░░░░░░░░░░░░░  ░░░░░░░░░░  ████░░░░░░        │
│ ████████░░░░░░░░░░░  ░░░░░░░░░░  ░░░░░░░░░░        │
│ ████░░░░░░░░░░░░░░░  ░░░░░░░░░░  ████░░░░░░        │
│ ████████░░░░░░░░░░░  ░░░░░░░░░░  ░░░░░░░░░░        │
└─────────────────────────────────────────────────────┘
```

### Empty State
```
┌─────────────────────────────────────────────────────┐
│                        📋                          │
│                 Keine Anträge                      │
│                                                     │
│        Es wurden noch keine Anträge erstellt.      │
│                                                     │
│              [+ Ersten Antrag erstellen]           │
└─────────────────────────────────────────────────────┘
```

### Error State
```
┌─────────────────────────────────────────────────────┐
│                        ⚠️                          │
│              Fehler beim Laden                      │
│                                                     │
│    Die Daten konnten nicht geladen werden.         │
│           Bitte versuchen Sie es erneut.           │
│                                                     │
│                 [🔄 Erneut laden]                  │
└─────────────────────────────────────────────────────┘
```