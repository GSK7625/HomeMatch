-- Enable PostGIS and UUID extensions
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Modular Monolith: One Database, Multiple Schemas
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS properties;
CREATE SCHEMA IF NOT EXISTS listings;
CREATE SCHEMA IF NOT EXISTS viewings;
CREATE SCHEMA IF NOT EXISTS messaging;
CREATE SCHEMA IF NOT EXISTS leasing;
CREATE SCHEMA IF NOT EXISTS billing;
CREATE SCHEMA IF NOT EXISTS notifications;

-- Comment on schemas for clarity
COMMENT ON SCHEMA identity IS 'Manages users, authentication, roles and permissions';
COMMENT ON SCHEMA properties IS 'Manages buildings, rooms and facilities';
COMMENT ON SCHEMA listings IS 'Manages rental listings, search and PostGIS geospatial queries';
COMMENT ON SCHEMA viewings IS 'Manages room viewing appointments and schedules';
COMMENT ON SCHEMA messaging IS 'Manages chat messages and direct conversations';
COMMENT ON SCHEMA leasing IS 'Manages rental contracts, lease terms and tenant records';
COMMENT ON SCHEMA billing IS 'Manages recurring rent bills, utility invoices and payment status';
COMMENT ON SCHEMA notifications IS 'Manages user notifications (push, in-app, email)';
