/**
 * Page Object Models Index
 * 
 * Centralized exports for all page object models
 */

export { BasePage } from './base-page'
export { DashboardPage } from './dashboard-page'
export { BezirkeOverviewPage } from './bezirke-overview-page'
export { BezirkeCreationPage, type BezirkFormData } from './bezirke-creation-page'

// Re-export common types
export type { Page, Locator } from '@playwright/test'