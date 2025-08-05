# Multi-stage Dockerfile for Next.js 14 KGV Frontend
# Optimized for production deployment with minimal image size

# ===========================
# Stage 1: Dependencies
# ===========================
FROM node:18-alpine AS deps
RUN apk add --no-cache libc6-compat

WORKDIR /app

# Copy package files
COPY package.json package-lock.json* ./

# Install dependencies
RUN npm ci --only=production --ignore-scripts

# ===========================
# Stage 2: Builder
# ===========================
FROM node:18-alpine AS builder
RUN apk add --no-cache libc6-compat

WORKDIR /app

# Copy package files and install all dependencies (including dev)
COPY package.json package-lock.json* ./
RUN npm ci --ignore-scripts

# Copy source code
COPY . .

# Build arguments for environment variables
ARG NEXT_PUBLIC_API_URL
ARG NEXT_PUBLIC_APP_ENV=production

# Set environment variables for build
ENV NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL}
ENV NEXT_PUBLIC_APP_ENV=${NEXT_PUBLIC_APP_ENV}
ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_ENV=production

# Build the application
RUN npm run build

# ===========================
# Stage 3: Runner (Production)
# ===========================
FROM node:18-alpine AS runner

WORKDIR /app

# Create non-root user for security
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

# Install security updates
RUN apk add --no-cache dumb-init
RUN apk upgrade --available

# Copy built application from builder stage
COPY --from=builder /app/public ./public
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static

# Set correct permissions
RUN chown -R nextjs:nodejs /app
USER nextjs

# Health check script
COPY --chown=nextjs:nodejs scripts/docker-healthcheck.sh /app/healthcheck.sh
RUN chmod +x /app/healthcheck.sh

# Environment variables
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

# Expose port
EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD /app/healthcheck.sh

# Use dumb-init for signal handling
ENTRYPOINT ["dumb-init", "--"]

# Start the application
CMD ["node", "server.js"]

# ===========================
# Stage 4: Development (Optional)
# ===========================
FROM node:18-alpine AS development

WORKDIR /app

# Install git for development
RUN apk add --no-cache git

# Copy package files
COPY package.json package-lock.json* ./

# Install all dependencies (including dev)
RUN npm ci

# Copy source code
COPY . .

# Environment variables for development
ENV NODE_ENV=development
ENV NEXT_TELEMETRY_DISABLED=1

# Expose port
EXPOSE 3000

# Start development server
CMD ["npm", "run", "dev"]

# ===========================
# Multi-target build support
# ===========================
# Usage:
# docker build --target runner -t kgv-frontend:latest .
# docker build --target development -t kgv-frontend:dev .