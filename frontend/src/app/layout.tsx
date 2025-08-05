// Root layout for KGV Frontend Application
import type { Metadata, Viewport } from 'next'
import { Inter } from 'next/font/google'
import { cn } from '@/lib/utils'
import { Providers } from './providers'
import './globals.css'

const inter = Inter({
  subsets: ['latin'],
  display: 'swap',
  variable: '--font-inter',
})

export const metadata: Metadata = {
  title: {
    default: 'KGV Verwaltung',
    template: '%s | KGV Verwaltung'
  },
  description: 'Kleingartenverein Verwaltungssystem - Moderne Lösung für die Antragsverwaltung',
  keywords: [
    'Kleingartenverein',
    'KGV',
    'Verwaltung',
    'Anträge',
    'Parzellen',
    'Deutschland'
  ],
  authors: [
    {
      name: 'KGV Development Team',
    }
  ],
  creator: 'KGV Development Team',
  publisher: 'KGV',
  formatDetection: {
    email: false,
    address: false,
    telephone: false,
  },
  metadataBase: new URL(process.env.NEXT_PUBLIC_APP_URL || 'http://localhost:3000'),
  alternates: {
    canonical: '/',
  },
  openGraph: {
    type: 'website',
    locale: 'de_DE',
    url: '/',
    title: 'KGV Verwaltung',
    description: 'Kleingartenverein Verwaltungssystem - Moderne Lösung für die Antragsverwaltung',
    siteName: 'KGV Verwaltung',
  },
  twitter: {
    card: 'summary_large_image',
    title: 'KGV Verwaltung',
    description: 'Kleingartenverein Verwaltungssystem - Moderne Lösung für die Antragsverwaltung',
  },
  robots: {
    index: false, // Internal application - don't index
    follow: false,
    noarchive: true,
    nosnippet: true,
    noimageindex: true,
    nocache: true,
  },
}

export const viewport: Viewport = {
  width: 'device-width',
  initialScale: 1,
  maximumScale: 5,
  userScalable: true,
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#0ea5e9' },
    { media: '(prefers-color-scheme: dark)', color: '#0284c7' },
  ],
  colorScheme: 'light dark',
}

interface RootLayoutProps {
  children: React.ReactNode
}

export default function RootLayout({ children }: RootLayoutProps) {
  return (
    <html 
      lang="de" 
      suppressHydrationWarning
      className={cn(
        'scroll-smooth antialiased',
        inter.variable
      )}
    >
      <head>
        {/* Preload critical resources */}
        <link rel="preload" href="/fonts/inter-var.woff2" as="font" type="font/woff2" crossOrigin="anonymous" />
        
        {/* Security headers */}
        <meta httpEquiv="X-Content-Type-Options" content="nosniff" />
        <meta httpEquiv="X-Frame-Options" content="DENY" />
        <meta httpEquiv="X-XSS-Protection" content="1; mode=block" />
        <meta httpEquiv="Referrer-Policy" content="strict-origin-when-cross-origin" />
        
        {/* Progressive Web App */}
        <meta name="application-name" content="KGV Verwaltung" />
        <meta name="apple-mobile-web-app-capable" content="yes" />
        <meta name="apple-mobile-web-app-status-bar-style" content="default" />
        <meta name="apple-mobile-web-app-title" content="KGV Verwaltung" />
        <meta name="mobile-web-app-capable" content="yes" />
        <meta name="msapplication-TileColor" content="#0ea5e9" />
        
        {/* Favicons */}
        <link rel="icon" href="/favicon.ico" sizes="any" />
        <link rel="icon" href="/icon.svg" type="image/svg+xml" />
        <link rel="apple-touch-icon" href="/apple-touch-icon.png" />
        <link rel="manifest" href="/manifest.webmanifest" />
      </head>
      <body 
        className={cn(
          'min-h-screen bg-white font-sans text-secondary-900 antialiased',
          'dark:bg-secondary-950 dark:text-secondary-50',
          inter.className
        )}
      >
        <Providers>
          {children}
        </Providers>
      </body>
    </html>
  )
}