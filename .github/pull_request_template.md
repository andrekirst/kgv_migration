# Pull Request

## 📋 Beschreibung
<!-- Kurze Beschreibung der Änderungen in diesem PR -->

## 🔗 Verknüpfte Issues
<!-- Verlinke die relevanten GitHub Issues -->
Closes #<!-- Issue Nummer -->

## 🚀 Art der Änderung
<!-- Markiere alle zutreffenden Optionen -->
- [ ] 🐛 Bug Fix (nicht-breaking change, der ein Problem behebt)
- [ ] ✨ Neues Feature (nicht-breaking change, der Funktionalität hinzufügt)
- [ ] 💥 Breaking Change (Fix oder Feature, der bestehende Funktionalität beeinflusst)
- [ ] 📚 Dokumentation (reine Dokumentationsänderungen)
- [ ] 🔧 Refactoring (Code-Änderungen ohne Funktionsänderung)
- [ ] ⚡ Performance Verbesserung
- [ ] 🔒 Security Fix
- [ ] 🏗️ Infrastructure/DevOps Änderungen
- [ ] 🗄️ Database Migration/Schema Änderungen

## 🏗️ Betroffene Komponenten
<!-- Markiere alle betroffenen Bereiche -->
- [ ] 🎨 Frontend (Next.js/React)
- [ ] 🔧 Backend (.NET 9 Web API)
- [ ] 🗄️ Datenbank (PostgreSQL)
- [ ] 🐳 Container/Docker
- [ ] ☸️ Kubernetes Konfiguration
- [ ] 🔄 CI/CD Pipeline
- [ ] 📖 Dokumentation
- [ ] 🧪 Tests
- [ ] 🔐 Security/Authentication

## 🔧 Technische Details

### Implementierte Änderungen
<!-- Beschreibe die wichtigsten technischen Änderungen -->
- 
- 
- 

### Architecture Pattern Usage
<!-- Falls relevante Patterns verwendet wurden -->
- [ ] Anti-Corruption Layer
- [ ] Circuit Breaker Pattern
- [ ] Strangler Fig Pattern
- [ ] CQRS
- [ ] Cache-Aside Pattern
- [ ] Repository Pattern
- [ ] Clean Architecture Layers

## 🧪 Testing

### Test Coverage
- [ ] Unit Tests hinzugefügt/aktualisiert
- [ ] Integration Tests hinzugefügt/aktualisiert
- [ ] E2E Tests hinzugefügt/aktualisiert
- [ ] Manual Testing durchgeführt

### Test Results
<!-- Füge Testergebnisse hinzu, wenn vorhanden -->
```
# Füge Test Output hier ein
```

### Test Instructions
<!-- Wie können die Änderungen getestet werden? -->
1. 
2. 
3. 

## 🐳 Container/Deployment

### Docker Changes
- [ ] Dockerfile aktualisiert
- [ ] docker-compose.yml geändert
- [ ] Container Image Size optimiert
- [ ] Health Checks aktualisiert

### Database Changes
- [ ] Migration Script hinzugefügt
- [ ] Schema Änderungen dokumentiert
- [ ] Backward Kompatibilität gewährleistet
- [ ] Data Migration getestet

### Deployment Notes
<!-- Besondere Hinweise für Deployment -->
- 
- 

## 🔍 Code Review Checklist

### Code Quality
- [ ] Code folgt den Projekt-Conventions
- [ ] Keine TODO/FIXME Kommentare ohne Issues
- [ ] Error Handling ist implementiert
- [ ] Logging ist angemessen
- [ ] Performance wurde berücksichtigt

### Security
- [ ] Keine Hardcoded Secrets/Passwords
- [ ] Input Validation implementiert
- [ ] SQL Injection Schutz (falls DB-Zugriff)
- [ ] XSS Schutz (falls Frontend-Änderungen)
- [ ] HTTPS/TLS korrekt konfiguriert

### Documentation
- [ ] Code ist selbst-dokumentierend oder kommentiert
- [ ] README.md aktualisiert (falls nötig)
- [ ] API Dokumentation aktualisiert (falls nötig)
- [ ] CLAUDE.md aktualisiert (falls Workflow-Änderungen)

## 📊 Performance Impact

### Before/After Metrics
<!-- Falls Performance-relevante Änderungen -->
| Metrik | Vorher | Nachher | Verbesserung |
|--------|--------|---------|--------------|
| Response Time | | | |
| Memory Usage | | | |
| Bundle Size | | | |
| Database Query Time | | | |

### Load Testing
- [ ] Load Tests durchgeführt
- [ ] Performance Regression Tests bestanden
- [ ] Memory Leaks überprüft

## 🌐 Browser/Environment Testing

### Browser Compatibility
- [ ] Chrome
- [ ] Firefox  
- [ ] Safari
- [ ] Edge

### Environment Testing
- [ ] Development (Docker Compose)
- [ ] Staging (Kubernetes)
- [ ] Production Ready

## 📱 Accessibility & UX

### Accessibility (WCAG 2.1 AA)
- [ ] Keyboard Navigation getestet
- [ ] Screen Reader kompatibel
- [ ] Color Contrast ausreichend
- [ ] Alt-Text für Bilder

### User Experience
- [ ] Mobile Responsive
- [ ] Loading States implementiert
- [ ] Error Messages benutzerfreundlich
- [ ] German Localization korrekt

## 🔄 Migration Notes
<!-- Nur für Migrations-relevante PRs -->

### Legacy System Impact
- [ ] Backward Kompatibilität gewährleistet
- [ ] Strangler Fig Pattern korrekt implementiert
- [ ] Dual-Write Strategy berücksichtigt
- [ ] Shadow Mode Testing durchgeführt

### Data Migration
- [ ] Daten-Integrität überprüft
- [ ] Rollback-Strategie dokumentiert
- [ ] Migration Performance getestet

## 📸 Screenshots/Recordings
<!-- Füge Screenshots für UI-Änderungen hinzu -->

### Before
<!-- Screenshot/Description des aktuellen Zustands -->

### After  
<!-- Screenshot/Description der Änderungen -->

## 🚨 Breaking Changes
<!-- Beschreibe Breaking Changes im Detail -->

### API Changes
- 
- 

### Database Schema Changes
- 
- 

### Configuration Changes
- 
- 

## 📝 Additional Notes
<!-- Weitere wichtige Informationen -->

### Deployment Instructions
<!-- Spezielle Deployment-Schritte -->
1. 
2. 
3. 

### Follow-up Tasks
<!-- Issues die nach diesem PR erstellt werden sollten -->
- [ ] Create issue for: 
- [ ] Create issue for: 

### Dependencies
<!-- Andere PRs oder externe Abhängigkeiten -->
- Depends on PR #
- Requires external service: 

---

## ✅ Reviewer Checklist
<!-- Für Reviewer -->
- [ ] Code Review abgeschlossen
- [ ] Funktionalität getestet
- [ ] Documentation überprüft
- [ ] Security Aspekte bewertet
- [ ] Performance Impact bewertet
- [ ] Breaking Changes dokumentiert

---

**PR Type**: <!-- Automatisch durch Git Flow gefüllt -->
**Issue**: <!-- Automatisch verlinkt -->
**Branch**: <!-- Automatisch erkannt -->
**Reviewers**: <!-- Automatisch zugewiesen -->