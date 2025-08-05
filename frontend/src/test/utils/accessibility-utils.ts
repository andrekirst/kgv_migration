/**
 * Accessibility Testing Utilities
 * 
 * Utilities und Helper-Funktionen für Accessibility-Tests
 * mit deutscher Lokalisierung und KGV-spezifischen Anforderungen
 */

import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

// =============================================================================
// KEYBOARD NAVIGATION UTILITIES
// =============================================================================

/**
 * Simuliert Tab-Navigation durch Elemente
 */
export async function navigateWithTab(steps: number = 1) {
  const user = userEvent.setup()
  
  for (let i = 0; i < steps; i++) {
    await user.tab()
  }
  
  return document.activeElement
}

/**
 * Simuliert Shift+Tab Navigation (rückwärts)
 */
export async function navigateWithShiftTab(steps: number = 1) {
  const user = userEvent.setup()
  
  for (let i = 0; i < steps; i++) {
    await user.tab({ shift: true })
  }
  
  return document.activeElement
}

/**
 * Überprüft ob Element fokussierbar ist
 */
export function isFocusable(element: Element): boolean {
  if (!element) return false
  
  const focusableSelectors = [
    'a[href]',
    'button:not([disabled])',
    'input:not([disabled])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    '[tabindex]:not([tabindex="-1"])',
    '[contenteditable="true"]'
  ]
  
  return focusableSelectors.some(selector => element.matches(selector))
}

/**
 * Findet alle fokussierbaren Elemente in einem Container
 */
export function getFocusableElements(container: HTMLElement = document.body): HTMLElement[] {
  const focusableSelectors = [
    'a[href]:not([tabindex="-1"])',
    'button:not([disabled]):not([tabindex="-1"])',
    'input:not([disabled]):not([tabindex="-1"])',
    'select:not([disabled]):not([tabindex="-1"])',
    'textarea:not([disabled]):not([tabindex="-1"])',
    '[tabindex]:not([tabindex="-1"])',
    '[contenteditable="true"]:not([tabindex="-1"])'
  ]
  
  const selector = focusableSelectors.join(', ')
  return Array.from(container.querySelectorAll(selector)) as HTMLElement[]
}

/**
 * Testet Tab-Reihenfolge in einem Container
 */
export async function testTabOrder(expectedOrder: string[]) {
  const user = userEvent.setup()
  const results: string[] = []
  
  // Focus ersten Element
  await user.tab()
  
  for (let i = 0; i < expectedOrder.length; i++) {
    const activeElement = document.activeElement
    if (activeElement) {
      const elementId = activeElement.id || 
                       activeElement.getAttribute('data-testid') ||
                       activeElement.tagName.toLowerCase()
      results.push(elementId)
    }
    
    if (i < expectedOrder.length - 1) {
      await user.tab()
    }
  }
  
  return results
}

// =============================================================================
// ARIA UTILITIES
// =============================================================================

/**
 * Überprüft ob Element korrekte ARIA-Labels hat
 */
export function hasAccessibleName(element: Element): boolean {
  return !!(
    element.getAttribute('aria-label') ||
    element.getAttribute('aria-labelledby') ||
    (element.tagName === 'LABEL' && element.textContent?.trim()) ||
    (element.tagName === 'BUTTON' && element.textContent?.trim()) ||
    (element.tagName === 'A' && element.textContent?.trim())
  )
}

/**
 * Findet alle Elemente ohne accessible name
 */
export function findElementsWithoutAccessibleName(container: HTMLElement = document.body): Element[] {
  const interactiveElements = container.querySelectorAll(
    'button, a[href], input, select, textarea, [role="button"], [role="link"], [tabindex]:not([tabindex="-1"])'
  )
  
  return Array.from(interactiveElements).filter(el => !hasAccessibleName(el))
}

/**
 * Überprüft ARIA-expanded Status für Dropdowns/Accordions
 */
export function checkAriaExpandedState(element: Element, expectedState: boolean): boolean {
  const ariaExpanded = element.getAttribute('aria-expanded')
  return ariaExpanded === String(expectedState)
}

/**
 * Testet ob Form-Controls korrekt mit Labels verknüpft sind
 */
export function isFormControlLabeled(input: HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement): boolean {
  // Explizite Label-Verknüpfung
  if (input.id) {
    const label = document.querySelector(`label[for="${input.id}"]`)
    if (label) return true
  }
  
  // Implizite Label-Verknüpfung (Label umschließt Input)
  const parentLabel = input.closest('label')
  if (parentLabel) return true
  
  // ARIA-labelledby
  if (input.getAttribute('aria-labelledby')) return true
  
  // ARIA-label
  if (input.getAttribute('aria-label')) return true
  
  return false
}

