# KGV Frank - User Journey Maps

## 1. Neuen Antrag erstellen (Kernuser Journey)

### Benutzer: Verwaltungsmitarbeiter (Sarah Müller, 35, Sachbearbeiterin)
### Ziel: Einen neuen KGV-Antrag im System erfassen

```
┌─────────────┬──────────────┬──────────────┬──────────────┬──────────────┐
│   PHASE     │   STARTEN    │  ERFASSEN    │ VALIDIEREN   │ ABSCHLIESSEN │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ TOUCHPOINTS │ • Dashboard  │ • Formular   │ • Prüfung    │ • Bestätigung│
│             │ • Navigation │ • Eingabe-   │ • Fehler-    │ • Warteliste │
│             │              │   felder     │   meldungen  │ • System     │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│  AKTIONEN   │ • Anmelden   │ • Personen-  │ • Daten      │ • Antrag     │
│             │ • Dashboard  │   daten      │   prüfen     │   speichern  │
│             │   öffnen     │   eingeben   │ • Fehler     │ • Akten-     │
│             │ • "Neue      │ • Kontakt-   │   korrig.    │   zeichen    │
│             │   Antrag"    │   daten      │ • Voll-      │   vergeben   │
│             │   klicken    │   eingeben   │   ständig-   │ • Warteliste │
│             │              │ • Bezirk     │   keit prüf. │   zuordnen   │
│             │              │   auswählen  │              │              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ GEDANKEN    │ "Neuer       │ "Hoffentlich │ "Stimmen     │ "Geschafft!  │
│             │  Antrag ist  │  ist alles   │  alle Daten? │  Der Antrag  │
│             │  eingegangen"│  richtig     │  Ist der     │  ist im      │
│             │ "Wo finde    │  ausgefüllt" │  Antrag      │  System"     │
│             │  ich das?"   │ "Welcher     │  vollständig?"│ "Wartelisten-│
│             │              │  Bezirk?"    │              │  platz ist   │
│             │              │              │              │  vergeben"   │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ EMOTIONEN   │ 😊 Routine   │ 😐 Konzent-  │ 😰 Unsicher- │ 😊 Zufrieden │
│             │    Neutral   │    riert     │    heit      │    Erfolg    │
│             │              │ 😤 Frustrier.│ 😓 Stress    │              │
│             │              │    bei       │    bei       │              │
│             │              │    Fehlern   │    Fehlern   │              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ PAIN POINTS │ • Unklare    │ • Zu viele   │ • Unklare    │ • Lange      │
│             │   Navigation │   Pflicht-   │   Fehler-    │   Speicher-  │
│             │ • Versteckte │   felder     │   meldungen  │   zeiten     │
│             │   Funktion   │ • Langsame   │ • Validierung│ • Fehlende   │
│             │              │   Eingabe    │   zu spät    │   Bestätigung│
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│OPPORTUNITIES│ • Prominent  │ • Autofill   │ • Live-      │ • Sofortige  │
│             │   Button     │ • Intelligte │   Validierung│   Bestätigung│
│             │ • Schnell-   │   Vorschläge │ • Klare      │ • Status-    │  
│             │   aktionen   │ • Form-      │   Hinweise   │   updates    │
│             │              │   wizard     │ • Progress   │ • Next steps │
│             │              │              │   indicator  │              │
└─────────────┴──────────────┴──────────────┴──────────────┴──────────────┘
```

### Design-Lösungen für Pain Points:

1. **Prominenter "Neuer Antrag" Button** im Dashboard
2. **Multi-Step Form Wizard** mit Progress-Anzeige
3. **Live-Validierung** mit hilfreichen Fehlermeldungen
4. **Auto-Save** Funktionalität
5. **Sofortige Bestätigung** mit nächsten Schritten

## 2. Warteliste verwalten (Administrative Journey)

### Benutzer: Teamleiter (Michael Weber, 42, Sachgebietsleiter)
### Ziel: Warteliste überprüfen und Rangfolge anpassen

