import React, { ReactElement } from 'react'
import { render, RenderOptions, RenderResult } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from 'next-themes'
import userEvent from '@testing-library/user-event'

// Test-spezifische Typen
export interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  withQueryClient?: boolean
  withThemeProvider?: boolean
  initialTheme?: 'light' | 'dark'
  queryClientOptions?: {
    defaultOptions?: {
      queries?: any
      mutations?: any
    }
  }
  withRouter?: boolean
  routerProps?: {
    pathname?: string
    query?: Record<string, string>
    asPath?: string
  }
}

// Test Wrapper für Providers
function TestWrapper({
  children,
  withQueryClient = true,
  withThemeProvider = true,
  queryClientOptions = {},
  initialTheme = 'light',
}: {
  children: React.ReactNode
  withQueryClient?: boolean
  withThemeProvider?: boolean
  queryClientOptions?: any
  initialTheme?: 'light' | 'dark'
}) {
  let wrapper = <>{children}</>

  // React Query Provider
  if (withQueryClient) {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
          refetchOnWindowFocus: false,
          refetchOnMount: false,
          refetchOnReconnect: false,
          staleTime: Infinity,
          ...queryClientOptions.defaultOptions?.queries,
        },
        mutations: {
          retry: false,
          ...queryClientOptions.defaultOptions?.mutations,
        },
      },
    })

    wrapper = (
      <QueryClientProvider client={queryClient}>
        {wrapper}
      </QueryClientProvider>
    )
  }

  // Theme Provider
  if (withThemeProvider) {
    wrapper = (
      <ThemeProvider
        attribute="class"
        defaultTheme={initialTheme}
        enableSystem={false}
        disableTransitionOnChange
      >
        {wrapper}
      </ThemeProvider>
    )
  }

  return wrapper
}

// Custom render function
export function customRender(
  ui: ReactElement,
  options: CustomRenderOptions = {}
): RenderResult & { user: ReturnType<typeof userEvent.setup> } {
  const {
    withQueryClient = true,
    withThemeProvider = true,
    initialTheme = 'light',
    queryClientOptions = {},
    ...renderOptions
  } = options

  const AllTheProviders = ({ children }: { children: React.ReactNode }) => (
    <TestWrapper
      withQueryClient={withQueryClient}
      withThemeProvider={withThemeProvider}
      initialTheme={initialTheme}
      queryClientOptions={queryClientOptions}
    >
      {children}
    </TestWrapper>
  )

  const user = userEvent.setup()

  return {
    user,
    ...render(ui, { wrapper: AllTheProviders, ...renderOptions }),
  }
}

// Hilfsfunktionen für Tests
export const waitForLoadingToFinish = () =>
  new Promise((resolve) => setTimeout(resolve, 0))

export const waitForNextTick = () => 
  new Promise((resolve) => process.nextTick(resolve))

// Mock User Event helpers mit deutschen Labels
export const testHelpers = {
  // Simuliert Formular-Eingaben
  async fillForm(user: ReturnType<typeof userEvent.setup>, formData: Record<string, string>) {
    for (const [field, value] of Object.entries(formData)) {
      const input = document.querySelector(`[name="${field}"]`) as HTMLInputElement
      if (input) {
        await user.clear(input)
        await user.type(input, value)
      }
    }
  },

  // Simuliert Klick auf Button mit deutschem Text
  async clickButtonByText(user: ReturnType<typeof userEvent.setup>, text: string) {
    const button = document.querySelector(`button:contains("${text}")`) as HTMLButtonElement
    if (button) {
      await user.click(button)
    }
  },

  // Simuliert Auswahl in Select-Element
  async selectOption(user: ReturnType<typeof userEvent.setup>, selectName: string, optionValue: string) {
    const select = document.querySelector(`[name="${selectName}"]`) as HTMLSelectElement
    if (select) {
      await user.selectOptions(select, optionValue)
    }
  },

  // Simuliert Checkbox-Interaktion
  async toggleCheckbox(user: ReturnType<typeof userEvent.setup>, checkboxName: string) {
    const checkbox = document.querySelector(`[name="${checkboxName}"]`) as HTMLInputElement
    if (checkbox) {
      await user.click(checkbox)
    }
  },
}

// Assertion helpers für deutsche KGV-spezifische Tests
export const assertHelpers = {
  // Überprüft, ob deutsche Validierungsmeldungen angezeigt werden
  expectGermanValidationMessage(container: HTMLElement, expectedMessage: string) {
    const errorElement = container.querySelector('[role="alert"], .error-message, [data-testid*="error"]')
    expect(errorElement).toHaveTextContent(expectedMessage)
  },

  // Überprüft deutsche Datumsformatierung
  expectGermanDateFormat(element: HTMLElement, dateValue: string) {
    // Erwartet Format: DD.MM.YYYY
    const germanDateRegex = /^\d{2}\.\d{2}\.\d{4}$/
    expect(element).toHaveTextContent(expect.stringMatching(germanDateRegex))
  },

  // Überprüft deutsche Zahlenformatierung
  expectGermanNumberFormat(element: HTMLElement) {
    // Erwartet Format: 1.234,56 (deutsche Tausendertrennzeichen)
    const germanNumberRegex = /^\d{1,3}(?:\.\d{3})*(?:,\d{2})?$/
    expect(element).toHaveTextContent(expect.stringMatching(germanNumberRegex))
  },

  // Überprüft Accessibility Labels auf Deutsch
  expectGermanAccessibilityLabels(element: HTMLElement) {
    const ariaLabel = element.getAttribute('aria-label')
    const ariaDescription = element.getAttribute('aria-describedby')
    
    if (ariaLabel) {
      expect(ariaLabel).toMatch(/^[A-ZÄÖÜ]/) // Beginnt mit Großbuchstabe
    }
  },
}

// Error boundary für Tests
export class TestErrorBoundary extends React.Component<
  { children: React.ReactNode },
  { hasError: boolean; error?: Error }
> {
  constructor(props: { children: React.ReactNode }) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Test Error Boundary gefangen:', error, errorInfo)
  }

  render() {
    if (this.state.hasError) {
      return (
        <div data-testid="error-boundary">
          <h2>Test Fehler aufgetreten</h2>
          <details>
            <summary>Fehlerdetails</summary>
            <pre>{this.state.error?.message}</pre>
          </details>
        </div>
      )
    }

    return this.props.children
  }
}

// Test-spezifische Konstanten
export const TEST_CONSTANTS = {
  TIMEOUTS: {
    SHORT: 1000,
    MEDIUM: 3000,
    LONG: 5000,
  },
  GERMAN_MONTHS: [
    'Januar', 'Februar', 'März', 'April', 'Mai', 'Juni',
    'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'
  ],
  GERMAN_WEEKDAYS: [
    'Montag', 'Dienstag', 'Mittwoch', 'Donnerstag', 'Freitag', 'Samstag', 'Sonntag'
  ],
  KGV_SPECIFIC: {
    BEZIRK_PREFIXES: ['BZ-', 'Bezirk ', 'B-'],
    PARZELLE_PREFIXES: ['P-', 'Parzelle ', 'Par-'],
    ANTRAG_PREFIXES: ['A-', 'Antrag ', 'Ant-'],
  },
}

// Re-export everything from testing-library/react
export * from '@testing-library/react'
export { customRender as render }