// Readiness check endpoint for Kubernetes
import { NextResponse } from 'next/server'

export async function GET() {
  try {
    // Check if application is ready to serve traffic
    // This can include checks for:
    // - Database connectivity
    // - External API availability
    // - Required environment variables
    // - File system access
    
    const checks = []
    
    // Check environment variables
    const requiredEnvVars = ['NEXT_PUBLIC_API_URL']
    for (const envVar of requiredEnvVars) {
      if (!process.env[envVar]) {
        checks.push({
          name: `env_${envVar}`,
          status: 'fail',
          message: `Required environment variable ${envVar} is not set`
        })
      } else {
        checks.push({
          name: `env_${envVar}`,
          status: 'pass',
          message: `Environment variable ${envVar} is set`
        })
      }
    }
    
    // Check if API URL is reachable (optional - don't fail readiness)
    try {
      const apiUrl = process.env.NEXT_PUBLIC_API_URL
      if (apiUrl) {
        // Note: In a real implementation, you might want to make a quick API call
        checks.push({
          name: 'api_url_configured',
          status: 'pass',
          message: 'API URL is configured'
        })
      }
    } catch (error) {
      checks.push({
        name: 'api_connectivity',
        status: 'warn',
        message: 'API connectivity check failed but not critical for readiness'
      })
    }
    
    // Determine overall status
    const failedChecks = checks.filter(check => check.status === 'fail')
    const isReady = failedChecks.length === 0
    
    const readinessData = {
      status: isReady ? 'ready' : 'not-ready',
      timestamp: new Date().toISOString(),
      checks,
      summary: {
        total: checks.length,
        passed: checks.filter(c => c.status === 'pass').length,
        failed: failedChecks.length,
        warnings: checks.filter(c => c.status === 'warn').length,
      }
    }
    
    return NextResponse.json(readinessData, { 
      status: isReady ? 200 : 503,
      headers: {
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        'Pragma': 'no-cache',
        'Expires': '0',
      }
    })
  } catch (error) {
    console.error('Readiness check failed:', error)
    
    return NextResponse.json(
      { 
        status: 'not-ready',
        timestamp: new Date().toISOString(),
        error: error instanceof Error ? error.message : 'Unknown error',
        checks: []
      },
      { 
        status: 503,
        headers: {
          'Cache-Control': 'no-cache, no-store, must-revalidate',
          'Pragma': 'no-cache',
          'Expires': '0',
        }
      }
    )
  }
}