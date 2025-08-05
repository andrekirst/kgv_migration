// API Client for KGV Frontend Application
import { ApiError, ApiResponse } from '@/types/api'

interface RequestConfig extends RequestInit {
  timeout?: number
}

class ApiClient {
  private baseURL: string
  private defaultTimeout: number

  constructor(baseURL: string = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api') {
    this.baseURL = baseURL.replace(/\/$/, '') // Remove trailing slash
    this.defaultTimeout = 30000 // 30 seconds
  }

  private async request<T>(
    endpoint: string,
    config: RequestConfig = {}
  ): Promise<ApiResponse<T>> {
    const { timeout = this.defaultTimeout, ...fetchConfig } = config
    
    const url = `${this.baseURL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`
    
    // Get auth token from localStorage
    const token = typeof window !== 'undefined' ? localStorage.getItem('auth_token') : null
    
    const defaultHeaders: HeadersInit = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      'Accept-Language': 'de-DE,de;q=0.9',
    }

    if (token) {
      defaultHeaders.Authorization = `Bearer ${token}`
    }

    const requestConfig: RequestInit = {
      ...fetchConfig,
      headers: {
        ...defaultHeaders,
        ...fetchConfig.headers,
      },
    }

    try {
      // Create timeout promise
      const timeoutPromise = new Promise<never>((_, reject) => {
        setTimeout(() => reject(new Error('Request timeout')), timeout)
      })

      // Make the request with timeout
      const response = await Promise.race([
        fetch(url, requestConfig),
        timeoutPromise
      ])

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        const error: ApiError = {
          message: errorData.message || `HTTP Error: ${response.status} ${response.statusText}`,
          status: response.status,
          details: errorData.errors || []
        }
        throw error
      }

      // Handle 204 No Content
      if (response.status === 204) {
        return {
          data: null as T,
          success: true
        }
      }

      const data = await response.json()
      return {
        data,
        success: true,
        message: data.message
      }
    } catch (error) {
      if (error instanceof Error) {
        if (error.message === 'Request timeout') {
          throw {
            message: 'Die Anfrage dauerte zu lange. Bitte versuchen Sie es erneut.',
            status: 408,
            details: []
          } as ApiError
        }
        
        if (error.name === 'TypeError' && error.message.includes('fetch')) {
          throw {
            message: 'Netzwerkfehler. Bitte überprüfen Sie Ihre Internetverbindung.',
            status: 0,
            details: []
          } as ApiError
        }
      }
      
      throw error
    }
  }

  // GET request
  async get<T>(endpoint: string, config: RequestConfig = {}): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { ...config, method: 'GET' })
  }

  // POST request
  async post<T>(endpoint: string, data?: unknown, config: RequestConfig = {}): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...config,
      method: 'POST',
      body: data ? JSON.stringify(data) : null,
    })
  }

  // PUT request
  async put<T>(endpoint: string, data?: unknown, config: RequestConfig = {}): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...config,
      method: 'PUT',
      body: data ? JSON.stringify(data) : null,
    })
  }

  // PATCH request
  async patch<T>(endpoint: string, data?: unknown, config: RequestConfig = {}): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...config,
      method: 'PATCH',
      body: data ? JSON.stringify(data) : null,
    })
  }

  // DELETE request
  async delete<T>(endpoint: string, config: RequestConfig = {}): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { ...config, method: 'DELETE' })
  }

  // Upload file
  async upload<T>(endpoint: string, file: File, config: RequestConfig = {}): Promise<ApiResponse<T>> {
    const formData = new FormData()
    formData.append('file', file)

    const token = typeof window !== 'undefined' ? localStorage.getItem('auth_token') : null
    const headers: HeadersInit = {}

    if (token) {
      headers.Authorization = `Bearer ${token}`
    }

    return this.request<T>(endpoint, {
      ...config,
      method: 'POST',
      headers: {
        ...headers,
        ...config.headers,
      },
      body: formData,
    })
  }

  // Download file
  async download(endpoint: string, config: RequestConfig = {}): Promise<Blob> {
    const { timeout = this.defaultTimeout, ...fetchConfig } = config
    const url = `${this.baseURL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`
    
    const token = typeof window !== 'undefined' ? localStorage.getItem('auth_token') : null
    const headers: HeadersInit = {
      'Accept': 'application/octet-stream',
    }

    if (token) {
      headers.Authorization = `Bearer ${token}`
    }

    const requestConfig: RequestInit = {
      ...fetchConfig,
      headers: {
        ...headers,
        ...fetchConfig.headers,
      },
    }

    try {
      const timeoutPromise = new Promise<never>((_, reject) => {
        setTimeout(() => reject(new Error('Request timeout')), timeout)
      })

      const response = await Promise.race([
        fetch(url, requestConfig),
        timeoutPromise
      ])

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        const error: ApiError = {
          message: errorData.message || `HTTP Error: ${response.status} ${response.statusText}`,
          status: response.status,
          details: errorData.errors || []
        }
        throw error
      }

      return await response.blob()
    } catch (error) {
      if (error instanceof Error && error.message === 'Request timeout') {
        throw {
          message: 'Download dauerte zu lange. Bitte versuchen Sie es erneut.',
          status: 408,
          details: []
        } as ApiError
      }
      throw error
    }
  }

  // Set auth token
  setAuthToken(token: string): void {
    if (typeof window !== 'undefined') {
      localStorage.setItem('auth_token', token)
    }
  }

  // Clear auth token
  clearAuthToken(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('auth_token')
    }
  }

  // Get current auth token
  getAuthToken(): string | null {
    if (typeof window !== 'undefined') {
      return localStorage.getItem('auth_token')
    }
    return null
  }

  // Check if user is authenticated
  isAuthenticated(): boolean {
    return !!this.getAuthToken()
  }
}

// Create singleton instance
export const apiClient = new ApiClient()

// Export the class for testing or custom instances
export { ApiClient }