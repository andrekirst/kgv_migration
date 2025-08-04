-- =============================================================================
-- KGV Migration: Sequences and Auto-Number Generation
-- Version: 1.0
-- Description: Atomic sequence generation for file references and entry numbers
-- =============================================================================

-- =============================================================================
-- SEQUENCE MANAGEMENT FOR ATOMIC NUMBER GENERATION
-- =============================================================================

-- Sequence table for tracking next numbers per district/year combination
CREATE TABLE number_sequences (
    id BIGSERIAL PRIMARY KEY,
    sequence_type VARCHAR(20) NOT NULL, -- 'file_reference' or 'entry_number'
    district_code VARCHAR(10) NOT NULL,
    year INTEGER NOT NULL,
    next_number INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT uk_number_sequences_type_district_year 
        UNIQUE (sequence_type, district_code, year),
    CONSTRAINT number_sequences_type_check 
        CHECK (sequence_type IN ('file_reference', 'entry_number')),
    CONSTRAINT number_sequences_year_check 
        CHECK (year >= 1900 AND year <= EXTRACT(YEAR FROM NOW()) + 10),
    CONSTRAINT number_sequences_next_number_check 
        CHECK (next_number > 0)
);

-- Index for performance
CREATE INDEX idx_number_sequences_lookup 
    ON number_sequences(sequence_type, district_code, year);

-- Trigger to update timestamp
CREATE TRIGGER trigger_number_sequences_updated_at 
    BEFORE UPDATE ON number_sequences 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================================================
-- ATOMIC NUMBER GENERATION FUNCTIONS
-- =============================================================================

-- Function to get next file reference number atomically
CREATE OR REPLACE FUNCTION get_next_file_reference_number(
    p_district_code VARCHAR(10),
    p_year INTEGER DEFAULT EXTRACT(YEAR FROM NOW())
) RETURNS INTEGER AS $$
DECLARE
    v_next_number INTEGER;
BEGIN
    -- Validate inputs
    IF p_district_code IS NULL OR LENGTH(TRIM(p_district_code)) = 0 THEN
        RAISE EXCEPTION 'District code cannot be null or empty';
    END IF;
    
    IF p_year < 1900 OR p_year > EXTRACT(YEAR FROM NOW()) + 10 THEN
        RAISE EXCEPTION 'Year must be between 1900 and %', EXTRACT(YEAR FROM NOW()) + 10;
    END IF;
    
    -- Insert or update sequence record atomically
    INSERT INTO number_sequences (sequence_type, district_code, year, next_number)
    VALUES ('file_reference', p_district_code, p_year, 2)
    ON CONFLICT (sequence_type, district_code, year)
    DO UPDATE SET 
        next_number = number_sequences.next_number + 1,
        updated_at = NOW()
    RETURNING CASE 
        WHEN xmax = 0 THEN 1  -- New record, return 1
        ELSE number_sequences.next_number - 1  -- Updated record, return previous value
    END INTO v_next_number;
    
    RETURN v_next_number;
END;
$$ LANGUAGE plpgsql;

-- Function to get next entry number atomically
CREATE OR REPLACE FUNCTION get_next_entry_number(
    p_district_code VARCHAR(10),
    p_year INTEGER DEFAULT EXTRACT(YEAR FROM NOW())
) RETURNS INTEGER AS $$
DECLARE
    v_next_number INTEGER;
BEGIN
    -- Validate inputs
    IF p_district_code IS NULL OR LENGTH(TRIM(p_district_code)) = 0 THEN
        RAISE EXCEPTION 'District code cannot be null or empty';
    END IF;
    
    IF p_year < 1900 OR p_year > EXTRACT(YEAR FROM NOW()) + 10 THEN
        RAISE EXCEPTION 'Year must be between 1900 and %', EXTRACT(YEAR FROM NOW()) + 10;
    END IF;
    
    -- Insert or update sequence record atomically
    INSERT INTO number_sequences (sequence_type, district_code, year, next_number)
    VALUES ('entry_number', p_district_code, p_year, 2)
    ON CONFLICT (sequence_type, district_code, year)
    DO UPDATE SET 
        next_number = number_sequences.next_number + 1,
        updated_at = NOW()
    RETURNING CASE 
        WHEN xmax = 0 THEN 1  -- New record, return 1
        ELSE number_sequences.next_number - 1  -- Updated record, return previous value
    END INTO v_next_number;
    
    RETURN v_next_number;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- HELPER FUNCTIONS FOR BUSINESS LOGIC
