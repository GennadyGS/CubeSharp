- [Introduction](#introduction)
- [Motivation](#motivation)
- [Getting Started](#getting-started)
- [Core Concepts](#core-concepts)
  - [Aggregation Definitions](#aggregation-definitions)
  - [Dimension Definitions](#dimension-definitions)
  - [Index Definitions](#index-definitions)
- [Building Cubes](#building-cubes)
  - [Basic Cube Construction](#basic-cube-construction)
  - [Type Arguments and Inference](#type-arguments-and-inference)
  - [Asynchronous Cube Building](#asynchronous-cube-building)
- [Advanced Dimension Features](#advanced-dimension-features)
  - [Hierarchical Indexes](#hierarchical-indexes)
  - [Default Indexes and Totals](#default-indexes-and-totals)
  - [Multi-selection Dimensions](#multi-selection-dimensions)
- [Querying Cubes](#querying-cubes)
  - [Basic Value Retrieval](#basic-value-retrieval)
  - [Cube Slicing](#cube-slicing)
  - [Breakdown Operations](#breakdown-operations)
- [Advanced Usage Patterns](#advanced-usage-patterns)
  - [Generic Cube Operations](#generic-cube-operations)
  - [Working with Dictionary Collections](#working-with-dictionary-collections)
  - [Cube Metadata and Introspection](#cube-metadata-and-introspection)

# Introduction

CubeSharp is a lightweight .NET library for building in-memory [data cubes](https://en.wikipedia.org/wiki/Data_cube). It provides a systematic approach to multi-dimensional data analysis and aggregation, enabling developers to create flexible, testable, and maintainable reporting solutions.

# Motivation

Traditional approaches to multi-dimensional reporting present several challenges:

**SQL Limitations**: When using SQL for multi-dimensional reports, you have limited possibilities for refactoring and testing your code, making it difficult to maintain complex aggregation logic.

**Ad-hoc LINQ Solutions**: Using native .NET capabilities like LINQ requires writing many ad-hoc data manipulations such as filtering, mapping, and aggregation, which leads to code duplication and maintenance difficulties.

**CubeSharp Solution**: The CubeSharp library systematizes your implementation approach to multi-factor reports by:
- Building structured in-memory data cubes from your source data
- Providing consistent APIs for data aggregation and dimension management  
- Enabling transformation of cubes into various tabular representations
- Supporting complex hierarchical and multi-selection scenarios

# Getting Started

Let's start with a practical example. Imagine you have a collection of order data:

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

You want to create a report with customer IDs as rows, order years as columns, and total quantity in each cell:

| Customers  | 2007 Year | 2008 Year | 2009 Year | Total |
|------------|-----------|-----------|-----------|-------|
| Customer A | 22        | 40        | 10        | 72    |
| Customer B | 20        | 12        | 15        | 47    |
| Customer C | 22        | 14        | 20        | 56    |
| Customer D | 0         | 0         | 60        | 60    |
| Total      | 64        | 66        | 105       | 235   |

Here's how to build this report with CubeSharp:

```csharp
// Define how to aggregate data (sum of quantities)
var aggregationDefinition = AggregationDefinition.CreateForCollection(
    orders,
    order => order.Quantity,
    (a, b) => a + b,
    seedValue: 0);

// Define customer dimension (rows)
var customerIdDimension = DimensionDefinition.CreateForCollection(
    orders,
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"),
    IndexDefinition.Create("C", "Customer C"),
    IndexDefinition.Create("D", "Customer D"))
        .WithTrailingDefaultIndex("Total");

// Define year dimension (columns)
var yearDimension = DimensionDefinition.CreateForCollection(
    orders,
    order => order.OrderDate.Year.ToString(),
    title: "Years",
    IndexDefinition.Create("2007", "2007 Year"),
    IndexDefinition.Create("2008", "2008 Year"),
    IndexDefinition.Create("2009", "2009 Year"))
        .WithTrailingDefaultIndex("Total");

// Build the cube
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Transform to tabular format
var report = cube
    .BreakdownByDimensions(..^1) // All dimensions except the last (rows)
    .Select(row => row
        .GetBoundDimensionsAndIndexes()
        .Select(dimensionAndIndex => KeyValuePair.Create(
            dimensionAndIndex.dimension.Title!,
            (object?)dimensionAndIndex.dimension[dimensionAndIndex.index].Title))
        .Concat(row
            .BreakdownByDimensions(^1) // Last dimension (columns)
            .Select(column => KeyValuePair.Create(
                column.GetBoundIndexDefinition(^1).Title!,
                (object?)column.GetValue())))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```

The result is a collection of dictionaries representing each row of your desired report.

# Core Concepts

## Aggregation Definitions

An **aggregation definition** specifies how to calculate cube cell values from your source data. It consists of three components:

1. **Value Selector**: Extracts the target value from each source record
2. **Aggregation Function**: Defines how to combine multiple values  
3. **Seed Value**: Provides the initial value and default for empty cells

### Creating Aggregation Definitions

The primary method is `AggregationDefinition.Create<TSource, T>()`:

```csharp
/// <summary>
/// Creates an aggregation definition for combining source data values.
/// </summary>
/// <typeparam name="TSource">The source data type</typeparam>
/// <typeparam name="T">The aggregation result type</typeparam>
/// <param name="valueSelector">Expression to extract values from source records</param>
/// <param name="aggregationFunction">Function to combine two values</param>
/// <param name="seedValue">Initial/default value for aggregation</param>
AggregationDefinition.Create(
    order => order.Quantity,           // Extract quantity from each order
    (a, b) => a + b,                  // Sum the values
    seedValue: 0);                    // Start with 0, use 0 for missing data
```

### Common Aggregation Patterns

```csharp
// Sum aggregation
AggregationDefinition.Create(
    (decimal d) => d, 
    (a, b) => a + b, 
    0m);

// Count aggregation  
AggregationDefinition.Create(
    (object item) => 1, 
    (a, b) => a + b, 
    0);

// Minimum aggregation
AggregationDefinition.Create(
    (int i) => i, 
    (a, b) => Math.Min(a, b), 
    int.MaxValue);

// Maximum aggregation
AggregationDefinition.Create(
    (int i) => i, 
    (a, b) => Math.Max(a, b), 
    int.MinValue);

// Collection aggregation
AggregationDefinition.Create(
    (string s) => new[] { s },
    (a, b) => a.Concat(b).ToArray(),
    Array.Empty<string>());
```

### Multiple Value Aggregations

For complex scenarios requiring multiple calculations per cell, create a composite type:

```csharp
public struct CountAndSum
{
    public CountAndSum(int count, decimal sum)
    {
        Count = count;
        Sum = sum;
    }

    public int Count { get; }
    public decimal Sum { get; }
    public decimal Average => Count > 0 ? Sum / Count : 0;

    public static CountAndSum Zero => new(0, 0);
    public static CountAndSum Combine(CountAndSum left, CountAndSum right) =>
        new(left.Count + right.Count, left.Sum + right.Sum);
}

// Use composite aggregation
var aggregation = AggregationDefinition.Create(
    (order) => new CountAndSum(1, order.Quantity),
    CountAndSum.Combine,
    CountAndSum.Zero);
```

## Dimension Definitions

A **dimension definition** specifies how to extract index values from source data and defines which values are relevant for analysis. Think of dimensions as the axes of your data cube.

### Creating Dimension Definitions

```csharp
/// <summary>
/// Creates a dimension definition for organizing data along an axis.
/// </summary>
/// <typeparam name="TSource">The source data type</typeparam>
/// <typeparam name="TIndex">The dimension index type</typeparam>
/// <param name="indexSelector">Expression to extract index values</param>
/// <param name="title">Human-readable dimension title</param>
/// <param name="indexDefinitions">Definitions of valid index values</param>
DimensionDefinition.Create(
    order => order.CustomerId,        // Extract customer ID from orders
    title: "Customers",               // Dimension title for display
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"),
    IndexDefinition.Create("C", "Customer C"));
```

### Advanced Index Selectors

Index selectors can include complex expressions:

```csharp
// Conditional logic
DimensionDefinition.Create(
    order => order.OrderDate.Year % 4 == 0 ? "leap" : "nonLeap",
    title: "Year Types",
    IndexDefinition.Create("leap", "Leap Years"),
    IndexDefinition.Create("nonLeap", "Non-leap Years"));

// Calculated values
DimensionDefinition.Create(
    order => order.OrderDate.DayOfWeek.ToString(),
    title: "Day of Week",
    IndexDefinition.Create("Monday", "Monday"),
    IndexDefinition.Create("Tuesday", "Tuesday"),
    // ... etc
    );

// Range categorization
DimensionDefinition.Create(
    order => order.Quantity switch {
        < 10 => "Small",
        < 100 => "Medium", 
        _ => "Large"
    },
    title: "Order Size",
    IndexDefinition.Create("Small", "Small Orders (< 10)"),
    IndexDefinition.Create("Medium", "Medium Orders (10-99)"),
    IndexDefinition.Create("Large", "Large Orders (100+)"));
```

## Index Definitions

**Index definitions** specify the valid values within a dimension and provide human-readable titles for display purposes.

### Basic Index Definitions

```csharp
/// <summary>
/// Creates an index definition with a value and optional title.
/// </summary>
/// <param name="value">The index value used for matching</param>
/// <param name="title">Human-readable title for display</param>
IndexDefinition.Create("A", "Customer A");
IndexDefinition.Create("2007", "Year 2007");
IndexDefinition.Create(1, "Category 1");
```

### Index Value Uniqueness

Index values must be unique within each dimension:

```csharp
// ? Valid - unique values
DimensionDefinition.Create(
    order => order.Region,
    title: "Regions",
    IndexDefinition.Create("North", "Northern Region"),
    IndexDefinition.Create("South", "Southern Region"),
    IndexDefinition.Create("East", "Eastern Region"));

// ? Invalid - duplicate values
DimensionDefinition.Create(
    order => order.Region,
    title: "Regions", 
    IndexDefinition.Create("North", "Northern Region"),
    IndexDefinition.Create("North", "North America")); // Duplicate!
```

### Explicit Type Specification

When type inference fails (e.g., with zero dimensions), specify types explicitly:

```csharp
var totalOnly = Enumerable.Range(1, 10)
    .BuildCube<int, int, int>(  // Explicit type arguments
        AggregationDefinition.Create(
            (int i) => i, 
            (a, b) => a + b, 
            0));
```

## Asynchronous Cube Building

CubeSharp supports asynchronous data sources through `BuildCubeAsync`:

```csharp
/// <summary>
/// Builds a data cube from an asynchronous source collection.
/// </summary>
public static async Task<CubeResult<TIndex, T>> BuildCubeAsync<TSource, TIndex, T>(
    this IAsyncEnumerable<TSource> source,
    AggregationDefinition<TSource, T> aggregationDefinition,
    params DimensionDefinition<TSource, TIndex>[] dimensionDefinitions)

// Usage example
IAsyncEnumerable<Order> asyncOrders = GetOrdersAsync();
var cube = await asyncOrders.BuildCubeAsync(
    aggregationDefinition,
    customerDimension, 
    yearDimension);
```

This is particularly useful when working with:
- Database queries returning `IAsyncEnumerable<T>`
- Web API responses with streaming data
- Large datasets that benefit from async processing

# Advanced Dimension Features

## Hierarchical Indexes

Index definitions support parent-child relationships, enabling subtotals and drill-down scenarios:

```csharp
/// <summary>
/// Creates hierarchical index definitions with parent-child relationships.
/// Child indexes contribute to their parent's aggregated value.
/// </summary>
IndexDefinition.Create(
    "Electronics",                    // Parent index value
    title: "Electronics Category",   // Parent display title
    IndexDefinition.Create("Phones", "Mobile Phones"),      // Child 1
    IndexDefinition.Create("Tablets", "Tablets"),           // Child 2  
    IndexDefinition.Create("Laptops", "Laptop Computers")); // Child 3
```

### Index Ordering in Hierarchies

By default, child indexes appear after their parent. You can reverse this:

```csharp
// Default ordering: Parent ? Children
IndexDefinition.Create(
    "Electronics", "Electronics Category",
    IndexDefinition.Create("Phones", "Mobile Phones"),
    IndexDefinition.Create("Tablets", "Tablets"));
// Result order: Electronics Category, Mobile Phones, Tablets

// Reversed ordering: Children ? Parent  
IndexDefinition.Create(
    new[] {
        IndexDefinition.Create("Phones", "Mobile Phones"),
        IndexDefinition.Create("Tablets", "Tablets")
    },
    "Electronics", "Electronics Category");
// Result order: Mobile Phones, Tablets, Electronics Category
```

### Multi-level Hierarchies

Hierarchies can have multiple levels:

```csharp
IndexDefinition.Create(
    "AllProducts", "All Products",
    IndexDefinition.Create(
        "Electronics", "Electronics",
        IndexDefinition.Create("Phones", "Mobile Phones"),
        IndexDefinition.Create("Tablets", "Tablets")),
    IndexDefinition.Create(
        "Clothing", "Clothing",
        IndexDefinition.Create("Shirts", "Shirts"),
        IndexDefinition.Create("Pants", "Pants")));
```

## Default Indexes and Totals

**Default indexes** represent totals across an entire dimension and use the `default` value (typically `null` for reference types):

### Understanding Default Indexes

```csharp
/// <summary>
/// Default indexes contain aggregated values for the entire dimension.
/// They use the default value of the index type (null for reference types, 0 for int, etc.)
/// </summary>
var dimension = DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create(
        (string?)default,           // Default index value  
        title: "Total",            // Display title
        IndexDefinition.Create("A", "Customer A"),  // Child indexes
        IndexDefinition.Create("B", "Customer B")));

// No explicit total index definition needed
```

### Constraints on Default Indexes

Default indexes have special rules:

```csharp
// ? Valid - default index as only root with children
DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers", 
    IndexDefinition.Create(
        (string?)default, "Total",
        IndexDefinition.Create("A", "Customer A"),
        IndexDefinition.Create("B", "Customer B")));

// ? Invalid - multiple root indexes including default
DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create((string?)default, "Total"),   // Default root
    IndexDefinition.Create("A", "Customer A"),           // Another root - Error!
    IndexDefinition.Create("B", "Customer B"));
```

### Adding Default Indexes with Extension Methods

CubeSharp provides convenient methods to add default indexes:

```csharp
/// <summary>
/// Adds a leading default index (total appears first in enumeration).
/// </summary>
var dimensionWithLeadingTotal = DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"))
    .WithLeadingDefaultIndex("Total");
// Order: Total, Customer A, Customer B

/// <summary>
/// Adds a trailing default index (total appears last in enumeration).
/// </summary>
var dimensionWithTrailingTotal = DimensionDefinition.Create(
    order => order.CustomerId, 
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"))
    .WithTrailingDefaultIndex("Total");
// Order: Customer A, Customer B, Total
```

### Default Dimensions

Sometimes you need placeholder dimensions with only a total value:

```csharp
/// <summary>
/// Creates a dimension containing only a default index.
/// Useful as placeholders or for maintaining consistent dimension counts.
/// </summary>
var placeholderDimension = DimensionDefinition.CreateDefault<Order, string>(
    title: "All Orders",
    indexTitle: "Total");
```

### Handling Type Conflicts

If `default` conflicts with actual data values, handle it in the index selector:

```csharp
// Problem: Using int indexes where 0 is both default and valid data
DimensionDefinition.Create(
    order => order.Priority == 0 ? (int?)null : order.Priority, // Map 0 to null
    title: "Priorities",
    IndexDefinition.Create((int?)default, "All Priorities"),
    IndexDefinition.Create(1, "High Priority"),
    IndexDefinition.Create(2, "Medium Priority")); // Safe to use 0
```

## Multi-selection Dimensions

**Multi-selection dimensions** handle scenarios where a single source record can belong to multiple index values within the same dimension.

### Understanding Multi-selection

Common scenarios include:
- Products with multiple tags ("bestseller", "discounted", "new")  
- Orders spanning multiple regions
- Articles in multiple categories
- Users with multiple roles

### Creating Multi-selection Dimensions

```csharp
/// <summary>
/// Creates a dimension where each source record can map to multiple index values.
/// The index selector returns IEnumerable<TIndex> instead of TIndex.
/// </summary>
var tagsDimension = DimensionDefinition.CreateWithMultiSelector(
    order => order.Tags,              // Returns IEnumerable<string>
    title: "Tags",
    IndexDefinition.Create(
        (string?)default, "Total",
        IndexDefinition.Create("bestseller", "Bestseller"),
        IndexDefinition.Create("discount", "Discounted"),
        IndexDefinition.Create("new", "New Products")));

// For collections - type inference variant
var tagsDimensionFromCollection = DimensionDefinition.CreateForCollectionWithMultiSelector(
    orders,                           // For type inference
    order => order.Tags,              // Multi-selector
    title: "Tags",
    IndexDefinition.Create("bestseller", "Bestseller"),
    IndexDefinition.Create("discount", "Discounted"));
```

### Multi-selection Example

```csharp
// Source data with multiple tags per order
var orders = new[] {
    new { OrderId = 1, Amount = 100m, Tags = new[] { "bestseller", "discount" } },
    new { OrderId = 2, Amount = 200m, Tags = new[] { "bestseller" } },
    new { OrderId = 3, Amount = 150m, Tags = new[] { "new", "discount" } }
};

var aggregation = AggregationDefinition.CreateForCollection(
    orders,
    order => order.Amount,
    (a, b) => a + b,
    0m);

var tagsDimension = DimensionDefinition.CreateForCollectionWithMultiSelector(
    orders,
    order => order.Tags,
    title: "Tags",
    IndexDefinition.Create("bestseller", "Bestseller"),
    IndexDefinition.Create("discount", "Discounted"),
    IndexDefinition.Create("new", "New Products"))
    .WithTrailingDefaultIndex("Total");

var cube = orders.BuildCube(aggregation, tagsDimension);

// Results:
cube.GetValue("bestseller"); // 300 (Order 1: 100 + Order 2: 200)
cube.GetValue("discount");   // 250 (Order 1: 100 + Order 3: 150)  
cube.GetValue("new");        // 150 (Order 3: 150)
cube.GetValue();            // 450 (Total across all orders)
``

### Multi-selection with Dictionary Collections

```csharp
/// <summary>
/// Multi-selection support for dictionary-based data sources.
/// </summary>
var tagsDimension = DimensionDefinition.CreateForDictionaryCollectionWithMultiSelector(
    dict => ((string[])dict["Tags"]).AsEnumerable(),
    title: "Tags",
    IndexDefinition.Create("bestseller", "Bestseller"),
    IndexDefinition.Create("discount", "Discounted"));
```

# Querying Cubes

## Basic Value Retrieval

The `CubeResult<TIndex, T>` class provides several methods for retrieving aggregated values from your cube:

### GetValue() Methods

```csharp
/// <summary>
/// Gets the aggregated value for the entire cube (total across all dimensions).
/// </summary>
public T GetValue();

/// <summary>
/// Gets the aggregated value by index in the first free dimension.
/// </summary>
/// <param name="index">Index value in the first dimension</param>
public T GetValue(TIndex? index);

/// <summary>
/// Gets the aggregated value by indexes in multiple free dimensions.
/// </summary>
/// <param name="indexes">Index values for each dimension in order</param>
public T GetValue(params TIndex?[] indexes);
```

### Basic Usage Examples

```csharp
var cube = orders.BuildCube(aggregation, customerDimension, yearDimension);

// Total across all customers and years
var grandTotal = cube.GetValue();

// Total for Customer A across all years
var customerATotal = cube.GetValue("A");

// Total for all customers in 2007
var year2007Total = cube.GetValue(default, "2007");

// Specific cell: Customer A in 2007
var specificValue = cube.GetValue("A", "2007");
```

### Using Default Values for Totals

Use `default` (typically `null`) to get totals along specific dimensions:

```csharp
// Customer A, all years
cube.GetValue("A", default);

// All customers, year 2007  
cube.GetValue(default, "2007");

// All customers, all years (same as cube.GetValue())
cube.GetValue(default, default);
```

### Shorthand Notation

You can omit trailing `default` values:

```csharp
// These are equivalent:
cube.GetValue("A", default);
cube.GetValue("A");

// These are equivalent:
cube.GetValue(default, default);
cube.GetValue(default);
cube.GetValue();
```

### Handling Missing Data

When querying with index values not defined in your dimensions, you get the seed value:

```csharp
// Returns seed value (0) since "Z" is not in customer dimension
var unknownCustomer = cube.GetValue("Z", "2007");

// Returns seed value (0) since "1999" is not in year dimension  
var unknownYear = cube.GetValue("A", "1999");
