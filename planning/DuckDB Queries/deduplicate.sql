-- Remove duplicate records from a dataset.

CREATE OR REPLACE TABLE {{DeduplicatedTable}} AS
SELECT DISTINCT *
FROM {{SourceTable}};
