// Lighthouse CI configuration for performance monitoring
module.exports = {
  ci: {
    collect: {
      startServerCommand: 'npm start',
      startServerReadyPattern: 'ready',
      url: [
        'http://localhost:3000',
        'http://localhost:3000/antraege',
        'http://localhost:3000/dashboard',
      ],
      numberOfRuns: 3,
      settings: {
        preset: 'desktop',
        chromeFlags: '--no-sandbox --disable-dev-shm-usage',
        onlyCategories: ['performance', 'accessibility', 'best-practices', 'seo'],
      },
    },
    assert: {
      assertions: {
        // Performance thresholds for German government standards
        'categories:performance': ['error', {minScore: 0.9}],
        'categories:accessibility': ['error', {minScore: 0.9}],
        'categories:best-practices': ['error', {minScore: 0.9}],
        'categories:seo': ['error', {minScore: 0.9}],
        
        // Core Web Vitals for German users
        'first-contentful-paint': ['error', {maxNumericValue: 2000}],
        'largest-contentful-paint': ['error', {maxNumericValue: 2500}],
        'first-input-delay': ['error', {maxNumericValue: 100}],
        'cumulative-layout-shift': ['error', {maxNumericValue: 0.1}],
        
        // Network performance for German infrastructure
        'speed-index': ['error', {maxNumericValue: 3000}],
        'total-blocking-time': ['error', {maxNumericValue: 300}],
        
        // Accessibility for German standards
        'color-contrast': 'error',
        'document-title': 'error',
        'html-has-lang': 'error',
        'image-alt': 'error',
        'label': 'error',
        'link-name': 'error',
        
        // Best practices
        'uses-https': 'error',
        'uses-http2': 'warn',
        'no-vulnerable-libraries': 'error',
        
        // SEO (for documentation/help pages)
        'meta-description': 'warn',
        'viewport': 'error',
      },
    },
    upload: {
      target: 'temporary-public-storage',
    },
    server: {
      port: 9009,
      storage: {
        storageMethod: 'sql',
        sqlDialect: 'sqlite',
        sqlDatabasePath: './lighthouse-ci.db',
      },
    },
  },
};