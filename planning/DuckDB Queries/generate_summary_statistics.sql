-- Generate summary statistics for the table 'tbl' and store the results in a new table 'tbl_summary'.

CREATE TABLE tbl_summary AS SELECT * FROM (SUMMARIZE tbl);