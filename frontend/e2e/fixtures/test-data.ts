/**
 * Test Data Fixtures for KGV E2E Tests
 * 
 * Centralized test data management and generation
 */

export interface TestBezirk {
  name: string
  beschreibung?: string
  bezirksleiter?: string
  telefon?: string
  email?: string
  adresse?: {
    strasse?: string
    hausnummer?: string
    plz?: string
    ort?: string
  }
}

export interface TestBezirkExpected {
  id?: string
  name: string
  beschreibung?: string
  aktiv: boolean
  statistiken?: {
    gesamtParzellen: number
    belegteParzellen: number
    freieParzellen: number
    warteliste: number
  }
}

export class BezirkTestData {
  private static counter = 0

  static generateValidBezirk(overrides: Partial<TestBezirk> = {}): TestBezirk {
    const timestamp = Date.now()
    const uniqueId = ++this.counter
    
    return {
      name: `T${timestamp.toString().slice(-3)}${uniqueId}`,
      beschreibung: `Test Bezirk für E2E Tests - ${new Date().toLocaleString('de-DE')}`,
      ...overrides
    }
  }

  static generateValidBezirkSet(count: number): TestBezirk[] {
    return Array.from({ length: count }, (_, index) => 
      this.generateValidBezirk({
        name: `Test${index + 1}`,
        beschreibung: `Test Bezirk ${index + 1} für Listendarstellung`
      })
    )
  }

  static getValidBezirkSamples(): TestBezirk[] {
    return [
      {
        name: 'Nord',
        beschreibung: 'Nördlicher Bezirk mit Haupteingang an der Nordstraße'
      },
      {
        name: 'Süd',
        beschreibung: 'Südlicher Bezirk in ruhiger Lage'
      },
      {
        name: 'Ost',
        beschreibung: 'Östlicher Bezirk mit Spielplatz und Gemeinschaftshaus'
      },
      {
        name: 'West',
        beschreibung: 'Westlicher Bezirk direkt am Bach gelegen'
      },
      {
        name: 'A1',
        beschreibung: 'Erster Bereich der A-Sektion'
      }
    ]
  }

  static getInvalidBezirkData(): Array<{
    name: string
    data: Partial<TestBezirk>
    expectedError: string | RegExp
  }> {
    return [
      {
        name: 'empty name',
        data: { name: '', beschreibung: 'Valid description' },
        expectedError: /name.*erforderlich|name.*required/i
      },
      {
        name: 'name too long',
        data: { name: 'VeryLongNameThatExceedsLimit', beschreibung: 'Valid description' },
        expectedError: /name.*10 zeichen|name.*too long/i
      },
      {
        name: 'whitespace only name',
        data: { name: '   ', beschreibung: 'Valid description' },
        expectedError: /name.*erforderlich|name.*required/i
      },
      {
        name: 'special characters in name',
        data: { name: 'Test@#$%', beschreibung: 'Valid description' },
        expectedError: /ungültige zeichen|invalid characters/i
      },
      {
        name: 'description too long',
        data: { 
          name: 'Valid',
          beschreibung: 'A'.repeat(501) // Assuming 500 character limit
        },
        expectedError: /beschreibung.*500|description.*too long/i
      },
      {
        name: 'numeric only name',
        data: { name: '12345', beschreibung: 'Valid description' },
        expectedError: /name.*buchstaben|name.*letters/i
      }
    ]
  }

  static getBoundaryTestData(): Array<{
    name: string
    data: TestBezirk
    description: string
  }> {
    return [
      {
        name: 'minimum valid name',
        data: { name: 'A' },
        description: 'Single character name should be valid'
      },
      {
        name: 'maximum valid name',
        data: { name: 'A'.repeat(10) },
        description: '10 character name should be valid'
      },
      {
        name: 'maximum valid description',
        data: { 
          name: 'Valid',
          beschreibung: 'A'.repeat(500)
        },
        description: '500 character description should be valid'
      },
      {
        name: 'empty description',
        data: { name: 'Valid', beschreibung: '' },
        description: 'Empty description should be valid'
      },
      {
        name: 'no description',
        data: { name: 'Valid' },
        description: 'Missing description should be valid'
      }
    ]
  }

  static getSpecialCharacterTestData(): Array<{
    name: string
    data: TestBezirk
    shouldBeValid: boolean
  }> {
    return [
      {
        name: 'german umlauts',
        data: { name: 'Größe', beschreibung: 'Test mit Umlauten' },
        shouldBeValid: true
      },
      {
        name: 'numbers and letters',
        data: { name: 'Bezirk1', beschreibung: 'Alphanumerischer Name' },
        shouldBeValid: true
      },
      {
        name: 'hyphen in name',
        data: { name: 'Nord-Ost', beschreibung: 'Name mit Bindestrich' },
        shouldBeValid: true
      },
      {
        name: 'underscore in name',
        data: { name: 'Nord_Ost', beschreibung: 'Name mit Unterstrich' },
        shouldBeValid: false
      },
      {
        name: 'special symbols',
        data: { name: 'Test@#$', beschreibung: 'Name mit Sonderzeichen' },
        shouldBeValid: false
      },
      {
        name: 'spaces in name',
        data: { name: 'Nord Ost', beschreibung: 'Name mit Leerzeichen' },
        shouldBeValid: false
      }
    ]
  }
}

