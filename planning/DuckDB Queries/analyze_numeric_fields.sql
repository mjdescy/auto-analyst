-- #############################################################################
-- Numeric Field Analysis Template
--
-- Computes summary statistics for numeric columns in the `source` table:
--   - Minimum / maximum value
--   - Sum, mean, median, standard deviation
--   - Count of unique numeric values
--   - Count of null / blank / otherwise-invalid values
--   - Count of zeros and negative values (data quality)
--
-- Usage:
--   1. Replace the column names in the UNPIVOT ON clause below with the actual
--      numeric column names from your table.
--   2. Make sure the source table is loaded (see import-csv.sql or
--      import-parquet.sql).
--   3. Run this script.
-- #############################################################################

-- =============================================================================
-- Setup: Unpivot numeric columns into (column_name, raw_value) pairs
-- =============================================================================
-- 👇 Replace `amount, quantity, price` with your actual numeric column names.
--    If you have many columns, you can also use COLUMNS('regex_pattern') to
--    select columns by pattern (e.g., COLUMNS('_amt$') for columns ending
--    in "_amt").
WITH unpivoted AS (
    UNPIVOT source
    ON amount, quantity, price
    INTO
        NAME column_name
        VALUE raw_value
),

-- =============================================================================
-- Classify each value: null, invalid (non-numeric string), or valid number
-- =============================================================================
with_validity AS (
    SELECT
        column_name,
        raw_value,
        value_status: CASE
            WHEN raw_value IS NULL THEN 'null'
            WHEN try_cast(raw_value::VARCHAR AS DOUBLE) IS NULL THEN 'invalid'
            ELSE 'valid'
        END,
        numeric_value: try_cast(raw_value::VARCHAR AS DOUBLE)
    FROM unpivoted
)

-- =============================================================================
-- Analysis: Per-column summary statistics
-- =============================================================================
SELECT
    column_name,

    -- Range and central tendency
    min_value: MIN(numeric_value),
    max_value: MAX(numeric_value),
    total_sum: SUM(numeric_value),
    mean: AVG(numeric_value),
    median_value: MEDIAN(numeric_value),
    std_dev: STDDEV_SAMP(numeric_value),

    -- Distribution shape
    skewness: SKEWNESS(numeric_value),
    kurtosis: KURTOSIS(numeric_value),

    -- Quantiles
    q1: PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY numeric_value),
    q3: PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY numeric_value),

    -- Uniqueness
    unique_values_count: COUNT(DISTINCT numeric_value),

    -- Data quality
    total_rows: COUNT(*),
    null_count: COUNT(*) FILTER (WHERE value_status = 'null'),
    invalid_count: COUNT(*) FILTER (WHERE value_status = 'invalid'),
    valid_count: COUNT(*) FILTER (WHERE value_status = 'valid'),

    -- Business-rule checks
    zero_count: COUNT(*) FILTER (WHERE numeric_value = 0),
    negative_count: COUNT(*) FILTER (WHERE numeric_value < 0),
    positive_count: COUNT(*) FILTER (WHERE numeric_value > 0)

FROM with_validity
GROUP BY column_name
ORDER BY column_name;
