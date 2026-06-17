-- Remove duplicate records from a dataset, keeping only the first occurrence of each unique
-- combination of the specified key fields.

CREATE OR REPLACE {{DeduplicatedTable}} AS
SELECT DISTINCT *
FROM {{SourceTable}}
QUALIFY ROW_NUMBER() OVER (PARTITION BY {{KeyFields}} ORDER BY {{OrderByColumn}}) = 1;