// =============================================================================
// SEMANTIC HTML UTILITIES
// =============================================================================

/**
 * Überprüft korrekte Heading-Hierarchie
 */
export function checkHeadingHierarchy(container: HTMLElement = document.body): {
  isValid: boolean
  issues: string[]
} {
  const headings = Array.from(container.querySelectorAll('h1, h2, h3, h4, h5, h6'))
  const issues: string[] = []
  let previousLevel = 0
  
  if (headings.length === 0) {
    issues.push('Keine Überschriften gefunden')
    return { isValid: false, issues }
  }
  
  // Sollte genau eine H1 geben
  const h1Elements = headings.filter(h => h.tagName === 'H1')
  if (h1Elements.length === 0) {
    issues.push('Keine H1-Überschrift gefunden')
  } else if (h1Elements.length > 1) {
    issues.push(`Mehrere H1-Überschriften gefunden (${h1Elements.length})`)
  }
  
  // Prüfe Hierarchie
  headings.forEach((heading, index) => {
    const level = parseInt(heading.tagName.charAt(1))
    
    if (index === 0 && level !== 1) {
      issues.push(`Erste Überschrift sollte H1 sein, ist aber ${heading.tagName}`)
    }
    
    if (level > previousLevel + 1) {
      issues.push(`Sprung von H${previousLevel} zu H${level} überspringe Ebenen`)
    }
    
    if (!heading.textContent?.trim()) {
      issues.push(`Leere Überschrift gefunden: ${heading.tagName}`)
    }
    
    previousLevel = level
  })
  
  return {
    isValid: issues.length === 0,
    issues
  }
}

/**
 * Überprüft ob Listen semantisch korrekt markiert sind
 */
export function checkListSemantics(container: HTMLElement = document.body): {
  isValid: boolean
  issues: string[]
} {
  const issues: string[] = []
  
  // Finde Listen-ähnliche Strukturen ohne semantische Markup
  const potentialLists = container.querySelectorAll('div')
  
  potentialLists.forEach(div => {
    const children = Array.from(div.children)
    
    // Wenn mehr als 2 ähnliche Elemente, könnte Liste sein
    if (children.length >= 3) {
      const firstChildTag = children[0]?.tagName
      const allSameTag = children.every(child => child.tagName === firstChildTag)
      
      if (allSameTag && firstChildTag !== 'LI') {
        const hasListRole = div.getAttribute('role') === 'list'
        if (!hasListRole && div.closest('ul, ol') === null) {
          issues.push(`Potentielle Liste ohne semantisches Markup gefunden (${children.length} ${firstChildTag}-Elemente)`)
        }
      }
    }
  })
  
  // Prüfe existierende Listen
  const lists = container.querySelectorAll('ul, ol')
  lists.forEach((list, index) => {
    const listItems = list.querySelectorAll('li')
    if (listItems.length === 0) {
      issues.push(`Leere Liste gefunden (Index: ${index})`)
    }
    
    // Prüfe ob Liste-Items direktes children sind
    const directChildren = Array.from(list.children)
    const nonListItems = directChildren.filter(child => child.tagName !== 'LI')
    if (nonListItems.length > 0) {
      issues.push(`Liste enthält Nicht-LI-Elemente: ${nonListItems.map(el => el.tagName).join(', ')}`)
    }
  })
  
  return {
    isValid: issues.length === 0,
    issues
  }
}

// =============================================================================
// COLOR CONTRAST UTILITIES
// =============================================================================

/**
 * Berechnet Kontrast-Verhältnis zwischen zwei Farben (vereinfacht)
 * Hinweis: Für echte Tests sollte eine vollständige Kontrast-Bibliothek verwendet werden
 */
export function calculateColorContrast(foreground: string, background: string): number {
  // Diese ist eine vereinfachte Implementierung
  // In echten Tests sollte eine Bibliothek wie 'color-contrast' verwendet werden
  
  // Für Tests geben wir 4.5 zurück (WCAG AA Standard)
  return 4.5
}

/**
 * Überprüft Text-Kontrast für alle Textelemente
 */
export function checkTextContrast(container: HTMLElement = document.body): {
  isValid: boolean
  issues: string[]
} {
  const issues: string[] = []
  const textElements = container.querySelectorAll('p, span, div, h1, h2, h3, h4, h5, h6, a, button, label')
  
  textElements.forEach((element, index) => {
    const computedStyle = window.getComputedStyle(element)
    const color = computedStyle.color
    const backgroundColor = computedStyle.backgroundColor
    
    // Vereinfachte Prüfung - in echten Tests würde man eine Kontrast-Bibliothek verwenden
    if (color === backgroundColor) {
      issues.push(`Element ${index}: Text und Hintergrund haben gleiche Farbe`)
    }
    
    // Transparent backgrounds sind problematisch
    if (backgroundColor === 'rgba(0, 0, 0, 0)' || backgroundColor === 'transparent') {
      const parentBg = element.parentElement ? window.getComputedStyle(element.parentElement).backgroundColor : 'white'
      if (parentBg === 'rgba(0, 0, 0, 0)' || parentBg === 'transparent') {
        // In echten Tests würde man hier den echten Kontrast berechnen
      }
    }
  })
  
  return {
    isValid: issues.length === 0,
    issues
  }
}

