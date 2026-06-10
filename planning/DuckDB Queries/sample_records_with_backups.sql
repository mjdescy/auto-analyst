-- Sampling can be accomplished in DuckDB using the reservoir sampling method, 
-- which allows for efficient sampling of large datasets. 
-- This query demonstrates how to sample records from a source table while categorizing them into 'Primary' and
-- 'Backup' samples based on specified sizes. 'Backup' samples are used to ensure that we have additional records
-- in case some of the 'Primary' samples are not suitable for analysis.


-- The logic in this section belongs in whatever code calls the DuckDB query, but is included here for completeness.

-- Input parameters
SET VARIABLE primary_sample_size = {{primary_sample_size}};
SET VARIABLE backup_sample_size = {{backup_sample_size}};
SET VARIABLE primary_sample_category_name = 'Primary';
SET VARIABLE backup_sample_category_name = 'Backup';
SET VARIABLE combined_sample_size = getvariable('primary_sample_size') + getvariable('backup_sample_size');

-- The rest of this code belongs in the DuckDB query itself.
-- Values in double curly braces {{}} are placeholders for variables that should be set before executing the query.

-- Set threads to 1 for reproducible sampling.
SET threads = 1;

-- Setup
CREATE OR REPLACE SEQUENCE sample_id_sequence;

-- Execution
SELECT 
    "sample_id": nextval('serial'),
    "sample_type": CASE
        WHEN "sample_id" <= {{primary_sample_size}} THEN getvariable('primary_sample_category_name')
        ELSE '{{backup_sample_category_name}}'
    END
FROM sample_source
USING SAMPLE reservoir({{sample_size_plus_backups}} ROWS) -- DuckDB variables cannot be used for row count
REPEATABLE({{random_number_seed}}); -- DuckDB variables cannot be used for seed value

-- Reset threads to default after sampling.
RESET threads;
