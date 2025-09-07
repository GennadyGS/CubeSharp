# CubeSharp

High-performance, zero-dependency (runtime) .NET library for constructing and querying in-memory multidimensional data cubes (a.k.a. OLAP style aggregates) using strongly-typed, testable C# code.

NuGet: [![NuGet](https://img.shields.io/nuget/v/Cube.svg)](https://www.nuget.org/packages/Cube)  
Targets: .NET 8, .NET 9

> Detailed walkthroughs and additional examples are provided in the notebooks:
> - Tutorial (concepts, end‑to‑end): doc/01-Tutorial.ipynb
> - Cookbook / patterns: doc/02-Examples.ipynb
>
> A concise narrative version is also in: doc/CubeSharp.md

---
## Contents
- Why CubeSharp
- Features
- Install
- Quick Start (5 minutes)
- Core Concepts (TL;DR)
- Slicing & Breakdown
- Generic Transformations (Cube -> Table)
- Advanced Scenarios
- Performance & Complexity
- FAQ
- Contributing
- License

---
## Why CubeSharp
Building multi‑factor tabular reports (dimensions, totals, hierarchies) directly with ad‑hoc LINQ or SQL quickly becomes fragile, repetitive, and hard to unit test. CubeSharp lets you:
- Declare aggregations (measures) and dimensions once
- Reuse & compose them across reports
- Query any cell, slice, or cross‑section precisely
- Transform cubes generically into tables / trees without bespoke loops

Everything runs purely in memory over existing collections / async streams.

---
## Features
- Arbitrary number of dimensions (including zero)
- Hierarchical dimension indexes & subtotal (default) index handling
- Multi‑selection dimensions (one source item contributes to multiple indexes)
- Pluggable aggregation (sum, min, max, collect, custom structs)
- Generic slicing (indexer or positional) & breakdown operations
- Works with synchronous IEnumerable<T> and IAsyncEnumerable<T>
- Strongly typed, test friendly (definitions are simple objects)
- No reflection at runtime for aggregation core path

---
## Install
```
dotnet add package CubeSharp
```
(Temporary package id retains historical name; library namespace / docs use CubeSharp.)

---
## Quick Start (5 minutes)
```csharp
var orders = new[] {
    new { OrderDate = new DateTime(2007,08,02), Product = "X", EmployeeId = 3, CustomerId = "A", Quantity = 10m },
    new { OrderDate = new DateTime(2007,12,24), Product = "Y", EmployeeId = 4, CustomerId = "B", Quantity = 12m },
    // ...
};

// 1. Aggregation (measure)
var qty = AggregationDefinition.CreateForCollection(
    orders, o => o.Quantity, (a,b) => a + b, 0m);

// 2. Dimensions (explicit index ordering + trailing total)
var customers = DimensionDefinition.CreateForCollection(
        orders, o => o.CustomerId, title: "Customers",
        IndexDefinition.Create("A","Customer A"),
        IndexDefinition.Create("B","Customer B"))
    .WithTrailingDefaultIndex("Total");

var years = DimensionDefinition.CreateForCollection(
        orders, o => o.OrderDate.Year.ToString(), title: "Years",
        IndexDefinition.Create("2007","2007 Year"),
        IndexDefinition.Create("2008","2008 Year"))
    .WithTrailingDefaultIndex("Total");

// 3. Build cube
var cube = orders.BuildCube(qty, customers, years);

// 4. Query some cells
var a2007 = cube.GetValue("A", "2007");
var all2007 = cube.GetValue(default, "2007");    // total over customers for 2007
var aAllYears = cube.GetValue("A");               // trailing default omitted
var grandTotal = cube.GetValue();                  // all defaults omitted

// 5. Create a table (rows: customers, columns: years)
var table = cube
    .BreakdownByDimensions(..^1) // all but last dimension => rows
    .Select(row => row
        .GetBoundDimensionsAndIndexes()
        .Select(di => KeyValuePair.Create(di.dimension.Title!, (object?)di.dimension[di.index].Title))
        .Concat(row.BreakdownByDimensions(^1)
            .Select(col => KeyValuePair.Create(col.GetBoundIndexDefinition(^1).Title!, (object?)col.GetValue())))
        .ToDictionary(k => k.Key, v => v.Value));
```
For a fuller narrative see doc/01-Tutorial.ipynb.

---
## Core Concepts (TL;DR)
- AggregationDefinition: valueSelector + aggregationFunction + seedValue
- DimensionDefinition: indexSelector (or multi-selector) + ordered IndexDefinition set + optional title
- IndexDefinition: value + optional title + optional hierarchical children
- Default (null / default(TIndex)) index: subtotal over whole dimension
- BuildCube: source.BuildCube(aggregation, dim1, dim2, ...)
- CubeResult: immutable object supporting GetValue, indexing (slicing), breakdown operations

---
## Slicing & Breakdown
- cube["A"] or cube.Slice(0, "A") fixes first dimension -> lower dimensional cube
- cube.Slice((1,"2008"),(0,"A")) slices by multiple dimensions in any order
- cube.BreakdownByDimensions(0) enumerates slices for every index (including totals) of dimension 0
- Ranges: .. (all), ..^1 (all except last), ^1 (last) enable dimension-agnostic generic code

---
## Generic Transformations (Cube -> Table)
Because slices retain (dimension, index) metadata you can:
1. Pick row dimension set R and column dimension set C
2. Breakdown over R -> per-row slice
3. Inside each row slice breakdown over shifted C
4. Build headers from bound dimension titles + index titles

See reusable helpers & variations in doc/02-Examples.ipynb (e.g. ToTable extension) and narrative in doc/01-Tutorial.ipynb (Generic Cube Operations section).

---
## Advanced Scenarios
- Multi-Selection Dimensions: One source element contributes to multiple indexes (tags, categories). Use DimensionDefinition.CreateWithMultiSelector(...).
- Hierarchies: Parent IndexDefinition with child indexes -> automatic subtotal roll up.
- Custom Aggregates: Use structs / records (define Zero + Combine).
- Async Pipelines: Use IAsyncEnumerable<T> + Async LINQ (System.Linq.Async) with the provided async BuildCube overloads (see source & tests).
- Placeholder Dimensions: DimensionDefinition.CreateDefault<TSource,TIndex>() keeps shape uniform.

---
## Performance & Complexity
| Aspect | Notes |
|--------|-------|
| Build | Single pass over source; each item routed to indexes of each dimension (multi-selection may fan out). |
| Memory | Stores aggregated values per Cartesian product of explicitly declared indexes (controlled cardinality). |
| Query | O(1) per cell lookup (array indexing). |
| Breakdown | Enumerates pre-computed slices; no re-aggregation. |

Design encourages declaring only required indexes (no auto-discovery unless you choose to). This makes cube size predictable & testable.

---
## FAQ (Abbreviated)
Q: How do I get a grand total?  A: cube.GetValue().
Q: How do I add a total row/column?  A: Add a default index (WithLeading/TrailingDefaultIndex).
Q: Missing index?  A: Returns seedValue (no exception).
Q: Different dimension key types?  A: Normalize to a single TIndex (e.g. string) during definition.

More Q&A patterns in doc/02-Examples.ipynb.

---
## Contributing
1. Clone repository
2. `dotnet build && dotnet test`
3. Add / adjust tests (CubeSharp.Tests)
4. Open PR

Issues / ideas: please include concise reproduction plus expected vs actual.

---
## License
MIT (see LICENSE). Packaged README references this file.

---
## Additional References
- doc/01-Tutorial.ipynb – Step-by-step tutorial
- doc/02-Examples.ipynb – Cookbook patterns & helper extensions
- doc/CubeSharp.md – Markdown export (narrative form)
- tests/ – Specification by example
