# Auto-Analyst

This program aims to replace [ACL Analytics][1] for the specific tasks I use it for, which include the following:

1. Importing data from [various formats][2]
1. Exporting data to [various formats][2]
1. Generating descriptive statistics
1. Random attribute sampling
1. Stratified random attribute sampling
1. Appending datasets
1. Filtering datasets
1. Executing arbitrary scripts that manipulate the data

Beyond performing these data analysis tasks, the program will also output work papers in .xlsx format that document the work performed and contain a copy of the exact commands used to perform the work.

## Supported data formats

The following data formats are supported for input and output:

1. .xlsx
1. .csv
1. .tsv
1. .parquet

[1]: https://www.diligent.com/products/acl-analytics
[2]: #supported-data-formats

## How to use

First, generate a configuration file using the console app:

```sh
# Generate a configuration file in the current working directory.
auto-analyst init

# Generate a configuration file in another directory.
auto-analyst init --output ~/project/config.json
```

Next, edit the generated configuration file in a text editor.

Finally, run the console app with the configuration file:

```sh
# Run the process defined in config.json.
auto-analyst run config.json
```
