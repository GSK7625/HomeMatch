-- ==========================================================================================
-- 01-INIT-SCHEMAS.SQL: KÍCH HOẠT EXTENSIONS & KHỞI TẠO 10 SCHEMAS (MODULAR MONOLITH)
-- ==========================================================================================

-- 1. EXTENSIONS
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_available_extensions WHERE name = 'postgis') THEN
        CREATE EXTENSION IF NOT EXISTS postgis;
    END IF;
END $$;

-- 2. SCHEMAS
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS properties;
CREATE SCHEMA IF NOT EXISTS listings;
CREATE SCHEMA IF NOT EXISTS viewings;
CREATE SCHEMA IF NOT EXISTS messaging;
CREATE SCHEMA IF NOT EXISTS leasing;
CREATE SCHEMA IF NOT EXISTS billing;
CREATE SCHEMA IF NOT EXISTS notifications;
CREATE SCHEMA IF NOT EXISTS admin;
CREATE SCHEMA IF NOT EXISTS monetization;

-- Cấu hình search_path toàn cục cho database
ALTER DATABASE homematch_db SET search_path TO "$user", public, identity, properties, listings, viewings, messaging, leasing, billing, notifications, admin, monetization;
