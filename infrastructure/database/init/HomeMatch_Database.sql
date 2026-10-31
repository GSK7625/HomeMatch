-- ==========================================================================================
-- CƠ SỞ DỮ LIỆU HOMEMATCH (HOMEMATCHDB) - POSTGRESQL 16/18
-- Kiến trúc Modular Monolith: 1 Database, Đa Schemas (44 Bảng - 10 Phân hệ Nghiệp vụ)
-- ==========================================================================================

SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SET search_path TO "$user", public, identity, properties, listings, viewings, messaging, leasing, billing, notifications, admin, monetization;

-- ==========================================================================================
-- 1. DỌN DẸP DỮ LIỆU & BẢNG CŨ (IDEMPOTENT DROP TABLES WITH CASCADE)
-- ==========================================================================================
DROP VIEW IF EXISTS public.vw_tenant_lease_status_filter CASCADE;
DROP VIEW IF EXISTS public.vw_monthly_utility_report CASCADE;
DROP VIEW IF EXISTS public.vw_tenant_dashboard_summary CASCADE;
DROP VIEW IF EXISTS public.vw_landlord_daily_stats CASCADE;
DROP VIEW IF EXISTS public.vw_overdue_debt_aging CASCADE;
DROP VIEW IF EXISTS public.vw_expiring_contracts_in_30_days CASCADE;
DROP VIEW IF EXISTS public.vw_landlord_property_summary CASCADE;
DROP VIEW IF EXISTS public.vw_landlord_dashboard_summary CASCADE;
DROP VIEW IF EXISTS public.vw_admin_pending_reports CASCADE;
DROP VIEW IF EXISTS public.vw_admin_platform_summary CASCADE;
DROP VIEW IF EXISTS public.vw_public_listings CASCADE;

DROP TABLE IF EXISTS monetization.platform_transactions CASCADE;
DROP TABLE IF EXISTS monetization.listing_promotions CASCADE;
DROP TABLE IF EXISTS monetization.landlord_subscriptions CASCADE;
DROP TABLE IF EXISTS monetization.platform_service_plans CASCADE;

DROP TABLE IF EXISTS admin.content_reports CASCADE;
DROP TABLE IF EXISTS admin.admin_audit_logs CASCADE;
DROP TABLE IF EXISTS admin.platform_configs CASCADE;

DROP TABLE IF EXISTS notifications.notifications CASCADE;

DROP TABLE IF EXISTS messaging.ai_message_suggestions CASCADE;
DROP TABLE IF EXISTS messaging.ai_chat_messages CASCADE;
DROP TABLE IF EXISTS messaging.ai_chat_sessions CASCADE;
DROP TABLE IF EXISTS leasing.ai_task_suggestions CASCADE;

DROP TABLE IF EXISTS leasing.maintenance_images CASCADE;
DROP TABLE IF EXISTS leasing.maintenance_requests CASCADE;

DROP TABLE IF EXISTS billing.payments CASCADE;
DROP TABLE IF EXISTS billing.invoices CASCADE;
DROP TABLE IF EXISTS billing.utility_readings CASCADE;
DROP TABLE IF EXISTS leasing.contracts CASCADE;

DROP TABLE IF EXISTS messaging.messages CASCADE;
DROP TABLE IF EXISTS messaging.message_drafts CASCADE;
DROP TABLE IF EXISTS messaging.conversations CASCADE;
DROP TABLE IF EXISTS listings.roommate_posts CASCADE;
DROP TABLE IF EXISTS messaging.leads CASCADE;

DROP TABLE IF EXISTS viewings.virtual_viewings CASCADE;
DROP TABLE IF EXISTS viewings.viewing_appointments CASCADE;

DROP TABLE IF EXISTS listings.marketing_clicks CASCADE;
DROP TABLE IF EXISTS listings.marketing_campaigns CASCADE;
DROP TABLE IF EXISTS listings.user_room_comparisons CASCADE;
DROP TABLE IF EXISTS listings.listing_views CASCADE;
DROP TABLE IF EXISTS listings.saved_listings CASCADE;
DROP TABLE IF EXISTS listings.listing_images CASCADE;
DROP TABLE IF EXISTS listings.listings CASCADE;

DROP TABLE IF EXISTS properties.utility_tariffs CASCADE;
DROP TABLE IF EXISTS properties.room_amenities CASCADE;
DROP TABLE IF EXISTS properties.amenities CASCADE;
DROP TABLE IF EXISTS properties.rooms CASCADE;
DROP TABLE IF EXISTS properties.properties CASCADE;
DROP TABLE IF EXISTS properties.administrative_wards CASCADE;
DROP TABLE IF EXISTS properties.administrative_provinces CASCADE;

DROP TABLE IF EXISTS identity.tenant_identity_shares CASCADE;
DROP TABLE IF EXISTS identity.tenant_identity_profiles CASCADE;
DROP TABLE IF EXISTS identity.user_tokens CASCADE;
DROP TABLE IF EXISTS identity.user_profiles CASCADE;
DROP TABLE IF EXISTS identity.users CASCADE;

-- ==========================================================================================
-- 2. TẠO 44 BẢNG THEO 10 SCHEMAS CHUẨN MODULAR MONOLITH
-- ==========================================================================================

-- ------------------------------------------------------------------------------------------
-- PHÂN HỆ 1: ĐỊA GIỚI HÀNH CHÍNH & BẤT ĐỘNG SẢN (SCHEMA: properties)
-- ------------------------------------------------------------------------------------------

-- 1. AdministrativeProvinces
CREATE TABLE properties.administrative_provinces (
    code VARCHAR(20) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    name_en VARCHAR(100) NULL,
    full_name VARCHAR(150) NOT NULL
);

-- 2. AdministrativeWards
CREATE TABLE properties.administrative_wards (
    code VARCHAR(20) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    name_en VARCHAR(100) NULL,
    full_name VARCHAR(150) NOT NULL,
    district_name VARCHAR(100) NULL,
    province_code VARCHAR(20) NOT NULL REFERENCES properties.administrative_provinces(code)
);

-- ------------------------------------------------------------------------------------------
-- PHÂN HỆ 2: TÀI KHOẢN, XÁC THỰC & ĐỊNH DANH CCCD (SCHEMA: identity)
-- ------------------------------------------------------------------------------------------

