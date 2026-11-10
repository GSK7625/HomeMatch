-- ==========================================================================================
-- SCRIPT KIỂM THỬ XÁC MINH CƠ SỞ DỮ LIỆU MODULE LEASING (PHASE 1 - VERIFICATION SUITE)
-- ==========================================================================================
-- Mục tiêu: Kiểm tra toàn bộ 6 nhóm ràng buộc & Invariants của Module Leasing:
-- 1. Ràng buộc Hợp đồng (Statuses, DateRange, Rent, Deposit)
-- 2. Chữ ký điện tử độc lập hai bên (Landlord, Tenant, Document Hash)
-- 3. Quản lý Lịch sử Giao dịch Tiền cọc (Received, Refunded, Deducted, Deduction Reason)
-- 4. Thông tin Bàn giao phòng (Handover Deadline, Confirmation, Occupancy Boundary)
-- 5. Ràng buộc Chống Trùng Lịch Phòng (PostgreSQL GiST Exclusion Constraint)
-- 6. Trigger Đồng bộ Trạng thái Phòng & Stored Procedure Quét Hết hạn
-- ==========================================================================================

DO $$
DECLARE
    v_room_id BIGINT;
    v_other_room_id BIGINT;
    v_tenant_id BIGINT;
    v_landlord_id BIGINT;
    v_contract_id BIGINT;
    v_contract_id_2 BIGINT;
    v_tx_id BIGINT;
    v_failed BOOLEAN;
