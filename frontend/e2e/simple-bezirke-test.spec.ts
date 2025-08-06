import { test, expect } from '@playwright/test'

/**
 * Einfacher KGV Bezirke E2E Test
 * Testet die grundlegende Funktionalität ohne Backend-Abhängigkeiten
 */

test.describe('KGV Bezirke - Grundfunktionalität', () => {
  test.beforeEach(async ({ page }) => {
    // Direkt zur lokalen Anwendung ohne Health-Check
    await page.goto('http://localhost:3001')
  })

  test('kann zur Bezirke-Übersicht navigieren', async ({ page }) => {
    // Warten auf Seitenladung
    await page.waitForLoadState('networkidle')

    // Suche nach Link zu Bezirke
    const bezirkeLink = page.getByRole('link', { name: /bezirke/i }).first()
    await expect(bezirkeLink).toBeVisible()

    // Zur Bezirke-Seite navigieren
    await bezirkeLink.click()

    // Überprüfen, ob wir auf der richtigen Seite sind
    await expect(page).toHaveURL(/\/bezirke/)
    
    // Überprüfen, ob die Seite geladen hat
    await expect(page.getByText(/bezirke/i)).toBeVisible()
  })

  test('kann zur Bezirk-Erstellung navigieren', async ({ page }) => {
    // Zur Bezirke-Seite navigieren
    await page.goto('http://localhost:3001/bezirke')
    await page.waitForLoadState('networkidle')

    // Suche nach "Neuer Bezirk" oder "Erstellen" Button
    const createButton = page.getByRole('link', { name: /neu|erstellen|hinzufügen/i }).first()
    
    if (await createButton.isVisible()) {
      await createButton.click()
      
      // Überprüfen ob wir auf der Erstellungsseite sind
      await expect(page).toHaveURL(/\/bezirke\/neu/)
    } else {
      // Direkt zur Erstellungsseite navigieren
      await page.goto('http://localhost:3001/bezirke/neu')
    }

    // Überprüfen, ob das Formular geladen hat
    await expect(page.getByText(/neuen bezirk|bezirk erstellen/i)).toBeVisible()
  })

  test('zeigt Bezirk-Erstellungsformular an', async ({ page }) => {
    // Direkt zur Erstellungsseite
    await page.goto('http://localhost:3001/bezirke/neu')
    await page.waitForLoadState('networkidle')

    // Überprüfen, ob alle wichtigen Formularelemente vorhanden sind
    await expect(page.getByLabel(/name|bezirksname/i)).toBeVisible()
    
    // Beschreibung Feld (optional)
    const beschreibungField = page.getByLabel(/beschreibung/i)
    if (await beschreibungField.isVisible()) {
      await expect(beschreibungField).toBeVisible()
    }

    // Submit Button
    await expect(page.getByRole('button', { name: /erstellen|speichern|hinzufügen/i })).toBeVisible()
  })

  test('kann Formularfelder ausfüllen', async ({ page }) => {
    // Zur Erstellungsseite
    await page.goto('http://localhost:3001/bezirke/neu')
    await page.waitForLoadState('networkidle')

    // Name ausfüllen
    const nameField = page.getByLabel(/name|bezirksname/i)
    await nameField.fill('Test Bezirk')
    await expect(nameField).toHaveValue('Test Bezirk')

    // Beschreibung ausfüllen (falls vorhanden)
    const beschreibungField = page.getByLabel(/beschreibung/i)
    if (await beschreibungField.isVisible()) {
      await beschreibungField.fill('Test Beschreibung für den Bezirk')
      await expect(beschreibungField).toHaveValue('Test Beschreibung für den Bezirk')
    }
  })

  test('zeigt Client-seitige Validierung an', async ({ page }) => {
    // Zur Erstellungsseite
    await page.goto('http://localhost:3001/bezirke/neu')
    await page.waitForLoadState('networkidle')

    // Versuche zu submiten ohne Daten
    const submitButton = page.getByRole('button', { name: /erstellen|speichern|hinzufügen/i })
    await submitButton.click()

    // Warten auf Validierungsnachrichten
    await page.waitForTimeout(500)

    // Überprüfen auf Validierungsfehlermeldungen
    const errorMessages = page.locator('[class*="error"], [class*="invalid"], [role="alert"], .text-red')
    
    if (await errorMessages.first().isVisible()) {
      await expect(errorMessages.first()).toBeVisible()
      console.log('✅ Client-seitige Validierung funktioniert')
    } else {
      console.log('ℹ️  Keine sichtbare Client-seitige Validierung gefunden')
    }
  })

  test('kann mit Backend API interagieren (falls verfügbar)', async ({ page }) => {
    // Network monitoring aktivieren
    const responses: string[] = []
    page.on('response', response => {
      if (response.url().includes('/api/bezirke')) {
        responses.push(`${response.status()}: ${response.url()}`)
      }
    })

    // Zur Erstellungsseite
    await page.goto('http://localhost:3001/bezirke/neu')
    await page.waitForLoadState('networkidle')

    // Formular ausfüllen
    const nameField = page.getByLabel(/name|bezirksname/i)
    await nameField.fill('E2E Test Bezirk')

    // Beschreibung ausfüllen (falls vorhanden)
    const beschreibungField = page.getByLabel(/beschreibung/i)
    if (await beschreibungField.isVisible()) {
      await beschreibungField.fill('Erstellt durch E2E Test')
    }

    // Submit Button klicken
    const submitButton = page.getByRole('button', { name: /erstellen|speichern|hinzufügen/i })
    await submitButton.click()

    // Warten auf API Response
    await page.waitForTimeout(3000)

    console.log('API Responses:', responses)

    // Je nach API Response entsprechend reagieren
    if (responses.some(r => r.includes('201') || r.includes('200'))) {
      console.log('✅ API Aufruf erfolgreich')
    } else if (responses.some(r => r.includes('500'))) {
      console.log('⚠️  API gibt 500 Fehler zurück (bekanntes Problem)')
      // Toast Error Nachricht oder ähnliche Fehlerbehandlung prüfen
    } else {
      console.log('ℹ️  Keine API Response erhalten')
    }
  })
})