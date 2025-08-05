# KGV Frontend - Kleingartenverein Verwaltungssystem

Ein modernes Next.js 14 Frontend für die Verwaltung von Kleingartenanträgen mit vollständiger Unterstützung für deutsche Behördenstandards und Container-native Deployment.

## 🎯 Projektübersicht

Dieses Frontend ist Teil des KGV-Verwaltungssystems und bietet:

- **Moderne React-Architektur** mit Next.js 14 App Router
- **Vollständige Lokalisierung** für deutsche Behörden
- **Container-optimiert** für Kubernetes und Docker
- **Accessibility-konform** nach WCAG 2.1 AA
- **Performance-optimiert** mit Core Web Vitals < 2.5s

## 🚀 Schnellstart

### Voraussetzungen

- Node.js 18.17.0 oder höher
- npm 9.0.0 oder höher
- Docker & Docker Compose (für Container-Deployment)

### Lokale Entwicklung

```bash
# Repository klonen
git clone <repository-url>
cd kgv_migration/frontend

# Dependencies installieren
npm install

# Umgebungsvariablen konfigurieren
cp .env.example .env.local

# Entwicklungsserver starten
npm run dev
```

Die Anwendung ist verfügbar unter: http://localhost:3000

### Docker Development

```bash
# Development Container starten
docker-compose --profile development up frontend-dev

# Oder mit vollständigem Stack
docker-compose --profile development up
```

### Production Build

```bash
# Production Build erstellen
npm run build

# Production Server starten
npm start

# Oder mit Docker
docker-compose up frontend
```

## 🏗️ Architektur

### Technologie-Stack

- **Framework**: Next.js 14 (App Router)
- **Sprache**: TypeScript (Strict Mode)
- **Styling**: Tailwind CSS + CVA
- **State Management**: Zustand + TanStack Query
- **Formulare**: React Hook Form + Zod
- **UI-Komponenten**: Radix UI + Custom Components
- **Icons**: Lucide React
- **Datum/Zeit**: date-fns (deutsche Lokalisierung)

### Projektstruktur

```
frontend/
├── src/
│   ├── app/                    # Next.js App Router
│   │   ├── (dashboard)/        # Dashboard Layout
│   │   ├── api/               # API Routes (Health Checks)
│   │   ├── globals.css        # Globale Styles
│   │   ├── layout.tsx         # Root Layout
│   │   └── providers.tsx      # App Providers
│   ├── components/
│   │   ├── ui/                # Basis UI Komponenten
│   │   ├── dashboard/         # Dashboard Komponenten
│   │   ├── antraege/          # Anträge-spezifische Komponenten
│   │   └── layout/            # Layout Komponenten
│   ├── hooks/                 # Custom React Hooks
│   ├── lib/                   # Utility Funktionen
│   ├── store/                 # Zustand Stores
│   ├── types/                 # TypeScript Definitionen
│   └── utils/                 # Helper Funktionen
├── public/                    # Statische Assets
├── scripts/                   # Build & Deployment Scripts
├── Dockerfile                 # Multi-stage Docker Build
├── docker-compose.yml         # Container Orchestration
└── package.json              # Projekt Konfiguration
```

## 🎨 Design System

### Deutsche UI-Standards

Das Design System folgt deutschen Behördenstandards:

- **Farbpalette**: Behördentaugliche Farben mit hohem Kontrast
- **Typografie**: Optimiert für deutsche Texte und Umlaute
- **Formulare**: Deutsche Validierungsmeldungen und Labels
- **Accessibility**: WCAG 2.1 AA konform

### Komponenten-Bibliothek

```tsx
// Beispiel Button Nutzung
<Button variant="primary" size="lg" loading={isLoading}>
  Antrag speichern
</Button>

// Beispiel Form Input
<Input
  label="Nachname"
  required
  error={errors.nachname}
  helperText="Bitte geben Sie Ihren vollständigen Nachnamen ein"
/>

// Status Badge
<Badge variant="genehmigt">Genehmigt</Badge>
```

## 📱 Responsive Design

- **Mobile-First**: Responsive Design ab 320px
- **Breakpoints**: xs(475px), sm(640px), md(768px), lg(1024px), xl(1280px)
- **Touch-Optimiert**: Mindestens 44px Touch-Targets
- **Performance**: Optimierte Bilder und Lazy Loading

## ♿ Accessibility

### WCAG 2.1 AA Compliance

- **Keyboard Navigation**: Vollständige Tastatursteuerung
- **Screen Reader**: Semantic HTML und ARIA Labels
- **Kontrast**: Mindestens 4.5:1 Kontrastverhältnis
- **Focus Management**: Sichtbare Focus-Indikatoren

### Deutsche Accessibility-Features

- **Skip Links**: "Zum Hauptinhalt springen"
- **Sprache**: Korrekte `lang="de"` Attribute
- **Formulare**: Deutsche Labels und Fehlermeldungen
- **Datum/Zeit**: Deutsche Formatierung

## 🔧 API Integration

### TanStack Query

```tsx
// Anträge laden
const { data: antraege, isLoading, error } = useQuery({
  queryKey: ['antraege', filters],
  queryFn: () => apiClient.get('/antraege', { params: filters }),
  staleTime: 5 * 60 * 1000, // 5 Minuten
})

// Antrag erstellen
const createMutation = useMutation({
  mutationFn: (data) => apiClient.post('/antraege', data),
  onSuccess: () => {
    queryClient.invalidateQueries(['antraege'])
    toast.success('Antrag erfolgreich erstellt')
  },
})
```

