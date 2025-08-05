// PostCSS configuration for optimized CSS processing
module.exports = {
  plugins: {
    // Tailwind CSS
    tailwindcss: {},
    
    // Autoprefixer for browser compatibility
    autoprefixer: {
      // Support last 2 versions of major browsers + IE 11
      browsers: [
        'last 2 versions',
        'IE 11',
        '> 1%',
        'not dead'
      ],
      // Add prefixes for German browser usage
      grid: 'autoplace',
      flexbox: 'no-2009',
    },
    
    // CSS optimization for production
    ...(process.env.NODE_ENV === 'production' && {
      // Remove unused CSS
      '@fullhuman/postcss-purgecss': {
        content: [
          './src/**/*.{js,jsx,ts,tsx,md,mdx}',
          './src/**/*.{html,vue}',
        ],
        defaultExtractor: (content) => {
          // Broad content extraction
          const broadMatches = content.match(/[^<>"'`\s]*[^<>"'`\s:]/g) || [];
          
          // Capture classes in Tailwind's arbitrary value notation
          const innerMatches = content.match(/[^<>"'`\s.()]*[^<>"'`\s.():]/g) || [];
          
          return broadMatches.concat(innerMatches);
        },
        safelist: [
          // Always keep these classes
          'html',
          'body',
          /^bg-/,
          /^text-/,
          /^border-/,
          /^hover:/,
          /^focus:/,
          /^active:/,
          /^disabled:/,
          /^data-/,
          /^aria-/,
          // Animation classes
          /^animate-/,
          /^transition-/,
          /^duration-/,
          /^ease-/,
          // German-specific utility classes
          /^status-/,
          /^badge-/,
          /^form-/,
          /^date-de/,
          /^focus-ring/,
          // Toast notification classes
          /^toast-/,
          // React Hot Toast classes
          /^Toaster/,
          /^toast/,
        ],
      },
      
      // Optimize CSS
      'cssnano': {
        preset: ['default', {
          // Preserve German character encoding
          normalizeCharset: false,
          // Keep important comments (licenses, etc.)
          discardComments: {
            removeAll: false,
          },
          // Optimize for German content
          normalizeUnicode: false,
          // Safe optimizations only
          calc: false,
          colormin: {
            // Preserve accessibility contrast
            legacy: false,
          },
          convertValues: {
            // Don't convert German-specific units
            length: false,
          },
          mergeLonghand: false,
          mergeRules: false,
          minifyFontValues: {
            // Preserve font stack for German umlauts
            removeAfterKeyword: false,
          },
          minifyParams: false,
          normalizePositions: false,
          normalizeRepeatStyle: false,
          normalizeTimingFunctions: false,
          normalizeUrl: false,
          orderedValues: false,
          reduceIdents: false,
          reduceTransforms: false,
          svgo: {
            // Safe SVG optimizations
            plugins: [
              {
                name: 'preset-default',
                params: {
                  overrides: {
                    // Preserve accessibility attributes
                    removeViewBox: false,
                    removeTitle: false,
                    removeDesc: false,
                  },
                },
              },
            ],
          },
          uniqueSelectors: false,
        }],
      },
    }),
    
    // PostCSS Import for better development experience
    'postcss-import': {},
    
    // PostCSS Nested for Sass-like syntax
    'postcss-nested': {},
    
    // PostCSS Custom Properties for CSS variables
    'postcss-custom-properties': {
      preserve: true,
      importFrom: [
        // Import custom properties from design tokens
        './src/styles/tokens.css',
      ],
    },
    
    // PostCSS Calc for calculations
    'postcss-calc': {
      preserve: false,
      warnWhenCannotResolve: true,
      mediaQueries: true,
      selectors: true,
    },
    
    // Logical properties for better RTL support (future-proofing)
    'postcss-logical': {
      dir: 'ltr', // German is LTR
      preserve: true,
    },
    
    // Focus visible for better accessibility
    'postcss-focus-visible': {
      preserve: true,
    },
    
    // Color function support
    'postcss-color-function': {},
    
    // Media query optimization
    'postcss-sort-media-queries': {
      sort: 'mobile-first',
    },
  },
};