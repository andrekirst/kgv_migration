/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/pages/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
    './src/lib/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  darkMode: 'class', // Enable dark mode with class strategy
  theme: {
    extend: {
      // Custom Color Palette für KGV Frank
      colors: {
        // Primary Colors - Verwaltungsblau
        primary: {
          50: '#eff6ff',
          100: '#dbeafe', 
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6', // Hauptfarbe
          600: '#2563eb',
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e293b',
          950: '#0f172a',
        },
        
        // Secondary Colors - Akzentgrün  
        secondary: {
          50: '#f0fdf4',
          100: '#dcfce7',
          200: '#bbf7d0', 
          300: '#86efac',
          400: '#4ade80',
          500: '#22c55e', // Hauptakzent
          600: '#16a34a',
          700: '#15803d',
          800: '#166534',
          900: '#14532d',
          950: '#052e16',
        },
        
        // Status Colors
        success: {
          50: '#ecfdf5',
          100: '#d1fae5',
          500: '#10b981',
          600: '#059669',
          700: '#047857',
        },
        warning: {
          50: '#fffbeb',
          100: '#fef3c7',
          500: '#f59e0b', 
          600: '#d97706',
          700: '#b45309',
        },
        error: {
          50: '#fef2f2',
          100: '#fee2e2',
          500: '#ef4444',
          600: '#dc2626', 
          700: '#b91c1c',
        },
        info: {
          50: '#eff6ff',
          100: '#dbeafe',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
        },
        
        // Enhanced Gray Scale
        gray: {
          25: '#fcfcfd',
          50: '#f9fafb',
          100: '#f3f4f6',
          200: '#e5e7eb',
          300: '#d1d5db',
          400: '#9ca3af',
          500: '#6b7280',
          600: '#4b5563',
          700: '#374151',
          800: '#1f2937',
          900: '#111827',
          950: '#030712',
        },
        
        // Application Specific Colors
        sidebar: {
          bg: '#1e293b',
          hover: '#334155',
          active: '#3b82f6',
          text: '#e2e8f0',
        },
        
        table: {
          header: '#f8fafc',
          border: '#e2e8f0',
          hover: '#f1f5f9',
          selected: '#dbeafe',
        },
        
        form: {
          bg: '#ffffff',
          border: '#d1d5db',
          focus: '#3b82f6',
          error: '#ef4444',
          disabled: '#f3f4f6',
        }
      },
      
      // Typography System
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'Consolas', 'monospace'],
      },
      
      fontSize: {
        // Responsive Typography
        'xs': ['0.75rem', { lineHeight: '1rem' }],        // 12px
        'sm': ['0.875rem', { lineHeight: '1.25rem' }],    // 14px
        'base': ['1rem', { lineHeight: '1.5rem' }],       // 16px
        'lg': ['1.125rem', { lineHeight: '1.75rem' }],    // 18px
        'xl': ['1.25rem', { lineHeight: '1.75rem' }],     // 20px
        '2xl': ['1.5rem', { lineHeight: '2rem' }],        // 24px
        '3xl': ['1.875rem', { lineHeight: '2.25rem' }],   // 30px
        '4xl': ['2.25rem', { lineHeight: '2.5rem' }],     // 36px
        '5xl': ['3rem', { lineHeight: '1' }],             // 48px
        '6xl': ['3.75rem', { lineHeight: '1' }],          // 60px
        
        // Application Specific Sizes
        'button-sm': ['0.875rem', { lineHeight: '1.25rem', fontWeight: '500' }],
        'button-md': ['1rem', { lineHeight: '1.5rem', fontWeight: '500' }],
        'button-lg': ['1.125rem', { lineHeight: '1.75rem', fontWeight: '500' }],
        'table-header': ['0.75rem', { lineHeight: '1rem', fontWeight: '600', letterSpacing: '0.05em', textTransform: 'uppercase' }],
        'form-label': ['0.875rem', { lineHeight: '1.25rem', fontWeight: '500' }],
      },
      
      // Spacing System
      spacing: {
        '18': '4.5rem',   // 72px
        '88': '22rem',    // 352px
        '128': '32rem',   // 512px
        
        // Application Specific Spacing
        'sidebar-width': '16rem',      // 256px
        'sidebar-collapsed': '4rem',   // 64px
        'header-height': '4rem',       // 64px
        'form-gap': '1.5rem',         // 24px
        'card-padding': '1.5rem',     // 24px
      },
      
      // Border Radius System
      borderRadius: {
        'none': '0',
        'sm': '0.125rem',    // 2px
        DEFAULT: '0.25rem',  // 4px  
        'md': '0.375rem',    // 6px
        'lg': '0.5rem',      // 8px
        'xl': '0.75rem',     // 12px
        '2xl': '1rem',       // 16px
        '3xl': '1.5rem',     // 24px
        'full': '9999px',
        
        // Component Specific
        'button': '0.375rem',  // 6px
        'card': '0.5rem',      // 8px
        'input': '0.375rem',   // 6px
      },
      
      // Box Shadow System
      boxShadow: {
        'xs': '0 1px 2px 0 rgb(0 0 0 / 0.05)',
        'sm': '0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)',
        DEFAULT: '0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)',
        'md': '0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)',
        'lg': '0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)',
        'xl': '0 25px 50px -12px rgb(0 0 0 / 0.25)',
        '2xl': '0 50px 100px -20px rgb(0 0 0 / 0.25)',
        'inner': 'inset 0 2px 4px 0 rgb(0 0 0 / 0.05)',
        
        // Component Specific Shadows
        'card': '0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)',
        'button': '0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)',
        'modal': '0 25px 50px -12px rgb(0 0 0 / 0.25)',
        'dropdown': '0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)',
      },
      
      // Screen Breakpoints
      screens: {
        'xs': '475px',      // Extra Small - Kleine Handys
        'sm': '640px',      // Small - Standard Handys
        'md': '768px',      // Medium - Tablets
        'lg': '1024px',     // Large - Small Desktops
        'xl': '1280px',     // Extra Large - Standard Desktops  
        '2xl': '1536px',    // 2X Large - Große Monitore
        
        // Custom Application Breakpoints
        'mobile': '640px',
        'tablet': '768px', 
        'desktop': '1024px',
        'wide': '1440px',
        
        // Max-width breakpoints
        'max-sm': {'max': '639px'},
        'max-md': {'max': '767px'},
        'max-lg': {'max': '1023px'},
        'max-xl': {'max': '1279px'},
      },
      
      // Animation & Transitions
      transitionDuration: {
        '150': '150ms',
        '200': '200ms',
        '250': '250ms',
        '300': '300ms',
        '400': '400ms',
      },
      
      transitionTimingFunction: {
        'smooth': 'cubic-bezier(0.4, 0, 0.2, 1)',
        'bounce-in': 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
      },
      
      // Z-Index Scale
      zIndex: {
        '1': '1',
        '10': '10',
        '20': '20',
        '30': '30',
        '40': '40',
        '50': '50',
        
        // Application Layers
        'dropdown': '1000',
        'sticky': '1020',
        'fixed': '1030',
        'modal-backdrop': '1040',
        'modal': '1050',
        'popover': '1060',
        'tooltip': '1070',
        'toast': '1080',
      },
      
      // Custom Utilities
      backdropBlur: {
        'xs': '2px',
        'sm': '4px',
        'md': '8px',
        'lg': '12px',
        'xl': '16px',
        '2xl': '24px',
        '3xl': '40px',
      },
      
      // Grid System
      gridTemplateColumns: {
        'auto-fit-200': 'repeat(auto-fit, minmax(200px, 1fr))',
        'auto-fit-250': 'repeat(auto-fit, minmax(250px, 1fr))',
        'auto-fit-300': 'repeat(auto-fit, minmax(300px, 1fr))',
        'sidebar-content': '16rem 1fr',
        'sidebar-collapsed-content': '4rem 1fr',
        'form-2': 'repeat(2, minmax(0, 1fr))',
        'form-3': 'repeat(3, minmax(0, 1fr))',
        'table-actions': '1fr auto',
      },
      
      // Custom Aspect Ratios
      aspectRatio: {
        'card': '16 / 10',
        'chart': '16 / 9',
      },
    },
  },
  
  plugins: [
    // Official Tailwind Plugins
    require('@tailwindcss/forms')({
      strategy: 'class', // Use class-based strategy for forms
    }),
    require('@tailwindcss/typography'),
    require('@tailwindcss/aspect-ratio'),
    require('@tailwindcss/container-queries'),
    
    // Custom Plugin für Application-spezifische Utilities
    function({ addUtilities, addComponents, theme }) {
      // Custom Utilities
      addUtilities({
        // Focus Utilities for Accessibility
        '.focus-ring': {
          '&:focus-visible': {
            outline: `2px solid ${theme('colors.primary.500')}`,
            outlineOffset: '2px',
          },
        },
        
        '.focus-ring-inset': {
          '&:focus-visible': {
            outline: `2px solid ${theme('colors.primary.500')}`,
            outlineOffset: '-2px',
          },
        },
        
        // Safe Area Utilities (for mobile)
        '.pb-safe': {
          paddingBottom: 'env(safe-area-inset-bottom)',
        },
        '.pt-safe': {
          paddingTop: 'env(safe-area-inset-top)',
        },
        
        // Scrollbar Styling
        '.scrollbar-thin': {
          scrollbarWidth: 'thin',
          '&::-webkit-scrollbar': {
            width: '6px',
            height: '6px',
          },
          '&::-webkit-scrollbar-track': {
            backgroundColor: theme('colors.gray.100'),
          },
          '&::-webkit-scrollbar-thumb': {
            backgroundColor: theme('colors.gray.400'),
            borderRadius: '3px',
          },
          '&::-webkit-scrollbar-thumb:hover': {
            backgroundColor: theme('colors.gray.500'),
          },
        },
        
        // Loading Animation
        '.animate-pulse-slow': {
          animation: 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        },
        
        // Skeleton Loading
        '.skeleton': {
          background: `linear-gradient(90deg, ${theme('colors.gray.200')} 25%, ${theme('colors.gray.300')} 50%, ${theme('colors.gray.200')} 75%)`,
          backgroundSize: '200% 100%',
          animation: 'skeleton-loading 1.5s infinite',
        },
      })
      
      // Custom Components
      addComponents({
        // Button Components
        '.btn': {
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          borderRadius: theme('borderRadius.button'),
          fontWeight: theme('fontWeight.medium'),
          transition: 'all 0.2s ease-in-out',
          cursor: 'pointer',
          textDecoration: 'none',
          border: '1px solid transparent',
          
          '&:disabled': {
            opacity: '0.5',
            cursor: 'not-allowed',
          },
          
          '&:focus-visible': {
            outline: `2px solid ${theme('colors.primary.500')}`,
            outlineOffset: '2px',
          },
        },
        
        '.btn-sm': {
          padding: `${theme('spacing.2')} ${theme('spacing.3')}`,
          fontSize: theme('fontSize.sm[0]'),
          lineHeight: theme('fontSize.sm[1].lineHeight'),
          minHeight: '2.25rem', // 36px for touch
        },
        
        '.btn-md': {
          padding: `${theme('spacing.3')} ${theme('spacing.4')}`,
          fontSize: theme('fontSize.base[0]'),
          lineHeight: theme('fontSize.base[1].lineHeight'),
          minHeight: '2.5rem', // 40px
        },
        
        '.btn-lg': {
          padding: `${theme('spacing.4')} ${theme('spacing.6')}`,
          fontSize: theme('fontSize.lg[0]'),
          lineHeight: theme('fontSize.lg[1].lineHeight'),
          minHeight: '3rem', // 48px
        },
        
        '.btn-primary': {
          backgroundColor: theme('colors.primary.600'),
          color: theme('colors.white'),
          
          '&:hover:not(:disabled)': {
            backgroundColor: theme('colors.primary.700'),
          },
          
          '&:active:not(:disabled)': {
            backgroundColor: theme('colors.primary.800'),
          },
        },
        
        '.btn-secondary': {
          backgroundColor: theme('colors.white'),
          color: theme('colors.gray.700'),
          borderColor: theme('colors.gray.300'),
          
          '&:hover:not(:disabled)': {
            backgroundColor: theme('colors.gray.50'),
            borderColor: theme('colors.gray.400'),
          },
        },
        
        '.btn-outline': {
          backgroundColor: 'transparent',
          color: theme('colors.primary.600'),
          borderColor: theme('colors.primary.600'),
          
          '&:hover:not(:disabled)': {
            backgroundColor: theme('colors.primary.50'),
          },
        },
        
        '.btn-ghost': {
          backgroundColor: 'transparent',
          color: theme('colors.gray.700'),
          
          '&:hover:not(:disabled)': {
            backgroundColor: theme('colors.gray.100'),
          },
        },
        
        // Form Components
        '.form-input': {
          width: '100%',
          padding: `${theme('spacing.3')} ${theme('spacing.3')}`,
          fontSize: theme('fontSize.base[0]'),
          lineHeight: theme('fontSize.base[1].lineHeight'),
          border: `1px solid ${theme('colors.form.border')}`,
          borderRadius: theme('borderRadius.input'),
          backgroundColor: theme('colors.form.bg'),
          minHeight: '2.75rem', // 44px for accessibility
          
          '&:focus': {
            outline: 'none',
            borderColor: theme('colors.form.focus'),
            boxShadow: `0 0 0 3px ${theme('colors.primary.100')}`,
          },
          
          '&:disabled': {
            backgroundColor: theme('colors.form.disabled'),
            cursor: 'not-allowed',
          },
          
          '&[aria-invalid="true"]': {
            borderColor: theme('colors.form.error'),
          },
        },
        
        '.form-select': {
          backgroundImage: `url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='m6 8 4 4 4-4'/%3e%3c/svg%3e")`,
          backgroundPosition: 'right 0.75rem center',
          backgroundRepeat: 'no-repeat',
          backgroundSize: '1rem',
          paddingRight: '2.5rem',
        },
        
        // Card Components
        '.card': {
          backgroundColor: theme('colors.white'),
          borderRadius: theme('borderRadius.card'),
          boxShadow: theme('boxShadow.card'),
          padding: theme('spacing.card-padding'),
        },
        
        '.card-header': {
          marginBottom: theme('spacing.4'),
          paddingBottom: theme('spacing.4'),
          borderBottom: `1px solid ${theme('colors.gray.200')}`,
        },
        
        '.card-title': {
          fontSize: theme('fontSize.lg[0]'),
          lineHeight: theme('fontSize.lg[1].lineHeight'),
          fontWeight: theme('fontWeight.semibold'),
          color: theme('colors.gray.900'),
        },
        
        '.card-description': {
          fontSize: theme('fontSize.sm[0]'),
          lineHeight: theme('fontSize.sm[1].lineHeight'),
          color: theme('colors.gray.600'),
          marginTop: theme('spacing.1'),
        },
        
        // Table Components
        '.table-responsive': {
          overflowX: 'auto',
          borderRadius: theme('borderRadius.lg'),
          border: `1px solid ${theme('colors.table.border')}`,
        },
        
        '.table': {
          width: '100%',
          borderCollapse: 'collapse',
        },
        
        '.table th': {
          backgroundColor: theme('colors.table.header'),
          padding: `${theme('spacing.3')} ${theme('spacing.6')}`,
          fontSize: theme('fontSize.table-header[0]'),
          fontWeight: theme('fontSize.table-header[1].fontWeight'),
          color: theme('colors.gray.500'),
          textAlign: 'left',
          letterSpacing: theme('fontSize.table-header[1].letterSpacing'),
          textTransform: theme('fontSize.table-header[1].textTransform'),
        },
        
        '.table td': {
          padding: `${theme('spacing.4')} ${theme('spacing.6')}`,
          borderTop: `1px solid ${theme('colors.table.border')}`,
        },
        
        '.table tbody tr:hover': {
          backgroundColor: theme('colors.table.hover'),
        },
        
        '.table tbody tr[aria-selected="true"]': {
          backgroundColor: theme('colors.table.selected'),
        },
      })
      
      // Add keyframe animations
      addUtilities({
        '@keyframes skeleton-loading': {
          '0%': { backgroundPosition: '200% 0' },
          '100%': { backgroundPosition: '-200% 0' },
        },
      })
    },
  ],
}