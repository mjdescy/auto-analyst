-- Values in double curly braces {{}} are placeholders for variables that should be set before executing the query.

-- Set threads to 1 for reproducible sampling.
SET threads = 1;

-- Pull a random attribute sample.
DROP TABLE IF EXISTS sample;

CREATE TABLE sample AS
SELECT *
FROM source
using sample reservoir({{sample_size}} ROWS) -- DuckDB variables cannot be used for row count
repeatable({{random_number_seed}}); -- DuckDB variables cannot be used for seed value

-- Reset threads to default after sampling.
RESET threads;
