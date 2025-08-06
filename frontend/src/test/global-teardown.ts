import type { Config } from 'jest'

export default async function globalTeardown(globalConfig: Config, projectConfig: Config) {
  const testStartTime = globalThis.__TEST_START_TIME__
  const testEndTime = Date.now()
  const testDuration = testEndTime - testStartTime
  
  console.log('\n🏁 Test Suite abgeschlossen')
  console.log(`⏱️  Gesamtdauer: ${(testDuration / 1000).toFixed(2)}s`)
  console.log(`📊 Tests ausgeführt: ${globalConfig.testPathPattern || 'Alle'}`)
  
  // Cleanup global resources if needed
  if (globalThis.__MSW_SERVER__) {
    console.log('🧹 MSW Server wird gestoppt...')
    globalThis.__MSW_SERVER__.close()
  }
  
  console.log('✅ Global Teardown abgeschlossen')
}