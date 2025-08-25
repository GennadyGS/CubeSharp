- [Introduction](#introduction)
- [Motivation](#motivation)
- [Getting Started](#getting-started)
  - [Sample Data](#sample-data)
  - [The Goal](#the-goal)
  - [Building the Report](#building-the-report)
- [Building Data Cubes](#building-data-cubes)
  - [The BuildCube Method](#the-buildcube-method)
    - [Generic Type Parameters](#generic-type-parameters)
    - [Working with Different Index Types](#working-with-different-index-types)
    - [Important Notes](#important-notes)
  - [Defining Aggregations](#defining-aggregations)
    - [Basic Aggregation Creation](#basic-aggregation-creation)
    - [Common Aggregation Patterns](#common-aggregation-patterns)
    - [Composite Aggregations](#composite-aggregations)
    - [Type Inference Helpers](#type-inference-helpers)
  - [Defining Dimensions](#defining-dimensions)
    - [Basic Dimension Creation](#basic-dimension-creation)
    - [Complex Selectors](#complex-selectors)
    - [Hierarchical Dimensions](#hierarchical-dimensions)
    - [Totals and Default Indices](#totals-and-default-indices)
    - [Working with Dictionary Data](#working-with-dictionary-data)
    - [Type Inference Helpers](#type-inference-helpers-1)
    - [Multi-selection](#multi-selection)
- [Querying Data Cubes](#querying-data-cubes)
  - [Direct Cell Access](#direct-cell-access)
    - [Working with Missing Values](#working-with-missing-values)
  - [Slicing Operations](#slicing-operations)
    - [Using Indexer Syntax](#using-indexer-syntax)
    - [Using Slice Method](#using-slice-method)
    - [Slice Information](#slice-information)
    - [Important Notes](#important-notes-1)
  - [Analysis Operations](#analysis-operations)
    - [Breaking Down Data](#breaking-down-data)
    - [Creating Reports](#creating-reports)
    - [Advanced Analysis Patterns](#advanced-analysis-patterns)

# Introduction

CubeSharp is a high-performance .NET library for building and analyzing in-memory [data cubes](https://en.wikipedia.org/wiki/Data_cube). It provides a flexible and type-safe way to perform multi-dimensional data analysis, aggregations, and reporting in your .NET applications.

Key features:

- Strong type safety with generics
- Support for hierarchical dimensions
- Flexible aggregation definitions
- Efficient memory usage
- LINQ-style querying
- Async support for large datasets
- Built-in support for common reporting scenarios

# Motivation

When building business reports or analytics features, you often need to analyze data across multiple dimensions (like time, geography, product categories) while calculating various metrics (like sales, counts, averages). Some common challenges include:

- Building reports with dynamic row/column combinations
- Calculating subtotals and grand totals
- Handling hierarchical data (e.g., product categories)
- Supporting drill-down capabilities
- Managing complex aggregations

Traditional approaches have limitations:

1. **SQL**: While powerful, complex multi-dimensional queries can be:
    - Hard to maintain and refactor
    - Difficult to test
    - Limited in reusability
    - Challenging to version control

2. **Raw LINQ**: Direct LINQ operations often lead to:
    - Repetitive filtering and grouping code
    - Complex aggregation logic
    - Poor performance with multiple dimensions
    - Hard to maintain ad-hoc solutions

CubeSharp solves these challenges by:

- Providing a systematic approach to multi-dimensional analysis
- Enabling clean separation of dimension and aggregation definitions
- Supporting composable and reusable components
- Optimizing memory usage and performance
- Offering a fluent API for transforming data into desired representations

# Getting Started

Let's walk through a practical example to see how CubeSharp works. We'll build a sales report showing totals by customer and year.

## Sample Data

Consider a collection of order records:

```csharp
var orders = new[] {
    new {
        OrderDate = new DateTime(2007, 08, 02),
        Product = "X",
        CustomerId = "A",
        Quantity = 10m
    },
    new {
        OrderDate = new DateTime(2007, 12, 24),
        Product = "Y",
        CustomerId = "B",
        Quantity = 12m
    },
    // ... more orders ...
};
```

## The Goal

We want to create a report showing:
- Customers as rows
- Years as columns
- Total quantity for each customer/year combination
- Row and column totals

The desired output should look like this:

| Customers  | 2007 Year | 2008 Year | 2009 Year | Total |
|------------|-----------|-----------|-----------|-------|
| Customer A | 22        | 40        | 10        | 72    |
| Customer B | 20        | 12        | 15        | 47    |
| Customer C | 22        | 14        | 20        | 56    |
| Customer D | 0         | 0         | 60        | 60    |
| Total      | 64        | 66        | 105       | 235   |

## Building the Report

Here's how to create this report using CubeSharp:

```csharp
// 1. Define how to aggregate the data
var aggregationDefinition = AggregationDefinition.CreateForCollection(
    orders,
    order => order.Quantity,    // Select the field to aggregate
    (a, b) => a + b,           // How to combine values (sum)
    seedValue: 0);             // Default value for empty cells

// 2. Define the customer dimension (rows)
var customerDimension = DimensionDefinition.CreateForCollection(
    orders,
    order => order.CustomerId,  // Field to group by
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"),
    IndexDefinition.Create("C", "Customer C"),
    IndexDefinition.Create("D", "Customer D"))
    .WithTrailingDefaultIndex("Total");  // Add a total row

// 3. Define the year dimension (columns)
var yearDimension = DimensionDefinition.CreateForCollection(
    orders,
    order => order.OrderDate.Year.ToString(),
    title: "Years",
    IndexDefinition.Create("2007", "2007 Year"),
    IndexDefinition.Create("2008", "2008 Year"),
    IndexDefinition.Create("2009", "2009 Year"))
    .WithTrailingDefaultIndex("Total");  // Add a total column

// 4. Build the cube
var cube = orders.BuildCube(
    aggregationDefinition,
    customerDimension,    // First dimension (rows)
    yearDimension);      // Second dimension (columns)

// 5. Transform the cube into a table format
var report = cube
    .BreakdownByDimensions(..^1)  // Break down by all dimensions except last
    .Select(row => row
        // Create header columns from dimension info
        .GetBoundDimensionsAndIndexes()
        .Select(pair => KeyValuePair.Create(
            pair.dimension.Title!,
            (object?)pair.dimension[pair.index].Title))
        // Add value columns
        .Concat(row
            .BreakdownByDimensions(^1)  // Break down by last dimension
            .Select(cell => KeyValuePair.Create(
                cell.GetBoundIndexDefinition(^1).Title!,
                (object?)cell.GetValue())))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```
For now it is enough to understand how requirements are reflected in this code, all details will be explained in following sections.
Result will be collection of dictionaries, which in table form equivalent to desired report.

# Building Data Cubes

The core functionality of CubeSharp revolves around building multi-dimensional data cubes. A cube combines:

1. Source data (any collection)
2. One or more dimensions to analyze the data by
3. An aggregation function to calculate values

## The BuildCube Method

The main entry point is the `BuildCube<TSource, TIndex, T>()` extension method:

```csharp
// Basic cube with one dimension
var cube1D = orders.BuildCube(
    aggregationDefinition,
    customerDimension);

// Two-dimensional cube
var cube2D = orders.BuildCube(
    aggregationDefinition,
    customerDimension,
    yearDimension);

// Three-dimensional cube
var cube3D = orders.BuildCube(
    aggregationDefinition,
    customerDimension,
    yearDimension,
    productDimension);
```

### Generic Type Parameters

The method has three generic type parameters:

1. `TSource` - Type of items in your source collection
2. `TIndex` - Type of the dimension indices (must be the same for all dimensions)
3. `T` - Type of the aggregated values in cube cells

These are usually inferred automatically, but can be specified explicitly when needed:

```csharp
// Explicitly specifying type parameters
var cube = Enumerable.Range(1, 10)
    .BuildCube<int, int, int>(
        AggregationDefinition.Create(
            i => i,           // Value selector
            (a, b) => a + b,  // Aggregation function
            0));              // Seed value
```

### Working with Different Index Types

When your dimensions naturally use different types (e.g., strings for customers, integers for years), you need to convert them to a common type. String is often a good choice:

```csharp
var yearDimension = DimensionDefinition.Create(
    order => order.OrderDate.Year.ToString(), // Convert int to string
    title: "Years",
    IndexDefinition.Create("2007", "2007 Year"),
    IndexDefinition.Create("2008", "2008 Year"));
```

### Important Notes

1. The order of dimension arguments is preserved in the resulting cube
2. All dimensions are treated equally by `BuildCube`
3. You can build a cube with zero dimensions (just aggregation)
4. For large datasets, use `BuildCubeAsync` with async collections:

```csharp
var cube = await asyncOrders.BuildCubeAsync(
    aggregationDefinition,
    customerDimension,
    yearDimension);
```

## Defining Aggregations

Aggregations (also called measures) define how to calculate values in cube cells. They specify:
- Which values to extract from your data
- How to combine those values
- What default value to use for empty cells

### Basic Aggregation Creation

The simplest way to create an aggregation is with `AggregationDefinition.Create`:

```csharp
var sumQuantity = AggregationDefinition.Create(
    valueSelector: order => order.Quantity,    // What to aggregate
    aggregator: (a, b) => a + b,              // How to combine values
    seedValue: 0);                            // Default/empty cell value
```

### Common Aggregation Patterns

Here are some typical aggregation patterns:

```csharp
// Sum of values
var sum = AggregationDefinition.Create(
    x => x.Value,
    (a, b) => a + b,
    0);

// Count of records
var count = AggregationDefinition.Create(
    x => 1,                 // Count each record as 1
    (a, b) => a + b,
    0);

// Minimum value
var min = AggregationDefinition.Create(
    x => x.Value,
    Math.Min,
    int.MaxValue);         // Start with highest possible value

// Maximum value
var max = AggregationDefinition.Create(
    x => x.Value,
    Math.Max,
    int.MinValue);         // Start with lowest possible value

// Average (using composite value)
var average = AggregationDefinition.Create(
    x => (sum: x.Value, count: 1),
    (a, b) => (a.sum + b.sum, a.count + b.count),
    (0, 0));

// Collection of values
var collect = AggregationDefinition.Create(
    x => new[] { x.Value },
    (a, b) => a.Concat(b).ToArray(),
    Array.Empty<int>());
```

### Composite Aggregations

You can combine multiple aggregations into one by using a custom type:

```csharp
public readonly record struct OrderMetrics
{
    public OrderMetrics(int count, decimal total, decimal min, decimal max)
    {
        Count = count;
        Total = total;
        Min = min;
        Max = max;
    }

    public int Count { get; }
    public decimal Total { get; }
    public decimal Min { get; }
    public decimal Max { get; }

    public static OrderMetrics Zero => new(0, 0, decimal.MaxValue, decimal.MinValue);

    public static OrderMetrics Combine(OrderMetrics a, OrderMetrics b) => new(
        count: a.Count + b.Count,
        total: a.Total + b.Total,
        min: Math.Min(a.Min, b.Min),
        max: Math.Max(a.Max, b.Max));
}

// Use the composite type in an aggregation
var orderMetrics = AggregationDefinition.Create(
    order => new OrderMetrics(1, order.Amount, order.Amount, order.Amount),
    OrderMetrics.Combine,
    OrderMetrics.Zero);
```

### Type Inference Helpers

CubeSharp provides helper methods for common scenarios:

```csharp
// For strongly-typed collections
var typedSum = AggregationDefinition.CreateForCollection(
    orders,                    // Collection for type inference
    order => order.Quantity,
    (a, b) => a + b,
    0);

// For dictionary-based data
var dictSum = AggregationDefinition.CreateForDictionaryCollection(
    dict => (decimal)dict["Quantity"],
    (a, b) => a + b,
    0);
```

The aggregation definition is a key part of building your cube - it determines what values will be stored in each cell and how they'll be combined when grouping by your chosen dimensions.

## Defining Dimensions

Dimensions define how to categorize and group your data. Each dimension:
- Specifies which field(s) to use from your data
- Defines the valid values (indices) for grouping
- Can include display titles for reporting
- May have hierarchical relationships

### Basic Dimension Creation

Here's a simple dimension definition:

```csharp
var customerDimension = DimensionDefinition.Create(
    selector: order => order.CustomerId,     // Field to group by
    title: "Customers",                      // Display name
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"),
    IndexDefinition.Create("C", "Customer C"));
```

### Complex Selectors

You can use any expression as a selector:

```csharp
// Group by year quarter
var quarterDimension = DimensionDefinition.Create(
    order => $"Q{(order.OrderDate.Month - 1) / 3 + 1}",
    title: "Quarters",
    IndexDefinition.Create("Q1", "Q1 2023"),
    IndexDefinition.Create("Q2", "Q2 2023"),
    IndexDefinition.Create("Q3", "Q3 2023"),
    IndexDefinition.Create("Q4", "Q4 2023"));

// Group by price range
var priceDimension = DimensionDefinition.Create(
    order => order.Price switch {
        <= 50 => "low",
        <= 200 => "medium",
        _ => "high"
    },
    title: "Price Range",
    IndexDefinition.Create("low", "Low (≤$50)"),
    IndexDefinition.Create("medium", "Medium ($51-$200)"),
    IndexDefinition.Create("high", "High (>$200)"));
```

### Hierarchical Dimensions

You can create hierarchical groupings using nested index definitions:

```csharp
var productDimension = DimensionDefinition.Create(
    order => order.ProductId,
    title: "Products",
    // Category A and its products
    IndexDefinition.Create("A", "Category A",
        IndexDefinition.Create("A1", "Product A1"),
        IndexDefinition.Create("A2", "Product A2")),
    // Category B and its products
    IndexDefinition.Create("B", "Category B",
        IndexDefinition.Create("B1", "Product B1"),
        IndexDefinition.Create("B2", "Product B2")));
```

The order of parent/child indices affects the display order in reports:

```csharp
// Children before parent
var dimensionWithChildrenFirst = DimensionDefinition.Create(
    new[] {
        IndexDefinition.Create("A1", "Product A1"),
        IndexDefinition.Create("A2", "Product A2")
    },
    "A",
    title: "Category A");
```

### Totals and Default Indices

CubeSharp uses `default` values to represent totals. You can:
1. Add a total row/column at the start
2. Add a total row/column at the end
3. Create dimension with only totals

```csharp
// 1. Total at the start
var dimensionWithLeadingTotal = dimension.WithLeadingDefaultIndex("Grand Total");

// 2. Total at the end (most common)
var dimensionWithTrailingTotal = dimension.WithTrailingDefaultIndex("Total");

// 3. Dimension with only total
var totalOnlyDimension = DimensionDefinition.CreateDefault<string, string>(
    title: "All Data",
    indexTitle: "Grand Total");
```

> **Note:** For numeric indices, consider using nullable types (`int?`) to avoid conflicts with `default(int)` being `0`.

### Working with Dictionary Data

For dictionary-based data sources:

```csharp
var dictDimension = DimensionDefinition.CreateForDictionaryCollection(
    dict => (string?)dict["region"],
    title: "Regions",
    IndexDefinition.Create("EMEA", "Europe & Middle East"),
    IndexDefinition.Create("APAC", "Asia Pacific"),
    IndexDefinition.Create("AMER", "Americas"));
```

### Type Inference Helpers

For better type inference with anonymous types:

```csharp
var typedDimension = DimensionDefinition.CreateForCollection(
    orders,  // Collection for type inference
    order => order.Region,
    title: "Regions",
    IndexDefinition.Create("EMEA", "Europe & Middle East"),
    IndexDefinition.Create("APAC", "Asia Pacific"),
    IndexDefinition.Create("AMER", "Americas"));
```

Dimensions form the structure of your data cube - they determine how data is grouped and aggregated, and how you can slice and analyze the results.

### Multi-selection

Sometimes target entity has collection of attributes (common example is informational tags) or connected to other entity with one-to-many or many-to-many relation.
For such cases there is method `DimensionDefinition.CreateWithMultiSelector(...)`, which takes index selector selecting multiple index values in the form of `Func<TSource, IEnumerable<TIndex>>`.
There are also modifications of this method similar to described above: `CreateForDictionaryCollectionWithMultiSelector(..)` and `CreateForCollectionWithMultiSelector(...)`.
For example:

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

# Querying Data Cubes

CubeSharp provides several ways to query and analyze your data cubes:
1. Direct value access
2. Slicing (filtering by dimension)
3. Breakdown analysis (grouping)
4. Generic operations

## Direct Cell Access

The simplest way to query a cube is using `GetValue()`:

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerDimension, yearDimension);

// Get value for specific customer and year
var sales2007A = cube.GetValue("A", "2007");

// Get total for a year across all customers
var sales2007Total = cube.GetValue(default, "2007");

// Get total for a customer across all years
var salesCustomerA = cube.GetValue("A", default);

// Get grand total
var grandTotal = cube.GetValue();  // Same as cube.GetValue(default, default)
```

### Working with Missing Values

When querying values that don't exist in your dimension definitions, you'll get the `seedValue` from your aggregation:

```csharp
// Returns 0 (seedValue) - customer "Z" isn't in the dimension
cube.GetValue("Z", "2007");

// Returns 0 (seedValue) - year 1999 isn't in the dimension
cube.GetValue("A", "1999");
```

## Slicing Operations

Slicing lets you "fix" one or more dimensions to specific values, creating a subcube. There are several ways to slice:

### Using Indexer Syntax

```csharp
// These are equivalent:
cube.GetValue("A", "2007");
cube["A"].GetValue("2007");
cube["A"]["2007"].GetValue();

// Useful for multiple operations on a slice
var customerASlice = cube["A"];
var sales2007 = customerASlice.GetValue("2007");
var sales2008 = customerASlice.GetValue("2008");
```

### Using Slice Method

The more flexible `Slice()` method supports:
- Slicing by dimension number
- Multiple dimensions at once
- Custom ordering

```csharp
// Slice by first dimension
var slice1 = cube.Slice(0, "A");

// Slice by last dimension
var slice2 = cube.Slice(^1, "2007");

// Slice multiple dimensions at once
var slice3 = cube.Slice(("A", "2007"));

// Slice with custom order
var slice4 = cube.Slice(
    (1, "2007"),  // Year first
    (0, "A"));    // Customer second
```

### Slice Information

Slices maintain information about what dimensions and values they represent:

```csharp
var slice = cube["A"];

// Get all bound dimensions and their values
var info = slice.GetBoundDimensionsAndIndexes();
// Returns: [(customerDimension, "A")]

// Get specific dimension
var dimension = slice.GetBoundDimension(0);  // customerDimension

// Get specific value
var value = slice.GetBoundIndex(0);  // "A"
```

### Important Notes

1. Dimension numbers shift after slicing:
   ```csharp
   var slice = cube.Slice(0, "A");     // First dimension
   var value = slice.Slice(0, "2007"); // Now year is first dimension
   ```

2. You can't slice by more dimensions than exist:
   ```csharp
   var dimensions = cube.FreeDimensionCount;  // Check before slicing
   ```

3. Slices are immutable and thread-safe - each operation creates a new cube.

## Analysis Operations

Beyond basic querying and slicing, CubeSharp provides powerful operations for analyzing your data.

### Breaking Down Data

The `BreakdownByDimensions()` method lets you enumerate all values in one or more dimensions:

```csharp
var cube = orders.BuildCube(
    aggregationDefinition,
    customerDimension,    // Dimension 0
    yearDimension);      // Dimension 1

// Get slices for each customer
var customerBreakdown = cube.BreakdownByDimensions(0);

// Get slices for each year
var yearBreakdown = cube.BreakdownByDimensions(^1);  // Last dimension

// Get slices for all customer/year combinations
var fullBreakdown = cube.BreakdownByDimensions(0, 1);

// Alternative ways to specify dimensions
var allDimensions = cube.BreakdownByDimensions(..);     // All dimensions
var allButLast = cube.BreakdownByDimensions(..^1);      // All except last
var firstTwoDims = cube.BreakdownByDimensions(0..2);    // Range of dimensions
```

### Creating Reports

A common use case is transforming cube data into table-like structures. Here's a complete example:

```csharp
public static class CubeReportExtensions
{
    public static IEnumerable<IDictionary<string, object>> ToReport<TIndex, T>(
        this CubeResult<TIndex, T> cube,
        bool includeRowTotals = true,
        bool includeColumnTotals = true)
        where TIndex : notnull
    {
        // Break down by all dimensions except last (rows)
        return cube.BreakdownByDimensions(..^1)
            .Select(row =>
            {
                // Get dimension labels for row headers
                var headers = row.GetBoundDimensionsAndIndexes()
                    .ToDictionary(
                        pair => pair.dimension.Title!,
                        pair => (object?)pair.dimension[pair.index].Title);

                // Get values for each column
                var values = row.BreakdownByDimensions(^1)
                    .ToDictionary(
                        cell => cell.GetBoundIndexDefinition(^1).Title!,
                        cell => (object?)cell.GetValue());

                // Combine headers and values
                return headers.Concat(values)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            });
    }
}
```

Usage example:

```csharp
// Create report
var report = cube.ToReport();

// Print as table
foreach (var row in report)
{
    foreach (var (header, value) in row)
    {
        Console.Write($"{value,10}");
    }
    Console.WriteLine();
}
```

Sample output:

```
Customers  2007 Year 2008 Year 2009 Year    Total
Customer A        22        40        10       72
Customer B        20        12        15       47
Customer C        22        14        20       56
Customer D         0         0        60       60
Total            64        66       105      235
```

### Advanced Analysis Patterns

You can combine operations for complex analysis:

```csharp
// Get year-over-year growth by customer
var yoyGrowth = cube.BreakdownByDimensions(0)  // By customer
    .Select(customer =>
    {
        var yearlyTotals = customer
            .BreakdownByDimensions(^1)    // By year
            .OrderBy(y => y.GetBoundIndex(^1))
            .Select(y => y.GetValue())
            .ToList();

        return new
        {
            Customer = customer.GetBoundIndex(0),
            YoYGrowth = yearlyTotals
                .Skip(1)
                .Zip(yearlyTotals,
                    (current, previous) =>
                        (current - previous) / previous * 100)
                .ToList()
        };
    });

// Find top performers by dimension
var topByYear = cube
    .BreakdownByDimensions(^1)           // By year
    .Select(year => new
    {
        Year = year.GetBoundIndex(^1),
        TopCustomers = year
            .BreakdownByDimensions(0)     // By customer
            .OrderByDescending(c => c.GetValue())
            .Take(3)
            .Select(c => new
            {
                Customer = c.GetBoundIndex(0),
                Value = c.GetValue()
            })
            .ToList()
    });
```

The combination of slicing, breakdown, and LINQ operations makes CubeSharp a powerful tool for data analysis.
