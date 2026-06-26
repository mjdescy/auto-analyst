-- Export the entire table to CSV with column headings.
-- Fields containing the delimiter are automatically quoted with ".
COPY source TO 'export.csv' (HEADER, DELIMITER ',', QUOTE '"');

-- Export to TSV with column headings.
-- Fields containing the delimiter are automatically quoted with ".
COPY source TO 'export.tsv' (HEADER, DELIMITER '\t', QUOTE '"');

-- Export to Parquet (column names are stored in the schema).
COPY source TO 'export.parquet' (FORMAT PARQUET);

-- Export to XLSX with column headings.
COPY source TO 'export.xlsx' (FORMAT XLSX, HEADER TRUE);

-- Export to JSON.
COPY source TO 'export.xlsx' (FORMAT JSON);