```
┌─────────────┬──────────────┬──────────────┬──────────────┬──────────────┐
│   PHASE     │  ÜBERBLICK   │  ANALYSE     │  ANPASSUNG   │  UMSETZUNG   │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ TOUCHPOINTS │ • Warteliste │ • Detail-    │ • Rang-      │ • Drag & Drop│
│             │   Dashboard  │   ansicht    │   berechnung │ • Speichern  │
│             │ • Filter     │ • Verlauf    │ • Manual     │ • Export     │
│             │              │              │   override   │              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│  AKTIONEN   │ • Warteliste │ • Einzelne   │ • Punkte     │ • Neue       │
│             │   öffnen     │   Anträge    │   berechnen  │   Reihenfolge│
│             │ • Status     │   prüfen     │ • Reihenfolge│   festlegen  │
│             │   overview   │ • Kriterien  │   ändern     │ • Änderungen │
│             │ • Filter     │   bewerten   │ • Begrün-    │   dokumentier│
│             │   setzen     │              │   dungen     │ • Benachrich-│
│             │              │              │   eingeben   │   tigungen   │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ GEDANKEN    │ "Wie ist der │ "Ist die     │ "Ist diese   │ "Alle sind   │
│             │  aktuelle    │  Bewertung   │  Anpassung   │  informiert? │
│             │  Stand?"     │  fair?"      │  gerechtfer- │  Dokument-   │
│             │ "Gibt es     │ "Stimmen die │  tigt?"      │  ation ist   │
│             │  Probleme?"  │  Kriterien?" │ "Wie        │  vollständig"│
│             │              │              │  begründe    │              │
│             │              │              │  ich das?"   │              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ EMOTIONEN   │ 🤔 Analytisch│ 😤 Frustrier.│ 😰 Verant-   │ 😌 Erleicht. │
│             │    Fokussiert│    bei       │    wortung   │    Vollständig│
│             │              │    Unfairness│ 🤯 Komplexität│              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ PAIN POINTS │ • Unüber-    │ • Fehlende   │ • Komplizierte│ • Manuelle   │
│             │   sichtliche │   Historie   │   Berechnung │   Nacharbeit │
│             │   Darstellung│ • Zu viele   │ • Keine      │ • Vergessene │
│             │ • Fehlende   │   Details    │   Transparenz│   Schritte   │
│             │   KPIs       │              │              │              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│OPPORTUNITIES│ • KPI        │ • Timeline   │ • Berechungs-│ • Automated  │
│             │   Dashboard  │   View       │   assistent  │   workflows  │
│             │ • Visual     │ • Comparison │ • What-if    │ • Benach-    │
│             │   indicators │   tools      │   scenarios  │   richtigungen│
│             │ • Quick      │              │ • Audit trail│ • Change log │
│             │   filters    │              │              │              │
└─────────────┴──────────────┴──────────────┴──────────────┴──────────────┘
```

## 3. Angebot erstellen (Service Delivery Journey)

### Benutzer: Verwaltungsmitarbeiter (Lisa Schmidt, 28, Sachbearbeiterin)
### Ziel: Parzellen-Angebot für wartenden Antragsteller erstellen

```
┌─────────────┬──────────────┬──────────────┬──────────────┬──────────────┐
│   PHASE     │ VORBEREITUNG │ ERSTELLUNG   │ ÜBERPRÜFUNG  │ VERSENDUNG   │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ TOUCHPOINTS │ • Warteliste │ • Angebots-  │ • Preview    │ • E-Mail     │
│             │ • Verfügbare │   formular   │ • Korrektur  │ • Post       │
│             │   Parzellen  │ • Template   │ • Freigabe   │ • Tracking   │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│  AKTIONEN   │ • Parzelle   │ • Angebots-  │ • Dokument   │ • Versand    │
│             │   auswählen  │   details    │   prüfen     │   auslösen   │
│             │ • Besten     │   eingeben   │ • Rechtschreib│ • Status auf │
│             │   Kandidaten │ • Preise     │   kontrolle  │   "Angebot   │
│             │   ermitteln  │   berechnen  │ • Vollständig│   versendet" │
│             │ • Präferenzen│ • Fristen    │   keit prüfen│ • Frist      │
│             │   abgleichen │   setzen     │              │   überwachen │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ GEDANKEN    │ "Welche      │ "Stimmen die │ "Ist alles   │ "Haben sie   │
│             │  Parzelle    │  Angaben?"   │  korrekt?"   │  das Angebot │
│             │  passt am    │ "Ist der     │ "Vergesse    │  erhalten?"  │
│             │  besten?"    │  Preis       │  ich etwas?" │ "Reagieren   │
│             │ "Wer ist     │  richtig?"   │              │  sie rechtz- │
│             │  als nächstes│              │              │  zeitig?"    │
│             │  dran?"      │              │              │              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ EMOTIONEN   │ 🤔 Sorgfältig│ 😓 Konzen-   │ 😰 Ängstlich │ 😊 Hoffnungs-│
│             │    Bedacht   │    triert    │    Fehler    │    voll      │
│             │              │ 😤 Gestresst │              │ 😬 Nervös    │
│             │              │    bei       │              │    wegen     │
│             │              │    Zeitdruck │              │    Reaktion  │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ PAIN POINTS │ • Manuelle   │ • Fehler-    │ • Keine      │ • Manuelles  │
│             │   Suche      │   anfällige  │   Korrektur- │   Nachfassen │
│             │ • Kein       │   Berechnung │   funktion   │ • Fehlende   │
│             │   Matching   │ • Template   │ • Kein       │   Automatisi-│
│             │   Algorithm  │   veraltet   │   Workflow   │   erung      │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│OPPORTUNITIES│ • AI-basiert │ • Smart      │ • Auto-      │ • Automated  │
│             │   Matching   │   templates  │   validation │   follow-up  │
│             │ • Präferenzen│ • Dynamic    │ • Collaborative│ • Status    │
│             │   Scoring    │   pricing    │   review     │   tracking   │
│             │ • Quick      │ • Pre-fill   │ • Version    │ • Reminder   │
│             │   actions    │   data       │   control    │   system     │
└─────────────┴──────────────┴──────────────┴──────────────┴──────────────┘
```

