// Enhanced API Client for KGV Frontend Application with Axios
import axios, { AxiosInstance, AxiosResponse, AxiosError, AxiosRequestConfig } from 'axios'
import { ApiError, ApiResponse } from '@/types/api'
import toast from 'react-hot-toast'

// German error messages for better UX
const GERMAN_ERROR_MESSAGES = {
  NETWORK_ERROR: 'Netzwerkfehler. Bitte überprüfen Sie Ihre Internetverbindung.',
  TIMEOUT_ERROR: 'Die Anfrage dauerte zu lange. Bitte versuchen Sie es erneut.',
  SERVER_ERROR: 'Serverfehler. Bitte versuchen Sie es später erneut.',
  UNAUTHORIZED: 'Sie sind nicht berechtigt, diese Aktion durchzuführen.',
  FORBIDDEN: 'Zugriff verweigert. Unzureichende Berechtigung.',
  NOT_FOUND: 'Die angeforderte Ressource wurde nicht gefunden.',
  VALIDATION_ERROR: 'Validierungsfehler. Bitte überprüfen Sie Ihre Eingaben.',
  CONFLICT: 'Konflikt. Die Ressource existiert bereits oder wird verwendet.',
  TOO_MANY_REQUESTS: 'Zu viele Anfragen. Bitte warten Sie einen Moment.',
  BAD_REQUEST: 'Ungültige Anfrage. Bitte überprüfen Sie Ihre Daten.'
} as const

interface RequestConfig extends AxiosRequestConfig {
  skipErrorToast?: boolean
}

class ApiClient {
  private axiosInstance: AxiosInstance
  private baseURL: string

  constructor(baseURL: string = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api') {
    this.baseURL = baseURL.replace(/\/$/, '') // Remove trailing slash
    
    // Create axios instance with default configuration
    this.axiosInstance = axios.create({
      baseURL: this.baseURL,
      timeout: 30000, // 30 seconds
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'Accept-Language': 'de-DE,de;q=0.9,en;q=0.1'
      },
      withCredentials: true // For cookies if needed
    })