### Error Handling

- **Global Error Boundary**: Zentrale Fehlerbehandlung
- **Toast Notifications**: Deutsche Fehlermeldungen
- **Retry Logic**: Automatische Wiederholung bei Netzwerkfehlern
- **Offline Support**: Service Worker für kritische Funktionen

## 🐳 Container Deployment

### Multi-Stage Dockerfile

```dockerfile
# Optimiert für Produktionsumgebung
FROM node:18-alpine AS deps
FROM node:18-alpine AS builder  
FROM node:18-alpine AS runner

# Finale Image-Größe: ~85MB
# Sicherheit: Non-root User
# Health Checks: Integriert
```

### Docker Compose Services

- **frontend**: Production Frontend
- **frontend-dev**: Development mit Hot Reload
- **api**: Backend API Service
- **database**: PostgreSQL Datenbank
- **redis**: Caching (optional)
- **traefik**: Reverse Proxy (optional)

### Kubernetes Deployment

```bash
# Helm Chart verfügbar
helm install kgv-frontend ./charts/frontend

# Oder kubectl
kubectl apply -f k8s/
```

## 🔐 Sicherheit

### Security Headers

- **CSP**: Content Security Policy
- **HSTS**: HTTP Strict Transport Security
- **X-Frame-Options**: Clickjacking-Schutz
- **X-Content-Type-Options**: MIME-Sniffing-Schutz

### JWT Authentication

```tsx
// Login
const { mutate: login } = useMutation({
  mutationFn: ({ email, password }) => 
    apiClient.post('/auth/login', { email, password }),
  onSuccess: (data) => {
    apiClient.setAuthToken(data.token)
    router.push('/dashboard')
  },
})
```

## 📊 Monitoring & Performance

### Core Web Vitals

- **LCP**: < 2.5s (Largest Contentful Paint)
- **FID**: < 100ms (First Input Delay)
- **CLS**: < 0.1 (Cumulative Layout Shift)

### Performance Features

- **Code Splitting**: Automatische Route-basierte Aufteilung
- **Image Optimization**: Next.js Image Component
- **Bundle Analysis**: Webpack Bundle Analyzer
- **Caching**: SWR mit optimalen Cache-Strategien

### Monitoring Stack

```yaml
# docker-compose.yml
services:
  prometheus:
    image: prom/prometheus:latest
  grafana:
    image: grafana/grafana:latest
```

## 🧪 Testing

### Test-Setup

```bash
# Unit Tests
npm run test

# E2E Tests
npm run e2e

# Coverage Report
npm run test:coverage

# Accessibility Tests
npm run test:a11y
```

### Testing Stack

- **Unit Tests**: Jest + Testing Library
- **E2E Tests**: Playwright
- **Accessibility**: axe-core
- **Visual Regression**: Chromatic (optional)

## 🚀 Deployment

### Umgebungen

- **Development**: `npm run dev`
- **Staging**: Docker Compose + Traefik
- **Production**: Kubernetes + Helm

### CI/CD Pipeline

```yaml
# GitHub Actions
name: Deploy Frontend
on:
  push:
    branches: [main]
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Build & Test
      - name: Docker Build
      - name: Deploy to K8s
```

### Environment Variables

```bash
# Wichtige Umgebungsvariablen
NEXT_PUBLIC_API_URL=https://api.kgv.de
JWT_SECRET_KEY=your-secret-key
DB_CONNECTION_STRING=postgresql://...
```

## 📚 Dokumentation

### Storybook

```bash
# Storybook starten
npm run storybook

# Storybook Build
npm run build-storybook
```

### API Dokumentation

- **OpenAPI**: Swagger-kompatible API-Docs
- **Postman**: Collection für API-Testing
- **TypeScript**: Vollständige Type-Definitionen

## 🤝 Entwicklung

### Code Standards

- **ESLint**: Strenge TypeScript-Regeln
- **Prettier**: Automatische Code-Formatierung
- **Husky**: Pre-commit Hooks
- **Conventional Commits**: Commit-Message-Standards

### Git Workflow

```bash
# Feature Branch
git checkout -b feature/neue-funktion

# Commits
git commit -m "feat: neue Antrag-Validierung hinzugefügt"

# Pull Request
git push origin feature/neue-funktion
```

## 📈 Roadmap

### Version 1.1 (Q2 2024)

- [ ] PWA Support
- [ ] Offline-Funktionalität
- [ ] Push-Benachrichtigungen
- [ ] Erweiterte Suchfilter

### Version 1.2 (Q3 2024)

- [ ] Mobile App (React Native)
- [ ] Advanced Analytics
- [ ] Workflow-Automatisierung
- [ ] Integration mit externen Systemen

## 🐛 Troubleshooting

### Häufige Probleme

**Build-Fehler:**
```bash
# Cache löschen
rm -rf .next node_modules
npm install && npm run build
```

**Docker-Probleme:**
```bash
# Container neu starten
docker-compose down && docker-compose up --build
```

**API-Verbindungsfehler:**
```bash
# Umgebungsvariablen prüfen
echo $NEXT_PUBLIC_API_URL
```

## 📞 Support

- **GitHub Issues**: Bug Reports und Feature Requests
- **Documentation**: [docs.kgv.de](https://docs.kgv.de)
- **Email**: support@kgv.de

## 📄 Lizenz

Dieses Projekt steht unter der [MIT Lizenz](LICENSE).

---

**Entwickelt mit ❤️ für deutsche Kleingartenvereine**