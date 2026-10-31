DO $$ DECLARE t record; value text; BEGIN
IF EXISTS(SELECT 1 FROM pg_constraint c JOIN pg_class target ON target.oid=c.confrelid JOIN pg_namespace n ON n.oid=target.relnamespace JOIN pg_class source ON source.oid=c.conrelid JOIN pg_namespace sn ON sn.oid=source.relnamespace WHERE c.contype='f' AND n.nspname='identity' AND sn.nspname<>'identity') THEN RAISE EXCEPTION 'External foreign keys prevent rollback'; END IF;
IF EXISTS(SELECT 1 FROM pg_tables WHERE schemaname='identity' AND tablename<>'__EFMigrationsHistory' AND tablename NOT IN(SELECT table_name FROM identity_uuid_archive.conversion_manifest)) THEN RAISE EXCEPTION 'Additional Identity tables prevent rollback'; END IF;
FOR t IN SELECT * FROM identity_uuid_archive.conversion_manifest LOOP
 EXECUTE format($q$SELECT md5(coalesce(string_agg(v,chr(10) ORDER BY v),'')) FROM (SELECT row_to_json(r)::text v FROM identity.%I r) q$q$,t.table_name) INTO value;
 IF value<>t.fingerprint THEN RAISE EXCEPTION 'Identity changed after conversion; rollback would lose data'; END IF;
END LOOP;
FOR t IN SELECT tablename FROM pg_tables WHERE schemaname='identity' AND tablename<>'__EFMigrationsHistory' LOOP
 EXECUTE format('DROP TABLE IF EXISTS identity.%I CASCADE',t.tablename);
END LOOP;
DROP TABLE identity_uuid_archive.conversion_manifest;
DROP TABLE identity_uuid_archive.credential_id_map;
DROP TABLE identity_uuid_archive.user_id_map;
FOR t IN SELECT tablename FROM pg_tables WHERE schemaname='identity_uuid_archive' LOOP
 EXECUTE format('ALTER TABLE identity_uuid_archive.%I SET SCHEMA identity',t.tablename);
END LOOP;
DROP SCHEMA identity_uuid_archive;
DROP FUNCTION IF EXISTS identity.check_refresh_family();
END $$;
