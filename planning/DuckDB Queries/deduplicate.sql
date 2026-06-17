-- Remove duplicate records from a dataset.

CREATE OR REPLACE {{DeduplicatedTable}} AS
SELECT DISTINCT *
FROM {{SourceTable}};