## 4. Antrag suchen & bearbeiten (Support Journey)

### Benutzer: Support-Mitarbeiter (Peter Jung, 55, erfahrener Sachbearbeiter)
### Ziel: Bestehenden Antrag finden und Daten aktualisieren

```
┌─────────────┬──────────────┬──────────────┬──────────────┬──────────────┐
│   PHASE     │   SUCHEN     │  ÖFFNEN      │ BEARBEITEN   │ SPEICHERN    │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ TOUCHPOINTS │ • Suchleiste │ • Antrag     │ • Formular   │ • Speicher   │
│             │ • Filter     │   Details    │ • Tabs       │   Dialog     │
│             │ • Resultate  │ • Historie   │ • Validierung│ • Bestätigung│
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│  AKTIONEN   │ • Suchbegriff│ • Antrag     │ • Daten      │ • Änderungen │
│             │   eingeben   │   auswählen  │   ändern     │   speichern  │
│             │ • Filter     │ • Überblick  │ • Zusätzliche│ • Änderungs- │
│             │   anwenden   │   verschaffen│   Info       │   log prüfen │
│             │ • Ergebnisse │ • Bearbeitungs│   hinzufügen │ • Workflows  │
│             │   durchsuchen│   modus      │ • Status     │   triggern   │
│             │              │   aktivieren │   ändern     │              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ GEDANKEN    │ "Wie hieß    │ "Ist das der │ "Was muss    │ "Sind alle   │
│             │  der Name    │  richtige    │  geändert    │  Änderungen  │
│             │  nochmal?"   │  Antrag?"    │  werden?"    │  korrekt?"   │
│             │ "Welche      │ "Was ist     │ "Welche      │ "Wer muss be-│
│             │  Kriterien   │  die aktuelle│  Auswirkungen│  nachrichtigt│
│             │  helfen?"    │  Situation?" │  hat das?"   │  werden?"    │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ EMOTIONEN   │ 😤 Frustrier.│ 😊 Erleich-  │ 🤔 Konzen-   │ 😌 Zufrieden │
│             │    bei       │    terung    │    triert    │    Vollendung│
│             │    No-Results│ 😐 Routine   │ 😓 Vorsichtig│              │
│             │ 🤔 Nachdenken│              │    bei       │              │
│             │              │              │    kritischen│              │
│             │              │              │    Änderungen│              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ PAIN POINTS │ • Langsame   │ • Überladene │ • Unklare    │ • Fehlende   │
│             │   Suche      │   Interface  │   Abhängig-  │   Validierung│
│             │ • Ungenaue   │ • Fehlende   │   keiten     │ • Lange      │
│             │   Resultate  │   Kontext-   │ • Verlust    │   Speicher-  │
│             │ • Schlechte  │   information│   ungespeich.│   zeiten     │
│             │   Filter     │              │   Änderungen │              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│OPPORTUNITIES│ • Intelligent│ • Kontext-   │ • Auto-save  │ • Batch      │
│             │   Search     │   basierte   │ • Change     │   validation │
│             │ • Saved      │   Informations│  tracking   │ • Progress   │
│             │   searches   │   darstellung│ • Undo/Redo  │   feedback   │
│             │ • Recent     │ • Quick      │ • Smart      │ • Auto       │
│             │   items      │   actions    │   suggestions│   notifications│
└─────────────┴──────────────┴──────────────┴──────────────┴──────────────┘
```

## 5. Bericht generieren (Management Journey)

### Benutzer: Abteilungsleiter (Dr. Andrea Hoffmann, 48, Bereichsleiterin)
### Ziel: Monatlichen Statusbericht für Leitung erstellen

