import React from 'react'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import { BezirkForm } from '../bezirk-form'
import { testDataFactories, GERMAN_TEST_LABELS } from '@/test/fixtures/kgv-data'
import { Bezirk } from '@/types/bezirke'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'

// Import MSW server
import '@/test/mocks/server'

describe('BezirkForm Component', () => {
  let mockOnSuccess: jest.Mock
  let mockOnCancel: jest.Mock
  let mockBezirk: Bezirk

  beforeEach(() => {
    mockOnSuccess = jest.fn()
    mockOnCancel = jest.fn()
    mockBezirk = testDataFactories.bezirk({
      id: 1,
      name: 'Test Bezirk',
      beschreibung: 'Ein Testbezirk für die Tests',
      bezirksleiter: 'Max Mustermann',
      telefon: '+49 30 12345678',
      email: 'test@kgv-bezirk.de',
      adresse: {
        strasse: 'Teststraße',
        hausnummer: '123',
        plz: '12345',
        ort: 'Teststadt',
      },
      aktiv: true,
    })
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Create Mode', () => {
    it('sollte das Formular im Erstellungsmodus korrekt rendern', () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      // Titel und Beschreibung
      expect(screen.getByText('Neuen Bezirk erstellen')).toBeInTheDocument()
      expect(screen.getByText('Erfassen Sie die Grunddaten für einen neuen Bezirk')).toBeInTheDocument()

      // Formular-Bereiche
      expect(screen.getByText('Grundinformationen')).toBeInTheDocument()
      expect(screen.getByText('Bezirksleitung')).toBeInTheDocument()
      expect(screen.getByText('Postanschrift')).toBeInTheDocument()

      // Aktions-Buttons
      expect(screen.getByRole('button', { name: /erstellen/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /abbrechen/i })).toBeInTheDocument()

      // Status-Sektion sollte nicht sichtbar sein im Create-Modus
      expect(screen.queryByText('Status')).not.toBeInTheDocument()
    })

    it('sollte alle Pflichtfelder korrekt kennzeichnen', () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      // Bezirksname ist Pflichtfeld
      expect(screen.getByLabelText(/bezirksname/i)).toBeRequired()
    })

    it('sollte leere Eingabefelder im Create-Modus haben', () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      expect(screen.getByRole('textbox', { name: /bezirksname/i })).toHaveValue('')
      expect(screen.getByRole('textbox', { name: /beschreibung/i })).toHaveValue('')
      expect(screen.getByRole('textbox', { name: /bezirksleiter/i })).toHaveValue('')
      expect(screen.getByRole('textbox', { name: /telefon/i })).toHaveValue('')
      expect(screen.getByRole('textbox', { name: /e-mail/i })).toHaveValue('')
      expect(screen.getByRole('textbox', { name: /straße/i })).toHaveValue('')
      expect(screen.getByRole('textbox', { name: /hausnummer/i })).toHaveValue('')
      expect(screen.getByRole('textbox', { name: /plz/i })).toHaveValue('')
      expect(screen.getByRole('textbox', { name: /ort/i })).toHaveValue('')
    })
  })

  describe('Edit Mode', () => {
    it('sollte das Formular im Bearbeitungsmodus korrekt rendern', () => {
      render(
        <BezirkForm
          mode="edit"
          initialData={mockBezirk}
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      // Titel und Beschreibung
      expect(screen.getByText('Bezirk bearbeiten')).toBeInTheDocument()
      expect(screen.getByText('Bearbeiten Sie die Bezirksdaten')).toBeInTheDocument()

      // Status-Sektion sollte sichtbar sein im Edit-Modus
      expect(screen.getByText('Status')).toBeInTheDocument()

      // Aktions-Button sollte "Speichern" sein
      expect(screen.getByRole('button', { name: /speichern/i })).toBeInTheDocument()
    })

    it('sollte die Eingabefelder mit vorhandenen Daten befüllen', () => {
      render(
        <BezirkForm
          mode="edit"
          initialData={mockBezirk}
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      expect(screen.getByDisplayValue('Test Bezirk')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Ein Testbezirk für die Tests')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Max Mustermann')).toBeInTheDocument()
      expect(screen.getByDisplayValue('+49 30 12345678')).toBeInTheDocument()
      expect(screen.getByDisplayValue('test@kgv-bezirk.de')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Teststraße')).toBeInTheDocument()
      expect(screen.getByDisplayValue('123')).toBeInTheDocument()
      expect(screen.getByDisplayValue('12345')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Teststadt')).toBeInTheDocument()
    })

    it('sollte die Aktiv-Checkbox korrekt setzen', () => {
      render(
        <BezirkForm
          mode="edit"
          initialData={mockBezirk}
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      const aktivCheckbox = screen.getByRole('checkbox', { name: /bezirk ist aktiv/i })
      expect(aktivCheckbox).toBeChecked()
    })
  })

  describe('Form Validation', () => {
    it('sollte Validierungsfehler für Pflichtfelder anzeigen', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      // Versuche zu speichern ohne Pflichtfelder auszufüllen
      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(GERMAN_TEST_LABELS.ERRORS.REQUIRED_FIELD)).toBeInTheDocument()
      })
    })

    it('sollte E-Mail-Format validieren', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      const emailInput = screen.getByRole('textbox', { name: /e-mail/i })
      await user.type(emailInput, 'ungueltige-email')

      const nameInput = screen.getByRole('textbox', { name: /bezirksname/i })
      await user.type(nameInput, 'Test Bezirk')

      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(GERMAN_TEST_LABELS.ERRORS.INVALID_EMAIL)).toBeInTheDocument()
      })
    })

    it('sollte Telefonnummer-Format validieren', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      const telefonInput = screen.getByRole('textbox', { name: /telefon/i })
      await user.type(telefonInput, 'abc123')

      const nameInput = screen.getByRole('textbox', { name: /bezirksname/i })
      await user.type(nameInput, 'Test Bezirk')

      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(GERMAN_TEST_LABELS.ERRORS.INVALID_PHONE)).toBeInTheDocument()
      })
    })

    it('sollte PLZ-Format validieren', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      const plzInput = screen.getByRole('textbox', { name: /plz/i })
      await user.type(plzInput, '123') // Zu kurz

      const nameInput = screen.getByRole('textbox', { name: /bezirksname/i })
      await user.type(nameInput, 'Test Bezirk')

      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(GERMAN_TEST_LABELS.ERRORS.INVALID_PLZ)).toBeInTheDocument()
      })
    })

    it('sollte Eingabelängen validieren', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      const nameInput = screen.getByRole('textbox', { name: /bezirksname/i })
      await user.type(nameInput, 'A') // Zu kurz

      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(GERMAN_TEST_LABELS.ERRORS.MIN_LENGTH(2))).toBeInTheDocument()
      })
    })
  })

  describe('Form Submission', () => {
    it('sollte erfolgreich einen neuen Bezirk erstellen', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      // Formular ausfüllen
      await user.type(screen.getByRole('textbox', { name: /bezirksname/i }), 'Neuer Testbezirk')
      await user.type(screen.getByRole('textbox', { name: /beschreibung/i }), 'Eine Testbeschreibung')
      await user.type(screen.getByRole('textbox', { name: /bezirksleiter/i }), 'Test Manager')
      await user.type(screen.getByRole('textbox', { name: /e-mail/i }), 'test@example.com')
      await user.type(screen.getByRole('textbox', { name: /telefon/i }), '0123456789')

      // Adresse
      await user.type(screen.getByRole('textbox', { name: /straße/i }), 'Teststraße')
      await user.type(screen.getByRole('textbox', { name: /hausnummer/i }), '42')
      await user.type(screen.getByRole('textbox', { name: /plz/i }), '12345')
      await user.type(screen.getByRole('textbox', { name: /ort/i }), 'Teststadt')

      // Submit
      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalledWith(
          expect.objectContaining({
            name: 'Neuer Testbezirk',
            beschreibung: 'Eine Testbeschreibung',
            bezirksleiter: 'Test Manager',
          })
        )
      })
    })

    it('sollte erfolgreich einen Bezirk aktualisieren', async () => {
      const { user } = render(
        <BezirkForm
          mode="edit"
          initialData={mockBezirk}
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      // Name ändern
      const nameInput = screen.getByRole('textbox', { name: /bezirksname/i })
      await user.clear(nameInput)
      await user.type(nameInput, 'Geänderter Bezirk')

      // Submit
      const submitButton = screen.getByRole('button', { name: /speichern/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalledWith(
          expect.objectContaining({
            name: 'Geänderter Bezirk',
          })
        )
      })
    })

    it('sollte Loading-State während Submission anzeigen', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      // Mindestdaten eingeben
      await user.type(screen.getByRole('textbox', { name: /bezirksname/i }), 'Test Bezirk')

      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      // Loading-State prüfen
      expect(screen.getByText('Speichert...')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /speichert/i })).toBeDisabled()
    })

    it('sollte das Formular nach erfolgreichem Erstellen zurücksetzen', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      const nameInput = screen.getByRole('textbox', { name: /bezirksname/i })
      await user.type(nameInput, 'Test Bezirk')

      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalled()
      })

      // Formular sollte zurückgesetzt sein
      await waitFor(() => {
        expect(nameInput).toHaveValue('')
      })
    })
  })

  describe('Form Actions', () => {
    it('sollte die Abbrechen-Funktion aufrufen', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      const cancelButton = screen.getByRole('button', { name: /abbrechen/i })
      await user.click(cancelButton)

      expect(mockOnCancel).toHaveBeenCalledTimes(1)
    })

    it('sollte "Fehler anzeigen" Button bei Validierungsfehlern anzeigen', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      // Ungültige Daten eingeben
      await user.type(screen.getByRole('textbox', { name: /e-mail/i }), 'ungueltig')

      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Fehler anzeigen')).toBeInTheDocument()
      })
    })

    it('sollte Submit-Button deaktivieren wenn Formular nicht valide ist', async () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      
      // Button sollte anfangs deaktiviert sein (da Pflichtfelder leer)
      expect(submitButton).toBeDisabled()
    })

    it('sollte Submit-Button aktivieren wenn Formular valide ist', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      // Mindestanforderungen erfüllen
      await user.type(screen.getByRole('textbox', { name: /bezirksname/i }), 'Test Bezirk')

      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      
      await waitFor(() => {
        expect(submitButton).not.toBeDisabled()
      })
    })
  })

  describe('Field Interactions', () => {
    it('sollte Felder während Submission deaktivieren', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      await user.type(screen.getByRole('textbox', { name: /bezirksname/i }), 'Test Bezirk')

      const nameInput = screen.getByRole('textbox', { name: /bezirksname/i })
      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      
      await user.click(submitButton)

      // Alle Eingabefelder sollten während Submission deaktiviert sein
      expect(nameInput).toBeDisabled()
    })

    it('sollte Autocomplete-Attribute korrekt setzen', () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      expect(screen.getByRole('textbox', { name: /bezirksleiter/i })).toHaveAttribute('autocomplete', 'name')
      expect(screen.getByRole('textbox', { name: /telefon/i })).toHaveAttribute('autocomplete', 'tel')
      expect(screen.getByRole('textbox', { name: /e-mail/i })).toHaveAttribute('autocomplete', 'email')
      expect(screen.getByRole('textbox', { name: /straße/i })).toHaveAttribute('autocomplete', 'street-address')
      expect(screen.getByRole('textbox', { name: /plz/i })).toHaveAttribute('autocomplete', 'postal-code')
      expect(screen.getByRole('textbox', { name: /ort/i })).toHaveAttribute('autocomplete', 'address-level2')
    })

    it('sollte PLZ-Eingabe auf 5 Zeichen begrenzen', () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      const plzInput = screen.getByRole('textbox', { name: /plz/i })
      expect(plzInput).toHaveAttribute('maxLength', '5')
    })
  })

  describe('Deutsche Lokalisierung', () => {
    it('sollte alle deutschen Labels und Texte anzeigen', () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      // Deutsche Sektions-Titel
      expect(screen.getByText('Grundinformationen')).toBeInTheDocument()
      expect(screen.getByText('Bezirksleitung')).toBeInTheDocument()
      expect(screen.getByText('Postanschrift')).toBeInTheDocument()

      // Deutsche Field Labels
      expect(screen.getByText('Bezirksname')).toBeInTheDocument()
      expect(screen.getByText('Beschreibung')).toBeInTheDocument()
      expect(screen.getByText('Bezirksleiter/in')).toBeInTheDocument()
      expect(screen.getByText('Telefon')).toBeInTheDocument()
      expect(screen.getByText('E-Mail')).toBeInTheDocument()
      expect(screen.getByText('Straße')).toBeInTheDocument()
      expect(screen.getByText('Hausnummer')).toBeInTheDocument()
      expect(screen.getByText('PLZ')).toBeInTheDocument()
      expect(screen.getByText('Ort')).toBeInTheDocument()

      // Deutsche Platzhalter
      expect(screen.getByPlaceholderText('z.B. Bezirk Nord, Zentrum, etc.')).toBeInTheDocument()
      expect(screen.getByPlaceholderText('Max Mustermann')).toBeInTheDocument()
      expect(screen.getByPlaceholderText('bezirksleiter@kgv-beispiel.de')).toBeInTheDocument()

      // Deutsche Buttons
      expect(screen.getByText('Abbrechen')).toBeInTheDocument()
      expect(screen.getByText('Erstellen')).toBeInTheDocument()
    })

    it('sollte deutsche Hilfetexte anzeigen', () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      expect(screen.getByText('Kurze Beschreibung des Bezirks (optional)')).toBeInTheDocument()
      expect(screen.getByText('Name der verantwortlichen Person')).toBeInTheDocument()
      expect(screen.getByText('Telefonnummer der Bezirksleitung')).toBeInTheDocument()
      expect(screen.getByText('E-Mail-Adresse der Bezirksleitung')).toBeInTheDocument()
    })

    it('sollte deutsche Status-Texte im Edit-Modus anzeigen', () => {
      render(
        <BezirkForm
          mode="edit"
          initialData={mockBezirk}
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      expect(screen.getByText('Bezirk ist aktiv')).toBeInTheDocument()
      expect(screen.getByText('Deaktivierte Bezirke werden nicht in Listen angezeigt')).toBeInTheDocument()
      expect(screen.getByText('Bezirk ist aktiv und verfügbar')).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('sollte korrekte ARIA-Labels haben', () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      // Alle Eingabefelder sollten Labels haben
      expect(screen.getByLabelText(/bezirksname/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/beschreibung/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/bezirksleiter/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/telefon/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/e-mail/i)).toBeInTheDocument()
    })

    it('sollte mit Tastatur navigierbar sein', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      const nameInput = screen.getByRole('textbox', { name: /bezirksname/i })
      
      await user.tab()
      expect(nameInput).toHaveFocus()

      await user.tab()
      expect(screen.getByRole('textbox', { name: /beschreibung/i })).toHaveFocus()
    })

    it('sollte Formular-Fehler accessible machen', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      await waitFor(() => {
        // Fehler sollten mit entsprechenden ARIA-Attributen verknüpft sein
        const nameInput = screen.getByRole('textbox', { name: /bezirksname/i })
        expect(nameInput).toHaveAttribute('aria-invalid', 'true')
      })
    })
  })

  describe('Error Handling', () => {
    it('sollte Server-Fehler korrekt behandeln', async () => {
      // Mock einen Server-Fehler
      server.use(
        http.post('/api/bezirke', () => {
          return HttpResponse.json(
            { error: 'Interner Serverfehler' },
            { status: 500 }
          )
        })
      )

      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={mockOnSuccess}
          onCancel={mockOnCancel}
        />
      )

      await user.type(screen.getByRole('textbox', { name: /bezirksname/i }), 'Test Bezirk')

      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(/fehler/i)).toBeInTheDocument()
      })
    })

    it('sollte mit fehlenden initialData umgehen', () => {
      expect(() => {
        render(
          <BezirkForm
            mode="edit"
            onSuccess={mockOnSuccess}
            onCancel={mockOnCancel}
          />
        )
      }).not.toThrow()
    })
  })
})