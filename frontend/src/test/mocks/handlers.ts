import { http, HttpResponse } from 'msw'
import { bezirkeData, parzellenData, antraegeData } from '../fixtures/kgv-data'

const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'

export const handlers = [
  // Bezirke API Endpoints
  http.get(`${API_BASE}/bezirke`, () => {
    return HttpResponse.json({
      data: bezirkeData,
      total: bezirkeData.length,
      page: 1,
      limit: 10,
      success: true,
    })
  }),

  http.get(`${API_BASE}/bezirke/:id`, ({ params }) => {
    const bezirk = bezirkeData.find(b => b.id === params.id)
    if (!bezirk) {
      return HttpResponse.json(
        { error: 'Bezirk nicht gefunden', success: false },
        { status: 404 }
      )
    }
    return HttpResponse.json({ data: bezirk, success: true })
  }),

  http.post(`${API_BASE}/bezirke`, async ({ request }) => {
    const newBezirk = (await request.json()) as any
    const createdBezirk = {
      id: `bz-${Date.now()}`,
      ...newBezirk,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
    
    return HttpResponse.json(
      { data: createdBezirk, success: true },
      { status: 201 }
    )
  }),

  http.put(`${API_BASE}/bezirke/:id`, async ({ params, request }) => {
    const updates = (await request.json()) as any
    const existingBezirk = bezirkeData.find(b => b.id === params.id)
    
    if (!existingBezirk) {
      return HttpResponse.json(
        { error: 'Bezirk nicht gefunden', success: false },
        { status: 404 }
      )
    }

    const updatedBezirk = {
      ...existingBezirk,
      ...updates,
      updatedAt: new Date().toISOString(),
    }

    return HttpResponse.json({ data: updatedBezirk, success: true })
  }),

  http.delete(`${API_BASE}/bezirke/:id`, ({ params }) => {
    const bezirk = bezirkeData.find(b => b.id === params.id)
    if (!bezirk) {
      return HttpResponse.json(
        { error: 'Bezirk nicht gefunden', success: false },
        { status: 404 }
      )
    }
    
    return HttpResponse.json({ success: true })
  }),

  // Parzellen API Endpoints
  http.get(`${API_BASE}/parzellen`, ({ request }) => {
    const url = new URL(request.url)
    const bezirkId = url.searchParams.get('bezirkId')
    const status = url.searchParams.get('status')
    
    let filteredParzellen = [...parzellenData]
    
    if (bezirkId) {
      filteredParzellen = filteredParzellen.filter(p => p.bezirkId === bezirkId)
    }
    
    if (status) {
      filteredParzellen = filteredParzellen.filter(p => p.status === status)
    }

    return HttpResponse.json({
      data: filteredParzellen,
      total: filteredParzellen.length,
      page: 1,
      limit: 10,
      success: true,
    })
  }),

  http.get(`${API_BASE}/parzellen/:id`, ({ params }) => {
    const parzelle = parzellenData.find(p => p.id === params.id)
    if (!parzelle) {
      return HttpResponse.json(
        { error: 'Parzelle nicht gefunden', success: false },
        { status: 404 }
      )
    }
    return HttpResponse.json({ data: parzelle, success: true })
  }),

  http.post(`${API_BASE}/parzellen`, async ({ request }) => {
    const newParzelle = (await request.json()) as any
    const createdParzelle = {
      id: `p-${Date.now()}`,
      ...newParzelle,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
    
    return HttpResponse.json(
      { data: createdParzelle, success: true },
      { status: 201 }
    )
  }),

  http.put(`${API_BASE}/parzellen/:id`, async ({ params, request }) => {
    const updates = (await request.json()) as any
    const existingParzelle = parzellenData.find(p => p.id === params.id)
    
    if (!existingParzelle) {
      return HttpResponse.json(
        { error: 'Parzelle nicht gefunden', success: false },
        { status: 404 }
      )
    }

    const updatedParzelle = {
      ...existingParzelle,
      ...updates,
      updatedAt: new Date().toISOString(),
    }

    return HttpResponse.json({ data: updatedParzelle, success: true })
  }),

  // Anträge API Endpoints
  http.get(`${API_BASE}/antraege`, ({ request }) => {
    const url = new URL(request.url)
    const status = url.searchParams.get('status')
    const bezirkId = url.searchParams.get('bezirkId')
    
    let filteredAntraege = [...antraegeData]
    
    if (status) {
      filteredAntraege = filteredAntraege.filter(a => a.status === status)
    }
    
    if (bezirkId) {
      filteredAntraege = filteredAntraege.filter(a => a.bezirkId === bezirkId)
    }

    return HttpResponse.json({
      data: filteredAntraege,
      total: filteredAntraege.length,
      page: 1,
      limit: 10,
      success: true,
    })
  }),

  http.get(`${API_BASE}/antraege/:id`, ({ params }) => {
    const antrag = antraegeData.find(a => a.id === params.id)
    if (!antrag) {
      return HttpResponse.json(
        { error: 'Antrag nicht gefunden', success: false },
        { status: 404 }
      )
    }
    return HttpResponse.json({ data: antrag, success: true })
  }),

  http.post(`${API_BASE}/antraege`, async ({ request }) => {
    const newAntrag = (await request.json()) as any
    const createdAntrag = {
      id: `a-${Date.now()}`,
      ...newAntrag,
      status: 'eingereicht',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
    
    return HttpResponse.json(
      { data: createdAntrag, success: true },
      { status: 201 }
    )
  }),

  // Error Scenarios für Tests
  http.get(`${API_BASE}/error/500`, () => {
    return HttpResponse.json(
      { error: 'Interner Serverfehler', success: false },
      { status: 500 }
    )
  }),

  http.get(`${API_BASE}/error/timeout`, () => {
    return new Promise(() => {
      // Simuliert Timeout - Promise wird nie resolved
    })
  }),

  http.get(`${API_BASE}/error/network`, () => {
    return HttpResponse.error()
  }),

  // Health Check Endpoints
  http.get(`${API_BASE}/health`, () => {
    return HttpResponse.json({
      status: 'ok',
      timestamp: new Date().toISOString(),
      services: {
        database: 'healthy',
        cache: 'healthy',
      },
    })
  }),
]