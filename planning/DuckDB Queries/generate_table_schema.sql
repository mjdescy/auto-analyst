-- Simplest table schema generation query for DuckDB. This query generates a table schema for a database table and stores it in a new table.
SELECT 
    column_name, 
    "data_type": column_type
FROM (DESCRIBE source);

-- Richer table schema generation query for DuckDB. This query generates a table schema for a database table and stores it in a new table.
SELECT
    ordinal_position,
    column_name, 
    data_type, 
    is_nullable, 
    column_default, 
    character_maximum_length, 
    numeric_precision, 
    numeric_scale
FROM information_schema.columns
WHERE table_schema = 'main' AND table_name = 'source';

-- The query below uses duckdb_tables() to find the schema of the source table, which is useful if the source table is not in the 'main' schema.
SELECT
    ordinal_position,
    column_name, 
    data_type, 
    is_nullable, 
    column_default, 
    character_maximum_length, 
    numeric_precision, 
    numeric_scale
FROM information_schema.columns
WHERE table_schema IN (
        SELECT table_schema
        FROM duckdb_tables()
        WHERE table_name = '{_sourceTableName.EscapeSingleQuote()}'
        )
    AND table_name = '{_sourceTableName.EscapeSingleQuote()}'
ORDER BY ordinal_position;
