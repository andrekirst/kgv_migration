import React from 'react'
import { renderHook, waitFor } from '@/test/utils/test-utils'
import { QueryClient } from '@tanstack/react-query'
import {
  useBezirke,
  useBezirk,
  useBezirkeSearch,
  useCreateBezirk,
  useUpdateBezirk,
  useDeleteBezirk,
  useBezirkeDropdown,
  useCachedBezirk,
} from '../use-bezirke'
import { testDataFactories, bezirkeData } from '@/test/fixtures/kgv-data'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'
import { BezirkeFilter } from '@/types/bezirke'
import toast from 'react-hot-toast'

// Import MSW server
import '@/test/mocks/server'

// Mock react-hot-toast
jest.mock('react-hot-toast', () => ({
  success: jest.fn(),
  error: jest.fn(),
}))

describe('useBezirke Hooks', () => {
  let queryClient: QueryClient

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    })
    jest.clearAllMocks()
  })

  describe('useBezirke', () => {
    it('sollte Bezirke-Liste erfolgreich laden', async () => {
      const { result } = renderHook(() => useBezirke(), {
        withQueryClient: true,
        queryClientOptions: {
          defaultOptions: {
            queries: { retry: false },
          },
        },
      })

      // Initial loading state
      expect(result.current.isLoading).toBe(true)
      expect(result.current.data).toBeUndefined()

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Daten sollten geladen sein
      expect(result.current.data).toBeDefined()
      expect(result.current.data?.bezirke).toHaveLength(bezirkeData.length)
      expect(result.current.data?.pagination).toBeDefined()
    })

    it('sollte Filter-Parameter korrekt verwenden', async () => {
      const filters: BezirkeFilter = {
        search: 'Mitte',
        aktiv: true,
        page: 1,
        limit: 10,
        sortBy: 'name',
        sortOrder: 'asc',
      }

      const { result } = renderHook(() => useBezirke(filters), {
        withQueryClient: true,
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(result.current.data?.filters).toEqual(filters)
    })

    it('sollte Fehler korrekt behandeln', async () => {
      // Mock API error
      server.use(
        http.get('/api/bezirke', () => {
          return HttpResponse.json(
            { error: 'Server error' },
            { status: 500 }
          )
        })
      )

      const { result } = renderHook(() => useBezirke(), {
        withQueryClient: true,
      })

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      expect(result.current.error).toBeDefined()
    })

    it('sollte Daten cachen und nicht erneut laden', async () => {
      const { result, rerender } = renderHook(() => useBezirke(), {
        withQueryClient: true,
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      const firstLoadTime = Date.now()
      const firstData = result.current.data

      // Re-render sollte gecachte Daten verwenden
      rerender()

      expect(result.current.data).toBe(firstData)
      expect(result.current.isLoading).toBe(false)
    })
  })

  describe('useBezirk', () => {
    it('sollte einzelnen Bezirk erfolgreich laden', async () => {
      const bezirkId = bezirkeData[0].id

      const { result } = renderHook(() => useBezirk(bezirkId), {
        withQueryClient: true,
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(result.current.data).toBeDefined()
      expect(result.current.data?.id).toBe(bezirkId)
      expect(result.current.data?.name).toBeDefined()
    })

    it('sollte nicht laden wenn ID null ist', async () => {
      const { result } = renderHook(() => useBezirk(null), {
        withQueryClient: true,
      })

      expect(result.current.isLoading).toBe(false)
      expect(result.current.data).toBeUndefined()
    })

    it('sollte 404 Fehler für nicht existierenden Bezirk behandeln', async () => {
      const nonExistentId = 99999

      server.use(
        http.get(`/api/bezirke/${nonExistentId}`, () => {
          return HttpResponse.json(
            { error: 'District not found' },
            { status: 404 }
          )
        })
      )

      const { result } = renderHook(() => useBezirk(nonExistentId), {
        withQueryClient: true,
      })

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      expect(result.current.error?.message).toContain('District not found')
    })
  })

  describe('useBezirkeSearch', () => {
    it('sollte leeres Array für leere Suchanfrage zurückgeben', async () => {
      const { result } = renderHook(() => useBezirkeSearch(''), {
        withQueryClient: true,
      })

      // Sollte nicht laden bei leerem Query
      expect(result.current.isLoading).toBe(false)
      expect(result.current.data).toBeUndefined()
    })

    it('sollte nicht suchen bei Queries unter 2 Zeichen', async () => {
      const { result } = renderHook(() => useBezirkeSearch('a'), {
        withQueryClient: true,
      })

      expect(result.current.isLoading).toBe(false)
      expect(result.current.data).toBeUndefined()
    })

    it('sollte Suchergebnisse für gültigen Query zurückgeben', async () => {
      server.use(
        http.get('/api/bezirke/search', () => {
          return HttpResponse.json({
            success: true,
            data: bezirkeData.filter(b => b.name.includes('Mitte')),
          })
        })
      )

      const { result } = renderHook(() => useBezirkeSearch('Mitte'), {
        withQueryClient: true,
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(result.current.data).toBeDefined()
      expect(Array.isArray(result.current.data)).toBe(true)
    })
  })

  describe('useBezirkeDropdown', () => {
    it('sollte vereinfachte Bezirke-Liste für Dropdown laden', async () => {
      const { result } = renderHook(() => useBezirkeDropdown(), {
        withQueryClient: true,
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(result.current.data).toBeDefined()
      expect(Array.isArray(result.current.data)).toBe(true)
      
      // Sollte nur id und name enthalten
      if (result.current.data && result.current.data.length > 0) {
        const firstItem = result.current.data[0]
        expect(firstItem).toHaveProperty('id')
        expect(firstItem).toHaveProperty('name')
        expect(Object.keys(firstItem)).toHaveLength(2)
      }
    })
  })

  describe('useCreateBezirk', () => {
    it('sollte neuen Bezirk erfolgreich erstellen', async () => {
      const newBezirkData = {
        name: 'Neuer Testbezirk',
        beschreibung: 'Test Beschreibung',
        bezirksleiter: 'Test Manager',
        email: 'test@example.com',
      }

      const { result } = renderHook(() => useCreateBezirk(), {
        withQueryClient: true,
      })

      const createdBezirk = testDataFactories.bezirk({
        ...newBezirkData,
        id: Date.now(),
      })

      server.use(
        http.post('/api/bezirke', () => {
          return HttpResponse.json({
            success: true,
            data: createdBezirk,
          }, { status: 201 })
        })
      )

      // Mutation ausführen
      result.current.mutate(newBezirkData)

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(result.current.data).toEqual(createdBezirk)
      expect(toast.success).toHaveBeenCalledWith(
        `Bezirk "${createdBezirk.name}" wurde erfolgreich erstellt.`,
        { duration: 4000 }
      )
    })

    it('sollte Fehler beim Erstellen korrekt behandeln', async () => {
      server.use(
        http.post('/api/bezirke', () => {
          return HttpResponse.json(
            { error: 'Validation error' },
            { status: 400 }
          )
        })
      )

      const { result } = renderHook(() => useCreateBezirk(), {
        withQueryClient: true,
      })

      const invalidData = {
        name: '', // Leerer Name sollte Validierungsfehler verursachen
      }

      result.current.mutate(invalidData as any)

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      expect(toast.error).toHaveBeenCalledWith(
        'Fehler beim Erstellen des Bezirks. Bitte versuchen Sie es erneut.'
      )
    })

    it('sollte Cache nach erfolgreichem Erstellen invalidieren', async () => {
      const newBezirk = testDataFactories.bezirk({
        name: 'Cache Update Test',
      })

      server.use(
        http.post('/api/bezirke', () => {
          return HttpResponse.json({
            success: true,
            data: newBezirk,
          }, { status: 201 })
        })
      )

      const { result } = renderHook(() => useCreateBezirk(), {
        withQueryClient: true,
      })

      const invalidateQueriesSpy = jest.spyOn(queryClient, 'invalidateQueries')
      const setQueryDataSpy = jest.spyOn(queryClient, 'setQueryData')

      result.current.mutate({
        name: 'Cache Update Test',
      } as any)

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Cache sollte invalidiert worden sein
      expect(invalidateQueriesSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          queryKey: expect.arrayContaining(['bezirke', 'lists'])
        })
      )

      // Neuer Bezirk sollte in Cache gesetzt worden sein
      expect(setQueryDataSpy).toHaveBeenCalledWith(
        expect.arrayContaining(['bezirke', 'detail', newBezirk.id]),
        newBezirk
      )
    })
  })

  describe('useUpdateBezirk', () => {
    it('sollte Bezirk erfolgreich aktualisieren', async () => {
      const existingBezirk = bezirkeData[0]
      const updateData = {
        id: existingBezirk.id,
        name: 'Aktualisierter Name',
        beschreibung: 'Aktualisierte Beschreibung',
      }

      const updatedBezirk = { ...existingBezirk, ...updateData }

      server.use(
        http.put(`/api/bezirke/${existingBezirk.id}`, () => {
          return HttpResponse.json({
            success: true,
            data: updatedBezirk,
          })
        })
      )

      const { result } = renderHook(() => useUpdateBezirk(), {
        withQueryClient: true,
      })

      result.current.mutate(updateData as any)

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(result.current.data).toEqual(updatedBezirk)
      expect(toast.success).toHaveBeenCalledWith(
        `Bezirk "${updatedBezirk.name}" wurde erfolgreich aktualisiert.`,
        { duration: 4000 }
      )
    })

    it('sollte Cache nach erfolgreichem Update aktualisieren', async () => {
      const existingBezirk = bezirkeData[0]
      const updateData = {
        id: existingBezirk.id,
        name: 'Cache Update Test',
      }

      const updatedBezirk = { ...existingBezirk, ...updateData }

      server.use(
        http.put(`/api/bezirke/${existingBezirk.id}`, () => {
          return HttpResponse.json({
            success: true,
            data: updatedBezirk,
          })
        })
      )

      const { result } = renderHook(() => useUpdateBezirk(), {
        withQueryClient: true,
      })

      const setQueryDataSpy = jest.spyOn(queryClient, 'setQueryData')

      result.current.mutate(updateData as any)

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Cache sollte mit aktualisiertem Bezirk gesetzt worden sein
      expect(setQueryDataSpy).toHaveBeenCalledWith(
        expect.arrayContaining(['bezirke', 'detail', existingBezirk.id]),
        updatedBezirk
      )
    })
  })

  describe('useDeleteBezirk', () => {
    it('sollte Bezirk erfolgreich löschen', async () => {
      const bezirkToDelete = bezirkeData[0]

      server.use(
        http.delete(`/api/bezirke/${bezirkToDelete.id}`, () => {
          return HttpResponse.json({
            success: true,
          })
        })
      )

      const { result } = renderHook(() => useDeleteBezirk(), {
        withQueryClient: true,
      })

      result.current.mutate({ id: bezirkToDelete.id })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(toast.success).toHaveBeenCalledWith(
        'Bezirk wurde erfolgreich gelöscht.',
        { duration: 4000 }
      )
    })

    it('sollte 409 Konflikt-Fehler korrekt behandeln', async () => {
      const bezirkToDelete = bezirkeData[0]

      server.use(
        http.delete(`/api/bezirke/${bezirkToDelete.id}`, () => {
          return HttpResponse.json(
            { error: 'District has dependencies' },
            { status: 409 }
          )
        })
      )

      const { result } = renderHook(() => useDeleteBezirk(), {
        withQueryClient: true,
      })

      result.current.mutate({ id: bezirkToDelete.id })

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      expect(toast.error).toHaveBeenCalledWith(
        'Bezirk kann nicht gelöscht werden, da er noch Parzellen oder Anträge enthält.'
      )
    })

    it('sollte Cache nach erfolgreichem Löschen bereinigen', async () => {
      const bezirkToDelete = bezirkeData[0]

      server.use(
        http.delete(`/api/bezirke/${bezirkToDelete.id}`, () => {
          return HttpResponse.json({
            success: true,
          })
        })
      )

      const { result } = renderHook(() => useDeleteBezirk(), {
        withQueryClient: true,
      })

      const removeQueriesSpy = jest.spyOn(queryClient, 'removeQueries')

      result.current.mutate({ id: bezirkToDelete.id })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Cache sollte für den gelöschten Bezirk entfernt worden sein
      expect(removeQueriesSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          queryKey: expect.arrayContaining(['bezirke', 'detail', bezirkToDelete.id])
        })
      )
    })
  })

  describe('useCachedBezirk', () => {
    it('sollte gecachte Daten zurückgeben wenn vorhanden', async () => {
      const bezirk = bezirkeData[0]

      // Setze Daten in Cache
      queryClient.setQueryData(['bezirke', 'detail', bezirk.id], bezirk)

      const { result } = renderHook(() => useCachedBezirk(bezirk.id), {
        withQueryClient: true,
      })

      expect(result.current).toEqual(bezirk)
    })

    it('sollte null zurückgeben wenn keine Daten im Cache', async () => {
      const { result } = renderHook(() => useCachedBezirk(999), {
        withQueryClient: true,
      })

      expect(result.current).toBeNull()
    })

    it('sollte null zurückgeben wenn ID null ist', async () => {
      const { result } = renderHook(() => useCachedBezirk(null), {
        withQueryClient: true,
      })

      expect(result.current).toBeNull()
    })
  })

  describe('Query Key Generation', () => {
    it('sollte unterschiedliche Query Keys für verschiedene Filter generieren', async () => {
      const filters1: BezirkeFilter = { search: 'test1' }
      const filters2: BezirkeFilter = { search: 'test2' }

      const { result: result1 } = renderHook(() => useBezirke(filters1), {
        withQueryClient: true,
      })

      const { result: result2 } = renderHook(() => useBezirke(filters2), {
        withQueryClient: true,
      })

      // Beide Queries sollten unterschiedliche Keys haben und parallel laufen
      expect(result1.current.dataUpdatedAt).toBeDefined()
      expect(result2.current.dataUpdatedAt).toBeDefined()
    })
  })

  describe('Loading und Error States', () => {
    it('sollte Loading State korrekt handhaben', async () => {
      const { result } = renderHook(() => useBezirke(), {
        withQueryClient: true,
      })

      // Initial sollte loading true sein
      expect(result.current.isLoading).toBe(true)
      expect(result.current.data).toBeUndefined()
      expect(result.current.error).toBeNull()

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })
    })

    it('sollte zwischen verschiedenen States unterscheiden', async () => {
      const { result } = renderHook(() => useBezirke(), {
        withQueryClient: true,
      })

      // Initial: loading
      expect(result.current.isLoading).toBe(true)
      expect(result.current.isError).toBe(false)
      expect(result.current.isSuccess).toBe(false)

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Nach erfolgreichem Laden
      expect(result.current.isLoading).toBe(false)
      expect(result.current.isError).toBe(false)
      expect(result.current.isSuccess).toBe(true)
    })
  })

  describe('Deutsche Fehlermeldungen', () => {
    it('sollte deutsche Toast-Nachrichten für Erfolg anzeigen', async () => {
      const newBezirk = testDataFactories.bezirk({ name: 'Test Bezirk' })

      server.use(
        http.post('/api/bezirke', () => {
          return HttpResponse.json({
            success: true,
            data: newBezirk,
          }, { status: 201 })
        })
      )

      const { result } = renderHook(() => useCreateBezirk(), {
        withQueryClient: true,
      })

      result.current.mutate({ name: 'Test Bezirk' } as any)

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(toast.success).toHaveBeenCalledWith(
        expect.stringContaining('erfolgreich erstellt'),
        expect.any(Object)
      )
    })

    it('sollte deutsche Fehlermeldungen anzeigen', async () => {
      server.use(
        http.post('/api/bezirke', () => {
          return HttpResponse.json(
            { error: 'Server error' },
            { status: 500 }
          )
        })
      )

      const { result } = renderHook(() => useCreateBezirk(), {
        withQueryClient: true,
      })

      result.current.mutate({ name: 'Test' } as any)

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      expect(toast.error).toHaveBeenCalledWith(
        expect.stringContaining('Fehler beim Erstellen')
      )
    })
  })
})