-- =============================================================================

-- Function to create a new file reference with automatic number generation
CREATE OR REPLACE FUNCTION create_file_reference(
    p_district_code VARCHAR(10),
    p_year INTEGER DEFAULT EXTRACT(YEAR FROM NOW())
) RETURNS TABLE(id BIGINT, uuid UUID, district_code VARCHAR(10), number INTEGER, year INTEGER) AS $$
DECLARE
    v_number INTEGER;
    v_result RECORD;
BEGIN
    -- Get next number
    v_number := get_next_file_reference_number(p_district_code, p_year);
    
    -- Insert new file reference
    INSERT INTO file_references (district_code, number, year)
    VALUES (p_district_code, v_number, p_year)
    RETURNING file_references.id, file_references.uuid, 
              file_references.district_code, file_references.number, file_references.year
    INTO v_result;
    
    RETURN QUERY SELECT v_result.id, v_result.uuid, v_result.district_code, v_result.number, v_result.year;
END;
$$ LANGUAGE plpgsql;

-- Function to create a new entry number with automatic number generation
CREATE OR REPLACE FUNCTION create_entry_number(
    p_district_code VARCHAR(10),
    p_year INTEGER DEFAULT EXTRACT(YEAR FROM NOW())
) RETURNS TABLE(id BIGINT, uuid UUID, district_code VARCHAR(10), number INTEGER, year INTEGER) AS $$
DECLARE
    v_number INTEGER;
    v_result RECORD;
BEGIN
    -- Get next number
    v_number := get_next_entry_number(p_district_code, p_year);
    
    -- Insert new entry number
    INSERT INTO entry_numbers (district_code, number, year)
    VALUES (p_district_code, v_number, p_year)
    RETURNING entry_numbers.id, entry_numbers.uuid, 
              entry_numbers.district_code, entry_numbers.number, entry_numbers.year
    INTO v_result;
    
    RETURN QUERY SELECT v_result.id, v_result.uuid, v_result.district_code, v_result.number, v_result.year;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- WAITING LIST RANKING FUNCTIONS
-- =============================================================================

-- Function to calculate waiting list position based on application date
CREATE OR REPLACE FUNCTION calculate_waiting_list_position(
    p_application_id BIGINT,
    p_list_type VARCHAR(2) DEFAULT '32'  -- '32' or '33'
) RETURNS INTEGER AS $$
DECLARE
    v_application_date DATE;
    v_position INTEGER;
BEGIN
    -- Get application date
    SELECT application_date INTO v_application_date
    FROM applications 
    WHERE id = p_application_id AND is_active = true;
    
    IF v_application_date IS NULL THEN
        RETURN NULL;
    END IF;
    
    -- Calculate position based on application date and list type
    IF p_list_type = '32' THEN
        SELECT COUNT(*) + 1 INTO v_position
        FROM applications
        WHERE is_active = true
          AND application_date < v_application_date
          AND waiting_list_number_32 IS NOT NULL;
    ELSIF p_list_type = '33' THEN
        SELECT COUNT(*) + 1 INTO v_position
        FROM applications
        WHERE is_active = true
          AND application_date < v_application_date
          AND waiting_list_number_33 IS NOT NULL;
    ELSE
        RAISE EXCEPTION 'Invalid list type. Use 32 or 33.';
    END IF;
    
    RETURN v_position;
END;
$$ LANGUAGE plpgsql;

-- Function to recalculate all waiting list positions
CREATE OR REPLACE FUNCTION recalculate_waiting_list_positions()
RETURNS INTEGER AS $$
DECLARE
    v_updated_count INTEGER := 0;
    v_app RECORD;
    v_position_32 INTEGER;
    v_position_33 INTEGER;
