# KGV Frank - Responsive Design System

## 1. Breakpoint-Definition

### Tailwind CSS Breakpoints
```javascript
// tailwind.config.js
module.exports = {
  theme: {
    screens: {
      'xs': '475px',    // Extra Small - Kleine Handys
      'sm': '640px',    // Small - Standard Handys
      'md': '768px',    // Medium - Tablets
      'lg': '1024px',   // Large - Small Desktops
      'xl': '1280px',   // Extra Large - Standard Desktops
      '2xl': '1536px',  // 2X Large - Große Monitore
      
      // Custom Breakpoints für spezielle Anwendungsfälle
      'mobile': '640px',
      'tablet': '768px',
      'desktop': '1024px',
      'wide': '1440px'
    }
  }
}
```

### Responsive Strategy
- **Mobile First Approach** - Design beginnt bei kleinster Auflösung
- **Progressive Enhancement** - Features werden mit größeren Screens hinzugefügt
- **Content-First** - Inhalt bestimmt Breakpoints, nicht Geräte
- **Flexible Grid System** - 12-Column Grid mit flexiblen Proportionen

## 2. Layout-System

### Container Sizes
```css
/* Container mit maximalen Breiten */
.container {
  width: 100%;
  margin-left: auto;
  margin-right: auto;
  padding-left: 1rem;
  padding-right: 1rem;
}

@media (min-width: 640px) {
  .container { max-width: 640px; }
}
@media (min-width: 768px) {
  .container { max-width: 768px; }
}
@media (min-width: 1024px) {
  .container { max-width: 1024px; }
}
@media (min-width: 1280px) {
  .container { max-width: 1280px; }
}
@media (min-width: 1536px) {
  .container { max-width: 1536px; }
}

/* Fluid Container */
.container-fluid {
  width: 100%;
  padding-left: 1rem;
  padding-right: 1rem;
}

/* App-spezifische Container */
.container-app {
  max-width: 1400px; /* Optimale Breite für Verwaltungsanwendung */
}
```

### Grid System
```css
/* 12-Column Grid */
.grid-12 {
  display: grid;
  grid-template-columns: repeat(12, minmax(0, 1fr));
  gap: 1.5rem;
}

/* Responsive Grid Patterns */
.grid-responsive {
  display: grid;
  gap: 1.5rem;
  grid-template-columns: 1fr; /* Mobile: 1 Column */
}

@media (min-width: 768px) {
  .grid-responsive {
    grid-template-columns: repeat(2, 1fr); /* Tablet: 2 Columns */
  }
}

@media (min-width: 1024px) {
  .grid-responsive {
    grid-template-columns: repeat(3, 1fr); /* Desktop: 3 Columns */
  }
}

/* Form Grid - Spezifisch für Formulare */
.form-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: 1fr;
}

@media (min-width: 768px) {
  .form-grid {
    grid-template-columns: repeat(2, 1fr);
    gap: 1.5rem;
  }
}

@media (min-width: 1024px) {
  .form-grid {
    grid-template-columns: repeat(3, 1fr);
    gap: 2rem;
  }
}
```

## 3. Component Responsive Behavior

### Navigation
```tsx
// Desktop Navigation - Sidebar
<nav className="hidden lg:flex lg:flex-col lg:fixed lg:inset-y-0 lg:z-50 lg:w-64">
  <SidebarContent />
</nav>

// Mobile Navigation - Overlay
<nav className="lg:hidden">
  <MobileNavOverlay isOpen={mobileMenuOpen} />
</nav>

// Header Navigation
<header className="sticky top-0 z-40 flex h-16 shrink-0 items-center gap-x-4 border-b border-gray-200 bg-white px-4 shadow-sm sm:gap-x-6 sm:px-6 lg:px-8">
  {/* Mobile menu button */}
  <button className="lg:hidden" onClick={() => setMobileMenuOpen(true)}>
    <Bars3Icon className="h-6 w-6" />
  </button>
  
  {/* Desktop content */}
  <div className="flex flex-1 gap-x-4 self-stretch lg:gap-x-6">
    <SearchBar className="flex-1" />
    <UserMenu className="hidden sm:block" />
  </div>
</header>
```

