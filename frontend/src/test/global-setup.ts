import type { Config } from 'jest'

export default async function globalSetup(globalConfig: Config, projectConfig: Config) {
  console.log('🚀 Starte KGV Frontend Test Suite...')
  console.log(`📁 Test Pattern: ${globalConfig.testPathPattern || 'Alle Tests'}`)
  console.log(`🔧 Jest Version: ${require('jest/package.json').version}`)
  console.log(`⚛️  React Version: ${require('react/package.json').version}`)
  console.log(`🧪 Testing Library React Version: ${require('@testing-library/react/package.json').version}`)
  
  // Set global test start time
  globalThis.__TEST_START_TIME__ = Date.now()
  
  // Configure timezone for consistent date testing
  process.env.TZ = 'Europe/Berlin'
  
  // Set German locale for tests
  process.env.LANG = 'de_DE.UTF-8'
  process.env.LC_ALL = 'de_DE.UTF-8'
  
  console.log('✅ Global Test Setup abgeschlossen')
}