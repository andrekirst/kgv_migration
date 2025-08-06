import { setupServer } from 'msw/node'
import { handlers } from './handlers'

// MSW Server für Node.js Tests
export const server = setupServer(...handlers)

// Server Lifecycle Management
beforeAll(() => {
  server.listen({
    onUnhandledRequest: 'warn',
  })
  globalThis.__MSW_SERVER__ = server
})

afterEach(() => {
  server.resetHandlers()
})

afterAll(() => {
  server.close()
})