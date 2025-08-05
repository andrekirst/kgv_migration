import { faker } from '@faker-js/faker'

// Konfiguriere Faker für deutsche Daten
faker.setLocale('de')

// Deutsche KGV-spezifische Daten
const DEUTSCHE_BEZIRK_NAMEN = [
  'Mitte-Nord',
  'Süd-Ost',
  'West-Ende',
  'Grüner Hügel',
  'Blumenwiese',
  'Sonnenfeld',
  'Waldrand',
  'Bachtal',
  'Rosengarten',
  'Birkenweg',
  'Lindenallee',
  'Eichenpark',
]

const DEUTSCHE_STRASSEN = [
  'Gartenstraße',
  'Blumenweg',
  'Rosenallee',
  'Birkenplatz',
  'Lindenstraße',
  'Eichenweg',
  'Tulpenstraße',
  'Nelkenweg',
  'Veilchenstraße',
  'Sonnenblumenallee',
]

const DEUTSCHE_STAEDTE = [
  'Berlin',
  'Hamburg',
  'München',
  'Köln',
  'Frankfurt am Main',
  'Stuttgart',
  'Düsseldorf',
  'Leipzig',
  'Dortmund',
  'Essen',
]

const PARZELLEN_STATUS = [
  'frei',
  'vergeben',
  'wartelist',
  'gekündigt',
  'stillgelegt',
] as const

const ANTRAG_STATUS = [
  'eingereicht',
  'in_bearbeitung',
  'genehmigt',
  'abgelehnt',
  'nachbesserung_erforderlich',
] as const

// Factory Functions
export const createBezirk = (overrides?: Partial<any>) => ({
  id: `bz-${faker.string.uuid()}`,
  name: faker.helpers.arrayElement(DEUTSCHE_BEZIRK_NAMEN),
  beschreibung: faker.lorem.sentences(2),
  adresse: {
    strasse: `${faker.helpers.arrayElement(DEUTSCHE_STRASSEN)} ${faker.number.int({ min: 1, max: 999 })}`,
    plz: faker.location.zipCode('####'),
    ort: faker.helpers.arrayElement(DEUTSCHE_STAEDTE),
  },
  kontakt: {
    telefon: faker.phone.number('+49 ### #######'),
    email: faker.internet.email(),
    ansprechpartner: faker.person.fullName(),
  },
  statistiken: {
    gesamtParzellen: faker.number.int({ min: 20, max: 200 }),
    freieParzellen: faker.number.int({ min: 0, max: 20 }),
    vergebeneParzellen: faker.number.int({ min: 10, max: 180 }),
    warteliste: faker.number.int({ min: 0, max: 50 }),
  },
  geografie: {
    flaeche: faker.number.float({ min: 5.0, max: 50.0, precision: 0.1 }), // Hektar
    koordinaten: {
      lat: faker.location.latitude({ min: 47.3, max: 54.9 }), // Deutschland
      lng: faker.location.longitude({ min: 5.9, max: 15.0 }),
    },
  },
  verwaltung: {
    vorstand: {
      vorsitzender: faker.person.fullName(),
      stellvertreter: faker.person.fullName(),
      kassenwart: faker.person.fullName(),
      schriftfuehrer: faker.person.fullName(),
    },
    vereinsregister: `VR ${faker.number.int({ min: 1000, max: 9999 })}`,
    steuerNummer: faker.number.int({ min: 10000000, max: 99999999 }).toString(),
  },
  beitraege: {
    jahresbeitrag: faker.number.float({ min: 200, max: 800, precision: 0.01 }),
    grundsteuer: faker.number.float({ min: 50, max: 200, precision: 0.01 }),
    wassergeld: faker.number.float({ min: 30, max: 100, precision: 0.01 }),
  },
  aktiv: faker.datatype.boolean({ probability: 0.9 }),
  createdAt: faker.date.past({ years: 5 }).toISOString(),
  updatedAt: faker.date.recent({ days: 30 }).toISOString(),
  ...overrides,
})