export class MockApiData {
  static generateBezirkResponse(bezirk: TestBezirk, id?: string): TestBezirkExpected {
    return {
      id: id || Math.random().toString(36).substring(2),
      name: bezirk.name,
      beschreibung: bezirk.beschreibung,
      aktiv: true,
      statistiken: {
        gesamtParzellen: Math.floor(Math.random() * 20) + 5,
        belegteParzellen: Math.floor(Math.random() * 15) + 1,
        freieParzellen: Math.floor(Math.random() * 10),
        warteliste: Math.floor(Math.random() * 5)
      }
    }
  }

  static generateBezirkeListResponse(bezirke: TestBezirk[]): {
    bezirke: TestBezirkExpected[]
    pagination: {
      page: number
      limit: number
      total: number
      totalPages: number
    }
  } {
    const bezirkeData = bezirke.map(bezirk => this.generateBezirkResponse(bezirk))
    
    return {
      bezirke: bezirkeData,
      pagination: {
        page: 1,
        limit: 20,
        total: bezirkeData.length,
        totalPages: Math.ceil(bezirkeData.length / 20)
      }
    }
  }

  static generateStatisticsResponse(): {
    gesamtBezirke: number
    aktiveBezirke: number
    gesamtParzellen: number
    belegteParzellen: number
    freieParzellen: number
    auslastung: number
    trends: {
      neueAntraege: number
      kuendigungen: number
      neueParzellen: number
      zeitraum: string
    }
  } {
    const gesamtBezirke = 12
    const aktiveBezirke = 10
    const gesamtParzellen = 150
    const belegteParzellen = 120
    
    return {
      gesamtBezirke,
      aktiveBezirke,
      gesamtParzellen,
      belegteParzellen,
      freieParzellen: gesamtParzellen - belegteParzellen,
      auslastung: Math.round((belegteParzellen / gesamtParzellen) * 100),
      trends: {
        neueAntraege: 5,
        kuendigungen: 2,
        neueParzellen: 3,
        zeitraum: 'Aktueller Monat'
      }
    }
  }

  static generateApiError(statusCode: number = 500): {
    error: string
    message: string
    status: number
    timestamp: string
  } {
    const errorMessages = {
      400: 'Ungültige Anfrage',
      404: 'Ressource nicht gefunden',
      500: 'Interner Serverfehler',
      503: 'Service nicht verfügbar'
    }
    
    return {
      error: 'API Error',
      message: errorMessages[statusCode as keyof typeof errorMessages] || 'Unbekannter Fehler',
      status: statusCode,
      timestamp: new Date().toISOString()
    }
  }

  static generateValidationError(field: string, message: string): {
    error: 'Validation Error'
    details: Array<{
      field: string
      message: string
      value?: any
    }>
    status: 400
    timestamp: string
  } {
    return {
      error: 'Validation Error',
      details: [{
        field,
        message,
        value: undefined
      }],
      status: 400,
      timestamp: new Date().toISOString()
    }
  }
}

export class TestDataManager {
  private createdBezirke: string[] = []

  addCreatedBezirk(id: string): void {
    this.createdBezirke.push(id)
  }

  getCreatedBezirke(): string[] {
    return [...this.createdBezirke]
  }

  clearCreatedBezirke(): void {
    this.createdBezirke = []
  }

  async cleanupCreatedBezirke(apiClient: any): Promise<void> {
    for (const id of this.createdBezirke) {
      try {
        // This would be implemented based on your API client
        // await apiClient.delete(`/bezirke/${id}`)
        console.log(`Cleaned up bezirk: ${id}`)
      } catch (error) {
        console.warn(`Failed to cleanup bezirk ${id}:`, error)
      }
    }
    this.clearCreatedBezirke()
  }
}

// Pre-defined test scenarios
export const TestScenarios = {
  happyPath: {
    bezirk: BezirkTestData.generateValidBezirk({
      name: 'TestOK',
      beschreibung: 'Happy Path Test Bezirk'
    })
  },
  
  apiError: {
    bezirk: BezirkTestData.generateValidBezirk({
      name: 'TestAPI',
      beschreibung: 'API Error Test'
    })
  },
  
  validation: {
    invalidBezirke: BezirkTestData.getInvalidBezirkData()
  },
  
  boundary: {
    testCases: BezirkTestData.getBoundaryTestData()
  },
  
  specialCharacters: {
    testCases: BezirkTestData.getSpecialCharacterTestData()
  }
}