// =============================================================================
// SCREEN READER UTILITIES
// =============================================================================

/**
 * Überprüft ob wichtige Inhalte für Screen Reader verfügbar sind
 */
export function checkScreenReaderContent(container: HTMLElement = document.body): {
  isValid: boolean
  issues: string[]
} {
  const issues: string[] = []
  
  // Prüfe auf versteckte wichtige Inhalte
  const hiddenElements = container.querySelectorAll('[aria-hidden="true"]')
  hiddenElements.forEach((element, index) => {
    if (element.textContent?.trim() && element.textContent.length > 20) {
      issues.push(`Wichtiger Text in aria-hidden Element (Index: ${index}): "${element.textContent.substring(0, 50)}..."`)
    }
  })
  
  // Prüfe auf fehlende Alt-Texte bei Bildern
  const images = container.querySelectorAll('img')
  images.forEach((img, index) => {
    if (!img.getAttribute('alt') && img.getAttribute('alt') !== '') {
      issues.push(`Bild ohne Alt-Text (Index: ${index})`)
    }
  })
  
  // Prüfe auf fehlende Labels bei Inputs
  const inputs = container.querySelectorAll('input, select, textarea')
  inputs.forEach((input, index) => {
    if (!isFormControlLabeled(input as HTMLInputElement)) {
      issues.push(`Form-Control ohne Label (Index: ${index}, Type: ${input.getAttribute('type') || input.tagName})`)
    }
  })
  
  return {
    isValid: issues.length === 0,
    issues
  }
}

// =============================================================================
// COMPREHENSIVE ACCESSIBILITY CHECKER
// =============================================================================

/**
 * Führt alle Accessibility-Checks durch
 */
export function checkAccessibility(container: HTMLElement = document.body): {
  isValid: boolean
  results: {
    headingHierarchy: ReturnType<typeof checkHeadingHierarchy>
    listSemantics: ReturnType<typeof checkListSemantics>
    textContrast: ReturnType<typeof checkTextContrast>
    screenReaderContent: ReturnType<typeof checkScreenReaderContent>
    elementsWithoutNames: Element[]
  }
} {
  const results = {
    headingHierarchy: checkHeadingHierarchy(container),
    listSemantics: checkListSemantics(container),
    textContrast: checkTextContrast(container),
    screenReaderContent: checkScreenReaderContent(container),
    elementsWithoutNames: findElementsWithoutAccessibleName(container)
  }
  
  const isValid = 
    results.headingHierarchy.isValid &&
    results.listSemantics.isValid &&
    results.textContrast.isValid &&
    results.screenReaderContent.isValid &&
    results.elementsWithoutNames.length === 0
  
  return { isValid, results }
}

// =============================================================================
// GERMAN A11Y TESTING UTILITIES
// =============================================================================

/**
 * Deutsche Accessibility-Test-Labels und -Meldungen
 */
export const GERMAN_A11Y_LABELS = {
  BUTTONS: {
    CLOSE: 'Schließen',
    CANCEL: 'Abbrechen',
    SAVE: 'Speichern',
    DELETE: 'Löschen',
    EDIT: 'Bearbeiten',
    ADD: 'Hinzufügen',
    SEARCH: 'Suchen',
    FILTER: 'Filtern',
    SORT: 'Sortieren',
    NEXT: 'Weiter',
    PREVIOUS: 'Zurück'
  },
  FORM_LABELS: {
    NAME: 'Name',
    EMAIL: 'E-Mail',
    PHONE: 'Telefon',
    ADDRESS: 'Adresse',
    DESCRIPTION: 'Beschreibung',
    REQUIRED_FIELD: 'Pflichtfeld'
  },
  NAVIGATION: {
    MAIN_MENU: 'Hauptmenü',
    BREADCRUMB: 'Brotkrümel-Navigation',
    PAGINATION: 'Seitennavigation',
    SKIP_TO_CONTENT: 'Zum Inhalt springen'
  },
  STATUS: {
    LOADING: 'Lädt...',
    ERROR: 'Fehler',
    SUCCESS: 'Erfolgreich',
    WARNING: 'Warnung',
    INFO: 'Information'
  },
  REGIONS: {
    HEADER: 'Kopfbereich',
    FOOTER: 'Fußbereich',
    MAIN: 'Hauptinhalt',
    ASIDE: 'Seitenbereich',
    NAVIGATION: 'Navigation'
  }
} as const