export const createParzelle = (overrides?: Partial<any>) => ({
  id: `p-${faker.string.uuid()}`,
  nummer: `P-${faker.number.int({ min: 1, max: 999 }).toString().padStart(3, '0')}`,
  bezirkId: overrides?.bezirkId || `bz-${faker.string.uuid()}`,
  status: faker.helpers.arrayElement(PARZELLEN_STATUS),
  flaeche: faker.number.int({ min: 200, max: 800 }), // Quadratmeter
  jahrespacht: faker.number.float({ min: 150, max: 600, precision: 0.01 }),
  beschreibung: faker.lorem.sentences(3),
  ausstattung: {
    laube: faker.datatype.boolean({ probability: 0.8 }),
    wasseranschluss: faker.datatype.boolean({ probability: 0.9 }),
    stromanschluss: faker.datatype.boolean({ probability: 0.7 }),
    gartenhaus: faker.datatype.boolean({ probability: 0.6 }),
    geraeteschuppen: faker.datatype.boolean({ probability: 0.5 }),
    kompost: faker.datatype.boolean({ probability: 0.9 }),
    regentonne: faker.datatype.boolean({ probability: 0.8 }),
  },
  lage: {
    reihe: faker.string.alpha({ length: 1, casing: 'upper' }),
    position: faker.number.int({ min: 1, max: 50 }),
    himmelsrichtung: faker.helpers.arrayElement(['Nord', 'Süd', 'Ost', 'West', 'Südwest', 'Nordost']),
    nachbarn: {
      links: faker.helpers.maybe(() => `P-${faker.number.int({ min: 1, max: 999 })}`, { probability: 0.8 }),
      rechts: faker.helpers.maybe(() => `P-${faker.number.int({ min: 1, max: 999 })}`, { probability: 0.8 }),
      gegenueber: faker.helpers.maybe(() => `P-${faker.number.int({ min: 1, max: 999 })}`, { probability: 0.8 }),
    },
  },
  paechter: overrides?.status === 'vergeben' ? {
    name: faker.person.fullName(),
    email: faker.internet.email(),
    telefon: faker.phone.number('+49 ### #######'),
    adresse: {
      strasse: `${faker.helpers.arrayElement(DEUTSCHE_STRASSEN)} ${faker.number.int({ min: 1, max: 999 })}`,
      plz: faker.location.zipCode('####'),
      ort: faker.helpers.arrayElement(DEUTSCHE_STAEDTE),
    },
    mitgliedSeit: faker.date.past({ years: 10 }).toISOString(),
    mitgliedsnummer: `M-${faker.number.int({ min: 1000, max: 9999 })}`,
  } : null,
  zustand: {
    bewertung: faker.number.int({ min: 1, max: 5 }),
    letzteInspektion: faker.date.recent({ days: 90 }).toISOString(),
    maengel: faker.helpers.maybe(() => [
      faker.helpers.arrayElement([
        'Unkraut im Gemeinschaftsbereich',
        'Reparatur der Laube erforderlich',
        'Wasserhahn defekt',
        'Zaunreparatur nötig',
        'Baumschnitt überfällig',
      ])
    ], { probability: 0.3 }),
  },
  createdAt: faker.date.past({ years: 3 }).toISOString(),
  updatedAt: faker.date.recent({ days: 7 }).toISOString(),
  ...overrides,
})

export const createAntrag = (overrides?: Partial<any>) => ({
  id: `a-${faker.string.uuid()}`,
  antragsnummer: `ANT-${new Date().getFullYear()}-${faker.number.int({ min: 1000, max: 9999 })}`,
  typ: faker.helpers.arrayElement([
    'parzelle_neuantrag',
    'parzelle_wechsel',
    'mitgliedschaft',
    'bauantrag',
    'kuendigung',
    'aenderung_stammdaten',
  ]),
  status: faker.helpers.arrayElement(ANTRAG_STATUS),
  antragsteller: {
    vorname: faker.person.firstName(),
    nachname: faker.person.lastName(),
    email: faker.internet.email(),
    telefon: faker.phone.number('+49 ### #######'),
    geburtsdatum: faker.date.birthdate({ min: 18, max: 80, mode: 'age' }).toISOString().split('T')[0],
    adresse: {
      strasse: `${faker.helpers.arrayElement(DEUTSCHE_STRASSEN)} ${faker.number.int({ min: 1, max: 999 })}`,
      plz: faker.location.zipCode('####'),
      ort: faker.helpers.arrayElement(DEUTSCHE_STAEDTE),
    },
    familienstand: faker.helpers.arrayElement(['ledig', 'verheiratet', 'geschieden', 'verwitwet']),
    beruf: faker.person.jobTitle(),
  },
  bezirkId: overrides?.bezirkId || `bz-${faker.string.uuid()}`,
  parzelleId: overrides?.parzelleId || null,
  gewuenschteParzelle: overrides?.typ === 'parzelle_neuantrag' ? {
    groesseMin: faker.number.int({ min: 200, max: 400 }),
    groesseMax: faker.number.int({ min: 400, max: 800 }),
    mitLaube: faker.datatype.boolean({ probability: 0.7 }),
    lageWunsch: faker.helpers.arrayElement(['sonnig', 'halbschatten', 'egal']),
    besonderheiten: faker.helpers.maybe(() => faker.lorem.sentence(), { probability: 0.4 }),
  } : null,
  antragsinhalt: {
    grund: faker.lorem.sentences(2),
    details: faker.lorem.paragraph(),
    anlagen: faker.helpers.arrayElements([
      'Personalausweis',
      'Führungszeugnis',
      'Einkommensnachweis',
      'Schufa-Auskunft',
      'Vereinssatzung bestätigt',
    ], { min: 1, max: 3 }),
  },
  bearbeitung: {
    eingegangen: faker.date.past({ days: 60 }).toISOString(),
    bearbeitetVon: faker.helpers.maybe(() => faker.person.fullName(), { probability: 0.6 }),
    letzteAenderung: faker.date.recent({ days: 14 }).toISOString(),
    frist: faker.date.future({ days: 30 }).toISOString(),
    kommentare: faker.helpers.maybe(() => [
      {
        autor: faker.person.fullName(),
        datum: faker.date.recent({ days: 7 }).toISOString(),
        text: faker.lorem.sentence(),
      }
    ], { probability: 0.4 }),
  },
  dokumente: faker.helpers.arrayElements([
    {
      id: faker.string.uuid(),
      name: 'Antrag_Parzelle.pdf',
      typ: 'pdf',
      groesse: faker.number.int({ min: 100000, max: 2000000 }),
      hochgeladen: faker.date.recent({ days: 30 }).toISOString(),
    },
    {
      id: faker.string.uuid(),
      name: 'Personalausweis.jpg',
      typ: 'image',
      groesse: faker.number.int({ min: 500000, max: 3000000 }),
      hochgeladen: faker.date.recent({ days: 30 }).toISOString(),
    },
  ], { min: 0, max: 2 }),
  createdAt: faker.date.past({ days: 90 }).toISOString(),
  updatedAt: faker.date.recent({ days: 3 }).toISOString(),
  ...overrides,
})

