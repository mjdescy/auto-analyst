-- A simple append tables query.
-- UNION [ALL] BY NAME does not require both queries to have the same number of columns.
--  Any columns that are only found in one of the queries are filled with NULL values for
--  the other query.
SELECT * FROM table_01
UNION ALL BY NAME
SELECT * FROM table_02
UNION ALL BY NAME
SELECT * FROM table_03;

-- A more complex append tables query.
-- This query appends three tables and adds a source_table_name_for_append column to capture
-- the name of each source table.
CREATE OR REPLACE TABLE "{destinationTableName}" AS
SELECT '{sourceTableName1}' AS "source_table_name_for_append", * FROM "{sourceTableName1}"
UNION ALL BY NAME
SELECT '{sourceTableName2}' AS "source_table_name_for_append", * FROM "{sourceTableName2}"
UNION ALL BY NAME
SELECT '{sourceTableName3}' AS "source_table_name_for_append", * FROM "{sourceTableName3}";