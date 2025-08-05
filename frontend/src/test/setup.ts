import '@testing-library/jest-dom'
import { configure } from '@testing-library/react'
import { vi } from 'vitest'

// Configure Testing Library für deutsche Lokalisierung
configure({
  testIdAttribute: 'data-testid',
  asyncUtilTimeout: 5000,
  // German error messages
  getElementError: (message, container) => {
    return new Error(
      `${message}\n\nHinweis: Element nicht gefunden. Überprüfen Sie die Selektoren und DOM-Struktur.\n\n${container.innerHTML}`
    )
  },
})

// Mock Next.js Router
vi.mock('next/navigation', () => ({
  useRouter: () => ({
    push: vi.fn(),
    replace: vi.fn(),
    back: vi.fn(),
    forward: vi.fn(),
    refresh: vi.fn(),
    prefetch: vi.fn(),
  }),
  usePathname: () => '/test-path',
  useSearchParams: () => new URLSearchParams(),
  useParams: () => ({}),
  redirect: vi.fn(),
}))

// Mock Next.js Image component
vi.mock('next/image', () => ({
  __esModule: true,
  default: ({ src, alt, ...props }: any) => {
    // eslint-disable-next-line @next/next/no-img-element
    return <img src={src} alt={alt} {...props} />
  },
}))

// Mock Next.js Link component
vi.mock('next/link', () => ({
  __esModule: true,
  default: ({ children, href, ...props }: any) => {
    return <a href={href} {...props}>{children}</a>
  },
}))

// Mock next-themes
vi.mock('next-themes', () => ({
  useTheme: () => ({
    theme: 'light',
    setTheme: vi.fn(),
    resolvedTheme: 'light',
    themes: ['light', 'dark'],
    systemTheme: 'light',
  }),
  ThemeProvider: ({ children }: { children: React.ReactNode }) => children,
}))

// Mock Intersection Observer API
global.IntersectionObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
  root: null,
  rootMargin: '',
  thresholds: [],
}))

// Mock ResizeObserver API
global.ResizeObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
}))

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(), // deprecated
    removeListener: vi.fn(), // deprecated
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

// Mock window.scrollTo
Object.defineProperty(window, 'scrollTo', {
  value: vi.fn(),
  writable: true,
})

// Mock HTMLElement.scrollIntoView
Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', {
  value: vi.fn(),
  writable: true,
})

// Mock document.createRange for selection tests
Object.defineProperty(document, 'createRange', {
  value: () => ({
    selectNodeContents: vi.fn(),
    setStart: vi.fn(),
    setEnd: vi.fn(),
    cloneContents: vi.fn(() => document.createDocumentFragment()),
    collapse: vi.fn(),
    commonAncestorContainer: document,
    deleteContents: vi.fn(),
    endContainer: document,
    endOffset: 0,
    extractContents: vi.fn(() => document.createDocumentFragment()),
    insertNode: vi.fn(),
    startContainer: document,
    startOffset: 0,
  }),
})

// Mock window.getSelection
Object.defineProperty(window, 'getSelection', {
  value: () => ({
    addRange: vi.fn(),
    removeAllRanges: vi.fn(),
    empty: vi.fn(),
    rangeCount: 0,
    getRangeAt: vi.fn(),
  }),
})

// Mock fetch for API calls
global.fetch = vi.fn()

// Mock console methods to reduce test noise
global.console = {
  ...console,
  warn: vi.fn(),
  error: vi.fn(),
  debug: vi.fn(),
}

// Mock process.env
process.env = {
  ...process.env,
  NODE_ENV: 'test',
  NEXT_PUBLIC_API_URL: 'http://localhost:5000/api',
}

// Setup cleanup
beforeEach(() => {
  vi.clearAllMocks()
})

// Error handling for unhandled promise rejections
process.on('unhandledRejection', (reason) => {
  console.error('Unhandled Promise Rejection:', reason)
})

// Set locale for date formatting
if (typeof Intl !== 'undefined') {
  Intl.DateTimeFormat = vi.fn().mockImplementation(() => ({
    format: vi.fn((date) => new Date(date).toLocaleDateString('de-DE')),
    formatToParts: vi.fn(),
  }))
}