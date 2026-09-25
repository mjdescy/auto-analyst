# Work Papers — Implementation Plan

> Status: **draft for planning**. The exact requirements for the human-readable
> work paper deliverable are **not yet defined** and will be specified later.

## Goal

Turn the generated SQL and the query results of a run into Excel work papers that
document the analysis process and its results, and make the work reproducible by
someone who was not involved in producing it.

### Required deliverable (requirements TBD)

We must produce **human-readable work papers** that contain:

- the **generated SQL** executed during the run (the exact commands used), and
- the **output of the various analysis steps** (summary statistics, field
  analyses, samples, etc.).

The exact workbook layout, contents, and formatting requirements will be defined
later.

## Design principles

1. **Decouple documentation from analysis.** The `steps:` list in the config stays
   purely analytical; the work paper is a post-run phase whose content is
   *derived from what the run did* — never re-declared in config.
2. **Make the `.duckdb` file the self-contained, self-documenting artifact.**
   The database stores its own config, input references, audit log, and outputs.
   The work paper is a *rendering* of the database, not a separately maintained
   record.
3. **Append-only audit history.** Commands are mostly `CREATE OR REPLACE TABLE`,
   so re-running a database overwrites outputs; the log is the only record of
   "what was done in run #N". One `run_id` per invocation of `auto-analyst run`.

---

## 1. Capture generated SQL in the database

`SqlCommandOrchestrator` already collects the exact SQL text of every executed
command (`SqlBatchResult`). Today that is only returned to C#. Change: persist it
in the database as part of execution.

Why the SQL text, not just the config: the SQL is the **executed ground truth**.
A `standard_analysis` step expands into one statement per column bucket with
type-detected column names; that expanded text is exactly what a reviewer needs
to replay. It also lets the work paper builder read the entire audit trail from
the engine instead of from in-memory hand-off.

## 2. Metadata tables — not views or macros — for the audit history

These are complementary tools with different jobs; for reproducibility use
**metadata tables**:

- **Metadata tables** — append-only records of what happened: config, SQL,
  timestamps, rows affected, status. This is the audit trail. **Chosen approach.**
- **Views** — query definitions, not records. Optional nicety for ad-hoc
  querying of results (`SELECT * FROM ap_analysis_summary`); they silently break
  if a later step `CREATE OR REPLACE`s a source table. Not the core need.
- **Macros** — reusable parameterized logic. Only relevant if the *database*
  should own transformation logic instead of C#. SQL generation intentionally
  stays in C# (with tests asserting exact SQL), so macros are **not** needed.
  Skip.

Use a dedicated schema (`audit`) so metadata never collides with user data.

### Schema (draft)

```sql
CREATE SCHEMA IF NOT EXISTS audit;

CREATE TABLE IF NOT EXISTS audit.runs (
    run_id        INTEGER PRIMARY KEY,
    started_at    TIMESTAMP,
    finished_at   TIMESTAMP,
    config_yaml   TEXT,      -- full text of the config file
    config_sha256 VARCHAR,   -- integrity check on the config
    status        VARCHAR
);

CREATE TABLE IF NOT EXISTS audit.command_log (
    run_id           INTEGER,
    seq              INTEGER,   -- execution order within the run
    step_type        VARCHAR,   -- e.g. 'standard_analysis', 'stratified_sample_with_backups'
    step_description VARCHAR,
    sql_text         TEXT,      -- exact SQL executed
    rows_affected    BIGINT,
    executed_at      TIMESTAMP,
    status           VARCHAR    -- success, or the error message
);

CREATE TABLE IF NOT EXISTS audit.input_files (
    run_id     INTEGER,
    glob       VARCHAR,   -- the glob pattern from config
    file_path  VARCHAR,   -- each matched file
    sha256     VARCHAR    -- lets the reviewer verify inputs are unchanged
);
```

Notes:

- Every command is logged, including temp-table cleanup `DROP`s — a reviewer
  should see the full sequence.
- The log is **append-only across runs**; `run_id` namespaces each invocation.
- `audit.input_files` supports the reproducibility promise: "re-run the
  documented commands" is only meaningful if inputs are provably unchanged.
  Same reasoning for `config_sha256`.

## 3. Store the config and input references in the database

- **Full YAML config text** in `audit.runs` — makes the artifact self-describing.
- **Work paper metadata** — read from the config YAML at run time and stored in
  `audit.runs` (the `work_paper.metadata` values) for reproducibility.