### Layout Patterns
```tsx
// Main Layout mit responsiver Sidebar
<div className="min-h-full">
  {/* Sidebar für Desktop */}
  <Sidebar className="hidden lg:fixed lg:inset-y-0 lg:z-50 lg:flex lg:w-64 lg:flex-col" />
  
  {/* Mobile Sidebar */}
  <MobileSidebar open={sidebarOpen} onClose={setSidebarOpen} />
  
  {/* Main Content */}
  <div className="lg:pl-64">
    <Header />
    <main className="py-10">
      <div className="px-4 sm:px-6 lg:px-8">
        {children}
      </div>
    </main>
  </div>
</div>
```

### Data Tables
```tsx
// Responsive Table mit verschiedenen Ansichten
function ResponsiveTable({ data, columns }) {
  return (
    <>
      {/* Desktop: Standard Table */}
      <div className="hidden md:block">
        <table className="min-w-full divide-y divide-gray-300">
          <thead>
            <tr>
              {columns.map(column => (
                <th key={column.key} className="px-6 py-3 text-left">
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.map(row => (
              <tr key={row.id}>
                {columns.map(column => (
                  <td key={column.key} className="px-6 py-4">
                    {row[column.key]}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      
      {/* Mobile: Card Layout */}
      <div className="md:hidden space-y-4">
        {data.map(row => (
          <div key={row.id} className="bg-white p-4 rounded-lg shadow">
            <div className="space-y-2">
              {columns.map(column => (
                <div key={column.key} className="flex justify-between">
                  <span className="font-medium text-gray-500">
                    {column.header}:
                  </span>
                  <span>{row[column.key]}</span>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </>
  )
}
```

### Forms
```tsx
// Responsive Form Layout
function ResponsiveForm() {
  return (
    <form className="space-y-6">
      {/* Single column on mobile, multi-column on larger screens */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <FormField label="Vorname">
          <Input className="w-full" />
        </FormField>
        <FormField label="Nachname">
          <Input className="w-full" />
        </FormField>
        <FormField label="E-Mail" className="sm:col-span-2 lg:col-span-1">
          <Input type="email" className="w-full" />
        </FormField>
      </div>
      
      {/* Full width fields */}
      <div className="grid grid-cols-1 gap-4">
        <FormField label="Straße">
          <Input className="w-full" />
        </FormField>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <FormField label="PLZ">
            <Input className="w-full" />
          </FormField>
          <FormField label="Ort" className="sm:col-span-2">
            <Input className="w-full" />
          </FormField>
        </div>
      </div>
      
      {/* Form Actions */}
      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
        <Button variant="outline" className="w-full sm:w-auto">
          Abbrechen
        </Button>
        <Button className="w-full sm:w-auto">
          Speichern
        </Button>
      </div>
    </form>
  )
}
```

## 4. Typography Scaling

### Responsive Font Sizes
```css
/* Basis-Typografie mit Responsive Scaling */
.text-responsive-xs {
  font-size: 0.75rem;  /* 12px */
  line-height: 1rem;
}
@media (min-width: 768px) {
  .text-responsive-xs { font-size: 0.875rem; } /* 14px */
}

.text-responsive-sm {
  font-size: 0.875rem; /* 14px */
  line-height: 1.25rem;
}
@media (min-width: 768px) {
  .text-responsive-sm { font-size: 1rem; } /* 16px */
}

.text-responsive-base {
  font-size: 1rem;     /* 16px */
  line-height: 1.5rem;
}
@media (min-width: 768px) {
  .text-responsive-base { font-size: 1.125rem; } /* 18px */
}

.text-responsive-lg {
  font-size: 1.125rem; /* 18px */
  line-height: 1.75rem;
}
@media (min-width: 768px) {
  .text-responsive-lg { font-size: 1.25rem; } /* 20px */
}

.text-responsive-xl {
  font-size: 1.25rem;  /* 20px */
  line-height: 1.75rem;
}
@media (min-width: 768px) {
  .text-responsive-xl { font-size: 1.5rem; } /* 24px */
}

/* Headings mit Responsive Scaling */
.heading-1 {
  font-size: 1.875rem; /* 30px */
  line-height: 2.25rem;
  font-weight: 800;
}
@media (min-width: 768px) {
  .heading-1 { font-size: 2.25rem; } /* 36px */
}
@media (min-width: 1024px) {
  .heading-1 { font-size: 3rem; } /* 48px */
}

.heading-2 {
  font-size: 1.5rem;   /* 24px */
  line-height: 2rem;
  font-weight: 700;
}
@media (min-width: 768px) {
  .heading-2 { font-size: 1.875rem; } /* 30px */
}
@media (min-width: 1024px) {
  .heading-2 { font-size: 2.25rem; } /* 36px */
}
```