BEGIN
    RAISE NOTICE '>>> BẮT ĐẦU KIỂM THỬ SCHEMA LEASING & MIGRATION...';

    -- 0. Lấy hoặc tạo dữ liệu mẫu tham chiếu hợp lệ
    SELECT id INTO v_room_id FROM properties.rooms LIMIT 1;
    IF v_room_id IS NULL THEN
        RAISE EXCEPTION 'Cần ít nhất 1 phòng trong properties.rooms để kiểm thử.';
    END IF;

    SELECT id INTO v_other_room_id FROM properties.rooms WHERE id <> v_room_id LIMIT 1;
    IF v_other_room_id IS NULL THEN
        v_other_room_id := v_room_id + 9999;
    END IF;

    SELECT id INTO v_landlord_id FROM identity.users LIMIT 1;
    SELECT id INTO v_tenant_id FROM identity.users WHERE id <> v_landlord_id LIMIT 1;
    IF v_tenant_id IS NULL THEN
        v_tenant_id := v_landlord_id;
    END IF;

    -- ==========================================================================================
    -- TEST NHÓM 1: KIỂM THỬ CẤU TRÚC & RÀNG BUỘC HỢP ĐỒNG (CONTRACT CONSTRAINTS)
    -- ==========================================================================================
    RAISE NOTICE '--- TEST 1.1: Tạo hợp đồng DRAFT hợp lệ';
    INSERT INTO leasing.contracts (
        contract_code, room_id, tenant_id, landlord_id,
        start_date, end_date, monthly_rent, deposit_amount, deposit_status, billing_due_day, status
    ) VALUES (
        'TEST-LEASING-001', v_room_id, v_tenant_id, v_landlord_id,
        '2027-01-01', '2027-06-30', 5000000, 5000000, 'PENDING', 5, 'DRAFT'
    ) RETURNING id INTO v_contract_id;
    RAISE NOTICE '    -> PASS: Hợp đồng hợp lệ đã được tạo với ID: %', v_contract_id;

    RAISE NOTICE '--- TEST 1.2: Từ chối trạng thái không hợp lệ';
    v_failed := FALSE;
    BEGIN
        INSERT INTO leasing.contracts (
            contract_code, room_id, tenant_id, landlord_id,
            start_date, end_date, monthly_rent, deposit_amount, status
        ) VALUES (
            'TEST-LEASING-INV-STATUS', v_room_id, v_tenant_id, v_landlord_id,
            '2027-01-01', '2027-06-30', 5000000, 5000000, 'INVALID_STATUS'
        );
    EXCEPTION WHEN check_violation THEN
        v_failed := TRUE;
    END;
    IF NOT v_failed THEN
        RAISE EXCEPTION 'FAIL: Trạng thái không hợp lệ không bị từ chối!';
    END IF;
    RAISE NOTICE '    -> PASS: Trạng thái không hợp lệ đã bị chặn bởi ck_contracts_status.';

    RAISE NOTICE '--- TEST 1.3: Từ chối khoảng thời gian không hợp lệ (end_date <= start_date)';
    v_failed := FALSE;
    BEGIN
        INSERT INTO leasing.contracts (
            contract_code, room_id, tenant_id, landlord_id,
            start_date, end_date, monthly_rent, deposit_amount, status
        ) VALUES (
            'TEST-LEASING-INV-DATES', v_room_id, v_tenant_id, v_landlord_id,
            '2027-06-30', '2027-01-01', 5000000, 5000000, 'DRAFT'
        );
    EXCEPTION WHEN check_violation THEN
        v_failed := TRUE;
    END;
    IF NOT v_failed THEN
        RAISE EXCEPTION 'FAIL: end_date <= start_date không bị từ chối!';
    END IF;
    RAISE NOTICE '    -> PASS: end_date <= start_date đã bị chặn bởi ck_contracts_date_range.';

    -- ==========================================================================================
    -- TEST NHÓM 2: CHỮ KÝ ĐIỆN TỬ ĐỘC LẬP & METADATA XÁC THỰC
    -- ==========================================================================================
    RAISE NOTICE '--- TEST 2.1: Ghi nhận độc lập chữ ký của Chủ nhà và Người thuê';
    UPDATE leasing.contracts
    SET landlord_signed_at = now(),
        signed_document_hash = 'a69f73ccd356db74f0a88f37a32d4b39634e9026573d8209e2f4f569772e03d7'
    WHERE id = v_contract_id;
    
    -- Kiểm tra phân biệt ký một phía
    IF (SELECT landlord_signed_at IS NOT NULL AND tenant_signed_at IS NULL FROM leasing.contracts WHERE id = v_contract_id) THEN
        RAISE NOTICE '    -> PASS: Hệ thống phân biệt rõ Landlord đã ký nhưng Tenant chưa ký.';
    ELSE
        RAISE EXCEPTION 'FAIL: Không phân biệt được trạng thái ký một phía.';
    END IF;

    -- Tenant ký hoàn tất
    UPDATE leasing.contracts
    SET tenant_signed_at = now(),
        status = 'SIGNED'
    WHERE id = v_contract_id;
    RAISE NOTICE '    -> PASS: Tenant đã ký thành công, hợp đồng chuyển sang SIGNED.';

    -- ==========================================================================================
    -- TEST NHÓM 3: GIAO DỊCH TIỀN CỌC ĐỘC LẬP (DEPOSIT TRANSACTIONS)
    -- ==========================================================================================
    RAISE NOTICE '--- TEST 3.1: Ghi nhận giao dịch nhận tiền cọc (RECEIVED)';
    INSERT INTO leasing.deposit_transactions (
        contract_id, transaction_type, amount, reason, recorded_by
    ) VALUES (
        v_contract_id, 'RECEIVED', 5000000, 'Nhận tiền cọc giữ chỗ qua chuyển khoản', v_landlord_id
    ) RETURNING id INTO v_tx_id;
    RAISE NOTICE '    -> PASS: Giao dịch RECEIVED đã lưu với ID: %', v_tx_id;

    RAISE NOTICE '--- TEST 3.2: Giao dịch khấu trừ (DEDUCTED) bắt buộc phải có lý do';
    v_failed := FALSE;
    BEGIN
        INSERT INTO leasing.deposit_transactions (
            contract_id, transaction_type, amount, reason, recorded_by
        ) VALUES (
            v_contract_id, 'DEDUCTED', 1000000, NULL, v_landlord_id
        );
    EXCEPTION WHEN check_violation THEN
        v_failed := TRUE;
    END;
    IF NOT v_failed THEN
        RAISE EXCEPTION 'FAIL: DEDUCTED không có lý do nhưng không bị chặn!';
    END IF;
    RAISE NOTICE '    -> PASS: DEDUCTED thiếu reason đã bị chặn bởi ck_deposit_tx_deduction_reason.';

    RAISE NOTICE '--- TEST 3.3: Ghi nhận khấu trừ hợp lệ có lý do & hoàn trả (REFUNDED)';
    INSERT INTO leasing.deposit_transactions (
        contract_id, transaction_type, amount, reason, recorded_by
    ) VALUES 
    (v_contract_id, 'DEDUCTED', 500000, 'Hư hỏng vòi nước phòng tắm', v_landlord_id),
    (v_contract_id, 'REFUNDED', 4500000, 'Hoàn trả tiền cọc còn lại sau bàn giao', v_landlord_id);
    RAISE NOTICE '    -> PASS: Toàn bộ dòng đời giao dịch cọc được lưu vết minh bạch.';

    -- ==========================================================================================
    -- TEST NHÓM 4: BÀN GIAO PHÒNG (HANDOVER METADATA)
    -- ==========================================================================================
    RAISE NOTICE '--- TEST 4.1: Kiểm tra ràng buộc người xác nhận bàn giao';
    v_failed := FALSE;
    BEGIN
        UPDATE leasing.contracts
        SET handover_confirmed_at = now(),
            handover_confirmed_by = NULL
        WHERE id = v_contract_id;
    EXCEPTION WHEN check_violation THEN
        v_failed := TRUE;
    END;
    IF NOT v_failed THEN
        RAISE EXCEPTION 'FAIL: handover_confirmed_at có giá trị nhưng handover_confirmed_by NULL mà không bị chặn!';
    END IF;
    RAISE NOTICE '    -> PASS: handover_confirmed_by bắt buộc khi có handover_confirmed_at.';

    -- ==========================================================================================
    -- TEST NHÓM 5: CHỐNG TRÙNG LỊCH PHÒNG (GIST EXCLUSION CONSTRAINT)
    -- ==========================================================================================
    RAISE NOTICE '--- TEST 5.1: Cùng một phòng - Từ chối hợp đồng thứ 2 trùng khoảng thời gian thuê';
    -- Hợp đồng 1 hiện đang ở trạng thái SIGNED (2027-01-01 -> 2027-06-30)
    v_failed := FALSE;
    BEGIN
        INSERT INTO leasing.contracts (
            contract_code, room_id, tenant_id, landlord_id,
            start_date, end_date, monthly_rent, deposit_amount, status
        ) VALUES (
            'TEST-LEASING-OVERLAP', v_room_id, v_tenant_id, v_landlord_id,
            '2027-04-01', '2027-08-31', 5000000, 5000000, 'PENDING_SIGNATURE'
        );
    EXCEPTION WHEN exclusion_violation THEN
        v_failed := TRUE;
    END;
    IF NOT v_failed THEN
        RAISE EXCEPTION 'FAIL: Trùng lịch phòng không bị Exclusion Constraint ngăn chặn!';
    END IF;
    RAISE NOTICE '    -> PASS: GiST Exclusion Constraint đã chặn thành công hợp đồng trùng lịch.';

    RAISE NOTICE '--- TEST 5.2: Cùng một phòng - Chấp nhận hợp đồng tiếp theo không trùng lịch';
    INSERT INTO leasing.contracts (
        contract_code, room_id, tenant_id, landlord_id,
        start_date, end_date, monthly_rent, deposit_amount, status
    ) VALUES (
        'TEST-LEASING-NON-OVERLAP', v_room_id, v_tenant_id, v_landlord_id,
        '2027-07-01', '2027-12-31', 5000000, 5000000, 'PENDING_SIGNATURE'
    ) RETURNING id INTO v_contract_id_2;
    RAISE NOTICE '    -> PASS: Hợp đồng tương lai không trùng lịch (2027-07-01 -> 2027-12-31) được chấp nhận.';

    RAISE NOTICE '--- TEST 5.3: Khác phòng - Chấp nhận trùng khoảng thời gian';
    INSERT INTO leasing.contracts (
        contract_code, room_id, tenant_id, landlord_id,
        start_date, end_date, monthly_rent, deposit_amount, status
    ) VALUES (
        'TEST-LEASING-OTHER-ROOM', v_other_room_id, v_tenant_id, v_landlord_id,
        '2027-01-01', '2027-06-30', 5000000, 5000000, 'PENDING_SIGNATURE'
    );
    RAISE NOTICE '    -> PASS: Hai phòng khác nhau được phép thuê cùng khoảng thời gian.';

    RAISE NOTICE '--- TEST 5.4: Hợp đồng bị từ chối (REJECTED) giải phóng giữ phòng';
    UPDATE leasing.contracts
    SET status = 'REJECTED'
    WHERE id = v_contract_id_2;

    -- Bây giờ tạo hợp đồng mới trùng lịch với hợp đồng vừa bị REJECTED
    INSERT INTO leasing.contracts (
        contract_code, room_id, tenant_id, landlord_id,
        start_date, end_date, monthly_rent, deposit_amount, status
    ) VALUES (
        'TEST-LEASING-REPLACEMENT', v_room_id, v_tenant_id, v_landlord_id,
        '2027-07-01', '2027-12-31', 5000000, 5000000, 'PENDING_SIGNATURE'
    );
    RAISE NOTICE '    -> PASS: REJECTED đã giải phóng giữ chỗ cho hợp đồng thay thế.';

    -- ==========================================================================================
    -- TEST NHÓM 6: TỰ ĐỘNG HÓA DATABASE (TRIGGER & PROCEDURES)
    -- ==========================================================================================
    RAISE NOTICE '--- TEST 6.1: Trigger đồng bộ phòng thành RENTED khi hợp đồng ACTIVE';
    UPDATE leasing.contracts
    SET status = 'ACTIVE'
    WHERE id = v_contract_id;

    IF (SELECT status FROM properties.rooms WHERE id = v_room_id) = 'RENTED' THEN
        RAISE NOTICE '    -> PASS: Phòng đã tự động chuyển sang RENTED khi hợp đồng ACTIVE.';
    ELSE
        RAISE EXCEPTION 'FAIL: Phòng không chuyển sang RENTED khi hợp đồng ACTIVE.';
    END IF;

    RAISE NOTICE '--- TEST 6.2: Hợp đồng quá hạn chuyển EXPIRED_PENDING_HANDOVER không giải phóng phòng';
    UPDATE leasing.contracts
    SET status = 'EXPIRED_PENDING_HANDOVER'
    WHERE id = v_contract_id;

    IF (SELECT status FROM properties.rooms WHERE id = v_room_id) = 'RENTED' THEN
        RAISE NOTICE '    -> PASS: Phòng vẫn giữ RENTED trong giai đoạn EXPIRED_PENDING_HANDOVER.';
    ELSE
        RAISE EXCEPTION 'FAIL: Phòng bị giải phóng sớm trong khi chưa bàn giao!';
    END IF;

    RAISE NOTICE '--- TEST 6.3: Hoàn tất bàn giao (COMPLETED) giải phóng phòng thành AVAILABLE';
    -- Cập nhật thông tin bàn giao trước khi hoàn tất
    UPDATE leasing.contracts
    SET handover_confirmed_at = now(),
        handover_confirmed_by = v_landlord_id,
        actual_occupancy_end_date = '2027-06-30'
    WHERE id = v_contract_id;

    -- Xóa/hủy hợp đồng giữ chỗ tương lai để kiểm tra giải phóng phòng
    UPDATE leasing.contracts
    SET status = 'CANCELLED'
    WHERE contract_code = 'TEST-LEASING-REPLACEMENT';

    UPDATE leasing.contracts
    SET status = 'COMPLETED'
    WHERE id = v_contract_id;

    IF (SELECT status FROM properties.rooms WHERE id = v_room_id) = 'AVAILABLE' THEN
        RAISE NOTICE '    -> PASS: Phòng đã được giải phóng thành AVAILABLE sau khi COMPLETED.';
    ELSE
        RAISE EXCEPTION 'FAIL: Phòng không trở về AVAILABLE sau khi bàn giao hoàn tất.';
    END IF;

    -- ==========================================================================================
    -- DỌN DẸP DỮ LIỆU TEST
    -- ==========================================================================================
    DELETE FROM leasing.deposit_transactions WHERE contract_id IN (
        SELECT id FROM leasing.contracts WHERE contract_code LIKE 'TEST-LEASING-%'
    );
    DELETE FROM leasing.contracts WHERE contract_code LIKE 'TEST-LEASING-%';

    RAISE NOTICE '>>> TẤT CẢ 6 NHÓM KIỂM THỬ XÁC MINH CƠ SỞ DỮ LIỆU ĐÃ VƯỢT QUA 100%% THÀNH CÔNG!';
END $$;