BEGIN
    -- Recalculate positions for all active applications
    FOR v_app IN 
        SELECT id, application_date, waiting_list_number_32, waiting_list_number_33
        FROM applications 
        WHERE is_active = true 
        ORDER BY application_date ASC
    LOOP
        -- Calculate position for list 32
        IF v_app.waiting_list_number_32 IS NOT NULL THEN
            v_position_32 := calculate_waiting_list_position(v_app.id, '32');
            
            UPDATE applications 
            SET waiting_list_number_32 = v_position_32::VARCHAR(20)
            WHERE id = v_app.id;
            
            v_updated_count := v_updated_count + 1;
        END IF;
        
        -- Calculate position for list 33
        IF v_app.waiting_list_number_33 IS NOT NULL THEN
            v_position_33 := calculate_waiting_list_position(v_app.id, '33');
            
            UPDATE applications 
            SET waiting_list_number_33 = v_position_33::VARCHAR(20)
            WHERE id = v_app.id;
            
            v_updated_count := v_updated_count + 1;
        END IF;
    END LOOP;
    
    RETURN v_updated_count;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- UTILITY FUNCTIONS
-- =============================================================================

-- Function to reset sequence for a specific district/year (admin function)
CREATE OR REPLACE FUNCTION reset_sequence(
    p_sequence_type VARCHAR(20),
    p_district_code VARCHAR(10),
    p_year INTEGER,
    p_start_number INTEGER DEFAULT 1
) RETURNS BOOLEAN AS $$
BEGIN
    -- Validate inputs
    IF p_sequence_type NOT IN ('file_reference', 'entry_number') THEN
        RAISE EXCEPTION 'Invalid sequence type. Use file_reference or entry_number.';
    END IF;
    
    IF p_start_number < 1 THEN
        RAISE EXCEPTION 'Start number must be greater than 0';
    END IF;
    
    -- Update or insert sequence
    INSERT INTO number_sequences (sequence_type, district_code, year, next_number)
    VALUES (p_sequence_type, p_district_code, p_year, p_start_number)
    ON CONFLICT (sequence_type, district_code, year)
    DO UPDATE SET 
        next_number = p_start_number,
        updated_at = NOW();
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- Function to get current sequence information
CREATE OR REPLACE FUNCTION get_sequence_info(
    p_sequence_type VARCHAR(20) DEFAULT NULL,
    p_district_code VARCHAR(10) DEFAULT NULL,
    p_year INTEGER DEFAULT NULL
) RETURNS TABLE(
    sequence_type VARCHAR(20),
    district_code VARCHAR(10),
    year INTEGER,
    next_number INTEGER,
    last_used INTEGER,
    created_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT ns.sequence_type, ns.district_code, ns.year, ns.next_number, 
           ns.next_number - 1 as last_used, ns.created_at, ns.updated_at
    FROM number_sequences ns
    WHERE (p_sequence_type IS NULL OR ns.sequence_type = p_sequence_type)
      AND (p_district_code IS NULL OR ns.district_code = p_district_code)
      AND (p_year IS NULL OR ns.year = p_year)
    ORDER BY ns.sequence_type, ns.district_code, ns.year;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- COMMENTS
-- =============================================================================

COMMENT ON TABLE number_sequences IS 'Atomic sequence generation for file references and entry numbers';
COMMENT ON FUNCTION get_next_file_reference_number(VARCHAR, INTEGER) IS 'Thread-safe function to get next file reference number';
COMMENT ON FUNCTION get_next_entry_number(VARCHAR, INTEGER) IS 'Thread-safe function to get next entry number';
COMMENT ON FUNCTION create_file_reference(VARCHAR, INTEGER) IS 'Creates new file reference with automatic number assignment';
COMMENT ON FUNCTION create_entry_number(VARCHAR, INTEGER) IS 'Creates new entry number with automatic number assignment';
COMMENT ON FUNCTION calculate_waiting_list_position(BIGINT, VARCHAR) IS 'Calculates waiting list position based on application date';
COMMENT ON FUNCTION recalculate_waiting_list_positions() IS 'Recalculates all waiting list positions (admin function)';
COMMENT ON FUNCTION reset_sequence(VARCHAR, VARCHAR, INTEGER, INTEGER) IS 'Resets sequence counter for specific district/year (admin function)';
COMMENT ON FUNCTION get_sequence_info(VARCHAR, VARCHAR, INTEGER) IS 'Returns current sequence information for monitoring';