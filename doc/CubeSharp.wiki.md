# CubeSharp Library - Wiki Documentation

## Table of Contents

- [Home](#home)
- [Getting Started](#getting-started)
- [Building Cubes](#building-cubes)
  - [Aggregation Definitions](#aggregation-definitions)
  - [Dimension Definitions](#dimension-definitions)
  - [Multi-selection Dimensions](#multi-selection-dimensions)
- [Querying Cubes](#querying-cubes)
  - [Single Cube Cells](#single-cube-cells)
  - [Slicing Operations](#slicing-operations)
  - [Breakdown Operations](#breakdown-operations)
  - [Generic Cube Operations](#generic-cube-operations)
- [API Reference](#api-reference)
  - [Core Classes](#core-classes)
  - [Extension Methods](#extension-methods)
- [Examples & Recipes](#examples--recipes)
- [Performance Guide](#performance-guide)

---

## Home

**CubeSharp** is a lightweight .NET library for building in-memory [data cubes](https://en.wikipedia.org/wiki/Data_cube). It provides a systematic approach to implementing multi-dimensional reports by building an in-memory data cube, which you can then transform into the required tabular representation.

### Key Features

- **Multi-dimensional analysis**: Build data cubes with any number of dimensions
- **Flexible aggregations**: Support for sum, count, min, max, and custom aggregation functions
- **Hierarchical dimensions**: Support for parent-child relationships in dimension indexes
- **Generic querying**: Slice, dice, and breakdown operations for flexible data analysis
- **Type-safe**: Full generic type support with compile-time type checking
- **LINQ integration**: Seamless integration with existing LINQ workflows
- **Async support**: Built-in support for async enumerable data sources

### Installation

Install via NuGet Package Manager:

```sh
dotnet add package Cube
```

### Target Frameworks

- .NET 9
- .NET 8

---

## Getting Started

### The Problem

Suppose you need to create a tabular report involving multiple factors (dimensions), aggregations, and calculations, including totals by one or more dimensions. Using SQL for this purpose limits your ability to refactor and test your code, making maintenance difficult. Native .NET capabilities, such as LINQ, require a lot of ad-hoc data manipulation (filtering, mapping, aggregation), which also complicates maintenance.

CubeSharp provides a systematic approach to implementing multi-dimensional reports by building an in-memory data cube, which you can then transform into the required tabular representation.

### Basic Example

Imagine you have a collection of data in the following form:

```csharp
var orders = new [] {
    new {
        OrderDate = new DateTime(2007, 08, 02),
        Product = "X",
        EmployeeId = 3,
        CustomerId = "A",
        Quantity = 10m,
    },
    new {
        OrderDate = new DateTime(2007, 12, 24),
        Product = "Y",
        EmployeeId = 4,
        CustomerId = "B",
        Quantity = 12m,
    },
    // ...
};
```

You want to build a report with customer IDs as rows and order years as columns, calculating the total quantity for each cell. You also have a fixed list of rows and columns to display, including totals.

The desired report might look like this:

| Customers  | 2007 Year | 2008 Year | 2009 Year | Total |
|------------|-----------|-----------|-----------|-------|
| Customer A | 22        | 40        | 10        | 72    |
| Customer B | 20        | 12        | 15        | 47    |
| Customer C | 22        | 14        | 20        | 56    |
| Customer D | 0         | 0         | 60        | 60    |
| Total      | 64        | 66        | 105       | 235   |

To build such a report with CubeSharp, you would write the following code:

```csharp
// Aggregation definition: calculate sum of Quantity field
var aggregationDefinition = AggregationDefinition.CreateForCollection(
    orders,
    order => order.Quantity,
    (a, b) => a + b,
    seedValue: 0);

// Rows dimension: use field CustomerId, specify list of values with titles to display,
// include total row at the bottom
var customerIdDimension = DimensionDefinition.CreateForCollection(
    orders,
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"),
    IndexDefinition.Create("C", "Customer C"),
    IndexDefinition.Create("D", "Customer D"))
        .WithTrailingDefaultIndex("Total");

// Columns dimension: use Year property of field CustomerId,
// specify list of values with titles to display, include total row at the bottom
var yearDimension = DimensionDefinition.CreateForCollection(
    orders,
    order => order.OrderDate.Year.ToString(),
    title: "Years",
    IndexDefinition.Create("2007", "2007 Year"),
    IndexDefinition.Create("2008", "2008 Year"),
    IndexDefinition.Create("2009", "2009 Year"))
        .WithTrailingDefaultIndex("Total");

// Build cube from source collection using aggregation and list of dimension definitions.
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Transform cube to table in form of collection of dictionaries.
var report = cube
    .BreakdownByDimensions(..^1) // ..^1 - build rows by range of all dimensions except last
    .Select(row => row
        .GetBoundDimensionsAndIndexes()
        .Select(dimensionAndIndex => KeyValuePair.Create(
            dimensionAndIndex.dimension.Title!,
            (object?)dimensionAndIndex.dimension[dimensionAndIndex.index].Title))
        .Concat(row
            .BreakdownByDimensions(^1) // ^1 - build columns by last dimension
            .Select(column => KeyValuePair.Create(
                column.GetBoundIndexDefinition(^1).Title!,
                (object?)column.GetValue())))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```

---

## Building Cubes

To build a cube, use the generic extension method `BuildCube(...)` on any collection, providing an aggregation definition and any number of dimension definitions (including zero) as a [parameter array](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params).

```csharp
orders.BuildCube(aggregationDefinition, customerIdDimension);

orders.BuildCube(aggregationDefinition, yearDimension, customerIdDimension);

orders.BuildCube(aggregationDefinition, yearDimension, customerIdDimension, productDimension);
```

### Type Arguments

The method `BuildCube<TSource, TIndex, T>(...)` is generic with the following type arguments:
- `TSource`: The type of the source collection's item.
- `TIndex`: The type of the dimension index. In the previous example, this is the type of the column and row indexes: `order.CustomerId` and `order.OrderDate.Year.ToString()`. All dimensions must use the same type, so if your row and column data types differ, convert them to a common type (e.g., `string`).
- `T`: The type of the aggregation result, i.e., the type of the cube cell. In our example, this is the type of `order.Quantity`, inferred as integer.

Usually, type arguments are inferred automatically, but if not (e.g., when there are no dimensions), you must specify them explicitly:

```csharp
Enumerable.Range(1, 10)
    .BuildCube<int, int, int>(
        AggregationDefinition.Create((int i) => i, (a, b) => a + b, 0));
```

### Async Support

CubeSharp also supports building cubes from async enumerables:

```csharp
IAsyncEnumerable<Order> asyncOrders = GetOrdersAsync();
var cube = await asyncOrders.BuildCubeAsync(aggregationDefinition, customerIdDimension, yearDimension);
```

---

## Aggregation Definitions

An aggregation (or measure) specifies how to calculate cube cells. Aggregation definitions are created with the static method `AggregationDefinition.Create(...)` (or its variants) as follows:

```csharp
AggregationDefinition.Create(
    order => order.Quantity,
    (a, b) => a + b,
    seedValue: 0);
```

### Type Arguments

The method `AggregationDefinition.Create<TSource, T>(...)` is generic with the following type arguments:
- `TSource`: The type of the source collection's item.
- `T`: The type of the aggregation result, i.e., the type of the cube cell.

### Parameters

- `valueSelector`: A lambda (or expression) specifying which field of the source collection to aggregate (e.g., `order => order.Quantity`). If your collection directly contains the values, use `x => x`. The expression can also combine multiple fields.
- `aggregationFunction`: A lambda with signature `Func<T, T, T>`, specifying how to aggregate data (e.g., sum, min, max).
- `seedValue`: The seed value for aggregation, which also defines the value for cells with no matching data.

The last two arguments correspond to those of the [LINQ Aggregate method](https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.aggregate?view=netcore-3.1).

### Common Aggregation Examples

```csharp
// Take sum
AggregationDefinition.Create((int i) => i, (a, b) => a + b, 0);
AggregationDefinition.Create((decimal d) => d, (a, b) => a + b, 0);

// Take product
AggregationDefinition.Create((int i) => i, (a, b) => a * b, 1);

// Take minimum
AggregationDefinition.Create((int i) => i, (a, b) => Math.Min(a, b), int.MinValue);

// Count the elements
AggregationDefinition.Create((int i) => 1, (a, b) => a + b, 0);

// Collect items
AggregationDefinition.Create(
    (int i) => new[] { i },
    (a, b) => a.Concat(b).ToArray(),
    Array.Empty<int>());
```

### Complex Aggregations

If you need to calculate multiple values for each cell, create a composite type and use it as the aggregation result type `T`:

```csharp
// Create structure for storing two aggregation values: count and sum
struct CountAndSum
{
    public CountAndSum(int count, decimal sum)
    {
        Count = count;
        Sum = sum;
    }

    public int Count { get; }

    public decimal Sum { get; }

    // Define Zero instance for seeding the aggregation
    public static CountAndSum Zero => new CountAndSum(0, 0);

    // Define Combine function for aggregation
    public static CountAndSum Combine(CountAndSum left, CountAndSum right) =>
        new CountAndSum(left.Count + right.Count, left.Sum + right.Sum);
}

// Create aggregation definition for calculating count amd sum
AggregationDefinition.Create(
    (int i) => new CountAndSum(1, i),
    CountAndSum.Combine,
    CountAndSum.Zero);
```

### Factory Method Variants

There are several variants of `AggregationDefinition.Create(...)`:

- `AggregationDefinition.CreateForDictionaryCollection(...)` is a shortcut for collections of `IDictionary<string, object>`, so you don't need to specify the type argument.
- `AggregationDefinition.CreateForCollection(...)` takes the collection as an additional argument for type inference, useful for anonymous types.

```csharp
// Take sum of field Quantity
AggregationDefinition.CreateForDictionaryCollection(
    dict => (decimal)dict["Quantity"], (a, b) => a + b, 0);
```

Method `AggregationDefinition.CreateForCollection(...)` takes collection as additional argument, which is used only for type inference. It can be used also for anonymous types, as we have in example.

```csharp
// Take sum of field Quantity
AggregationDefinition.CreateForCollection(
    orders,
    order => order.Quantity,
    (a, b) => a + b,
    0);
```

---

## Dimension Definitions

A dimension definition specifies which field of the source collection to use and which values are of interest. Create a dimension definition with the static method `DimensionDefinition.Create(...)` (or its variants) as follows:

```csharp
DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"),
    IndexDefinition.Create("C", "Customer C"));
```

### Type Arguments

The method `DimensionDefinition.Create<TSource, TIndex>(...)` is generic with:
- `TSource`: The type of the source collection's item.
- `TIndex`: The type of the index, i.e., the field specified in `indexSelector`.

### Parameters

- `indexSelector`: A lambda (or expression) specifying which field of the source collection to use as the index. More generally, it can compute the index from any expression.

```csharp
DimensionDefinition.Create(
    order => order.OrderDate.Year % 4 == 0 ? "leap" : "nonLeap",
    title: "Years",
    IndexDefinition.Create("leap", "Leap years"),
    IndexDefinition.Create("nonLeap", "Non-leap years"));
```

- `title`: The dimension's title, used for tabular representation.
- `indexDefinitions`: A [params](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params) array of index definitions.

### Index Definitions

Index definitions are created with static method `IndexDefinition.Create(...)` in following way:

```csharp
IndexDefinition.Create("A", "Customer A");
```

Index values must be unique within a dimension.

### Hierarchical Indexes

Indexes can be hierarchical, with parent indexes representing subtotals for their children. Child indexes are specified as additional [params](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params) arguments:

```csharp
IndexDefinition.Create(
    "A",
    title: "Product category A",
    IndexDefinition.Create("A1", "Product A1"),
    IndexDefinition.Create("A2", "Product A2"));
```

A dimension definition implements [IEnumerable<>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1?view=netcore-3.1) of `IndexDefinition<TIndex>`, allowing you to traverse index definitions in order—useful for building tables.

By default, child indexes follow their parent, but you can reverse the order with an overload of `IndexDefinition.Create(...)`:

```csharp
// Order of indexes:
// Product A1
// Product A2
// Product category A
IndexDefinition.Create(
    new[] {
        IndexDefinition.Create("A1", "Product A1"),
        IndexDefinition.Create("A2", "Product A2"),
    },
    "A",
    title: "Product category A");
```

### Default Index

The value `default` (or `null` for reference/nullable types) is reserved for the _Default index_, representing the total for the dimension (e.g., a total row or column).

> **_NOTE:_** If `default` conflicts with a regular index value (e.g., `0` for `int`), consider using a nullable type (`int?`) or mapping `default` to another value in `indexSelector`.

The _Default index_ must be the only root index and can have other indexes as children:

```csharp
/// Correct
DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create(
        (string?)default,
        title: "Total",
        IndexDefinition.Create("A", "Customer A"),
        IndexDefinition.Create("B", "Customer B")));

// Error: Default index should be the only root index
DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create((string)default, "Total"),
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"));
```

### Default Index Shortcuts

There are shortcuts for creating a default index in an existing dimension: `.WithLeadingDefaultIndex(...)` and `.WithTrailingDefaultIndex(...)`, which create a new `DimensionDefinition` with the default index and nest all existing indexes as its children:

```csharp
// Order of indexes:
// Total
// Customer A
// Customer B
DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"))
        .WithLeadingDefaultIndex("Total");

// Order of indexes:
// Customer A
// Customer B
// Total
DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"))
        .WithTrailingDefaultIndex("Total");
```

### Default Dimensions

Sometimes, you may need a _Default dimension_—a dimension with only the default index. This is a degenerate form, useful as a placeholder to keep a constant number of dimensions. Since it has no actual index values, the `indexSelector` is redundant. Use `DimensionDefinition.CreateDefault(...)` to create default dimensions:

```csharp
DimensionDefinition.CreateDefault<string, string>(
    title: "Customers",
    indexTitle: "All customers");
```

### Factory Method Variants

There are also variants for dictionary collections and for type inference with anonymous types:

```csharp
DimensionDefinition.CreateForDictionaryCollection(
    dict => (string?)dict["CustomerId"],
    title: "Customers",
    IndexDefinition.Create(
        (string?)default,
        title: "Total",
        IndexDefinition.Create("A", "Customer A"),
        IndexDefinition.Create("B", "Customer B")));
```

Method `DimensionDefinition.CreateForCollection(...)` takes collection as additional argument, which is used only for type inference. It can be used also for anonymous types, as we have in example:

```csharp
DimensionDefinition.CreateForCollection(
    orders,
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create(
        (string?)default,
        title: "Total",
        IndexDefinition.Create("A", "Customer A"),
        IndexDefinition.Create("B", "Customer B")));
```

---

## Multi-selection Dimensions

If the target entity has a collection of attributes (e.g., tags) or a one-to-many/many-to-many relationship, use `DimensionDefinition.CreateWithMultiSelector(...)`, which takes an index selector returning multiple values (`Func<TSource, IEnumerable<TIndex>>`). There are also variants for dictionary collections and for type inference.

```csharp
DimensionDefinition.CreateForCollectionWithMultiSelector(
    orders,
    order => order.Tags,
    title: "Tags",
    IndexDefinition.Create(
        (string?)default,
        title: "Total",
        IndexDefinition.Create("Bestseller", "Bestseller"),
        IndexDefinition.Create("Discount", "Discount")));
```

This is particularly useful for scenarios where a single source record should contribute to multiple dimension values, such as:

- Products with multiple categories
- Orders with multiple tags
- Customers associated with multiple regions
- Events spanning multiple time periods

---

## Querying Cubes

## Single Cube Cells

The basic operation for querying cubes is the `GetValue(...)` method of `CubeResult`, which returns the aggregated data for a specified cell. Pass any number of index values (as a [params](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params) array), each corresponding to a cube dimension in the order specified in `BuildCube(...)`.

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Get aggregated value for customer "A" and year 2007
cube.GetValue("A", "2007");
```

To get a total by a dimension, specify `default` (or `null` for reference/nullable types) in the corresponding position.

```csharp
// Get aggregated value for all customers and year 2007
cube.GetValue(default, "2007");

// Get aggregated value for customer "A" and all years
cube.GetValue("A", default);

// Get aggregated value for all records
cube.GetValue(default, default);
```

You can also omit trailing `default` index values as a shorthand:

```csharp
// The same as cube.GetValue("A", default)
cube.GetValue("A");

// The same as cube.GetValue(default, default) or cube.GetValue(default)
cube.GetValue();
```

> **_NOTE:_** Omitting trailing default indexes allows you to write generic code that does not depend on the number of dimensions. For example, `cube.GetValue()` gets the total for any cube, and `cube.GetValue(index)` gets a value by index in the first dimension (if present). See [Generic cube operations](#generic-cube-operations) for more.

`GetValue(...)` always returns a result unless you specify more parameters than there are dimensions (see `CubeResult.FreeDimensionCount`). If any index is not included in the dimension's index definitions, the result will be the `seedValue` from the aggregation definition.

```csharp
// Returns 0, since customer id "Z" is not specified in customerId dimension
// and seedValue of aggregationDefinition is 0
cube.GetValue("Z", "2007"); // 0

// Returns 0, since year 1999 is not specified in yearDimension dimension
// and seedValue of aggregationDefinition is 0
cube.GetValue("A", "1999"); // 0
```

---

## Slicing Operations

A _slice_ operation can be thought of as pinning the indexes for some dimensions. There are several ways to achieve the same result:

```csharp
// Get aggregated value for customer "A" and year 2007
cube.GetValue("A", "2007");
// vs
cube["A"].GetValue("2007");
// vs
cube["A"]["2007"].GetValue();

// Get aggregated value for customer "A" and years 2007 and 2008
cube.GetValue("A", "2007");
cube.GetValue("A", "2008");
// vs
var slice = cube["A"];
slice.GetValue("2007");
slice.GetValue("2008");
```

In these examples, the slice operation uses the index operator `[]`.

Formally, a _slice_ extracts an _(n-k)_-dimensional cube from an _n_-dimensional cube by picking values for _k_ dimensions (usually _k = 1_).

### Slice Methods

You can slice with the `[]` operator or the more general `Slice(...)` method. The latter allows slicing by multiple dimensions in any order, while `[]` only slices by the first free dimension. The dimension number can be specified as an [Index](https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/ranges-indexes#language-support-for-indices-and-ranges), convertible to `int`.

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Returns the cube slice by customer ID "A", the same as cube["A"]
cube.Slice(0, "A");

// Returns the cube slice by year 2007, where 1 means second dimension from beginning
cube.Slice(1, "2007");

// Returns the cube slice by year 2007, where ^1 means first dimension from end
cube.Slice(^1, "2007");
```

To slice by multiple dimensions at once, use overloads of `Slice(...)` that take multiple indexes or pairs of dimension numbers and indexes as a [params](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params) array.

```csharp
// Returns the cube slice by customer ID "A" and year 2007
cube.Slice("A", "2007");
cube.Slice((0, "A"), (1, "2007"));

// Returns the cube slice by year 2007 and customer ID "A"
cube.Slice((1, "2007"), (0, "A"));
```

### Accessing Bound Dimensions

The applied indexes and corresponding _bound_ dimensions are stored in the result of the slice operation and can be retrieved as a collection of pairs with `GetBoundDimensionsAndIndexes()`, or separately by `GetBoundDimension(...)` and `GetBoundIndex(...)`.

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Cube slice by customer ID "A"
var sliceByCustomerIdA = cube["A"];
sliceByCustomerIdA.GetBoundDimensionsAndIndexes(); // new[] { (customerIdDimension, "A") }
sliceByCustomerIdA.GetBoundDimension(0); // customerIdDimension
sliceByCustomerIdA.GetBoundIndex(0); // "a"

// Cube slice by year 2007, where 1 means second dimension from beginning
var sliceByYear2007 = cube.Slice(1, "2007");
sliceByYear2007.GetBoundDimensionsAndIndexes(); // new[] { (yearDimension, "2007") }
sliceByYear2007.GetBoundDimension(0); // yearDimension
sliceByYear2007.GetBoundIndex(0); // "2007"

// Cube slice by customer ID "A" and year 2007
var sliceByCustomerIdAAndYear2007 = cube.Slice("A", "2007");
sliceByCustomerIdAAndYear2007.GetBoundDimensionsAndIndexes(); // new[] { (customerIdDimension, "A"), (yearDimension, "2007") }
sliceByCustomerIdAAndYear2007.GetBoundDimension(0); // customerIdDimension
sliceByCustomerIdAAndYear2007.GetBoundDimension(1); // yearDimension

// Cube slice by customer ID "A" and year 2007
var sliceByYear2007AndCustomerIdA = cube.Slice((1, "2007"), (0, "A"));
sliceByYear2007AndCustomerIdA.GetBoundDimensionsAndIndexes(); // new[] { (yearDimension, "2007"), (customerIdDimension, "A") }
sliceByYear2007AndCustomerIdA.GetBoundDimension(0); // yearDimension
sliceByYear2007AndCustomerIdA.GetBoundDimension(1); // customerIdDimension
```

Note that the result of `Slice(...)` is a cube with fewer free dimensions, and dimension numbers may shift:

```csharp
// Cube slice by customer ID "A", the same as cube["A"]
cube.Slice(0, "A");

// Slice by dimension number 0, not 1 because of shifting of dimension numbers
sliceByCustomerIdA.Slice(0, "2007");
```

---

## Breakdown Operations

The _breakdown_ operation creates a collection of slices by all indexes of one or more dimensions.

Returning to the initial example, each report row corresponds to a cube slice by a specific customer ID. Since all customer IDs of interest are defined in the dimension definition, you can enumerate them. The breakdown operation combines enumerating dimension indexes and slicing the cube by each.

_Breakdown_ is implemented in the `BreakdownByDimensions(...)` extension methods of `CubeResult`. These methods take one or more free dimension numbers as a [params](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params) array, or a [Range](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/ranges). The result is a collection of cube slices (instances of `CubeResult`), one per index definition (including totals). When multiple dimensions are specified, the result is a collection of slices for all combinations of index definitions.

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Returns collection of all slices by customer IDs
cube.BreakdownByDimensions(0);

// Returns collection of all slices by years
cube.BreakdownByDimensions(1);
// or
cube.BreakdownByDimensions(^1);

// Returns collection of all slices by customer IDs and years
cube.BreakdownByDimensions(0, 1);
// or
cube.BreakdownByDimensions(0..);
// or
cube.BreakdownByDimensions(..^0);
// or
cube.BreakdownByDimensions(..);

// Returns collection of all slices by years and customer IDs
cube.BreakdownByDimensions(1, 0);
```

---

## Generic Cube Operations

Returning to the initial example in [Getting Started](#getting-started), where we created a tabular report from the cube:

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);
```

Let's revisit the part that builds the report from the cube:

```csharp
cube
    .BreakdownByDimensions(..^1) // ..^1 - build rows by range of all dimensions except last
    .Select(row => row
        .GetBoundDimensionsAndIndexes()
        .Select(dimensionAndIndex => KeyValuePair.Create(
            dimensionAndIndex.dimension.Title!,
            (object?)dimensionAndIndex.dimension[dimensionAndIndex.index].Title))
        .Concat(row
            .BreakdownByDimensions(^1) // ^1 - first from end
            .Select(column => KeyValuePair.Create(
                column.GetBoundIndexDefinition(^1).Title!,
                (object?)column.GetValue())))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```

Notice that this code is generic: it does not depend on any specific cube or definitions. It builds a tabular report where rows correspond to all dimensions except the last, and columns correspond to the last dimension. This is possible because `CubeResult` contains all the necessary information to create such a report. This demonstrates that you can create generic transformations of any cube into a collection, table, or tree.

### Single Table Row Example

For example, to build a single table row as a dictionary (keys are indexes in dimension `0`, values are aggregated values):

```csharp
cube
    .BreakdownByDimensions(0) // use dimension 0 (customerId) for columns
    .Select(column => KeyValuePair.Create( // create KeyValuePair instance for each column
        column.GetBoundIndexDefinition(^1).Title, // use title of last bound index for key
        column.GetValue())) // use value for dictionary value
    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
```

| key | value  |
|------------|-----|
| Customer A | 72  |
| Customer B | 47  |
| Customer C | 56  |
| Customer D | 60  |
| Total      | 235 |

### Reusable Generic Methods

You can generalize this snippet into a reusable method by extracting `cubeResult` and `columnDimensionNumber` as parameters:

```csharp
IDictionary<string, object?> GetTableBodyColumns<TIndex, T>(
    CubeResult<TIndex, T> cubeResult, Index columnDimensionNumber)
    where TIndex : notnull =>
cubeResult
    .BreakdownByDimensions(columnDimensionNumber)
    .Select(column => KeyValuePair.Create(
        column.GetBoundIndexDefinition(^1).Title!,
        (object?)column.GetValue()))
    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

// Example of call
GetTableColumns(cube, 0);
```

Now, let's outline the rows of the report. Each row corresponds to an index in dimension `0` and is represented by a single-entry dictionary with the aggregated value:

```csharp
cube
    .BreakdownByDimensions(0)
    .Select(row =>
        new[] { KeyValuePair.Create("Value", (object)row.GetValue()) }
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```

| Value |
|-----|
| 72  |
| 47  |
| 56  |
| 60  |
| 235 |

`BreakdownByDimensions(...)` can take multiple dimension numbers (as an array or [range](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/ranges)), so you can create a table by multiple dimensions as rows:

```csharp
// Creates collection of total values for all combinations of customerID and year (including totals)
cube
    .BreakdownByDimensions(..) // build rows by all dimensions
    .Select(row => new[] { KeyValuePair.Create("Value", (object)row.GetValue()) }
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```

To add header columns, use `CubeResult.GetBoundDimensionsAndIndexes(...)`, which returns pairs of bound dimension and index. Each pair can be transformed into a `KeyValuePair<string, object>` with the dimension title as the key and the index title as the value:

```csharp
cube
    .BreakdownByDimensions(0)
    .Select(row => row
        .GetBoundDimensionsAndIndexes() // get collection of pairs of bound dimension and index
        .Select(dimensionAndIndex => KeyValuePair.Create( // create header column
            dimensionAndIndex.dimension.Title!, // use dimension title for key
            (object?)dimensionAndIndex.dimension[dimensionAndIndex.index].Title)) // use index title for value
        .Concat(new[] { KeyValuePair.Create("Value", (object?)row.GetValue()) })
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```

| Customers  | Value |
|------------|-------|
| Customer A | 72    |
| Customer B | 47    |
| Customer C | 56    |
| Customer D | 60    |
| Total      | 235   |

### Complete Generic Table Method

You can generalize this code into a reusable method by extracting parameters for the cube result, row dimensions, and functions to get header and body columns:

```csharp
IEnumerable<IDictionary<string, object>> GetTable<TIndex, T>(
    CubeResult<TIndex, T> cubeResult,
    Range rowDimensions,
    Func<CubeResult<TIndex, T>, IDictionary<string, object?>> getHeaderColumns,
    Func<CubeResult<TIndex, T>, IDictionary<string, object?>> getBodyColumns)
    where TIndex : notnull =>
    from row in cubeResult.BreakdownByDimensions(rowDimensions)
    select getHeaderColumns(row)
        .Concat(getBodyColumns(row)) // get body columns
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

IDictionary<string, object?> GetTableHeaderColumns<TIndex, T>(CubeResult<TIndex, T> row)
    where TIndex : notnull =>
    row.GetBoundDimensionsAndIndexes()
        .Select(dimensionAndIndex => KeyValuePair.Create(
            dimensionAndIndex.dimension.Title!,
            (object?)dimensionAndIndex.dimension[dimensionAndIndex.index].Title))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

// Example of call
GetTable(
    cube,
    ..^1, // ..^1 - build rows by range of all dimensions except last
    GetTableHeaderColumns,
    row => new Dictionary<string, object?> { ["Value"] = (object)row.GetValue() });
```

Now, put everything together by calling the extracted `GetTable(...)` method, using `GetTableColumns(...)` to generate body columns:

```csharp
GetTable(
    cube,
    ..^1, // ..^1 - build rows by range of all dimensions except last
    GetTableHeaderColumns,
    row => GetTableBodyColumns(row, ^1)); // ^1 - build columns by last dimension
```

| Customers  | 2007 Year | 2008 Year | 2009 Year | Total |
|------------|-----------|-----------|-----------|-------|
| Customer A | 22        | 40        | 10        | 72    |
| Customer B | 20        | 12        | 15        | 47    |
| Customer C | 22        | 14        | 20        | 56    |
| Customer D | 0         | 0         | 60        | 60    |
| Total      | 64        | 66        | 105       | 235   |

If you inline the extracted generic methods, you'll get code similar to the initial example.

As a result, you can build the desired report and create reusable, generic methods for similar reports.

---

## API Reference

### Core Classes

#### CubeResult&lt;TIndex, T&gt;

Represents the result of data cube generation and provides methods for querying cube data.

**Key Properties:**
- `FreeDimensionCount`: Number of free (unbound) dimensions
- `BoundDimensionCount`: Number of bound dimensions

**Key Methods:**
- `GetValue(params TIndex?[] indexes)`: Gets aggregated value for specific cell
- `Slice(Index dimensionNumber, TIndex? index)`: Creates slice by binding dimension
- `GetFreeDimension(Index number)`: Gets free dimension by number
- `GetBoundDimension(Index number)`: Gets bound dimension by number
- `AsDictionary()`: Converts cube to dictionary representation

#### AggregationDefinition&lt;TSource, T&gt;

Defines how to aggregate source data into cube cells.

**Factory Methods:**
- `AggregationDefinition.Create<TSource, T>()`
- `AggregationDefinition.CreateForCollection<TSource, T>()`
- `AggregationDefinition.CreateForDictionaryCollection<T>()`

#### DimensionDefinition&lt;TSource, TIndex&gt;

Defines a cube dimension including index selector and available index values.

**Factory Methods:**
- `DimensionDefinition.Create<TSource, TIndex>()`
- `DimensionDefinition.CreateWithMultiSelector<TSource, TIndex>()`
- `DimensionDefinition.CreateDefault<TSource, TIndex>()`
- `DimensionDefinition.CreateForCollection<TSource, TIndex>()`
- `DimensionDefinition.CreateForDictionaryCollection<TIndex>()`

**Extension Methods:**
- `WithLeadingDefaultIndex(string title)`: Adds total index at beginning
- `WithTrailingDefaultIndex(string title)`: Adds total index at end

#### IndexDefinition&lt;TIndex&gt;

Defines an index value within a dimension, supporting hierarchical structures.

**Factory Methods:**
- `IndexDefinition.Create(TIndex? value, string? title, params IndexDefinition<TIndex>[] children)`

### Extension Methods

#### CubeBuilder Extensions

- `BuildCube<TSource, TIndex, T>()`: Builds cube from enumerable
- `BuildCubeAsync<TSource, TIndex, T>()`: Builds cube from async enumerable

#### CubeResult Extensions

- `BreakdownByDimensions(params Index[] dimensionNumbers)`: Creates collection of slices
- `BreakdownByDimensions(Range dimensionRange)`: Creates collection of slices using range
- `GetBoundIndexDefinition(Index dimensionNumber)`: Gets bound index definition

---

## Examples & Recipes

### Sales Analysis Report

```csharp
public class SalesRecord
{
    public DateTime Date { get; set; }
    public string Product { get; set; }
    public string Region { get; set; }
    public decimal Amount { get; set; }
}

var salesData = GetSalesData();

// Create aggregation for sum of sales amount
var sumAggregation = AggregationDefinition.Create(
    (SalesRecord s) => s.Amount,
    (a, b) => a + b,
    0m);

// Create time dimension (quarters)
var timeDimension = DimensionDefinition.Create(
    (SalesRecord s) => $"Q{(s.Date.Month - 1) / 3 + 1} {s.Date.Year}",
    title: "Quarter",
    IndexDefinition.Create("Q1 2023", "Q1 2023"),
    IndexDefinition.Create("Q2 2023", "Q2 2023"),
    IndexDefinition.Create("Q3 2023", "Q3 2023"),
    IndexDefinition.Create("Q4 2023", "Q4 2023"))
    .WithTrailingDefaultIndex("Total");

// Create product dimension with categories
var productDimension = DimensionDefinition.Create(
    (SalesRecord s) => s.Product,
    title: "Product",
    IndexDefinition.Create("Electronics", "Electronics",
        IndexDefinition.Create("Laptop", "Laptop"),
        IndexDefinition.Create("Phone", "Phone")),
    IndexDefinition.Create("Books", "Books",
        IndexDefinition.Create("Fiction", "Fiction"),
        IndexDefinition.Create("Technical", "Technical")))
    .WithTrailingDefaultIndex("All Products");

// Build and query cube
var cube = salesData.BuildCube(sumAggregation, productDimension, timeDimension);
var totalSales = cube.GetValue(); // Grand total
var electronicsSales = cube.GetValue("Electronics"); // Electronics total across all quarters
var q1LaptopSales = cube.GetValue("Laptop", "Q1 2023"); // Specific product and quarter
```

### Customer Segmentation Analysis

```csharp
public class Customer
{
    public int Id { get; set; }
    public string Segment { get; set; }  // Premium, Standard, Basic
    public string[] Interests { get; set; }  // Multiple interests per customer
    public decimal LifetimeValue { get; set; }
}

var customers = GetCustomers();

// Count customers
var countAggregation = AggregationDefinition.Create(
    (Customer c) => 1,
    (a, b) => a + b,
    0);

// Segment dimension
var segmentDimension = DimensionDefinition.Create(
    (Customer c) => c.Segment,
    title: "Segment",
    IndexDefinition.Create("Premium", "Premium"),
    IndexDefinition.Create("Standard", "Standard"),
    IndexDefinition.Create("Basic", "Basic"))
    .WithTrailingDefaultIndex("All Segments");

// Multi-select interests dimension
var interestDimension = DimensionDefinition.CreateWithMultiSelector(
    (Customer c) => c.Interests.Cast<string?>(),
    title: "Interests",
    IndexDefinition.Create("Sports", "Sports"),
    IndexDefinition.Create("Technology", "Technology"),
    IndexDefinition.Create("Travel", "Travel"))
    .WithTrailingDefaultIndex("All Interests");

var cube = customers.BuildCube(countAggregation, segmentDimension, interestDimension);

// Analysis
var premiumSportsCustomers = cube.GetValue("Premium", "Sports");
var totalCustomersByInterest = cube.BreakdownByDimensions(1)
    .Select(slice => new {
        Interest = slice.GetBoundIndex(0),
        Count = slice.GetValue()
    });
```

### Time Series Analysis

```csharp
public class MetricRecord
{
    public DateTime Timestamp { get; set; }
    public string MetricName { get; set; }
    public double Value { get; set; }
}

// Create time-based dimensions at different granularities
var hourlyDimension = DimensionDefinition.Create(
    (MetricRecord m) => m.Timestamp.ToString("yyyy-MM-dd HH:00"),
    title: "Hour");

var dailyDimension = DimensionDefinition.Create(
    (MetricRecord m) => m.Timestamp.ToString("yyyy-MM-dd"),
    title: "Day");

var metricDimension = DimensionDefinition.Create(
    (MetricRecord m) => m.MetricName,
    title: "Metric",
    IndexDefinition.Create("CPU", "CPU Usage"),
    IndexDefinition.Create("Memory", "Memory Usage"),
    IndexDefinition.Create("Disk", "Disk Usage"));

// Average aggregation
var avgAggregation = AggregationDefinition.Create(
    (MetricRecord m) => new { Sum = m.Value, Count = 1 },
    (a, b) => new { Sum = a.Sum + b.Sum, Count = a.Count + b.Count },
    new { Sum = 0.0, Count = 0 });

var cube = GetMetricData().BuildCube(avgAggregation, metricDimension, dailyDimension);

// Calculate daily averages
var dailyAverages = cube["CPU"]
    .BreakdownByDimensions(0)
    .Select(slice => new {
        Date = slice.GetBoundIndex(0),
        Average = slice.GetValue().Count > 0 ? slice.GetValue().Sum / slice.GetValue().Count : 0
    });
```

---

## Performance Guide

### Memory Considerations

- **In-memory processing**: CubeSharp loads all data into memory for processing
- **Dimension cardinality**: High-cardinality dimensions increase memory usage exponentially
- **Sparse vs dense cubes**: Cubes with many empty cells are handled efficiently

### Optimization Tips

1. **Limit dimension cardinality**: Keep the number of unique values per dimension reasonable
2. **Use appropriate data types**: Smaller index types (`int` vs `string`) can reduce memory usage
3. **Consider hierarchical dimensions**: Use parent-child relationships to reduce flat dimension size
4. **Batch processing**: For large datasets, consider processing in batches
5. **Async processing**: Use `BuildCubeAsync` for non-blocking cube construction

### Example: Optimized Large Dataset Processing

```csharp
// Instead of processing millions of individual timestamps
var inefficientTimeDimension = DimensionDefinition.Create(
    (Record r) => r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), // High cardinality
    title: "Time");

// Group by hour or day
var optimizedTimeDimension = DimensionDefinition.Create(
    (Record r) => r.Timestamp.ToString("yyyy-MM-dd HH:00"), // Lower cardinality
    title: "Hour");

// Or use hierarchical time dimensions
var hierarchicalTimeDimension = DimensionDefinition.Create(
    (Record r) => r.Timestamp.ToString("yyyy-MM-dd"),
    title: "Date",
    IndexDefinition.Create("2023-01-01", "January 1",
        IndexDefinition.Create("2023-01-01 09:00", "9 AM"),
        IndexDefinition.Create("2023-01-01 10:00", "10 AM")));
```

### Monitoring Performance

```csharp
var stopwatch = Stopwatch.StartNew();
var cube = largeDataset.BuildCube(aggregation, dimension1, dimension2);
stopwatch.Stop();

Console.WriteLine($"Cube built in {stopwatch.ElapsedMilliseconds}ms");
Console.WriteLine($"Memory usage: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
```

This comprehensive wiki documentation provides a complete reference for the CubeSharp library while preserving all the original code examples from CubeSharp.md and expanding with additional practical examples and guidance.