## 5. Spacing System

### Responsive Spacing
```css
/* Padding und Margin mit Responsive Scaling */
.space-responsive-sm {
  margin: 0.5rem;  /* 8px */
}
@media (min-width: 768px) {
  .space-responsive-sm { margin: 0.75rem; } /* 12px */
}
@media (min-width: 1024px) {
  .space-responsive-sm { margin: 1rem; } /* 16px */
}

.space-responsive-md {
  margin: 1rem;    /* 16px */
}
@media (min-width: 768px) {
  .space-responsive-md { margin: 1.5rem; } /* 24px */
}
@media (min-width: 1024px) {
  .space-responsive-md { margin: 2rem; } /* 32px */
}

.space-responsive-lg {
  margin: 1.5rem;  /* 24px */
}
@media (min-width: 768px) {
  .space-responsive-lg { margin: 2rem; } /* 32px */
}
@media (min-width: 1024px) {
  .space-responsive-lg { margin: 3rem; } /* 48px */
}

/* Container Padding */
.container-padding {
  padding-left: 1rem;
  padding-right: 1rem;
}
@media (min-width: 640px) {
  .container-padding {
    padding-left: 1.5rem;
    padding-right: 1.5rem;
  }
}
@media (min-width: 1024px) {
  .container-padding {
    padding-left: 2rem;
    padding-right: 2rem;
  }
}
```

## 6. Interactive Elements

### Touch-Optimierte Buttons
```css
/* Basis Button Sizes */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.375rem;
  font-weight: 500;
  transition: all 0.2s;
}

/* Touch-optimierte Größen */
.btn-sm {
  padding: 0.5rem 1rem;
  font-size: 0.875rem;
  min-height: 2.25rem; /* 36px - Minimum für Touch */
}

.btn-md {
  padding: 0.75rem 1.5rem;
  font-size: 1rem;
  min-height: 2.5rem; /* 40px */
}

.btn-lg {
  padding: 1rem 2rem;
  font-size: 1.125rem;
  min-height: 3rem; /* 48px - Optimal für Touch */
}

/* Mobile: Größere Buttons für bessere Touchability */
@media (max-width: 767px) {
  .btn-sm { min-height: 2.5rem; }
  .btn-md { min-height: 3rem; }
  .btn-lg { min-height: 3.5rem; }
}
```

### Form Controls
```css
/* Touch-optimierte Form Controls */
.form-input {
  width: 100%;
  padding: 0.75rem;
  font-size: 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  min-height: 2.75rem; /* 44px - iOS Guideline */
}

/* Mobile: Größere Inputs */
@media (max-width: 767px) {
  .form-input {
    padding: 1rem;
    font-size: 1.125rem; /* Verhindert Zoom auf iOS */
    min-height: 3rem;
  }
}

/* Select Dropdown */
.form-select {
  background-image: url("data:image/svg+xml,...");
  background-position: right 0.75rem center;
  background-repeat: no-repeat;
  background-size: 1rem;
  padding-right: 2.5rem;
}
```

## 7. Performance Optimierungen

