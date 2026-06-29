-- #############################################################################
-- Column Value Classification (Frequency Distribution) Template
--
-- For each specified column, lists every distinct value and how many records
-- have that value. All columns are output together in a single table.
--
-- Output columns: column_name, value, record_count
--
-- Usage:
--   1. Replace the column names in the UNPIVOT ON clause below with the actual
--      column names from your table.
--   2. Make sure the source table is loaded (see import_csv.sql or
--      import_parquet.sql).
--   3. Run this script.
-- #############################################################################

-- =============================================================================
-- Setup: Unpivot target columns into (column_name, raw_value) pairs
-- =============================================================================
-- 👇 Replace `column1, column2, column3` with your actual column names.
--    You can also use COLUMNS('regex_pattern') to select columns by pattern
--    (e.g., COLUMNS('_code$') for columns ending in "_code").
WITH unpivoted AS (
    UNPIVOT source
    ON column1, column2, column3
    INTO
        NAME column_name
        VALUE raw_value
),

-- =============================================================================
-- Normalize: cast all values to VARCHAR for consistent grouping;
--            treat NULLs as '<NULL>' so they show up in results.
--            (Remove COALESCE if you want NULLs to be excluded from counts.)
-- =============================================================================
normalized AS (
    SELECT
        column_name,
        value: COALESCE(raw_value::VARCHAR, '<NULL>')
    FROM unpivoted
)

-- =============================================================================
-- Frequency: count records per distinct value, per column
-- =============================================================================
SELECT
    column_name,
    value,
    record_count: COUNT(*)
FROM normalized
GROUP BY column_name, value
ORDER BY column_name, record_count DESC;