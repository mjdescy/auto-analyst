# Assumptions

The following things are assumed. If these things are not true or are not present then the program will not work properly.

## Assumptions about usage

1. The most commonly used input data file format will be .xlsx.

## Assumptions about data files

1. Data files in .xlsx format have their data in the first worksheet and that data's range starts in cell A1.
2. Data files always have column names in the first row.
3. Each data column has a unique column name.
4. Column names do not contain line breaks.
5. In delimited data files double quotation marks (") enclose field values that contain the delimiter.
6. Columns will be interpreted as varchar unless another data type (date, decimal, or integer) is specified.

## Assumptions about how the program is implemented

1. The database engine used by the program is an implementation detail that can change at any time.
2. A change to the database engine used by the program will not break any functionality other than the execution of running custom SQL commands.
3. The program will delete any temporary files it creates.
4. The program will always add a "filename" column to imported data files.