### Lazy Loading & Code Splitting
```tsx
// Responsive Image Loading
function ResponsiveImage({ src, alt, sizes }) {
  return (
    <img
      src={src}
      alt={alt}
      sizes={sizes || "(max-width: 768px) 100vw, (max-width: 1024px) 50vw, 33vw"}
      loading="lazy"
      className="w-full h-auto"
    />
  )
}

// Component Level Code Splitting
const DesktopChart = lazy(() => import('./DesktopChart'))
const MobileChart = lazy(() => import('./MobileChart'))

function ResponsiveChart() {
  const [isMobile, setIsMobile] = useState(false)
  
  useEffect(() => {
    const checkMobile = () => setIsMobile(window.innerWidth < 768)
    checkMobile()
    window.addEventListener('resize', checkMobile)
    return () => window.removeEventListener('resize', checkMobile)
  }, [])
  
  return (
    <Suspense fallback={<ChartSkeleton />}>
      {isMobile ? <MobileChart /> : <DesktopChart />}
    </Suspense>
  )
}
```

### CSS-in-JS Responsive Utilities
```tsx
// Custom Hook für Responsive Behavior
function useResponsive() {
  const [breakpoint, setBreakpoint] = useState('mobile')
  
  useEffect(() => {
    const updateBreakpoint = () => {
      const width = window.innerWidth
      if (width >= 1024) setBreakpoint('desktop')
      else if (width >= 768) setBreakpoint('tablet')
      else setBreakpoint('mobile')
    }
    
    updateBreakpoint()
    window.addEventListener('resize', updateBreakpoint)
    return () => window.removeEventListener('resize', updateBreakpoint)
  }, [])
  
  return {
    isMobile: breakpoint === 'mobile',
    isTablet: breakpoint === 'tablet',
    isDesktop: breakpoint === 'desktop',
    breakpoint
  }
}

// Usage in Components
function AdaptiveLayout() {
  const { isMobile, isDesktop } = useResponsive()
  
  return (
    <div className={cn(
      "flex",
      isMobile ? "flex-col" : "flex-row",
      isDesktop ? "gap-8" : "gap-4"
    )}>
      <Sidebar className={cn(
        isMobile && "order-2"
      )} />
      <MainContent />
    </div>
  )
}
```

## 8. Testing Responsive Design

### Viewport Testing Matrix
```
Mobile Devices:
- iPhone SE (375x667)
- iPhone 12 (390x844)
- Samsung Galaxy S21 (360x800)

Tablets:
- iPad (768x1024)
- iPad Pro (1024x1366)
- Surface Pro (912x1368)

Desktop:
- Laptop (1366x768)
- Desktop (1920x1080)
- Ultrawide (2560x1440)
```

### Responsive Testing Checklist
```markdown
□ Navigation funktioniert auf allen Breakpoints
□ Formulare sind touch-optimiert
□ Tabellen sind mobile-friendly (Cards/Accordion)
□ Bilder skalieren korrekt
□ Text ist lesbar ohne Zoom
□ Buttons haben Mindestgröße von 44px
□ Horizontales Scrollen vermieden
□ Performance ist auf mobilen Geräten akzeptabel
□ Touch-Gesten funktionieren korrekt
□ Landscape/Portrait Modi funktionieren
```

## 9. Accessibility in Responsive Design

### Screen Reader Support
```tsx
// Skip Links für verschiedene Viewports
<nav className="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 z-50">
  <a href="#main-content" className="bg-blue-600 text-white px-4 py-2 rounded">
    Zum Hauptinhalt springen
  </a>
  <a href="#navigation" className="bg-blue-600 text-white px-4 py-2 rounded ml-2">
    Zur Navigation springen
  </a>
</nav>

// Responsive ARIA Labels
<button 
  aria-label={isMobile ? "Menü öffnen" : "Navigation öffnen"}
  aria-expanded={menuOpen}
  className="lg:hidden"
>
  <Bars3Icon className="h-6 w-6" />
</button>
```

### Focus Management
```css
/* Sichtbare Focus Indicators für alle Viewports */
.focus-visible {
  outline: 2px solid #3b82f6;
  outline-offset: 2px;
}

/* Touch-optimierte Focus States */
@media (max-width: 767px) {
  .focus-visible {
    outline-width: 3px;
    outline-offset: 3px;
  }
}
```