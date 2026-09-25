namespace AutoAnalyst.Library.Data;

/// <summary>
/// Specifies the type of join to perform between two tables.
/// </summary>
public enum JoinType
{
    /// <summary>Returns rows with matching values from both tables.</summary>
    Inner,

    /// <summary>Returns every row from the left table and matching rows from the right table.</summary>
    LeftOuter,

    /// <summary>Returns every row from the right table and matching rows from the left table.</summary>
    RightOuter,

    /// <summary>Returns every row from both tables, matching rows where possible.</summary>
    FullOuter,

    /// <summary>Returns left-table rows with no match in the right table.</summary>
    LeftAnti,

    /// <summary>Returns right-table rows with no match in the left table.</summary>
    RightAnti
}