- **Input file references with SHA-256 hashes** in `audit.input_files` — the data
  files themselves are external client files and cannot live in the DB, but their
  paths and hashes can. The runner computes the SHA-256 hash of each input file
  at run time.

## 4. Work paper generation (post-run phase)

New `WorkPaperBuilder` in `AutoAnalyst.Library`:

- **Inputs**: work-paper template (an app resource selected by step type), the
  `DatabaseEngine` (reads the latest run from `audit.runs` + `audit.command_log`
  + result tables — no in-memory plumbing needed). Work paper metadata is read
  from the database (`audit.runs`) only, not from the config file.
- **Excel library**: **ClosedXML** (MIT — matches repo license, actively
  maintained, loads existing `.xlsx` templates). EPPlus and DuckDB's native
  `COPY (FORMAT XLSX)` are intentionally excluded (EPPlus licensing; DuckDB COPY
  is single-table, data-only, cannot build a styled template-based multi-sheet
  workbook).
- **Data access**: pull result tables with `DuckDBConnection` + `ExecuteReader`
  (same pattern as `StratifiedSamplePlanBuilder`) and write into sheets. Avoids
  the unsupported XLSX import path entirely.
- **Work paper model**: the program outputs **multiple work papers — one per
  step** — not a single workbook with one worksheet per step. Each work paper is
  its own Excel workbook built from a template. A `standard_analysis` step,
  being a roll-up of sub-steps, produces exactly **one** work paper containing
  all of its outputs.
- **Work paper contents** (based on templates such as
  `planning/Work Paper Templates/TEMPLATE - Work Paper Overview Worksheet.xlsx`):
  - **Work paper heading**: every work paper includes a heading table (Company,
    Project, Client, EIC, Author, Date, Purpose) populated from the metadata
    stored in `audit.runs` (originally from the config `work_paper.metadata`
    block), output to Excel in a table format.
  - **Procedure**: the step's exact SQL under "Procedure" (fulfills the README
    promise of "a copy of the exact commands used"); for `standard_analysis`,
    the SQL of all sub-steps is appended together.
  - **Results**: each step output table rendered at its corresponding named
    range in the template. A template can (and typically does) have **more than
    one named range** that information is output to — e.g. `standard_analysis`
    and sampling steps output data tables to numerous named ranges.

### Template observations (planning)

- Every work paper template is expected to contain a **work paper heading** (a
  metadata table, e.g. Company/Project/Client/EIC/Author/Date/Purpose) populated
  from config, plus **one or more named ranges** for step outputs.
- The current example template (`TEMPLATE - Work Paper Overview Worksheet.xlsx`)
  contains a Heading table at `A2:B11` and three task tables at `A16`, `A24`,
  `A33`. Because each task now has its own work paper, the multi-task table is
  no longer needed; templates are created per step type as app resources.
- The application never creates worksheets; work papers are based on templates.
  Dataset output is truncated to fit the available space in the template (Excel
  row/column limits, adjusted for the top-left starting cell of the output).

## 5. Runner changes (two phases)

1. **Run phase** — execute each config step, capturing a per-step record
   `{ description, sql statements, rows affected, output table names }`. The
   runner reads the work paper metadata from the config YAML, computes the
   SHA-256 hash of each input file, and writes `audit.runs`,
   `audit.command_log`, and `audit.input_files`.
2. **Work paper phase** — build one work paper per step from the database +
   per-step template + metadata.

### Code changes (summary)

- New `ExecutedStep` record — `{ Description, SqlStatements, RowsAffected,
  OutputTables }`; the runner produces one per config step. `SqlBatchResult`
  stays as-is.
- Persist audit rows during/after execution.
- New `WorkPaperBuilder` class (ClosedXML).
- Config: top-level `work_paper:` block (see sketch below).

### Config shape (draft)

```yaml
steps:
  # ... analytical steps only; no export steps for documentation purposes ...

# No export steps needed for documentation. One block declares how to document:
work_paper:
  output_directory: work_papers/   # one .xlsx work paper per step is written here
  metadata:                        # used for every work paper's heading table
    company: Example LLC
    client: ACME Corp
    project: 2026 Q1 AP Audit
    eic: J. Smith
    author: M. Descy
    date: 2026-08-02
    purpose: Verify completeness and accuracy of AP extract
  include_tables: auto        # bind each step's output table(s) to named ranges in the template
  include_sql: true           # step SQL under "Procedure"
  # Templates are app resources selected by step type; per-step overrides optional.
```

