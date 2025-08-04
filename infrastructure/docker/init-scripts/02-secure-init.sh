#!/bin/bash
# Secure PostgreSQL Initialization Script
# This script sets up application user with password from environment variable

set -e

# Only run if POSTGRES_APP_PASSWORD is set
if [ -n "$POSTGRES_APP_PASSWORD" ]; then
    echo "Setting up application user with secure password..."
    
    # Use psql to set the password securely
    PGPASSWORD="$POSTGRES_PASSWORD" psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
        -- Set password for application user
        ALTER ROLE kgv_app WITH PASSWORD '$POSTGRES_APP_PASSWORD';
        
        -- Grant necessary permissions
        GRANT CONNECT ON DATABASE $POSTGRES_DB TO kgv_app;
        GRANT USAGE ON SCHEMA public TO kgv_app;
        GRANT CREATE ON SCHEMA public TO kgv_app;
        GRANT ALL ON ALL TABLES IN SCHEMA public TO kgv_app;
        GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO kgv_app;
        GRANT ALL ON ALL FUNCTIONS IN SCHEMA public TO kgv_app;
        
        -- Set default privileges for future objects
        ALTER DEFAULT PRIVILEGES IN SCHEMA public 
            GRANT ALL ON TABLES TO kgv_app;
        ALTER DEFAULT PRIVILEGES IN SCHEMA public 
            GRANT ALL ON SEQUENCES TO kgv_app;
        ALTER DEFAULT PRIVILEGES IN SCHEMA public 
            GRANT EXECUTE ON FUNCTIONS TO kgv_app;
        
        -- Create application-specific schema with restricted access
        CREATE SCHEMA IF NOT EXISTS app_data AUTHORIZATION kgv_app;
        
        -- Enable row-level security for sensitive tables (to be implemented)
        -- This is a placeholder for RLS policies
EOSQL
    
    echo "Application user configured successfully."
else
    echo "WARNING: POSTGRES_APP_PASSWORD not set. Application user created without password."
    echo "Please set password manually using: ALTER ROLE kgv_app WITH PASSWORD 'secure_password';"
fi