    this.setupInterceptors()
  }

  private setupInterceptors(): void {
    // Request interceptor for auth token
    this.axiosInstance.interceptors.request.use(
      (config) => {
        // Add auth token if available
        const token = typeof window !== 'undefined' ? localStorage.getItem('auth_token') : null
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
        return config
      },
      (error) => {
        console.error('Request interceptor error:', error)
        return Promise.reject(error)
      }
    )

    // Response interceptor for error handling
    this.axiosInstance.interceptors.response.use(
      (response: AxiosResponse) => {
        // Success response - return as is
        return response
      },
      (error: AxiosError) => {
        const apiError = this.handleAxiosError(error)
        
        // Show toast notification for errors (unless explicitly disabled)
        if (!error.config?.skipErrorToast) {
          this.showErrorToast(apiError)
        }
        
        return Promise.reject(apiError)
      }
    )
  }

  private handleAxiosError(error: AxiosError): ApiError {
    if (error.code === 'ECONNABORTED' || error.message.includes('timeout')) {
      return {
        message: GERMAN_ERROR_MESSAGES.TIMEOUT_ERROR,
        status: 408,
        details: ['Zeitüberschreitung der Anfrage']
      }
    }

    if (!error.response) {
      return {
        message: GERMAN_ERROR_MESSAGES.NETWORK_ERROR,
        status: 0,
        details: ['Keine Verbindung zum Server']
      }
    }

    const status = error.response.status
    const responseData = error.response.data as any

    let message: string
    let details: string[] = []

    // Handle different HTTP status codes with German messages
    switch (status) {
      case 400:
        message = responseData?.message || GERMAN_ERROR_MESSAGES.BAD_REQUEST
        details = responseData?.errors || responseData?.details || []
        break
      case 401:
        message = GERMAN_ERROR_MESSAGES.UNAUTHORIZED
        // Clear auth token on unauthorized
        this.clearAuthToken()
        break
      case 403:
        message = GERMAN_ERROR_MESSAGES.FORBIDDEN
        break
      case 404:
        message = GERMAN_ERROR_MESSAGES.NOT_FOUND
        break
      case 409:
        message = responseData?.message || GERMAN_ERROR_MESSAGES.CONFLICT
        details = responseData?.errors || responseData?.details || []
        break
      case 422:
        message = responseData?.message || GERMAN_ERROR_MESSAGES.VALIDATION_ERROR
        details = responseData?.errors || responseData?.details || []
        break
      case 429:
        message = GERMAN_ERROR_MESSAGES.TOO_MANY_REQUESTS
        break
      case 500:
      case 502:
      case 503:
      case 504:
        message = responseData?.message || GERMAN_ERROR_MESSAGES.SERVER_ERROR
        break
      default:
        message = responseData?.message || `HTTP Fehler: ${status}`
        details = responseData?.errors || responseData?.details || []
    }

    return {
      message,
      status,
      details
    }
  }

  private showErrorToast(error: ApiError): void {
    // Don't show toast for certain status codes in development
    if (process.env.NODE_ENV === 'development' && [401, 403].includes(error.status)) {
      console.warn('API Error:', error)
      return
    }

    toast.error(error.message, {
      duration: error.status >= 500 ? 8000 : 6000,
      position: 'top-right'
    })
  }

  private async request<T>(
    endpoint: string,
    config: RequestConfig = {}
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.axiosInstance.request<T>({
        url: endpoint,
        ...config
      })

      // Handle 204 No Content
      if (response.status === 204) {
        return {
          data: null as T,
          success: true
        }
      }

      return {
        data: response.data,
        success: true,
        message: (response.data as any)?.message
      }
    } catch (error) {
      // Re-throw the processed error from interceptor
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
      data
    })
  }

  // PUT request
  async put<T>(endpoint: string, data?: unknown, config: RequestConfig = {}): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...config,
      method: 'PUT',
      data
    })
  }

  // PATCH request
  async patch<T>(endpoint: string, data?: unknown, config: RequestConfig = {}): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...config,
      method: 'PATCH',
      data
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

    return this.request<T>(endpoint, {
      ...config,
      method: 'POST',
      data: formData,
      headers: {
        'Content-Type': 'multipart/form-data',
        ...config.headers
      }
    })
  }

  // Upload multiple files
  async uploadMultiple<T>(endpoint: string, files: File[], fieldName: string = 'files', config: RequestConfig = {}): Promise<ApiResponse<T>> {
    const formData = new FormData()
    files.forEach(file => {
      formData.append(fieldName, file)
    })

    return this.request<T>(endpoint, {
      ...config,
      method: 'POST',
      data: formData,
      headers: {
        'Content-Type': 'multipart/form-data',
        ...config.headers
      }
    })
  }

  // Download file
  async download(endpoint: string, config: RequestConfig = {}): Promise<Blob> {
    try {
      const response = await this.axiosInstance.request({
        url: endpoint,
        method: 'GET',
        responseType: 'blob',
        headers: {
          'Accept': 'application/octet-stream',
          ...config.headers
        },
        ...config
      })

      return response.data
    } catch (error) {
      if (error instanceof Error && error.message.includes('timeout')) {
        const timeoutError: ApiError = {
          message: 'Download dauerte zu lange. Bitte versuchen Sie es erneut.',
          status: 408,
          details: ['Zeitüberschreitung beim Download']
        }
        throw timeoutError
      }
      throw error
    }
  }

  // Set auth token
  setAuthToken(token: string): void {
    if (typeof window !== 'undefined') {
      localStorage.setItem('auth_token', token)
      // Update axios default header
      this.axiosInstance.defaults.headers.common.Authorization = `Bearer ${token}`
    }
  }

  // Clear auth token
  clearAuthToken(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('auth_token')
      // Remove from axios default headers
      delete this.axiosInstance.defaults.headers.common.Authorization
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

  // Refresh auth token
  async refreshToken(): Promise<void> {
    try {
      const refreshToken = typeof window !== 'undefined' ? localStorage.getItem('refresh_token') : null
      if (!refreshToken) {
        throw new Error('No refresh token available')
      }

      const response = await this.post<{ token: string; refreshToken: string }>('/auth/refresh', {
        refreshToken
      }, { skipErrorToast: true })

      if (response.success && response.data) {
        this.setAuthToken(response.data.token)
        if (typeof window !== 'undefined') {
          localStorage.setItem('refresh_token', response.data.refreshToken)
        }
      }
    } catch (error) {
      // Clear tokens on refresh failure
      this.clearAuthToken()
      if (typeof window !== 'undefined') {
        localStorage.removeItem('refresh_token')
      }
      throw error
    }
  }

  // Get axios instance for advanced usage
  getAxiosInstance(): AxiosInstance {
    return this.axiosInstance
  }

  // Create a request with custom success toast
  async requestWithSuccessToast<T>(
    endpoint: string,
    config: RequestConfig & { successMessage?: string } = {}
  ): Promise<ApiResponse<T>> {
    const { successMessage, ...requestConfig } = config
    const response = await this.request<T>(endpoint, requestConfig)
    
    if (response.success && successMessage) {
      toast.success(successMessage, {
        duration: 4000,
        position: 'top-right'
      })
    }
    
    return response
  }
}

// Create singleton instance
export const apiClient = new ApiClient()

// Export the class for testing or custom instances
export { ApiClient }