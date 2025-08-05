#!/bin/sh
# Docker health check script for Next.js application

# Set timeout
TIMEOUT=${HEALTH_CHECK_TIMEOUT:-10}

# Check if the application is running on the expected port
PORT=${PORT:-3000}
URL="http://localhost:${PORT}/api/health"

# Function to check HTTP endpoint
check_http() {
    if command -v curl > /dev/null 2>&1; then
        curl -f -s --max-time $TIMEOUT "$URL" > /dev/null
    elif command -v wget > /dev/null 2>&1; then
        wget --quiet --timeout=$TIMEOUT --tries=1 --spider "$URL"
    else
        # Fallback using nc (netcat)
        nc -z localhost $PORT
    fi
}

# Perform health check
if check_http; then
    echo "Health check passed: Application is responding"
    exit 0
else
    echo "Health check failed: Application is not responding"
    exit 1
fi