// Generiere Testdaten-Arrays
export const bezirkeData = Array.from({ length: 12 }, () => createBezirk())
export const parzellenData = Array.from({ length: 50 }, (_, index) => 
  createParzelle({ 
    bezirkId: bezirkeData[index % bezirkeData.length].id,
    status: index % 5 === 0 ? 'frei' : 'vergeben',
  })
)
export const antraegeData = Array.from({ length: 25 }, (_, index) => 
  createAntrag({ 
    bezirkId: bezirkeData[index % bezirkeData.length].id,
    typ: index % 3 === 0 ? 'parzelle_neuantrag' : 'mitgliedschaft',
  })
)

// Factory functions für einzelne Tests
export const testDataFactories = {
  bezirk: createBezirk,
  parzelle: createParzelle,
  antrag: createAntrag,
  
  // Spezielle Fabriken für häufige Testfälle
  freeParzelle: (overrides?: any) => createParzelle({ status: 'frei', ...overrides }),
  vergebeneParzelle: (overrides?: any) => createParzelle({ status: 'vergeben', ...overrides }),
  neuerAntrag: (overrides?: any) => createAntrag({ status: 'eingereicht', ...overrides }),
  genehmigterAntrag: (overrides?: any) => createAntrag({ status: 'genehmigt', ...overrides }),
  
  // Batch-Generierung
  multipleBezirke: (count: number, overrides?: any) => 
    Array.from({ length: count }, () => createBezirk(overrides)),
  multipleParzellen: (count: number, bezirkId?: string, overrides?: any) => 
    Array.from({ length: count }, () => createParzelle({ bezirkId, ...overrides })),
  multipleAntraege: (count: number, overrides?: any) => 
    Array.from({ length: count }, () => createAntrag(overrides)),
}

// Hilfsfunktionen für Tests
export const getRandomBezirk = () => faker.helpers.arrayElement(bezirkeData)
export const getRandomParzelle = () => faker.helpers.arrayElement(parzellenData)
export const getRandomAntrag = () => faker.helpers.arrayElement(antraegeData)

export const getParzellenByBezirk = (bezirkId: string) => 
  parzellenData.filter(p => p.bezirkId === bezirkId)

export const getAntraegeByStatus = (status: string) => 
  antraegeData.filter(a => a.status === status)

// Reset-Funktion für Tests
export const resetTestData = () => {
  faker.seed(123) // Für deterministische Tests
}

// Deutsche Test-Labels und Messages
export const GERMAN_TEST_LABELS = {
  ERRORS: {
    REQUIRED_FIELD: 'Dieses Feld ist erforderlich',
    INVALID_EMAIL: 'Bitte geben Sie eine gültige E-Mail-Adresse ein',
    INVALID_PHONE: 'Bitte geben Sie eine gültige Telefonnummer ein',
    INVALID_PLZ: 'Bitte geben Sie eine gültige Postleitzahl ein',
    MIN_LENGTH: (min: number) => `Mindestens ${min} Zeichen erforderlich`,
    MAX_LENGTH: (max: number) => `Maximal ${max} Zeichen erlaubt`,
  },
  SUCCESS: {
    SAVED: 'Erfolgreich gespeichert',
    DELETED: 'Erfolgreich gelöscht',
    UPDATED: 'Erfolgreich aktualisiert',
    CREATED: 'Erfolgreich erstellt',
  },
  ACTIONS: {
    SAVE: 'Speichern',
    CANCEL: 'Abbrechen',
    DELETE: 'Löschen',
    EDIT: 'Bearbeiten',
    CREATE: 'Erstellen',
    SEARCH: 'Suchen',
    FILTER: 'Filtern',
  },
}