-- 3. Users
CREATE TABLE identity.users (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    email VARCHAR(255) NOT NULL CONSTRAINT uq_users_email UNIQUE,
    phone_number VARCHAR(20) NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'TENANT' CONSTRAINT ck_users_role CHECK (role IN ('TENANT', 'LANDLORD', 'ADMIN')),
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE' CONSTRAINT ck_users_status CHECK (status IN ('ACTIVE', 'INACTIVE', 'LOCKED')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX uq_users_phone ON identity.users(phone_number) WHERE phone_number IS NOT NULL;

-- 4. UserProfiles
CREATE TABLE identity.user_profiles (
    user_id BIGINT PRIMARY KEY REFERENCES identity.users(id) ON DELETE CASCADE,
    full_name VARCHAR(100) NOT NULL,
    avatar_url VARCHAR(500) NULL,
    date_of_birth DATE NULL,
    gender VARCHAR(10) NULL CONSTRAINT ck_userprofiles_gender CHECK (gender IN ('MALE', 'FEMALE', 'OTHER')),
    notify_email BOOLEAN NOT NULL DEFAULT TRUE,
    notify_sms BOOLEAN NOT NULL DEFAULT FALSE,
    notify_push BOOLEAN NOT NULL DEFAULT TRUE,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 5. UserTokens
CREATE TABLE identity.user_tokens (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    token_hash CHAR(64) NOT NULL,
    token_type VARCHAR(30) NOT NULL CONSTRAINT ck_usertokens_type CHECK (token_type IN ('REFRESH', 'RESET_PASSWORD', 'REMEMBER_ME')),
    device_info VARCHAR(255) NULL,
    ip_address VARCHAR(45) NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 6. TenantIdentityProfiles
CREATE TABLE identity.tenant_identity_profiles (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    tenant_id BIGINT NOT NULL CONSTRAINT uq_tenantkyc_tenant UNIQUE REFERENCES identity.users(id) ON DELETE CASCADE,
    id_card_number_encrypted VARCHAR(255) NOT NULL,
    id_card_last4 CHAR(4) NOT NULL CONSTRAINT ck_tenantkyc_last4 CHECK (LENGTH(id_card_last4) = 4 AND id_card_last4 ~ '^[0-9]+$'),
    id_card_hmac_hash CHAR(64) NOT NULL CONSTRAINT uq_tenantkyc_hmac UNIQUE,
    masked_id_card VARCHAR(10) GENERATED ALWAYS AS ('******' || id_card_last4) STORED,
    full_name VARCHAR(150) NOT NULL,
    date_of_birth DATE NOT NULL,
    front_card_url VARCHAR(500) NOT NULL,
    back_card_url VARCHAR(500) NOT NULL,
    is_system_verified BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 7. TenantIdentityShares
CREATE TABLE identity.tenant_identity_shares (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    tenant_profile_id BIGINT NOT NULL REFERENCES identity.tenant_identity_profiles(id) ON DELETE CASCADE,
    target_landlord_id BIGINT NOT NULL REFERENCES identity.users(id),
    contract_id BIGINT NULL, -- Added FK to leasing.contracts later via ALTER TABLE
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING' CONSTRAINT ck_tenantshares_status CHECK (status IN ('PENDING', 'APPROVED', 'REJECTED')),
    reviewed_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 8. Notifications
CREATE TABLE notifications.notifications (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    type VARCHAR(50) NOT NULL,
    reference_type VARCHAR(50) NULL,
    reference_id BIGINT NULL,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ------------------------------------------------------------------------------------------
-- PHÂN HỆ 3: TÒA NHÀ, CĂN PHÒNG, TIỆN ÍCH & BIỂU GIÁ (SCHEMA: properties)
-- ------------------------------------------------------------------------------------------

-- 9. Properties
CREATE TABLE properties.properties (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    landlord_id BIGINT NOT NULL REFERENCES identity.users(id),
    name VARCHAR(200) NOT NULL,
    province_code VARCHAR(20) NOT NULL REFERENCES properties.administrative_provinces(code),
    ward_code VARCHAR(20) NOT NULL REFERENCES properties.administrative_wards(code),
    address_detail VARCHAR(255) NOT NULL,
    property_type VARCHAR(50) NOT NULL CONSTRAINT ck_properties_type CHECK (property_type IN ('APARTMENT', 'BOARDING_HOUSE', 'SERVICED_APARTMENT', 'HOMESTAY')),
    latitude DECIMAL(9,6) NULL,
    longitude DECIMAL(9,6) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tích hợp PostGIS không gian địa lý (Tự động kích hoạt khi môi trường hỗ trợ PostGIS như Docker postgis/postgis:16-3.4)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_available_extensions WHERE name = 'postgis') THEN
        EXECUTE 'CREATE EXTENSION IF NOT EXISTS postgis';
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'properties' AND table_name = 'properties' AND column_name = 'geom') THEN
            EXECUTE 'ALTER TABLE properties.properties ADD COLUMN geom geometry(Point, 4326) GENERATED ALWAYS AS (CASE WHEN latitude IS NOT NULL AND longitude IS NOT NULL THEN ST_SetSRID(ST_MakePoint(longitude, latitude), 4326) ELSE NULL END) STORED';
            EXECUTE 'CREATE INDEX IF NOT EXISTS ix_properties_geom ON properties.properties USING GIST (geom)';
        END IF;
    END IF;
END $$;

-- 10. Rooms
CREATE TABLE properties.rooms (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    property_id BIGINT NOT NULL REFERENCES properties.properties(id) ON DELETE CASCADE,
    room_number VARCHAR(50) NOT NULL,
    floor INT NOT NULL DEFAULT 1,
    area_sqm DECIMAL(6,2) NOT NULL CONSTRAINT ck_rooms_area CHECK (area_sqm > 0),
    max_occupants INT NOT NULL DEFAULT 2 CONSTRAINT ck_rooms_occupants CHECK (max_occupants > 0),
    base_price BIGINT NOT NULL CONSTRAINT ck_rooms_price CHECK (base_price >= 0),
    room_type VARCHAR(50) NOT NULL DEFAULT 'STUDIO' CONSTRAINT ck_rooms_type CHECK (room_type IN ('STUDIO', '1B1B', '2B1B', 'DORMITORY', 'ROOM')),
    status VARCHAR(20) NOT NULL DEFAULT 'AVAILABLE' CONSTRAINT ck_rooms_status CHECK (status IN ('AVAILABLE', 'RENTED', 'SUSPENDED')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_rooms_property_number UNIQUE (property_id, room_number)
);

-- 11. Amenities
CREATE TABLE properties.amenities (
    id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL CONSTRAINT uq_amenities_code UNIQUE,
    icon_url VARCHAR(500) NULL,
    category VARCHAR(50) NOT NULL CONSTRAINT ck_amenities_category CHECK (category IN ('ROOM', 'BUILDING', 'SECURITY', 'SERVICE'))
);

-- 12. RoomAmenities
CREATE TABLE properties.room_amenities (
    room_id BIGINT NOT NULL REFERENCES properties.rooms(id) ON DELETE CASCADE,
    amenity_id INT NOT NULL REFERENCES properties.amenities(id) ON DELETE CASCADE,
    PRIMARY KEY (room_id, amenity_id)
);

-- 13. UtilityTariffs
CREATE TABLE properties.utility_tariffs (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    property_id BIGINT NOT NULL REFERENCES properties.properties(id) ON DELETE CASCADE,
    room_id BIGINT NULL REFERENCES properties.rooms(id),
    tariff_type VARCHAR(30) NOT NULL CONSTRAINT ck_tariffs_type CHECK (tariff_type IN ('ELECTRICITY', 'WATER', 'INTERNET', 'SERVICE', 'CLEANING', 'PARKING')),
    calc_method VARCHAR(30) NOT NULL CONSTRAINT ck_tariffs_method CHECK (calc_method IN ('PER_KWH', 'PER_M3', 'PER_PERSON', 'FIXED_PER_MONTH', 'FREE')),
    unit_price BIGINT NOT NULL CONSTRAINT ck_tariffs_price CHECK (unit_price >= 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_tariffs_scope UNIQUE (property_id, room_id, tariff_type)
);

-- ------------------------------------------------------------------------------------------
-- PHÂN HỆ 4: TIN ĐĂNG, HÌNH ẢNH, SO SÁNH & TIẾP THỊ (SCHEMA: listings)
-- ------------------------------------------------------------------------------------------

-- 14. Listings
CREATE TABLE listings.listings (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    room_id BIGINT NOT NULL REFERENCES properties.rooms(id),
    landlord_id BIGINT NOT NULL REFERENCES identity.users(id),
    title VARCHAR(255) NOT NULL,
    slug VARCHAR(300) NOT NULL CONSTRAINT uq_listings_slug UNIQUE,
    description TEXT NOT NULL,
    price BIGINT NOT NULL CONSTRAINT ck_listings_price CHECK (price > 0),
    deposit_months INT NOT NULL DEFAULT 1 CONSTRAINT ck_listings_deposit CHECK (deposit_months >= 0),
    min_lease_months INT NOT NULL DEFAULT 6 CONSTRAINT ck_listings_minlease CHECK (min_lease_months > 0),
    allow_pets BOOLEAN NOT NULL DEFAULT FALSE,
    published_at TIMESTAMPTZ NULL,
    expires_at TIMESTAMPTZ NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'DRAFT' CONSTRAINT ck_listings_status CHECK (status IN ('DRAFT', 'PUBLISHED', 'HIDDEN', 'EXPIRED')),
    view_count INT NOT NULL DEFAULT 0 CONSTRAINT ck_listings_viewcount CHECK (view_count >= 0),
    is_featured BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX uq_listings_active_room ON listings.listings(room_id) WHERE status = 'PUBLISHED';
CREATE INDEX ix_listings_public_search ON listings.listings(price, room_id) WHERE status = 'PUBLISHED';

-- 15. ListingImages
CREATE TABLE listings.listing_images (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    listing_id BIGINT NOT NULL REFERENCES listings.listings(id) ON DELETE CASCADE,
    image_url VARCHAR(500) NOT NULL,
    is_primary BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order INT NOT NULL DEFAULT 0,
    media_type VARCHAR(20) NOT NULL DEFAULT 'IMAGE' CONSTRAINT ck_listingimages_mediatype CHECK (media_type IN ('IMAGE', 'VIDEO', 'VIRTUAL_TOUR_360')),
    thumbnail_url VARCHAR(500) NULL,
    duration_seconds INT NULL CONSTRAINT ck_listingimages_duration CHECK (duration_seconds IS NULL OR duration_seconds > 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 16. SavedListings
CREATE TABLE listings.saved_listings (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    listing_id BIGINT NOT NULL REFERENCES listings.listings(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_savedlistings_user_listing UNIQUE (user_id, listing_id)
);

-- 17. ListingViews
CREATE TABLE listings.listing_views (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    listing_id BIGINT NOT NULL REFERENCES listings.listings(id) ON DELETE CASCADE,
    user_id BIGINT NULL REFERENCES identity.users(id) ON DELETE SET NULL,
    ip_address VARCHAR(45) NULL,
    viewed_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 18. UserRoomComparisons
CREATE TABLE listings.user_room_comparisons (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    listing_id BIGINT NOT NULL REFERENCES listings.listings(id) ON DELETE CASCADE,
    slot INT NOT NULL CONSTRAINT ck_comparisons_slot CHECK (slot BETWEEN 1 AND 3),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_comparisons_user_slot UNIQUE (user_id, slot),
    CONSTRAINT uq_comparisons_user_listing UNIQUE (user_id, listing_id)
);

-- 19. MarketingCampaigns
CREATE TABLE listings.marketing_campaigns (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    listing_id BIGINT NOT NULL REFERENCES listings.listings(id) ON DELETE CASCADE,
    utm_source VARCHAR(50) NOT NULL,
    utm_medium VARCHAR(50) NOT NULL,
    utm_campaign VARCHAR(100) NOT NULL,
    campaign_code VARCHAR(100) NOT NULL CONSTRAINT uq_campaigns_code UNIQUE,
    total_clicks INT NOT NULL DEFAULT 0 CONSTRAINT ck_campaigns_clicks CHECK (total_clicks >= 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 20. MarketingClicks
CREATE TABLE listings.marketing_clicks (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    campaign_id BIGINT NOT NULL REFERENCES listings.marketing_campaigns(id) ON DELETE CASCADE,
    ip_address VARCHAR(45) NULL,
    referrer VARCHAR(500) NULL,
    clicked_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 21. RoommatePosts
CREATE TABLE listings.roommate_posts (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    tenant_id BIGINT NOT NULL REFERENCES identity.users(id),
    current_room_id BIGINT NULL REFERENCES properties.rooms(id) ON DELETE SET NULL,
    target_province_code VARCHAR(20) NOT NULL REFERENCES properties.administrative_provinces(code),
    target_ward_code VARCHAR(20) NOT NULL REFERENCES properties.administrative_wards(code),
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    budget_min BIGINT NOT NULL CONSTRAINT ck_roommate_bmin CHECK (budget_min >= 0),
    budget_max BIGINT NOT NULL CONSTRAINT ck_roommate_bmax CHECK (budget_max >= budget_min),
    preferred_gender VARCHAR(10) NOT NULL DEFAULT 'ANY' CONSTRAINT ck_roommate_gender CHECK (preferred_gender IN ('ANY', 'MALE', 'FEMALE')),
    lifestyle_notes TEXT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE' CONSTRAINT ck_roommate_status CHECK (status IN ('ACTIVE', 'CLOSED', 'EXPIRED', 'DELETED')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ------------------------------------------------------------------------------------------
-- PHÂN HỆ 5: LỊCH HẸN XEM PHÒNG & BUỔI XEM ONLINE (SCHEMA: viewings)
-- ------------------------------------------------------------------------------------------

-- 22. ViewingAppointments
CREATE TABLE viewings.viewing_appointments (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    tenant_id BIGINT NOT NULL REFERENCES identity.users(id),
    listing_id BIGINT NOT NULL REFERENCES listings.listings(id),
    appointment_time TIMESTAMPTZ NOT NULL,
    duration_minutes INT NOT NULL DEFAULT 30 CONSTRAINT ck_appointments_duration CHECK (duration_minutes BETWEEN 15 AND 180),
    notes VARCHAR(500) NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING' CONSTRAINT ck_appointments_status CHECK (status IN ('PENDING', 'CONFIRMED', 'CANCELLED', 'COMPLETED')),
    cancelled_by VARCHAR(20) NULL CONSTRAINT ck_appointments_cancelled_by CHECK (cancelled_by IN ('TENANT', 'LANDLORD', 'SYSTEM')),
    cancelled_reason VARCHAR(500) NULL,
    remind_at TIMESTAMPTZ NULL,
    is_reminded BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 23. VirtualViewings
CREATE TABLE viewings.virtual_viewings (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    appointment_id BIGINT NOT NULL CONSTRAINT uq_virtualviewings_app UNIQUE REFERENCES viewings.viewing_appointments(id) ON DELETE CASCADE,
    room_url VARCHAR(500) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'SCHEDULED' CONSTRAINT ck_virtualviewings_status CHECK (status IN ('SCHEDULED', 'IN_PROGRESS', 'ENDED', 'CANCELLED')),
    started_at TIMESTAMPTZ NULL,
    ended_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ------------------------------------------------------------------------------------------
-- PHÂN HỆ 6: KHÁCH TIỀM NĂNG, HỘI THOẠI & TRỢ LÝ AI (SCHEMA: messaging)
-- ------------------------------------------------------------------------------------------

-- 24. Leads
CREATE TABLE messaging.leads (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    tenant_id BIGINT NOT NULL REFERENCES identity.users(id),
    listing_id BIGINT NOT NULL REFERENCES listings.listings(id),
    status VARCHAR(20) NOT NULL DEFAULT 'NEW' CONSTRAINT ck_leads_status CHECK (status IN ('NEW', 'CONTACTED', 'VIEWING_SCHEDULED', 'CONTRACT_SIGNED', 'LOST')),
    notes VARCHAR(500) NULL,
    source VARCHAR(50) NOT NULL DEFAULT 'DIRECT' CONSTRAINT ck_leads_source CHECK (source IN ('DIRECT', 'SEARCH_FILTER', 'COMPARISON', 'AI_CHAT')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_leads_tenant_listing UNIQUE (tenant_id, listing_id)
);

-- 25. AIChatSessions
CREATE TABLE messaging.ai_chat_sessions (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id BIGINT NULL REFERENCES identity.users(id) ON DELETE SET NULL,
    session_title VARCHAR(200) NOT NULL DEFAULT 'Hội thoại tìm phòng với AI',
    context_summary TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 26. Conversations
CREATE TABLE messaging.conversations (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    initiator_id BIGINT NOT NULL REFERENCES identity.users(id),
    recipient_id BIGINT NOT NULL REFERENCES identity.users(id),
    listing_id BIGINT NULL REFERENCES listings.listings(id),
    roommate_post_id BIGINT NULL REFERENCES listings.roommate_posts(id),
    ai_session_id BIGINT NULL REFERENCES messaging.ai_chat_sessions(id),
    conversation_type VARCHAR(20) NOT NULL DEFAULT 'LISTING' CONSTRAINT ck_conversations_type CHECK (conversation_type IN ('LISTING', 'ROOMMATE', 'AI_ESCALATED')),
    user_min_id BIGINT GENERATED ALWAYS AS (LEAST(initiator_id, recipient_id)) STORED,
    user_max_id BIGINT GENERATED ALWAYS AS (GREATEST(initiator_id, recipient_id)) STORED,
    last_message_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_conversations_parties CHECK (initiator_id <> recipient_id)
);

CREATE UNIQUE INDEX uq_conversations_listingchat ON messaging.conversations(user_min_id, user_max_id, listing_id) WHERE listing_id IS NOT NULL;
CREATE UNIQUE INDEX uq_conversations_roommatechat ON messaging.conversations(user_min_id, user_max_id, roommate_post_id) WHERE roommate_post_id IS NOT NULL;

-- 27. MessageDrafts
CREATE TABLE messaging.message_drafts (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    conversation_id BIGINT NOT NULL REFERENCES messaging.conversations(id) ON DELETE CASCADE,
    author_id BIGINT NOT NULL REFERENCES identity.users(id),
    source VARCHAR(20) NOT NULL DEFAULT 'MANUAL' CONSTRAINT ck_drafts_source CHECK (source IN ('MANUAL', 'AI_ASSISTANT')),
    content TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_drafts_author UNIQUE (conversation_id, author_id)
);

-- 28. Messages
CREATE TABLE messaging.messages (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    conversation_id BIGINT NOT NULL REFERENCES messaging.conversations(id) ON DELETE CASCADE,
    sender_id BIGINT NOT NULL REFERENCES identity.users(id),
    message_type VARCHAR(20) NOT NULL DEFAULT 'TEXT' CONSTRAINT ck_messages_type CHECK (message_type IN ('TEXT', 'IMAGE', 'SYSTEM', 'AI_SUGGESTION')),
    content TEXT NOT NULL,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 29. AIChatMessages
CREATE TABLE messaging.ai_chat_messages (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    session_id BIGINT NOT NULL REFERENCES messaging.ai_chat_sessions(id) ON DELETE CASCADE,
    sender_type VARCHAR(20) NOT NULL CONSTRAINT ck_aimessages_sender CHECK (sender_type IN ('USER', 'ASSISTANT')),
    content TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 30. AIMessageSuggestions
CREATE TABLE messaging.ai_message_suggestions (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    message_id BIGINT NOT NULL REFERENCES messaging.ai_chat_messages(id) ON DELETE CASCADE,
    listing_id BIGINT NOT NULL REFERENCES listings.listings(id) ON DELETE CASCADE,
    rank_order INT NOT NULL DEFAULT 1,
    relevance_score DECIMAL(5,2) NULL,
    suggested_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ------------------------------------------------------------------------------------------
-- PHÂN HỆ 7: HỢP ĐỒNG, ĐIỆN NƯỚC, HÓA ĐƠN & BẢO TRÌ (SCHEMA: leasing & billing)
-- ------------------------------------------------------------------------------------------

-- 31. Contracts
CREATE TABLE leasing.contracts (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    contract_code VARCHAR(50) NOT NULL CONSTRAINT uq_contracts_code UNIQUE,
    room_id BIGINT NOT NULL REFERENCES properties.rooms(id),
    tenant_id BIGINT NOT NULL REFERENCES identity.users(id),
    landlord_id BIGINT NOT NULL REFERENCES identity.users(id),
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    monthly_rent BIGINT NOT NULL CONSTRAINT ck_contracts_rent CHECK (monthly_rent > 0),
    deposit_amount BIGINT NOT NULL CONSTRAINT ck_contracts_deposit CHECK (deposit_amount >= 0),
    deposit_status VARCHAR(30) NOT NULL DEFAULT 'PENDING' CONSTRAINT ck_contracts_deposit_status CHECK (deposit_status IN ('PENDING', 'HELD_IN_ESCROW', 'PAID_TO_LANDLORD', 'REFUNDED', 'FORFEITED')),
    billing_due_day INT NOT NULL DEFAULT 5 CONSTRAINT ck_contracts_billing_due_day CHECK (billing_due_day BETWEEN 1 AND 28),
    status VARCHAR(20) NOT NULL DEFAULT 'DRAFT' CONSTRAINT ck_contracts_status CHECK (status IN ('DRAFT', 'ACTIVE', 'TERMINATED', 'EXPIRED')),
    signed_at TIMESTAMPTZ NULL,
    actual_return_date DATE NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_contracts_date_range CHECK (end_date > start_date)
);

CREATE UNIQUE INDEX uq_contracts_active_room ON leasing.contracts(room_id) WHERE status = 'ACTIVE';

-- Link TenantIdentityShares to Contracts now that Contracts is created
ALTER TABLE identity.tenant_identity_shares
ADD CONSTRAINT fk_tenantshares_contract FOREIGN KEY (contract_id) REFERENCES leasing.contracts(id) ON DELETE SET NULL;

-- 32. MaintenanceRequests
CREATE TABLE leasing.maintenance_requests (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    request_code VARCHAR(50) NOT NULL CONSTRAINT uq_maintenance_code UNIQUE,
    room_id BIGINT NOT NULL REFERENCES properties.rooms(id),
    contract_id BIGINT NULL REFERENCES leasing.contracts(id),
    tenant_id BIGINT NOT NULL REFERENCES identity.users(id),
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    priority VARCHAR(20) NOT NULL DEFAULT 'MEDIUM' CONSTRAINT ck_maintenance_priority CHECK (priority IN ('LOW', 'MEDIUM', 'HIGH', 'EMERGENCY')),
    status VARCHAR(20) NOT NULL DEFAULT 'REPORTED' CONSTRAINT ck_maintenance_status CHECK (status IN ('REPORTED', 'IN_PROGRESS', 'RESOLVED', 'REJECTED')),
    resolved_at TIMESTAMPTZ NULL,
    cost BIGINT NOT NULL DEFAULT 0 CONSTRAINT ck_maintenance_cost CHECK (cost >= 0),
    paid_by VARCHAR(20) NOT NULL DEFAULT 'LANDLORD' CONSTRAINT ck_maintenance_paidby CHECK (paid_by IN ('LANDLORD', 'TENANT', 'SHARED')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 33. MaintenanceImages
CREATE TABLE leasing.maintenance_images (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    request_id BIGINT NOT NULL REFERENCES leasing.maintenance_requests(id) ON DELETE CASCADE,
    image_url VARCHAR(500) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 34. AITaskSuggestions
CREATE TABLE leasing.ai_task_suggestions (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    landlord_id BIGINT NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    task_type VARCHAR(50) NOT NULL CONSTRAINT ck_aitasks_type CHECK (task_type IN ('VACANT_ROOM_PRICE_CUT', 'OVERDUE_DEBT_ALERT', 'CONTRACT_RENEWAL', 'MAINTENANCE_ESCALATION')),
    reference_type VARCHAR(50) NULL,
    reference_id BIGINT NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    suggested_action TEXT NULL,
    is_dismissed BOOLEAN NOT NULL DEFAULT FALSE,
    is_resolved BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 35. UtilityReadings
CREATE TABLE billing.utility_readings (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    room_id BIGINT NOT NULL REFERENCES properties.rooms(id),
    contract_id BIGINT NOT NULL REFERENCES leasing.contracts(id),
    recorded_by BIGINT NOT NULL REFERENCES identity.users(id),
    billing_month INT NOT NULL CONSTRAINT ck_readings_month CHECK (billing_month BETWEEN 1 AND 12),
    billing_year INT NOT NULL CONSTRAINT ck_readings_year CHECK (billing_year >= 2020),
    electricity_old INT NOT NULL CONSTRAINT ck_readings_eold CHECK (electricity_old >= 0),
    electricity_new INT NOT NULL,
    electricity_usage INT GENERATED ALWAYS AS (electricity_new - electricity_old) STORED,
    electricity_cost BIGINT NOT NULL CONSTRAINT ck_readings_ecost CHECK (electricity_cost >= 0),
    water_old INT NOT NULL CONSTRAINT ck_readings_wold CHECK (water_old >= 0),
    water_new INT NOT NULL,
    water_usage INT GENERATED ALWAYS AS (water_new - water_old) STORED,
    water_cost BIGINT NOT NULL CONSTRAINT ck_readings_wcost CHECK (water_cost >= 0),
    reading_date DATE NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_readings_e_progress CHECK (electricity_new >= electricity_old),
    CONSTRAINT ck_readings_w_progress CHECK (water_new >= water_old),
    CONSTRAINT uq_readings_room_period UNIQUE (room_id, billing_year, billing_month)
);

-- 36. Invoices
CREATE TABLE billing.invoices (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    invoice_code VARCHAR(50) NOT NULL CONSTRAINT uq_invoices_code UNIQUE,
    contract_id BIGINT NOT NULL REFERENCES leasing.contracts(id),
    utility_reading_id BIGINT NULL REFERENCES billing.utility_readings(id),
    billing_month INT NOT NULL CONSTRAINT ck_invoices_month CHECK (billing_month BETWEEN 1 AND 12),
    billing_year INT NOT NULL CONSTRAINT ck_invoices_year CHECK (billing_year >= 2020),
    rent_amount BIGINT NOT NULL CONSTRAINT ck_invoices_rent CHECK (rent_amount >= 0),
    electricity_amount BIGINT NOT NULL DEFAULT 0 CONSTRAINT ck_invoices_elec CHECK (electricity_amount >= 0),
    water_amount BIGINT NOT NULL DEFAULT 0 CONSTRAINT ck_invoices_water CHECK (water_amount >= 0),
    service_amount BIGINT NOT NULL DEFAULT 0 CONSTRAINT ck_invoices_service CHECK (service_amount >= 0),
    discount_amount BIGINT NOT NULL DEFAULT 0 CONSTRAINT ck_invoices_discount CHECK (discount_amount >= 0),
    total_amount BIGINT GENERATED ALWAYS AS (rent_amount + electricity_amount + water_amount + service_amount - discount_amount) STORED,
    paid_amount BIGINT NOT NULL DEFAULT 0,
    due_date DATE NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'UNPAID' CONSTRAINT ck_invoices_status CHECK (status IN ('UNPAID', 'PARTIALLY_PAID', 'PAID', 'OVERDUE', 'CANCELLED')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_invoices_paid_limit CHECK (paid_amount <= (rent_amount + electricity_amount + water_amount + service_amount - discount_amount)),
    CONSTRAINT uq_invoices_contract_period UNIQUE (contract_id, billing_year, billing_month)
);

-- 37. Payments
CREATE TABLE billing.payments (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    invoice_id BIGINT NOT NULL REFERENCES billing.invoices(id),
    amount BIGINT NOT NULL CONSTRAINT ck_payments_amount CHECK (amount > 0),
    payment_method VARCHAR(30) NOT NULL CONSTRAINT ck_payments_method CHECK (payment_method IN ('CASH', 'BANK_TRANSFER', 'VNPAY', 'MOMO')),
    transaction_ref VARCHAR(100) NULL,
    proof_image_url VARCHAR(500) NULL,
    paid_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING' CONSTRAINT ck_payments_status CHECK (status IN ('PENDING', 'VERIFIED', 'REJECTED')),
    verified_by BIGINT NULL REFERENCES identity.users(id),
    verified_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_payments_verified CHECK (status <> 'VERIFIED' OR verified_by IS NOT NULL)
);

-- ------------------------------------------------------------------------------------------
-- PHÂN HỆ 8 & 9: QUẢN TRỊ ADMIN & DOANH THU SÀN (SCHEMA: admin & monetization)
-- ------------------------------------------------------------------------------------------

-- 38. PlatformConfigs
CREATE TABLE admin.platform_configs (
    config_key VARCHAR(100) PRIMARY KEY,
    config_value TEXT NOT NULL,
    data_type VARCHAR(20) NOT NULL CONSTRAINT ck_configs_datatype CHECK (data_type IN ('STRING', 'INT', 'DECIMAL', 'BOOLEAN', 'JSON')),
    category VARCHAR(50) NOT NULL CONSTRAINT ck_configs_category CHECK (category IN ('LISTING', 'BILLING', 'SECURITY', 'SYSTEM')),
    description VARCHAR(500) NULL,
    updated_by BIGINT NOT NULL REFERENCES identity.users(id),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 39. AdminAuditLogs
CREATE TABLE admin.admin_audit_logs (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    admin_id BIGINT NOT NULL REFERENCES identity.users(id),
    action_type VARCHAR(50) NOT NULL CONSTRAINT ck_auditlogs_action CHECK (action_type IN ('LOCK_USER', 'UNLOCK_USER', 'HIDE_LISTING', 'APPROVE_KYC', 'REJECT_KYC', 'RESOLVE_REPORT', 'UPDATE_CONFIG', 'REFUND_ESCROW')),
    target_table VARCHAR(50) NOT NULL,
    target_id BIGINT NOT NULL,
    old_values TEXT NULL,
    new_values TEXT NULL,
    reason VARCHAR(500) NOT NULL,
    ip_address VARCHAR(45) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 40. ContentReports
CREATE TABLE admin.content_reports (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    reporter_id BIGINT NOT NULL REFERENCES identity.users(id),
    target_type VARCHAR(20) NOT NULL CONSTRAINT ck_reports_targettype CHECK (target_type IN ('LISTING', 'USER', 'ROOMMATE_POST', 'CONTRACT')),
    target_id BIGINT NOT NULL,
    report_reason VARCHAR(50) NOT NULL CONSTRAINT ck_reports_reason CHECK (report_reason IN ('FAKE_LISTING', 'SCAM_DEPOSIT', 'INCORRECT_PRICING', 'INAPPROPRIATE_BEHAVIOR', 'DUPLICATE_SPAM', 'OTHER')),
    description TEXT NOT NULL,
    evidence_urls TEXT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING' CONSTRAINT ck_reports_status CHECK (status IN ('PENDING', 'INVESTIGATING', 'RESOLVED', 'DISMISSED')),
    resolution_notes TEXT NULL,
    resolved_by BIGINT NULL REFERENCES identity.users(id),
    resolved_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 41. PlatformServicePlans
CREATE TABLE monetization.platform_service_plans (
    id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL CONSTRAINT uq_serviceplans_code UNIQUE,
    plan_type VARCHAR(30) NOT NULL CONSTRAINT ck_serviceplans_type CHECK (plan_type IN ('SAAS_PMS', 'LISTING_BOOST', 'AI_ADDON')),
    price BIGINT NOT NULL CONSTRAINT ck_serviceplans_price CHECK (price >= 0),
    duration_days INT NOT NULL CONSTRAINT ck_serviceplans_duration CHECK (duration_days > 0),
    max_rooms_allowed INT NULL,
    vip_boost_quota INT NOT NULL DEFAULT 0,
    ai_query_quota INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- 42. LandlordSubscriptions
CREATE TABLE monetization.landlord_subscriptions (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    landlord_id BIGINT NOT NULL REFERENCES identity.users(id),
    plan_id INT NOT NULL REFERENCES monetization.platform_service_plans(id),
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE' CONSTRAINT ck_subs_status CHECK (status IN ('ACTIVE', 'EXPIRED', 'CANCELLED', 'UPGRADED')),
    auto_renew BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_subs_daterange CHECK (end_date >= start_date)
);

-- 43. ListingPromotions
CREATE TABLE monetization.listing_promotions (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    listing_id BIGINT NOT NULL REFERENCES listings.listings(id) ON DELETE CASCADE,
    promotion_type VARCHAR(30) NOT NULL CONSTRAINT ck_promotions_type CHECK (promotion_type IN ('TOP_RECOMMENDED', 'BADGE_VERIFIED', 'HIGHLIGHT_BORDER')),
    priority_score INT NOT NULL DEFAULT 1 CONSTRAINT ck_promotions_priority CHECK (priority_score BETWEEN 1 AND 10),
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ NOT NULL,
    amount_paid BIGINT NOT NULL CONSTRAINT ck_promotions_amount CHECK (amount_paid >= 0),
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE' CONSTRAINT ck_promotions_status CHECK (status IN ('ACTIVE', 'EXPIRED')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_promotions_timerange CHECK (end_time > start_time)
);

-- 44. PlatformTransactions
CREATE TABLE monetization.platform_transactions (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    transaction_code VARCHAR(50) NOT NULL CONSTRAINT uq_plattrx_code UNIQUE,
    payer_user_id BIGINT NOT NULL REFERENCES identity.users(id),
    revenue_stream VARCHAR(50) NOT NULL CONSTRAINT ck_plattrx_stream CHECK (revenue_stream IN ('SAAS_SUBSCRIPTION', 'LISTING_VIP_BOOST', 'BROKERAGE_COMMISSION', 'ESCROW_FEE', 'AI_ADDON_FEE')),
    reference_table VARCHAR(50) NULL,
    reference_id BIGINT NULL,
    amount BIGINT NOT NULL CONSTRAINT ck_plattrx_amount CHECK (amount > 0),
    payment_method VARCHAR(30) NOT NULL CONSTRAINT ck_plattrx_method CHECK (payment_method IN ('VNPAY', 'MOMO', 'BANK_TRANSFER')),
    gateway_transaction_id VARCHAR(100) NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING' CONSTRAINT ck_plattrx_status CHECK (status IN ('PENDING', 'COMPLETED', 'FAILED', 'REFUNDED')),
    completed_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ==========================================================================================
-- 3. HỆ THỐNG TRIGGERS TOÀN VẸN NGHIỆP VỤ (16 TRIGGERS & FUNCTIONS)
-- ==========================================================================================

-- 1. trg_validate_listing_landlord
CREATE OR REPLACE FUNCTION fn_validate_listing_landlord()
RETURNS TRIGGER AS $$
DECLARE
    v_owner_id BIGINT;
BEGIN
    SELECT p.landlord_id INTO v_owner_id
    FROM properties.rooms r
    JOIN properties.properties p ON p.id = r.property_id
    WHERE r.id = NEW.room_id;
    
    IF v_owner_id IS NOT NULL AND v_owner_id <> NEW.landlord_id THEN
        RAISE EXCEPTION 'Lỗi toàn vẹn dữ liệu: landlord_id trong Listings phải trùng khớp với landlord_id của Tòa nhà sở hữu căn phòng tại thời điểm tạo hoặc gán lại!'
        USING ERRCODE = '50001';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_listing_landlord
BEFORE INSERT OR UPDATE ON listings.listings
FOR EACH ROW EXECUTE FUNCTION fn_validate_listing_landlord();

-- 2. trg_validate_contract_landlord
CREATE OR REPLACE FUNCTION fn_validate_contract_landlord()
RETURNS TRIGGER AS $$
DECLARE
    v_owner_id BIGINT;
BEGIN
    SELECT p.landlord_id INTO v_owner_id
    FROM properties.rooms r
    JOIN properties.properties p ON p.id = r.property_id
    WHERE r.id = NEW.room_id;
    
    IF v_owner_id IS NOT NULL AND v_owner_id <> NEW.landlord_id THEN
        RAISE EXCEPTION 'Lỗi toàn vẹn hợp đồng: Bên Cho Thuê (landlord_id) trên hợp đồng phải là chủ sở hữu hợp pháp của căn phòng tại thời điểm ký kết hoặc điều chỉnh!'
        USING ERRCODE = '50002';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_contract_landlord
BEFORE INSERT OR UPDATE ON leasing.contracts
FOR EACH ROW EXECUTE FUNCTION fn_validate_contract_landlord();

-- 3. trg_validate_property_landlord_change
CREATE OR REPLACE FUNCTION fn_validate_property_landlord_change()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.landlord_id <> OLD.landlord_id THEN
        IF EXISTS (
            SELECT 1 FROM listings.listings l
            JOIN properties.rooms r ON r.id = l.room_id
            WHERE r.property_id = OLD.id AND l.status = 'PUBLISHED'
        ) OR EXISTS (
            SELECT 1 FROM leasing.contracts c
            JOIN properties.rooms r ON r.id = c.room_id
            WHERE r.property_id = OLD.id AND c.status = 'ACTIVE'
        ) THEN
            RAISE EXCEPTION 'Lỗi chuyển nhượng tòa nhà: Không thể thay đổi chủ sở hữu (landlord_id) khi tòa nhà vẫn còn tin đăng PUBLISHED hoặc hợp đồng ACTIVE! Vui lòng tất toán hợp đồng và ẩn các tin đăng trước khi chuyển giao.'
            USING ERRCODE = '50003';
        END IF;
        
        UPDATE listings.listings
        SET landlord_id = NEW.landlord_id
        WHERE room_id IN (SELECT id FROM properties.rooms WHERE property_id = OLD.id)
          AND status <> 'PUBLISHED';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_property_landlord_change
BEFORE UPDATE OF landlord_id ON properties.properties
FOR EACH ROW EXECUTE FUNCTION fn_validate_property_landlord_change();

-- 4. trg_validate_conversation_parties
CREATE OR REPLACE FUNCTION fn_validate_conversation_parties()
RETURNS TRIGGER AS $$
DECLARE
    v_listing_landlord_id BIGINT;
BEGIN
    IF NEW.conversation_type = 'LISTING' AND NEW.listing_id IS NOT NULL THEN
        SELECT landlord_id INTO v_listing_landlord_id
        FROM listings.listings
        WHERE id = NEW.listing_id;
        
        IF v_listing_landlord_id IS NOT NULL AND NEW.initiator_id <> v_listing_landlord_id AND NEW.recipient_id <> v_listing_landlord_id THEN
            RAISE EXCEPTION 'Lỗi phân quyền hội thoại: Trong hội thoại thuê phòng (LISTING), bắt buộc một trong hai người tham gia phải là chủ sở hữu của tin đăng!'
            USING ERRCODE = '50004';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_conversation_parties
BEFORE INSERT OR UPDATE ON messaging.conversations
FOR EACH ROW EXECUTE FUNCTION fn_validate_conversation_parties();

-- 5. trg_validate_message_sender
CREATE OR REPLACE FUNCTION fn_validate_message_sender()
RETURNS TRIGGER AS $$
DECLARE
    v_init_id BIGINT;
    v_recip_id BIGINT;
BEGIN
    SELECT initiator_id, recipient_id INTO v_init_id, v_recip_id
    FROM messaging.conversations
    WHERE id = NEW.conversation_id;
    
    IF NEW.sender_id <> v_init_id AND NEW.sender_id <> v_recip_id THEN
        RAISE EXCEPTION 'Lỗi an ninh tin nhắn: Người gửi tin nhắn (sender_id) bắt buộc phải là một trong hai thành viên tham gia cuộc hội thoại!'
        USING ERRCODE = '50005';
    END IF;
    
    UPDATE messaging.conversations
    SET last_message_at = NEW.created_at
    WHERE id = NEW.conversation_id;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_message_sender
BEFORE INSERT ON messaging.messages
FOR EACH ROW EXECUTE FUNCTION fn_validate_message_sender();

-- 6. trg_validate_maintenance_contract
CREATE OR REPLACE FUNCTION fn_validate_maintenance_contract()
RETURNS TRIGGER AS $$
DECLARE
    v_room_id BIGINT;
    v_tenant_id BIGINT;
BEGIN
    IF NEW.contract_id IS NOT NULL THEN
        SELECT room_id, tenant_id INTO v_room_id, v_tenant_id
        FROM leasing.contracts
        WHERE id = NEW.contract_id;
        
        IF v_room_id <> NEW.room_id OR v_tenant_id <> NEW.tenant_id THEN
            RAISE EXCEPTION 'Lỗi toàn vẹn sự cố: Căn phòng và người thuê trong yêu cầu bảo trì phải khớp với hợp đồng được tham chiếu!'
            USING ERRCODE = '50006';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_maintenance_contract
BEFORE INSERT OR UPDATE ON leasing.maintenance_requests
FOR EACH ROW EXECUTE FUNCTION fn_validate_maintenance_contract();

-- 7. trg_validate_utility_contract
CREATE OR REPLACE FUNCTION fn_validate_utility_contract()
RETURNS TRIGGER AS $$
DECLARE
    v_room_id BIGINT;
BEGIN
    IF NEW.contract_id IS NOT NULL THEN
        SELECT room_id INTO v_room_id
        FROM leasing.contracts
        WHERE id = NEW.contract_id;
        
        IF v_room_id <> NEW.room_id THEN
            RAISE EXCEPTION 'Lỗi toàn vẹn chỉ số: Căn phòng ghi nhận chỉ số điện nước phải khớp với căn phòng của hợp đồng được tham chiếu!'
            USING ERRCODE = '50007';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_utility_contract
BEFORE INSERT OR UPDATE ON billing.utility_readings
FOR EACH ROW EXECUTE FUNCTION fn_validate_utility_contract();

-- 8. trg_validate_invoice_utility_reading
CREATE OR REPLACE FUNCTION fn_validate_invoice_utility_reading()
RETURNS TRIGGER AS $$
DECLARE
    v_cid BIGINT;
    v_month INT;
    v_year INT;
BEGIN
    IF NEW.utility_reading_id IS NOT NULL THEN
        SELECT contract_id, billing_month, billing_year INTO v_cid, v_month, v_year
        FROM billing.utility_readings
        WHERE id = NEW.utility_reading_id;
        
        IF v_cid <> NEW.contract_id OR v_month <> NEW.billing_month OR v_year <> NEW.billing_year THEN
            RAISE EXCEPTION 'Lỗi hóa đơn: Phiếu chỉ số điện nước liên kết phải thuộc cùng hợp đồng và cùng kỳ thanh toán (tháng/năm) với hóa đơn!'
            USING ERRCODE = '50008';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_invoice_utility_reading
BEFORE INSERT OR UPDATE ON billing.invoices
FOR EACH ROW EXECUTE FUNCTION fn_validate_invoice_utility_reading();

-- 9. trg_validate_payment_verifier
CREATE OR REPLACE FUNCTION fn_validate_payment_verifier()
RETURNS TRIGGER AS $$
DECLARE
    v_landlord_id BIGINT;
    v_verifier_role VARCHAR(20);
BEGIN
    IF NEW.verified_by IS NOT NULL THEN
        SELECT c.landlord_id INTO v_landlord_id
        FROM billing.invoices i
        JOIN leasing.contracts c ON c.id = i.contract_id
        WHERE i.id = NEW.invoice_id;
        
        SELECT role INTO v_verifier_role
        FROM identity.users
        WHERE id = NEW.verified_by;
        
        IF NEW.verified_by <> v_landlord_id AND v_verifier_role <> 'ADMIN' THEN
            RAISE EXCEPTION 'Lỗi phân quyền thanh toán: Người duyệt thanh toán (verified_by) bắt buộc phải là chủ nhà sở hữu hợp đồng hoặc Quản trị viên hệ thống (Admin)!'
            USING ERRCODE = '50009';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_payment_verifier
BEFORE INSERT OR UPDATE ON billing.payments
FOR EACH ROW EXECUTE FUNCTION fn_validate_payment_verifier();

-- 10. trg_validate_appointment_schedule
CREATE OR REPLACE FUNCTION fn_validate_appointment_schedule()
RETURNS TRIGGER AS $$
DECLARE
    v_room_id BIGINT;
BEGIN
    IF NEW.status IN ('PENDING', 'CONFIRMED') THEN
        SELECT room_id INTO v_room_id
        FROM listings.listings
        WHERE id = NEW.listing_id;
        
        IF EXISTS (
            SELECT 1
            FROM viewings.viewing_appointments a
            JOIN listings.listings l ON l.id = a.listing_id
            WHERE l.room_id = v_room_id
              AND a.id <> COALESCE(NEW.id, -1)
              AND a.status IN ('PENDING', 'CONFIRMED')
              AND (
                  NEW.appointment_time < (a.appointment_time + (a.duration_minutes || ' minutes')::interval)
                  AND
                  (NEW.appointment_time + (NEW.duration_minutes || ' minutes')::interval) > a.appointment_time
              )
        ) THEN
            RAISE EXCEPTION 'Lỗi xung đột lịch hẹn: Căn phòng này đã có một lịch xem phòng khác trùng hoặc chồng lấn thời gian trong khoảng thời lượng dự kiến!'
            USING ERRCODE = '50010';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_appointment_schedule
BEFORE INSERT OR UPDATE ON viewings.viewing_appointments
FOR EACH ROW EXECUTE FUNCTION fn_validate_appointment_schedule();

-- 11. trg_sync_invoice_payment
CREATE OR REPLACE FUNCTION fn_sync_invoice_payment()
RETURNS TRIGGER AS $$
DECLARE
    v_target_invoice_id BIGINT;
    v_sum_paid BIGINT;
    v_total_amount BIGINT;
BEGIN
    v_target_invoice_id := COALESCE(NEW.invoice_id, OLD.invoice_id);
    
    SELECT COALESCE(SUM(amount), 0) INTO v_sum_paid
    FROM billing.payments
    WHERE invoice_id = v_target_invoice_id AND status = 'VERIFIED';
    
    SELECT total_amount INTO v_total_amount
    FROM billing.invoices
    WHERE id = v_target_invoice_id;
    
    IF v_sum_paid > v_total_amount THEN
        RAISE EXCEPTION 'Lỗi thanh toán: Tổng số tiền thanh toán đã xác thực (VERIFIED) vượt quá tổng giá trị hóa đơn (total_amount)!'
        USING ERRCODE = '50011';
    END IF;
    
    UPDATE billing.invoices
    SET paid_amount = v_sum_paid,
        status = CASE
            WHEN v_sum_paid >= v_total_amount THEN 'PAID'
            WHEN v_sum_paid > 0 THEN 'PARTIALLY_PAID'
            ELSE status
        END
    WHERE id = v_target_invoice_id;
    
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_sync_invoice_payment
AFTER INSERT OR UPDATE OR DELETE ON billing.payments
FOR EACH ROW EXECUTE FUNCTION fn_sync_invoice_payment();

-- 12. trg_validate_listing_promotion
CREATE OR REPLACE FUNCTION fn_validate_listing_promotion()
RETURNS TRIGGER AS $$
DECLARE
    v_listing_status VARCHAR(20);
BEGIN
    SELECT status INTO v_listing_status
    FROM listings.listings
    WHERE id = NEW.listing_id;
    
    IF v_listing_status IS NULL OR v_listing_status <> 'PUBLISHED' THEN
        RAISE EXCEPTION 'Lỗi gói tin VIP: Chỉ các tin đăng đang công khai (PUBLISHED) mới được kích hoạt hoặc gia hạn gói đẩy tin nổi bật!'
        USING ERRCODE = '50020';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_listing_promotion
BEFORE INSERT OR UPDATE ON monetization.listing_promotions
FOR EACH ROW EXECUTE FUNCTION fn_validate_listing_promotion();

-- 13. trg_validate_admin_audit_action
CREATE OR REPLACE FUNCTION fn_validate_admin_audit_action()
RETURNS TRIGGER AS $$
DECLARE
    v_role VARCHAR(20);
BEGIN
    SELECT role INTO v_role
    FROM identity.users
    WHERE id = NEW.admin_id;
    
    IF v_role IS NULL OR v_role <> 'ADMIN' THEN
        RAISE EXCEPTION 'Lỗi an ninh phân quyền: Chỉ người dùng có vai trò Quản trị viên (ADMIN) mới được phép thực thi và ghi nhận hành động quản trị!'
        USING ERRCODE = '50024';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_validate_admin_audit_action
BEFORE INSERT ON admin.admin_audit_logs
FOR EACH ROW EXECUTE FUNCTION fn_validate_admin_audit_action();

-- 14. trg_sync_marketing_clicks
CREATE OR REPLACE FUNCTION fn_sync_marketing_clicks()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE listings.marketing_campaigns
    SET total_clicks = total_clicks + 1
    WHERE id = NEW.campaign_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_sync_marketing_clicks
AFTER INSERT ON listings.marketing_clicks
FOR EACH ROW EXECUTE FUNCTION fn_sync_marketing_clicks();

-- 15. trg_sync_listing_views
CREATE OR REPLACE FUNCTION fn_sync_listing_views()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE listings.listings
    SET view_count = view_count + 1
    WHERE id = NEW.listing_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trg_sync_listing_views
AFTER INSERT ON listings.listing_views
FOR EACH ROW EXECUTE FUNCTION fn_sync_listing_views();

-- ==========================================================================================
-- 4. VIEWS NGHIỆP VỤ & BÁO CÁO (11 VIEWS)
-- ==========================================================================================

-- 1. vw_public_listings
CREATE OR REPLACE VIEW public.vw_public_listings AS
SELECT 
    l.id AS listing_id,
    l.title,
    l.slug,
    l.price,
    l.deposit_months,
    l.min_lease_months,
    l.allow_pets,
    l.published_at,
    l.expires_at,
    l.view_count,
    l.is_featured,
    pimg.image_url AS primary_image_url,
    COALESCE(vprom.priority_score, 0) AS vip_priority_score,
    COALESCE(vprom.is_promoted, FALSE) AS is_vip_promoted,
    r.id AS room_id,
    r.room_number,
    r.floor,
    r.area_sqm,
    r.max_occupants,
    r.room_type,
    p.id AS property_id,
    p.name AS property_name,
    p.address_detail,
    p.latitude,
    p.longitude,
    p.ward_code,
    w.name AS ward_name,
    w.district_name,
    p.province_code,
    pr.name AS province_name,
    u.id AS landlord_id,
    up.full_name AS landlord_name,
    u.phone_number AS landlord_phone
FROM listings.listings l
JOIN properties.rooms r ON r.id = l.room_id
JOIN properties.properties p ON p.id = r.property_id
JOIN properties.administrative_wards w ON w.code = p.ward_code
JOIN properties.administrative_provinces pr ON pr.code = p.province_code
JOIN identity.users u ON u.id = l.landlord_id
LEFT JOIN identity.user_profiles up ON up.user_id = u.id
LEFT JOIN LATERAL (
    SELECT img.image_url
    FROM listings.listing_images img
    WHERE img.listing_id = l.id
    ORDER BY img.is_primary DESC, img.sort_order ASC, img.id ASC
    LIMIT 1
) pimg ON TRUE
LEFT JOIN LATERAL (
    SELECT lp.priority_score, TRUE AS is_promoted
    FROM monetization.listing_promotions lp
    WHERE lp.listing_id = l.id
      AND lp.status = 'ACTIVE'
      AND lp.end_time > CURRENT_TIMESTAMP
    ORDER BY lp.priority_score DESC, lp.end_time DESC
    LIMIT 1
) vprom ON TRUE
WHERE l.status = 'PUBLISHED'
  AND (l.expires_at IS NULL OR l.expires_at > CURRENT_TIMESTAMP)
  AND r.status = 'AVAILABLE';

-- 2. vw_admin_platform_summary
CREATE OR REPLACE VIEW public.vw_admin_platform_summary AS
WITH UserStats AS (
    SELECT 
        COUNT(*) AS total_users,
        COUNT(*) FILTER (WHERE role = 'TENANT') AS total_tenants,
        COUNT(*) FILTER (WHERE role = 'LANDLORD') AS total_landlords,
        COUNT(*) FILTER (WHERE status = 'ACTIVE') AS active_users
    FROM identity.users
),
InventoryStats AS (
    SELECT 
        COUNT(DISTINCT p.id) AS total_properties,
        COUNT(r.id) AS total_rooms,
        COUNT(r.id) FILTER (WHERE r.status = 'RENTED') AS rented_rooms,
        COUNT(r.id) FILTER (WHERE r.status = 'AVAILABLE') AS available_rooms,
        ROUND(COALESCE(COUNT(r.id) FILTER (WHERE r.status = 'RENTED')::numeric / NULLIF(COUNT(r.id), 0) * 100, 0), 2) AS occupancy_rate_percent
    FROM properties.properties p
    LEFT JOIN properties.rooms r ON r.property_id = p.id
),
FinancialStats AS (
    SELECT 
        COALESCE(SUM(amount), 0) AS total_platform_revenue_vnd,
        COALESCE(SUM(amount) FILTER (WHERE revenue_stream = 'SAAS_SUBSCRIPTION'), 0) AS saas_revenue_vnd,
        COALESCE(SUM(amount) FILTER (WHERE revenue_stream = 'LISTING_VIP_BOOST'), 0) AS vip_boost_revenue_vnd,
        COALESCE(SUM(amount) FILTER (WHERE revenue_stream = 'BROKERAGE_COMMISSION'), 0) AS commission_revenue_vnd,
        COALESCE(SUM(amount) FILTER (WHERE revenue_stream NOT IN ('SAAS_SUBSCRIPTION', 'LISTING_VIP_BOOST', 'BROKERAGE_COMMISSION')), 0) AS other_platform_revenue_vnd
    FROM monetization.platform_transactions
    WHERE status = 'COMPLETED'
),
ModerationStats AS (
    SELECT 
        COUNT(*) FILTER (WHERE status = 'PENDING') AS pending_reports_count,
        COUNT(*) FILTER (WHERE status = 'RESOLVED') AS resolved_reports_count
    FROM admin.content_reports
),
ListingMetricStats AS (
    SELECT 
        COUNT(*) FILTER (WHERE status = 'PUBLISHED') AS published_listings_count
    FROM listings.listings
)
SELECT 
    u.*,
    i.*,
    f.*,
    m.*,
    l.*
FROM UserStats u
CROSS JOIN InventoryStats i
CROSS JOIN FinancialStats f
CROSS JOIN ModerationStats m
CROSS JOIN ListingMetricStats l;

-- 3. vw_admin_pending_reports
CREATE OR REPLACE VIEW public.vw_admin_pending_reports AS
SELECT 
    cr.id AS report_id,
    cr.reporter_id,
    up.full_name AS reporter_name,
    u.email AS reporter_email,
    u.phone_number AS reporter_phone,
    cr.target_type,
    cr.target_id,
    cr.report_reason,
    cr.description,
    cr.evidence_urls,
    cr.created_at,
    ROUND(EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - cr.created_at))/3600.0, 1) AS hours_pending
FROM admin.content_reports cr
JOIN identity.users u ON u.id = cr.reporter_id
LEFT JOIN identity.user_profiles up ON up.user_id = u.id
WHERE cr.status = 'PENDING';

-- 4. vw_landlord_dashboard_summary
CREATE OR REPLACE VIEW public.vw_landlord_dashboard_summary AS
WITH PropertyStats AS (
    SELECT 
        p.landlord_id,
        COUNT(DISTINCT p.id) AS total_properties,
        COUNT(r.id) AS total_rooms,
        COUNT(r.id) FILTER (WHERE r.status = 'AVAILABLE') AS available_rooms,
        COUNT(r.id) FILTER (WHERE r.status = 'RENTED') AS rented_rooms,
        ROUND(COALESCE(COUNT(r.id) FILTER (WHERE r.status = 'RENTED')::numeric / NULLIF(COUNT(r.id), 0) * 100, 0), 2) AS occupancy_rate_percent
    FROM properties.properties p
    LEFT JOIN properties.rooms r ON r.property_id = p.id
    GROUP BY p.landlord_id
),
ContractStats AS (
    SELECT 
        c.landlord_id,
        COUNT(c.id) AS active_contracts,
        COALESCE(SUM(c.monthly_rent), 0) AS total_monthly_expected_rent
    FROM leasing.contracts c
    WHERE c.status = 'ACTIVE'
    GROUP BY c.landlord_id
),
ListingStats AS (
    SELECT 
        l.landlord_id,
        COUNT(l.id) FILTER (WHERE l.status = 'PUBLISHED') AS published_listings,
        COALESCE(SUM(l.view_count), 0) AS total_listing_views
    FROM listings.listings l
    GROUP BY l.landlord_id
)
SELECT 
    u.id AS landlord_id,
    COALESCE(ps.total_properties, 0) AS total_properties,
    COALESCE(ps.total_rooms, 0) AS total_rooms,
    COALESCE(ps.available_rooms, 0) AS available_rooms,
    COALESCE(ps.rented_rooms, 0) AS rented_rooms,
    COALESCE(ps.occupancy_rate_percent, 0) AS occupancy_rate_percent,
    COALESCE(cs.active_contracts, 0) AS active_contracts,
    COALESCE(cs.total_monthly_expected_rent, 0) AS total_monthly_expected_rent,
    COALESCE(ls.published_listings, 0) AS published_listings,
    COALESCE(ls.total_listing_views, 0) AS total_listing_views
FROM identity.users u
LEFT JOIN PropertyStats ps ON ps.landlord_id = u.id
LEFT JOIN ContractStats cs ON cs.landlord_id = u.id
LEFT JOIN ListingStats ls ON ls.landlord_id = u.id
WHERE u.role = 'LANDLORD';

-- 5. vw_landlord_property_summary
CREATE OR REPLACE VIEW public.vw_landlord_property_summary AS
SELECT 
    p.id AS property_id,
    p.landlord_id,
    p.name AS property_name,
    p.address_detail,
    w.name AS ward_name,
    w.district_name,
    pr.name AS province_name,
    COUNT(r.id) AS total_rooms,
    COUNT(r.id) FILTER (WHERE r.status = 'AVAILABLE') AS available_rooms,
    COUNT(r.id) FILTER (WHERE r.status = 'RENTED') AS rented_rooms,
    ROUND(COALESCE(COUNT(r.id) FILTER (WHERE r.status = 'RENTED')::numeric / NULLIF(COUNT(r.id), 0) * 100, 0), 2) AS occupancy_rate_percent,
    COALESCE(SUM(r.base_price), 0) AS total_potential_monthly_revenue
FROM properties.properties p
JOIN properties.administrative_wards w ON w.code = p.ward_code
JOIN properties.administrative_provinces pr ON pr.code = p.province_code
LEFT JOIN properties.rooms r ON r.property_id = p.id
GROUP BY p.id, p.landlord_id, p.name, p.address_detail, w.name, w.district_name, pr.name;

-- 6. vw_expiring_contracts_in_30_days
CREATE OR REPLACE VIEW public.vw_expiring_contracts_in_30_days AS
SELECT 
    c.id AS contract_id,
    c.contract_code,
    c.landlord_id,
    c.tenant_id,
    up.full_name AS tenant_name,
    u.phone_number AS tenant_phone,
    r.id AS room_id,
    r.room_number,
    p.id AS property_id,
    p.name AS property_name,
    c.start_date,
    c.end_date,
    (c.end_date - CURRENT_DATE) AS days_remaining,
    c.monthly_rent,
    c.deposit_amount,
    c.deposit_status
FROM leasing.contracts c
JOIN identity.users u ON u.id = c.tenant_id
LEFT JOIN identity.user_profiles up ON up.user_id = u.id
JOIN properties.rooms r ON r.id = c.room_id
JOIN properties.properties p ON p.id = r.property_id
WHERE c.status = 'ACTIVE'
  AND c.end_date BETWEEN CURRENT_DATE AND (CURRENT_DATE + INTERVAL '30 days');

-- 7. vw_overdue_debt_aging
CREATE OR REPLACE VIEW public.vw_overdue_debt_aging AS
SELECT 
    i.id AS invoice_id,
    i.invoice_code,
    c.id AS contract_id,
    c.landlord_id,
    c.tenant_id,
    up.full_name AS tenant_name,
    u.phone_number AS tenant_phone,
    r.room_number,
    p.name AS property_name,
    i.billing_month,
    i.billing_year,
    i.total_amount,
    i.paid_amount,
    (i.total_amount - i.paid_amount) AS remaining_debt,
    i.due_date,
    (CURRENT_DATE - i.due_date) AS overdue_days,
    CASE 
        WHEN CURRENT_DATE <= i.due_date THEN 'CHUA_DEN_HAN'
        WHEN (CURRENT_DATE - i.due_date) BETWEEN 1 AND 30 THEN 'QUA_HAN_1_30_NGAY'
        WHEN (CURRENT_DATE - i.due_date) BETWEEN 31 AND 60 THEN 'QUA_HAN_31_60_NGAY'
        ELSE 'QUA_HAN_TREN_60_NGAY'
    END AS aging_group
FROM billing.invoices i
JOIN leasing.contracts c ON c.id = i.contract_id
JOIN identity.users u ON u.id = c.tenant_id
LEFT JOIN identity.user_profiles up ON up.user_id = u.id
JOIN properties.rooms r ON r.id = c.room_id
JOIN properties.properties p ON p.id = r.property_id
WHERE i.status IN ('UNPAID', 'PARTIALLY_PAID', 'OVERDUE');

-- 8. vw_landlord_daily_stats
CREATE OR REPLACE VIEW public.vw_landlord_daily_stats AS
SELECT 
    u.id AS landlord_id,
    COUNT(DISTINCT l.id) FILTER (WHERE l.status = 'PUBLISHED') AS active_listings_count,
    COUNT(DISTINCT a.id) FILTER (WHERE a.status = 'CONFIRMED' AND a.appointment_time::date = CURRENT_DATE) AS appointments_today_count,
    COUNT(DISTINCT ld.id) FILTER (WHERE ld.status = 'NEW') AS new_leads_count
FROM identity.users u
LEFT JOIN listings.listings l ON l.landlord_id = u.id
LEFT JOIN viewings.viewing_appointments a ON a.listing_id = l.id
LEFT JOIN messaging.leads ld ON ld.listing_id = l.id
WHERE u.role = 'LANDLORD'
GROUP BY u.id;

-- 9. vw_tenant_dashboard_summary
CREATE OR REPLACE VIEW public.vw_tenant_dashboard_summary AS
SELECT 
    u.id AS tenant_id,
    COUNT(DISTINCT lv.id) AS total_viewed_listings,
    COUNT(DISTINCT sl.id) AS total_saved_listings,
    COUNT(DISTINCT va.id) AS total_appointments,
    COUNT(DISTINCT c.id) AS total_conversations
FROM identity.users u
LEFT JOIN listings.listing_views lv ON lv.user_id = u.id
LEFT JOIN listings.saved_listings sl ON sl.user_id = u.id
LEFT JOIN viewings.viewing_appointments va ON va.tenant_id = u.id
LEFT JOIN messaging.conversations c ON (c.initiator_id = u.id OR c.recipient_id = u.id)
WHERE u.role = 'TENANT'
GROUP BY u.id;

-- 10. vw_monthly_utility_report
CREATE OR REPLACE VIEW public.vw_monthly_utility_report AS
SELECT 
    ur.id AS reading_id,
    p.id AS property_id,
    p.landlord_id,
    p.name AS property_name,
    r.id AS room_id,
    r.room_number,
    c.id AS contract_id,
    ur.billing_month,
    ur.billing_year,
    ur.electricity_old,
    ur.electricity_new,
    ur.electricity_usage,
    ur.electricity_cost,
    ur.water_old,
    ur.water_new,
    ur.water_usage,
    ur.water_cost,
    (ur.electricity_cost + ur.water_cost) AS total_utility_cost,
    ur.reading_date
FROM billing.utility_readings ur
JOIN properties.rooms r ON r.id = ur.room_id
JOIN properties.properties p ON p.id = r.property_id
JOIN leasing.contracts c ON c.id = ur.contract_id;

-- 11. vw_tenant_lease_status_filter
CREATE OR REPLACE VIEW public.vw_tenant_lease_status_filter AS
SELECT 
    u.id AS tenant_id,
    up.full_name AS tenant_name,
    u.email AS tenant_email,
    u.phone_number AS tenant_phone,
    c.id AS contract_id,
    c.contract_code,
    c.landlord_id,
    c.room_id,
    r.room_number,
    p.id AS property_id,
    p.name AS property_name,
    c.start_date,
    c.end_date,
    c.monthly_rent,
    c.status AS contract_status,
    CASE 
        WHEN c.status = 'ACTIVE' AND c.end_date BETWEEN CURRENT_DATE AND (CURRENT_DATE + INTERVAL '30 days') THEN 'EXPIRING_SOON'
        WHEN c.status = 'ACTIVE' THEN 'RENTING'
        WHEN c.status IN ('TERMINATED', 'EXPIRED') THEN 'MOVED_OUT'
        ELSE 'OTHER'
    END AS lease_lifecycle_status,
    COALESCE(debt.total_unpaid_amount, 0) AS total_overdue_debt,
    COALESCE(debt.unpaid_invoice_count, 0) AS overdue_invoice_count
FROM identity.users u
JOIN leasing.contracts c ON c.tenant_id = u.id
JOIN properties.rooms r ON r.id = c.room_id
JOIN properties.properties p ON p.id = r.property_id
LEFT JOIN identity.user_profiles up ON up.user_id = u.id
LEFT JOIN (
    SELECT 
        contract_id,
        COUNT(id) AS unpaid_invoice_count,
        SUM(total_amount - paid_amount) AS total_unpaid_amount
    FROM billing.invoices
    WHERE status IN ('UNPAID', 'PARTIALLY_PAID', 'OVERDUE')
      AND due_date < CURRENT_DATE
    GROUP BY contract_id
) debt ON debt.contract_id = c.id
WHERE u.role = 'TENANT';

-- ==========================================================================================
-- 5. STORED PROCEDURES & FUNCTIONS
-- ==========================================================================================

-- 1. sp_book_viewing_appointment (Sử dụng pg_advisory_xact_lock theo room_id chống Deadlock & Race Condition)
CREATE OR REPLACE FUNCTION viewings.sp_book_viewing_appointment(
    p_tenant_id BIGINT,
    p_listing_id BIGINT,
    p_appointment_time TIMESTAMPTZ,
    p_duration_minutes INT DEFAULT 30,
    p_notes VARCHAR(500) DEFAULT NULL
)
RETURNS BIGINT AS $$
DECLARE
    v_room_id BIGINT;
    v_new_id BIGINT;
BEGIN
    SELECT room_id INTO v_room_id
    FROM listings.listings
    WHERE id = p_listing_id AND status = 'PUBLISHED';
    
    IF v_room_id IS NULL THEN
        RAISE EXCEPTION 'Tin đăng không tồn tại hoặc chưa công khai khi đặt lịch!' USING ERRCODE = '50012';
    END IF;
    
    -- Tuần tự hóa theo room_id bằng PostgreSQL Advisory Lock
    PERFORM pg_advisory_xact_lock(hashtext('RoomAppointmentLock_' || v_room_id::text));
    
    INSERT INTO viewings.viewing_appointments (
        tenant_id, listing_id, appointment_time, duration_minutes, notes, status, remind_at
    ) VALUES (
        p_tenant_id, p_listing_id, p_appointment_time, p_duration_minutes, p_notes, 'PENDING',
        p_appointment_time - INTERVAL '30 minutes'
    )
    RETURNING id INTO v_new_id;
    
    RETURN v_new_id;
END;
$$ LANGUAGE plpgsql;

-- 2. sp_process_daily_contract_expiration_and_reminders
CREATE OR REPLACE FUNCTION leasing.sp_process_daily_contract_expiration_and_reminders()
RETURNS VOID AS $$
BEGIN
    -- 1. Quét hợp đồng hết hạn
    UPDATE leasing.contracts
    SET status = 'EXPIRED',
        updated_at = CURRENT_TIMESTAMP
    WHERE status = 'ACTIVE'
      AND end_date < CURRENT_DATE;
      
    -- 2. Quét tin đăng hết hạn
    UPDATE listings.listings
    SET status = 'EXPIRED',
        updated_at = CURRENT_TIMESTAMP
    WHERE status = 'PUBLISHED'
      AND expires_at IS NOT NULL
      AND expires_at < CURRENT_TIMESTAMP;
      
    -- 3. Quét gói đẩy tin VIP hết hạn
    UPDATE monetization.listing_promotions
    SET status = 'EXPIRED'
    WHERE status = 'ACTIVE'
      AND end_time < CURRENT_TIMESTAMP;
      
    -- 4. Quét thuê bao SaaS hết hạn (nếu không bật auto_renew)
    UPDATE monetization.landlord_subscriptions
    SET status = 'EXPIRED'
    WHERE status = 'ACTIVE'
      AND end_date < CURRENT_DATE
      AND auto_renew = FALSE;
END;
$$ LANGUAGE plpgsql;

-- 3. fn_get_landlord_financial_report
CREATE OR REPLACE FUNCTION billing.fn_get_landlord_financial_report(
    p_landlord_id BIGINT,
    p_year INT,
    p_month INT DEFAULT NULL
)
RETURNS TABLE (
    period_year INT,
    period_month INT,
    accrued_revenue_vnd BIGINT,
    cash_inflow_vnd BIGINT,
    overdue_debt_vnd BIGINT
) AS $$
BEGIN
    RETURN QUERY
    WITH Accrued AS (
        SELECT 
            i.billing_year,
            i.billing_month,
            SUM(i.total_amount) AS accrued_rev,
            SUM(CASE WHEN i.status IN ('UNPAID', 'PARTIALLY_PAID', 'OVERDUE') AND i.due_date < CURRENT_DATE THEN (i.total_amount - i.paid_amount) ELSE 0 END) AS debt_amt
        FROM billing.invoices i
        JOIN leasing.contracts c ON c.id = i.contract_id
        WHERE c.landlord_id = p_landlord_id
          AND i.billing_year = p_year
          AND (p_month IS NULL OR i.billing_month = p_month)
        GROUP BY i.billing_year, i.billing_month
    ),
    Cash AS (
        SELECT 
            EXTRACT(YEAR FROM p.paid_at)::int AS pay_year,
            EXTRACT(MONTH FROM p.paid_at)::int AS pay_month,
            SUM(p.amount) AS cash_in
        FROM billing.payments p
        JOIN billing.invoices i ON i.id = p.invoice_id
        JOIN leasing.contracts c ON c.id = i.contract_id
        WHERE c.landlord_id = p_landlord_id
          AND p.status = 'VERIFIED'
          AND EXTRACT(YEAR FROM p.paid_at) = p_year
          AND (p_month IS NULL OR EXTRACT(MONTH FROM p.paid_at) = p_month)
        GROUP BY EXTRACT(YEAR FROM p.paid_at), EXTRACT(MONTH FROM p.paid_at)
    )
    SELECT 
        COALESCE(a.billing_year, c.pay_year) AS period_year,
        COALESCE(a.billing_month, c.pay_month) AS period_month,
        COALESCE(a.accrued_rev, 0)::BIGINT AS accrued_revenue_vnd,
        COALESCE(c.cash_in, 0)::BIGINT AS cash_inflow_vnd,
        COALESCE(a.debt_amt, 0)::BIGINT AS overdue_debt_vnd
    FROM Accrued a
    FULL OUTER JOIN Cash c ON c.pay_year = a.billing_year AND c.pay_month = a.billing_month
    ORDER BY period_year, period_month;
END;
$$ LANGUAGE plpgsql;

-- 4. fn_search_properties_within_radius (Tìm kiếm bất động sản theo bán kính PostGIS)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'postgis') THEN
        EXECUTE $fn$
        CREATE OR REPLACE FUNCTION properties.fn_search_properties_within_radius(
            p_center_lng DOUBLE PRECISION,
            p_center_lat DOUBLE PRECISION,
            p_radius_meters DOUBLE PRECISION DEFAULT 3000
        )
        RETURNS TABLE (
            property_id BIGINT,
            property_name VARCHAR(200),
            address_detail VARCHAR(255),
            latitude DECIMAL(9,6),
            longitude DECIMAL(9,6),
            distance_meters DOUBLE PRECISION
        ) AS $body$
        BEGIN
            RETURN QUERY
            SELECT 
                p.id,
                p.name,
                p.address_detail,
                p.latitude,
                p.longitude,
                ROUND(ST_DistanceSphere(p.geom, ST_SetSRID(ST_MakePoint(p_center_lng, p_center_lat), 4326))::numeric, 1)::double precision AS distance_meters
            FROM properties.properties p
            WHERE p.geom IS NOT NULL
              AND ST_DWithin(
                  p.geom::geography, 
                  ST_SetSRID(ST_MakePoint(p_center_lng, p_center_lat), 4326)::geography, 
                  p_radius_meters
              )
            ORDER BY distance_meters ASC;
        END;
        $body$ LANGUAGE plpgsql;
        $fn$;
    END IF;
END $$;

-- ==========================================================================================
-- 6. DỮ LIỆU MẪU SEED CHUẨN HÓA (44 TABLES SEED DATA)
-- ==========================================================================================

-- 1. Provinces & Wards
INSERT INTO properties.administrative_provinces (code, name, name_en, full_name) VALUES
('01', 'Hà Nội', 'Ha Noi', 'Thành phố Hà Nội'),
('79', 'Hồ Chí Minh', 'Ho Chi Minh', 'Thành phố Hồ Chí Minh')
ON CONFLICT (code) DO NOTHING;

INSERT INTO properties.administrative_wards (code, name, name_en, full_name, district_name, province_code) VALUES
('001', 'Dịch Vọng Hậu', 'Dich Vong Hau', 'Phường Dịch Vọng Hậu', 'Cầu Giấy', '01'),
('002', 'Bến Nghé', 'Ben Nghe', 'Phường Bến Nghé', 'Quận 1', '79')
ON CONFLICT (code) DO NOTHING;

-- 2. Users (Admin, Landlord, Tenants)
INSERT INTO identity.users (id, email, phone_number, password_hash, role, status) VALUES
(1, 'admin@homematch.vn', '0901000001', '$2a$12$e80yV9Z.X9pP2YkQ.Z5nOe3gH8YJ2Y.Z5nOe3gH8YJ2Y.Z5nOe3gH', 'ADMIN', 'ACTIVE'),
(2, 'chutro.hoang@gmail.com', '0902000002', '$2a$12$e80yV9Z.X9pP2YkQ.Z5nOe3gH8YJ2Y.Z5nOe3gH8YJ2Y.Z5nOe3gH', 'LANDLORD', 'ACTIVE'),
(3, 'khachthue.an@gmail.com', '0903000003', '$2a$12$e80yV9Z.X9pP2YkQ.Z5nOe3gH8YJ2Y.Z5nOe3gH8YJ2Y.Z5nOe3gH', 'TENANT', 'ACTIVE'),
(4, 'khachthue.binh@gmail.com', NULL, '$2a$12$e80yV9Z.X9pP2YkQ.Z5nOe3gH8YJ2Y.Z5nOe3gH8YJ2Y.Z5nOe3gH', 'TENANT', 'ACTIVE')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('identity.users', 'id'), (SELECT MAX(id) FROM identity.users));

-- 3. UserProfiles
INSERT INTO identity.user_profiles (user_id, full_name, avatar_url, date_of_birth, gender) VALUES
(1, 'Quản Trị Viên Hệ Thống', 'https://assets.homematch.vn/avatars/admin.png', '1990-01-01', 'OTHER'),
(2, 'Nguyễn Văn Hoàng (Chủ Trọ)', 'https://assets.homematch.vn/avatars/landlord_hoang.png', '1982-05-15', 'MALE'),
(3, 'Trần Bình An (Khách Thuê)', 'https://assets.homematch.vn/avatars/tenant_an.png', '2001-08-20', 'MALE'),
(4, 'Lê Thị Bình (Khách Thuê)', 'https://assets.homematch.vn/avatars/tenant_binh.png', '2002-11-10', 'FEMALE')
ON CONFLICT (user_id) DO NOTHING;

-- 4. TenantIdentityProfiles & Shares
INSERT INTO identity.tenant_identity_profiles (id, tenant_id, id_card_number_encrypted, id_card_last4, id_card_hmac_hash, full_name, date_of_birth, front_card_url, back_card_url, is_system_verified) VALUES
(1, 3, 'ENC_CCCD_001201001234_AES256', '1234', 'a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e', 'TRẦN BÌNH AN', '2001-08-20', 'https://assets.homematch.vn/kyc/an_front.jpg', 'https://assets.homematch.vn/kyc/an_back.jpg', TRUE)
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('identity.tenant_identity_profiles', 'id'), (SELECT MAX(id) FROM identity.tenant_identity_profiles));

INSERT INTO identity.tenant_identity_shares (id, tenant_profile_id, target_landlord_id, status) VALUES
(1, 1, 2, 'APPROVED')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('identity.tenant_identity_shares', 'id'), (SELECT MAX(id) FROM identity.tenant_identity_shares));

-- 5. Properties & Rooms
INSERT INTO properties.properties (id, landlord_id, name, province_code, ward_code, address_detail, property_type, latitude, longitude) VALUES
(1, 2, 'Tòa Nhà HomeMatch Cầu Giấy', '01', '001', 'Số 18 Ngõ 86 Phố Duy Tân', 'BOARDING_HOUSE', 21.031500, 105.783200)
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('properties.properties', 'id'), (SELECT MAX(id) FROM properties.properties));

INSERT INTO properties.rooms (id, property_id, room_number, floor, area_sqm, max_occupants, base_price, room_type, status) VALUES
(1, 1, 'P.201', 2, 28.50, 2, 4500000, 'STUDIO', 'AVAILABLE'),
(2, 1, 'P.202', 2, 32.00, 3, 5200000, '1B1B', 'RENTED'),
(3, 1, 'P.301', 3, 25.00, 2, 4000000, 'STUDIO', 'AVAILABLE')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('properties.rooms', 'id'), (SELECT MAX(id) FROM properties.rooms));

-- 6. Amenities & RoomAmenities
INSERT INTO properties.amenities (id, name, code, icon_url, category) VALUES
(1, 'Điều hòa Inverter', 'AIR_CONDITIONER', 'https://assets.homematch.vn/icons/ac.svg', 'ROOM'),
(2, 'Bình nóng lạnh', 'WATER_HEATER', 'https://assets.homematch.vn/icons/heater.svg', 'ROOM'),
(3, 'Máy giặt chung', 'WASHING_MACHINE', 'https://assets.homematch.vn/icons/washer.svg', 'BUILDING'),
(4, 'Khóa cửa vân tay', 'SMART_LOCK', 'https://assets.homematch.vn/icons/smartlock.svg', 'SECURITY')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('properties.amenities', 'id'), (SELECT MAX(id) FROM properties.amenities));

INSERT INTO properties.room_amenities (room_id, amenity_id) VALUES
(1, 1), (1, 2), (1, 4),
(2, 1), (2, 2), (2, 3), (2, 4),
(3, 1), (3, 2)
ON CONFLICT (room_id, amenity_id) DO NOTHING;

-- 7. UtilityTariffs
INSERT INTO properties.utility_tariffs (id, property_id, room_id, tariff_type, calc_method, unit_price) VALUES
(1, 1, NULL, 'ELECTRICITY', 'PER_KWH', 3800),
(2, 1, NULL, 'WATER', 'PER_M3', 25000),
(3, 1, NULL, 'INTERNET', 'FIXED_PER_MONTH', 100000),
(4, 1, NULL, 'SERVICE', 'FIXED_PER_MONTH', 50000)
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('properties.utility_tariffs', 'id'), (SELECT MAX(id) FROM properties.utility_tariffs));

-- 8. Listings & Images
INSERT INTO listings.listings (id, room_id, landlord_id, title, slug, description, price, deposit_months, min_lease_months, allow_pets, published_at, expires_at, status, view_count, is_featured) VALUES
(1, 1, 2, 'Phòng Studio Full Đồ Ban Công Thoáng Phố Duy Tân Cầu Giấy', 'phong-studio-full-do-duy-tan-cau-giay-p201', 'Phòng đầy đủ tiện nghi, có ban công thoáng mát, an ninh vân tay 24/7.', 4500000, 1, 6, FALSE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '30 days', 'PUBLISHED', 45, TRUE),
(2, 3, 2, 'Phòng Trọ Khép Kín Giá Rẻ Tầng 3 Ngõ 86 Duy Tân', 'phong-tro-khep-kin-gia-re-duy-tan-p301', 'Phòng sạch sẽ, điều hòa nóng lạnh, giờ giấc tự do.', 4000000, 1, 6, FALSE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '30 days', 'PUBLISHED', 12, FALSE)
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('listings.listings', 'id'), (SELECT MAX(id) FROM listings.listings));

INSERT INTO listings.listing_images (id, listing_id, image_url, is_primary, sort_order, media_type, thumbnail_url, duration_seconds) VALUES
(1, 1, 'https://assets.homematch.vn/listings/room1_main.jpg', TRUE, 1, 'IMAGE', NULL, NULL),
(2, 1, 'https://assets.homematch.vn/listings/room1_video_tour.mp4', FALSE, 2, 'VIDEO', 'https://assets.homematch.vn/listings/room1_video_thumb.jpg', 67),
(3, 1, 'https://assets.homematch.vn/listings/room1_wc.jpg', FALSE, 3, 'IMAGE', NULL, NULL),
(4, 2, 'https://assets.homematch.vn/listings/room3_main.jpg', TRUE, 1, 'IMAGE', NULL, NULL)
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('listings.listing_images', 'id'), (SELECT MAX(id) FROM listings.listing_images));

-- 9. Contracts
INSERT INTO leasing.contracts (id, contract_code, room_id, tenant_id, landlord_id, start_date, end_date, monthly_rent, deposit_amount, deposit_status, billing_due_day, status, signed_at) VALUES
(1, 'HD-2026-001', 2, 3, 2, '2026-01-01', '2026-12-31', 5200000, 5200000, 'PAID_TO_LANDLORD', 5, 'ACTIVE', '2025-12-28 10:00:00+07')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('leasing.contracts', 'id'), (SELECT MAX(id) FROM leasing.contracts));

-- Update TenantIdentityShares contract_id
UPDATE identity.tenant_identity_shares SET contract_id = 1 WHERE id = 1;

-- 10. UtilityReadings & Invoices & Payments
INSERT INTO billing.utility_readings (id, room_id, contract_id, recorded_by, billing_month, billing_year, electricity_old, electricity_new, electricity_cost, water_old, water_new, water_cost, reading_date) VALUES
(1, 2, 1, 2, 10, 2026, 1200, 1350, 570000, 85, 91, 150000, '2026-10-25')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('billing.utility_readings', 'id'), (SELECT MAX(id) FROM billing.utility_readings));

INSERT INTO billing.invoices (id, invoice_code, contract_id, utility_reading_id, billing_month, billing_year, rent_amount, electricity_amount, water_amount, service_amount, discount_amount, paid_amount, due_date, status) VALUES
(1, 'INV-202610-001', 1, 1, 10, 2026, 5200000, 570000, 150000, 150000, 0, 0, '2026-11-05', 'UNPAID')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('billing.invoices', 'id'), (SELECT MAX(id) FROM billing.invoices));

INSERT INTO billing.payments (id, invoice_id, amount, payment_method, transaction_ref, status, verified_by, verified_at) VALUES
(1, 1, 6070000, 'BANK_TRANSFER', 'FT261025888999', 'VERIFIED', 2, CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('billing.payments', 'id'), (SELECT MAX(id) FROM billing.payments));

-- 11. Appointments & VirtualViewings
INSERT INTO viewings.viewing_appointments (id, tenant_id, listing_id, appointment_time, duration_minutes, notes, status) VALUES
(1, 4, 1, CURRENT_TIMESTAMP + INTERVAL '2 days', 30, 'Muốn xem phòng vào buổi chiều sau 17h', 'CONFIRMED')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('viewings.viewing_appointments', 'id'), (SELECT MAX(id) FROM viewings.viewing_appointments));

INSERT INTO viewings.virtual_viewings (id, appointment_id, room_url, status) VALUES
(1, 1, 'https://meet.google.com/hom-mtch-demo', 'SCHEDULED')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('viewings.virtual_viewings', 'id'), (SELECT MAX(id) FROM viewings.virtual_viewings));

-- 12. Monetization & Admin Seeds
INSERT INTO admin.platform_configs (config_key, config_value, data_type, category, description, updated_by) VALUES
('MAX_FREE_LISTINGS_PER_LANDLORD', '3', 'INT', 'LISTING', 'Hạn mức tin đăng miễn phí cho mỗi chủ nhà', 1),
('LISTING_DEFAULT_EXPIRY_DAYS', '30', 'INT', 'LISTING', 'Thời hạn hiển thị mặc định của tin đăng (ngày)', 1),
('DEPOSIT_ESCROW_FEE_PERCENT', '1.5', 'DECIMAL', 'BILLING', 'Tỷ lệ phí dịch vụ giữ cọc ký quỹ an toàn (%)', 1)
ON CONFLICT (config_key) DO NOTHING;

INSERT INTO admin.admin_audit_logs (id, admin_id, action_type, target_table, target_id, old_values, new_values, reason) VALUES
(1, 1, 'APPROVE_KYC', 'TenantIdentityProfiles', 1, '{"is_system_verified": false}', '{"is_system_verified": true}', 'Hồ sơ CCCD gắn chip hợp lệ và ảnh chụp rõ nét')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('admin.admin_audit_logs', 'id'), (SELECT MAX(id) FROM admin.admin_audit_logs));

INSERT INTO admin.content_reports (id, reporter_id, target_type, target_id, report_reason, description, status, resolved_by, resolved_at, resolution_notes) VALUES
(1, 3, 'LISTING', 2, 'INCORRECT_PRICING', 'Tin đăng ghi giá 4 triệu nhưng chủ trọ báo 4.5 triệu', 'RESOLVED', 1, CURRENT_TIMESTAMP, 'Đã nhắc nhở chủ nhà cập nhật đúng giá niêm yết')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('admin.content_reports', 'id'), (SELECT MAX(id) FROM admin.content_reports));

INSERT INTO monetization.platform_service_plans (id, name, code, plan_type, price, duration_days, max_rooms_allowed, vip_boost_quota, ai_query_quota, is_active) VALUES
(1, 'Gói Quản Lý Trọ Pro', 'SAAS_PRO_MONTHLY', 'SAAS_PMS', 299000, 30, 30, 2, 100, TRUE),
(2, 'Gói Đẩy Tin VIP Bạc 7 Ngày', 'VIP_BOOST_SILVER_7D', 'LISTING_BOOST', 199000, 7, NULL, 1, 0, TRUE)
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('monetization.platform_service_plans', 'id'), (SELECT MAX(id) FROM monetization.platform_service_plans));

INSERT INTO monetization.landlord_subscriptions (id, landlord_id, plan_id, start_date, end_date, status, auto_renew) VALUES
(1, 2, 1, CURRENT_DATE, CURRENT_DATE + INTERVAL '30 days', 'ACTIVE', TRUE)
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('monetization.landlord_subscriptions', 'id'), (SELECT MAX(id) FROM monetization.landlord_subscriptions));

INSERT INTO monetization.listing_promotions (id, listing_id, promotion_type, priority_score, start_time, end_time, amount_paid, status) VALUES
(1, 1, 'TOP_RECOMMENDED', 10, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '7 days', 199000, 'ACTIVE')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('monetization.listing_promotions', 'id'), (SELECT MAX(id) FROM monetization.listing_promotions));

INSERT INTO monetization.platform_transactions (id, transaction_code, payer_user_id, revenue_stream, reference_table, reference_id, amount, payment_method, status, completed_at) VALUES
(1, 'TXN_PLT_20261025_001', 2, 'SAAS_SUBSCRIPTION', 'LandlordSubscriptions', 1, 299000, 'VNPAY', 'COMPLETED', CURRENT_TIMESTAMP),
(2, 'TXN_PLT_20261025_002', 2, 'LISTING_VIP_BOOST', 'ListingPromotions', 1, 199000, 'MOMO', 'COMPLETED', CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('monetization.platform_transactions', 'id'), (SELECT MAX(id) FROM monetization.platform_transactions));