```
┌─────────────┬──────────────┬──────────────┬──────────────┬──────────────┐
│   PHASE     │ ANFORDERUNG  │ KONFIGURATION│ GENERATION   │ DISTRIBUTION │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ TOUCHPOINTS │ • Berichte   │ • Parameter  │ • Report     │ • Export     │
│             │   Dashboard  │   setup      │   builder    │ • E-Mail     │
│             │ • Templates  │ • Data       │ • Preview    │ • Archive    │
│             │              │   sources    │              │              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│  AKTIONEN   │ • Report-Typ │ • Zeitraum   │ • Bericht    │ • Format     │
│             │   auswählen  │   definieren │   generieren │   auswählen  │
│             │ • Template   │ • KPIs       │ • Daten      │ • Empfänger  │
│             │   laden      │   auswählen  │   überprüfen │   festlegen  │
│             │ • Letzte     │ • Filter     │ • Format     │ • Versenden  │
│             │   Berichte   │   setzen     │   anpassen   │ • Archivieren│
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ GEDANKEN    │ "Welche      │ "Sind die    │ "Sehen die   │ "Haben alle  │
│             │  Kennzahlen  │  Parameter   │  Zahlen      │  den Bericht │
│             │  sind        │  richtig?"   │  plausibel   │  erhalten?"  │
│             │  wichtig?"   │ "Fehlen      │  aus?"       │ "Ist er      │
│             │ "Was         │  wichtige    │ "Entspricht  │  verständlich│
│             │  interessiert│  Details?"   │  das den     │  aufbereitet?"│
│             │  die Leitung?"│              │  Erwartungen?"│              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ EMOTIONEN   │ 🤔 Strategisch│ 😰 Unsicher- │ 😬 Nervös    │ 😊 Stolz     │
│             │    Planend   │    heit über │    über      │    über      │
│             │              │    Vollstän- │    Qualität  │    Qualität  │
│             │              │    digkeit   │ 😤 Frustrier.│ 😌 Erleichter│
│             │              │              │    bei Fehlern│              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ PAIN POINTS │ • Veraltete  │ • Komplexe   │ • Lange      │ • Manuelle   │
│             │   Templates  │   Parameter  │   Generierung│   Verteilung │
│             │ • Fehlende   │ • Keine      │ • Fehlerhafte│ • Keine      │
│             │   Flexibilität│   Vorschau  │   Daten      │   Bestätigung│
│             │ • Manuelle   │ • Unklare    │ • Format-    │ • Vergessene │
│             │   Arbeit     │   Abhängig-  │   probleme   │   Empfänger  │
│             │              │   keiten     │              │              │
├─────────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│OPPORTUNITIES│ • Smart      │ • Visual     │ • Real-time  │ • Automated  │
│             │   templates  │   builder    │   preview    │   distribution│
│             │ • Saved      │ • What-if    │ • Data       │ • Subscription│
│             │   configs    │   scenarios  │   validation │   management │
│             │ • Auto       │ • Dependency │ • Multiple   │ • Read        │
│             │   suggestions│   checking   │   formats    │   confirmations│
└─────────────┴──────────────┴──────────────┴──────────────┴──────────────┘
```

## 6. Journey-übergreifende Insights

### Gemeinsame Pain Points:
1. **Langsame Systemperformance** - Wartezeiten bei Ladevorgängen
2. **Unklare Fehlermeldungen** - Schwer verständliche Validierungshinweise
3. **Fehlende Automatisierung** - Viele manuelle, repetitive Schritte
4. **Inkonsistente Navigation** - Unterschiedliche Patterns in verschiedenen Bereichen
5. **Mangelnde Transparenz** - Unklare Prozessstatus und nächste Schritte

### Übergreifende Opportunities:
1. **Intelligente Automatisierung** - AI-unterstützte Workflows
2. **Einheitliche Design Language** - Konsistente UI/UX Patterns
3. **Proaktive Benachrichtigungen** - Status-Updates und Erinnerungen
4. **Kontext-sensitive Hilfe** - Integrierte Guidance und Tooltips
5. **Performance-Optimierung** - Schnelle Ladezeiten und Responsezeiten

### Empfohlene Design-Prinzipien:
1. **Clarity First** - Klare, verständliche Informationsdarstellung
2. **Efficiency Focus** - Minimierung von Arbeitsschritten
3. **Error Prevention** - Proaktive Validierung und Guidance
4. **Progress Transparency** - Sichtbare Fortschrittsindikatoren
5. **Flexible Workflows** - Anpassbare Prozesse für verschiedene Nutzertypen