## 6. Reproducibility / future replay

- The concatenated `audit.command_log.sql_text` for a run *is* a valid replay
  script. This is a zero-code property worth documenting.
- A future `verify`/`replay` command could read `audit.command_log` and re-execute
  the statements in order.

## 7. Data flow (read / write / retain)

### Key fact

Query results are **already in the database** by the time each command finishes.
Commands do not return result sets to C# — they *materialize* them
(`CREATE OR REPLACE TABLE x AS SELECT ...` writes the output to a table in the
DuckDB file). Nothing about the results is ever held in memory; the only
transient, in-memory data is small metadata (SQL text strings, row counts, table
names).

### Write path (during the run)

```
1. Runner reads config → builds commands (ISqlCommandBatch)
2. Command.BuildSql() → SQL text (in memory, tiny)
3. Command.Execute() → DuckDB executes; result materializes
   into a table in the .duckdb file  ← results are now PERSISTED
4. Runner appends a row to audit.command_log:
   { run_id, seq, step_type, sql_text, rows_affected, status }
5. Also writes audit.runs (full config YAML + hash) and
   audit.input_files (paths + SHA-256) at run start
6. In memory: a per-step ExecutedStep record accumulates
   { description, sql text, rows affected, output table names }
```

Notes:

- Step 4 is a small metadata write per command, not a data movement.
- SQL is logged **as each command executes, not at the end**, so even a run that
  fails partway retains everything that was attempted (matters for reproducing
  partial work).
- Capture and rendering are **decoupled**: capture is incremental and happens
  during the run; rendering happens once, after the run.

### Read path (after the run)

```
7. WorkPaperBuilder opens the same .duckdb file
8. Reads audit.runs         → config, work-paper metadata
   Reads audit.command_log  → the generated SQL, in order
   Queries each output table via DuckDBConnection + ExecuteReader
9. Writes one .xlsx work paper per step via ClosedXML
   (heading table + SQL + output tables at named ranges)
```

The builder streams table data row-by-row into the workbook; it never loads
results into memory either.

### Why work papers are rendered after the run, not during it

Work papers are rendered **after the run phase completes** — one work paper per
step — rather than immediately after each step executes:

- A `standard_analysis` step expands into many SQL statements at run time, but is
  captured as that many rows in `audit.command_log` and rendered as a *single*
  work paper containing all of its outputs, using the step boundary recorded in
  `ExecutedStep`. Step complexity affects neither capture nor the render timing.
- Rendering is fully decoupled from execution: the run phase stays purely about
  executing steps and capturing the audit log; everything the builder needs
  (config/metadata, generated SQL, result tables) is already in the database by
  the end of the run.

### Retention

Everything is retained in the **.duckdb file**, which is the real deliverable:

| What | Where it lives |
|---|---|
| Query results | User tables in the DB (persisted by the commands themselves) |
| Generated SQL | `audit.command_log` |
| Config + hashes | `audit.runs` |
| Input files + hashes | `audit.input_files` |

The `.xlsx` is a *derived rendering* — regenerating it anytime means re-running
the builder against the same database file. Nothing is lost if the process
crashes mid-run (which is why per-command logging matters).

### Implication for `SqlBatchResult`

`SqlBatchResult` currently aggregates all SQL in memory and is only returned at
the end. Adopting this flow means either writing to `audit.command_log` as the
run goes (recommended) or keeping both the in-memory record and the DB write.
The in-memory side stays small either way — text and integers, never result data.

## 8. Excel work paper templates (recommendations)

### The template owns the layout — via named ranges, not code or config

Cell addresses must not live in C# code or in a separate mapping configuration.
The template declares "where data goes" with **named ranges**; code and config
only ever refer to names, never coordinates. This removes the two-source-of-truth
problem of the old design (template + address config that drifted and was
annoying to set up) and survives layout edits: moving things in Excel is a
template edit, not a code/config change. ClosedXML reads defined names back
directly (`workbook.DefinedNames` / `NamedRanges`), so this is well supported.

### Binding by convention (no mapping table)