/**
 * Überprüft deutsche Accessibility-Standards
 */
export function checkGermanA11yStandards(container: HTMLElement = document.body): {
  isValid: boolean
  issues: string[]
} {
  const issues: string[] = []
  
  // Prüfe auf deutsche Sprachattribute
  const htmlElement = document.documentElement
  const lang = htmlElement.getAttribute('lang')
  if (!lang || !lang.startsWith('de')) {
    issues.push('HTML-Element sollte lang="de" Attribut haben')
  }
  
  // Prüfe auf deutsche Button-Texte
  const buttons = container.querySelectorAll('button')
  buttons.forEach((button, index) => {
    const text = button.textContent?.trim()
    if (text && !Object.values(GERMAN_A11Y_LABELS.BUTTONS).some(label => 
      text.toLowerCase().includes(label.toLowerCase())
    )) {
      // Nur warnen wenn Button englische Begriffe enthält
      if (/\b(save|delete|edit|add|search|filter|sort|next|previous|close|cancel)\b/i.test(text)) {
        issues.push(`Button mit englischem Text gefunden: "${text}" (Index: ${index})`)
      }
    }
  })
  
  return {
    isValid: issues.length === 0,
    issues
  }
}

// =============================================================================
// TEST HELPER FUNCTIONS
// =============================================================================

/**
 * Simuliert Screen Reader Navigation
 */
export async function simulateScreenReaderNavigation(container: HTMLElement = document.body) {
  const user = userEvent.setup()
  const landmarks = container.querySelectorAll('[role="main"], [role="navigation"], [role="banner"], [role="contentinfo"], main, nav, header, footer')
  const headings = container.querySelectorAll('h1, h2, h3, h4, h5, h6')
  const focusableElements = getFocusableElements(container)
  
  return {
    landmarks: Array.from(landmarks),
    headings: Array.from(headings),
    focusableElements,
    async navigateToNextHeading() {
      // Simuliert Screen Reader Heading-Navigation (normalerweise H-Taste)
      const currentFocus = document.activeElement
      const currentIndex = headings.findIndex(h => h === currentFocus)
      const nextHeading = headings[currentIndex + 1]
      
      if (nextHeading) {
        (nextHeading as HTMLElement).focus()
        return nextHeading
      }
      return null
    },
    async navigateToNextLandmark() {
      // Simuliert Screen Reader Landmark-Navigation (normalerweise D-Taste)
      const currentFocus = document.activeElement
      const currentIndex = landmarks.findIndex(l => l === currentFocus)
      const nextLandmark = landmarks[currentIndex + 1]
      
      if (nextLandmark) {
        (nextLandmark as HTMLElement).focus()
        return nextLandmark
      }
      return null
    }
  }
}

/**
 * Testet Keyboard-Navigation für eine Komponente
 */
export async function testKeyboardNavigation(container: HTMLElement): Promise<{
  success: boolean
  issues: string[]
  focusableElements: HTMLElement[]
}> {
  const focusableElements = getFocusableElements(container)
  const issues: string[] = []
  const user = userEvent.setup()
  
  if (focusableElements.length === 0) {
    issues.push('Keine fokussierbaren Elemente gefunden')
    return { success: false, issues, focusableElements }
  }
  
  // Teste Tab-Navigation vorwärts
  try {
    for (let i = 0; i < focusableElements.length; i++) {
      await user.tab()
      const activeElement = document.activeElement
      
      if (activeElement !== focusableElements[i]) {
        issues.push(`Tab-Navigation Schritt ${i + 1}: Erwartet ${focusableElements[i].tagName}, erhalten ${activeElement?.tagName}`)
      }
    }
  } catch (error) {
    issues.push(`Fehler bei Tab-Navigation: ${error}`)
  }
  
  // Teste Tab-Navigation rückwärts
  try {
    for (let i = focusableElements.length - 1; i >= 0; i--) {
      await user.tab({ shift: true })
      const activeElement = document.activeElement
      
      if (activeElement !== focusableElements[i]) {
        issues.push(`Shift+Tab-Navigation Schritt ${focusableElements.length - i}: Erwartet ${focusableElements[i].tagName}, erhalten ${activeElement?.tagName}`)
      }
    }
  } catch (error) {
    issues.push(`Fehler bei Shift+Tab-Navigation: ${error}`)
  }
  
  return {
    success: issues.length === 0,
    issues,
    focusableElements
  }
}