| Data | Named range in template | Rule |
|---|---|---|
| Metadata (`company`, `purpose`, ...) | `Company`, `Purpose`, ... | name = metadata key |
| Step output tables | `SampleOutput` for table `sample_output` | name = normalized table name |

Binding is per **step output**, and the number of outputs depends on the step type:

- **Most steps**: one output — the destination table — bound by its normalized
  table name.
- **Sampling steps**: two outputs — the **sampling plan** and the **sample** —
  bound to *different* named ranges in the template (e.g. `SamplePlan` and
  `SampleOutput`).
- **`standard_analysis`**: a roll-up of sub-steps; all of its destination tables
  are outputs, each bound to a corresponding named range. The SQL of all the
  sub-steps is **appended together** under the step's Procedure section.

Every work paper template combines both kinds of output: the **work paper
heading** (metadata table, from config `work_paper.metadata`) and the step's
**output tables**. A template can be expected to have **more than one named
range** that information is output to.

The builder:
1. Loads the template, builds a lookup of defined names → cells.
2. Writes metadata values into any name matching a metadata key; skips missing names.
3. For each step output, looks up a defined name matching its logical output
   name (see step types above). If found, the name's top-left cell is an
   **anchor**: the table is streamed downward/rightward from there via
   `ExecuteReader`. If not found, it falls back to the **default template
   layout** (heading table + SQL under Procedure + results below).
4. Work paper file names follow a deterministic convention (step index + name).
   The application never creates worksheets; it fills in the ones the template
   provides.

### Templates are app resources; external overrides are opt-in

Work paper templates are created by the developer and stored within the app as
**resources**. The default template for each step type is used automatically for
every project. A config override exists only for the rare project needing
something custom; because a template self-describes via named ranges, a custom
template requires zero code or mapping-config changes — it is just an Excel file
with the right named ranges. This avoids the per-project template setup burden.

### Two layers, one builder

- **Default templates per step type** → stored as app resources; fully
  automatic, identical work paper shape every engagement.
- **Custom template** (opt-in) → overrides where data lands, purely by authoring
  named ranges in Excel. No code, no mapping config.

### What lives where

| Concern | Owner |
|---|---|
| Layout, cell addresses, metadata placement | **The template** (named ranges) |
| Binding rule (name = normalized table/key) | **C# code** (one convention, invariant) |
| Which template to use for each step type | **App resources** (templates created by the developer) |
| Per-step template overrides | **Config**, optional |

### Reducing setup friction

- **Validation report at load time**: list every defined name found, every output
  matched, and every output not placed (fell back to default layout). Setup
  mistakes surface immediately instead of in the finished workbook.
- **Documented naming convention** (e.g. lowercase, underscores for spaces) so
  authoring names is predictable; missing names degrade gracefully, not fatally.
- **Named ranges per output** — each output table and each heading field has its
  own named range; templates for a step type are authored once and reused, so
  setup cost is one-time template authoring, not a per-project effort.

---

## Open decisions

- [ ] Exact work paper requirements (layout, contents, formatting) — **Templates will be created by the developer. Those need to be stored within the app as resources.**.
- [ ] `include_tables: auto` convention. - **RESOLVED: except for sampling steps, a step's destination table is the only table output to a work paper. Sampling steps output both the sampling plan and the sample, to different named ranges in the template. A `standard_analysis` step outputs all of its destination tables, each to a corresponding named range; all SQL for its sub-steps is appended together in the work paper.**
- [x] Overview task-table handling beyond the template's 9-task limit. - **RESOLVED: the app has no upper limit to the number of tasks it can perform in a run. Each task has its own output work paper, so the template's multi-task table is no longer used.**
- [ ] Work paper file naming and truncation rules. - **The application never creates worksheets; work papers are based on templates. Dataset output is truncated to fit the available space in the template (.xlsx row/column limits, adjusted to account for the starting position, which is the top-left cell, of the table that is output). Work paper file names follow a deterministic convention (step index + name).**
- [ ] Whether `audit.input_files` hashing is computed in C# at run time. - **Yes, the runner will compute the SHA-256 hash of each input file at run time and store it in the `audit.input_files` table.**
- [ ] Whether work paper metadata is read from config YAML or from `audit.runs`
  (config is stored in DB, so the builder can read it from there). - **The work paper metadata will be read from the config YAML at run time and stored in the `audit.runs` table for reproducibility. It does not matter where it is read from when the work papers are built, but I would prefer it to be read from the database only.**
