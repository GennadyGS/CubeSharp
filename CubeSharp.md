- [Introduction](#introduction)
- [Motivation](#motivation)
- [Getting Started](#getting-started)
- [Building Cubes](#building-cubes)
  - [Aggregation Definitions](#aggregation-definitions)
  - [Dimension Definitions](#dimension-definitions)
    - [Multi-selection](#multi-selection)
- [Querying Cubes](#querying-cubes)
  - [Querying Single Cube Cells](#querying-single-cube-cells)
  - [Slicing Cubes](#slicing-cubes)
  - [Breakdown Operations](#breakdown-operations)
  - [Generic Cube Operations](#generic-cube-operations)

# Introduction

CubeSharp is a lightweight library for building in-memory [data cubes](https://en.wikipedia.org/wiki/Data_cube).

# Motivation

Imagine you need to build a tabular report involving multiple factors (or dimensions), data aggregations, calculations, and totals across one or more dimensions.

If you use SQL for this purpose, you have limited possibilities for refactoring and testing your code, making it difficult to maintain. If you use native .NET capabilities like LINQ, you need to write many ad-hoc data manipulations such as filtering, mapping, and aggregation, which also makes code difficult to maintain.

The CubeSharp library allows you to systematize your implementation approach to multi-factor (or multi-dimensional) reports by building in-memory data cubes and then transforming them into the required tabular representation.

# Getting Started

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

You need to build a report with customer IDs as rows and order years as columns, calculating the total quantity for each cell. You also have a fixed list of rows and columns to display, including totals.

The desired report may look like this:

| Customers  | 2007 Year | 2008 Year | 2009 Year | Total |
|------------|-----------|-----------|-----------|-------|
| Customer A | 22        | 40        | 10        | 72    |
| Customer B | 20        | 12        | 15        | 47    |
| Customer C | 22        | 14        | 20        | 56    |
| Customer D | 0         | 0         | 60        | 60    |
| Total      | 64        | 66        | 105       | 235   |

To build such a report with the CubeSharp library, you need to write the following code:

```csharp
// Aggregation definition: calculate sum of Quantity field
var aggregationDefinition = AggregationDefinition.CreateForCollection(
    orders,
    order => order.Quantity,
    (a, b) => a + b,
    seedValue: 0);

// Rows dimension: use CustomerId field, specify list of values with titles to display, 
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

// Columns dimension: use Year property of OrderDate field, 
// specify list of values with titles to display, include total column on the right
var yearDimension = DimensionDefinition.CreateForCollection(
    orders,
    order => order.OrderDate.Year.ToString(),
    title: "Years",
    IndexDefinition.Create("2007", "2007 Year"),
    IndexDefinition.Create("2008", "2008 Year"),
    IndexDefinition.Create("2009", "2009 Year"))
        .WithTrailingDefaultIndex("Total");

// Build cube from source collection using aggregation and dimension definitions
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Transform cube to table in the form of a collection of dictionaries
var report = cube
    .BreakdownByDimensions(..^1) // ..^1 - build rows by all dimensions except the last
    .Select(row => row
        .GetBoundDimensionsAndIndexes()
        .Select(dimensionAndIndex => KeyValuePair.Create(
            dimensionAndIndex.dimension.Title!,
            (object?)dimensionAndIndex.dimension[dimensionAndIndex.index].Title))
        .Concat(row
            .BreakdownByDimensions(^1) // ^1 - build columns by the last dimension
            .Select(column => KeyValuePair.Create(
                column.GetBoundIndexDefinition(^1).Title!,
                (object?)column.GetValue())))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```

For now, it's sufficient to understand how requirements are reflected in this code. All details will be explained in the following sections. The result will be a collection of dictionaries, which in tabular form is equivalent to the desired report.

# Building Cubes

To build a cube, you need to use the generic extension method `BuildCube(...)` on any collection, providing an aggregation definition and any number of dimension definitions (including zero) as a [parameter array](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params).

```csharp
orders.BuildCube(aggregationDefinition, customerIdDimension);

orders.BuildCube(aggregationDefinition, yearDimension, customerIdDimension);

orders.BuildCube(aggregationDefinition, yearDimension, customerIdDimension, productDimension);
```

`BuildCube<TSource, TIndex, T>(...)` is a generic method with the following type arguments:
- `TSource` - the type of the source collection's item
- `TIndex` - the type of dimension index. In the previous example, this is the type of column and row indexes: `order.CustomerId` and `order.OrderDate.Year.ToString()`. The type should be the same for all dimensions. This means that if the data types for rows and columns are different, you need to bring them to a common type - in our case, `string`. That's why `.ToString()` is used for years
- `T` - the type of aggregation result, which is basically the type of cube cell. In our example, it's the type of `order.Quantity`, which is inferred as decimal

Usually, type arguments are inferred automatically, but when this is impossible (e.g., in the absence of dimensions), you need to specify them explicitly:

```csharp
Enumerable.Range(1, 10)
    .BuildCube<int, int, int>(
        AggregationDefinition.Create((int i) => i, (a, b) => a + b, 0));
```

The order of dimension definition arguments is arbitrary. The `BuildCube(...)` method treats all dimension definitions equally. However, note that the order of dimensions in the resulting cube will be the same as the order of dimension definition arguments.

The `BuildCube(...)` method returns a data cube in the form of a `CubeResult` instance. See the [Querying Cubes](#querying-cubes) section for details.

## Aggregation Definitions

Aggregation (alternatively called measure) specifies how to calculate cube cells. Aggregation definitions can be created with the static method `AggregationDefinition.Create(...)` (or its variations) in the following way:

```csharp
AggregationDefinition.Create(
    order => order.Quantity,
    (a, b) => a + b,
    seedValue: 0);
```

The method `AggregationDefinition.Create<TSource, T>(...)` is generic with the following type arguments:
- `TSource` - the type of the source collection's item
- `T` - the type of aggregation result, which is basically the type of cube cell. In our example, it's the type of `order.Quantity`, which is inferred as decimal

The method `AggregationDefinition.Create(...)` has the following parameters:
- `valueSelector` - a lambda function (more precisely, an expression) that specifies what field of the source collection will be used for aggregation. In our example, `order => order.Quantity`. If your collection directly contains values of interest, specify just `x => x`. The expression can also include multiple fields, e.g., `order => order.Quantity * order.Amount`
- `aggregationFunction` - a lambda function with signature `Func<T, T, T>` that specifies how data should be aggregated (e.g., sum, min, max). For our case, it's `(a, b) => a + b`
- `seedValue` - specifies the seed value for aggregation. It also defines the value for cells that don't have matching data

The last two arguments correspond to similar arguments of the [LINQ Aggregate method](https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.aggregate?view=netcore-3.1).

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
    (int i)  => new[] { i },
    (a, b) => a.Concat(b).ToArray(),
    Array.Empty<int>());
```

If you need to calculate multiple values for each cell, you can create a composite type and use it as the aggregation result type `T`, for instance:

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

// Create aggregation definition for calculating count and sum
AggregationDefinition.Create(
    (int i) => new CountAndSum(1, i),
    CountAndSum.Combine,
    CountAndSum.Zero);
```

There are several variations of the `AggregationDefinition.Create(...)` method.

The method `AggregationDefinition.CreateForDictionaryCollection(...)` is a shortcut that fixes the type of collection elements to `IDictionary<string, object>`, which frees you from specifying this type argument explicitly.

```csharp
// Take sum of field Quantity
AggregationDefinition.CreateForDictionaryCollection(
    dict => (decimal)dict["Quantity"], (a, b) => a + b, 0);
```

The method `AggregationDefinition.CreateForCollection(...)` takes a collection as an additional argument, which is used only for type inference. It can also be used for anonymous types, as we have in the example.

```csharp
// Take sum of field Quantity
AggregationDefinition.CreateForCollection(
    orders,
    order => order.Quantity,
    (a, b) => a + b,
    0);
```

## Dimension Definitions

Dimension definitions specify which field of the source collection to use and what values of this field we are interested in. Dimension definitions can be created with the static method `DimensionDefinition.Create(...)` (or its variations) in the following way:

```csharp
DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"),
    IndexDefinition.Create("C", "Customer C"));
```

The method `DimensionDefinition.Create<TSource, TIndex>(...)` is generic with the following type arguments:
- `TSource` - the type of the source collection's item
- `TIndex` - the type of index, which is basically the type of field specified in the `indexSelector` parameter. In our example, it's the type of `order.CustomerId`, which is inferred as string

The method `DimensionDefinition.Create(...)` has the following parameters:
- `indexSelector` - a lambda function (more precisely, an expression) that specifies which field of the source collection will be used as index. In our example, `order => order.CustomerId`. More generally, `indexSelector` calculates index values based on arbitrary expressions:

```csharp
DimensionDefinition.Create(
    order => order.OrderDate.Year % 4 == 0 ? "leap" : "nonLeap",
    title: "Years",
    IndexDefinition.Create("leap", "Leap years"),
    IndexDefinition.Create("nonLeap", "Non-leap years"));
```

- `title` - the title of the dimension, which can be used for data representation in tabular form
- `indexDefinitions` - a [variable size parameter](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params) for specifying a collection of index definitions

Index definitions are created with the static method `IndexDefinition.Create(...)` in the following way:

```csharp
IndexDefinition.Create("A", "Customer A");
```

Index values should be unique within the scope of a dimension.

Indexes can be organized in a hierarchy, where a parent index corresponds to a subtotal value of its children. Child indexes can be specified as an additional [variable size parameter](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params) of the `IndexDefinition.Create(...)` method:

```csharp
IndexDefinition.Create(
    "A",
    title: "Product category A",
    IndexDefinition.Create("A1", "Product A1"),
    IndexDefinition.Create("A2", "Product A2"));
```

Dimension definitions implement the [IEnumerable<>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1?view=netcore-3.1) interface of `IndexDefinition<TIndex>`, which allows you to traverse incorporated index definitions in the specified order. This is useful for building table representations of cubes.

For child indexes, they usually follow after the parent index as in the previous example. However, the parent index can be placed after the children with an overload of the `IndexDefinition.Create(...)` method with reversed order of arguments:

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

The `default` value (which is the same as `null` for reference and nullable types) of index is reserved for the so-called _Default index_, which contains the total value over the entire dimension and usually represents the total row or column in tabular representation.

> **_NOTE:_** If the `default` value conflicts with a usual index value (e.g., `0` value for `integer` type), you can consider converting the index type to nullable (`int?`) or mapping the `default` value to some other value in the `indexSelector` lambda function.

The _Default index_ must always be the only root index of a dimension and can have other indexes as children:

```csharp
// Correct
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

There are shortcuts for creating a default index in an existing dimension in the form of `DimensionDefinition`'s extension methods `.WithLeadingDefaultIndex(...)` and `.WithTrailingDefaultIndex(...)`, which create a new instance of `DimensionDefinition` with a default index and nest all existing indexes as its children:

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

Sometimes it's useful to have so-called _Default dimensions_, which are dimensions with only the default index. This is a degenerate form of dimension that can be used, for instance, as a placeholder to keep a constant number of dimensions across the code when fewer dimensions are required. Since _Default dimensions_ don't have actual index values, the `indexSelector` parameter (lambda expression) is redundant. There's a shortcut method `DimensionDefinition.CreateDefault(...)` for creating default dimensions:

```csharp
DimensionDefinition.CreateDefault<string, string>(
    title: "Customers",
    indexTitle: "All customers");
```

There are several variations of the `DimensionDefinition.Create(...)` method.

The method `DimensionDefinition.CreateForDictionaryCollection(...)` is a shortcut that fixes the type of collection elements to `IDictionary<string, object>`, which frees you from specifying this type argument explicitly:

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

The method `DimensionDefinition.CreateForCollection(...)` takes a collection as an additional argument, which is used only for type inference. It can also be used for anonymous types, as we have in the example:

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

### Multi-selection

Sometimes a target entity has a collection of attributes (a common example is informational tags) or is connected to another entity with one-to-many or many-to-many relations. For such cases, there's the method `DimensionDefinition.CreateWithMultiSelector(...)`, which takes an index selector that selects multiple index values in the form of `Func<TSource, IEnumerable<TIndex>>`. There are also variations of this method similar to those described above: `CreateForDictionaryCollectionWithMultiSelector(..)` and `CreateForCollectionWithMultiSelector(...)`.

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

# Querying Cubes

## Querying Single Cube Cells

The basic operation for querying cubes is the `GetValue(...)` method of the `CubeResult` class. It returns aggregated data for a specified cube cell. This method takes any number of index values as a [parameter array](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params), each corresponding to the cube's dimensions in the order specified in the `BuildCube(...)` method.

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Get aggregated value for customer "A" and year 2007
cube.GetValue("A", "2007");
```

To get the total by some dimension, you need to specify `default` (which is the same as `null` for reference and nullable types) in the corresponding position.

```csharp
// Get aggregated value for all customers and year 2007
cube.GetValue(default, "2007");

// Get aggregated value for customer "A" and all years
cube.GetValue("A", default);

// Get aggregated value for all records
cube.GetValue(default, default);
```

As a shorthand, you can also omit trailing `default` index values:

```csharp
// The same as cube.GetValue("A", default)
cube.GetValue("A");

// The same as cube.GetValue(default, default) or cube.GetValue(default)
cube.GetValue();
```

> **_NOTE:_** Omitting trailing default indexes allows you to write generic code that doesn't depend on the actual number of dimensions. For instance, `cube.GetValue()` gets the total value for any cube, `cube.GetValue(index)` gets a specific value by index in the first dimension, provided that the cube has at least one dimension. See more details in the section [Generic Cube Operations](#generic-cube-operations).

The `GetValue(...)` method always returns a result, unless the number of specified parameters is greater than the number of cube's dimensions, which can be retrieved by the property `CubeResult.FreeDimensionCount`. When one or more indexes are not included in the dimension's index definitions, the result will be the `seedValue` specified in the aggregation definition.

```csharp
// Returns 0, since customer id "Z" is not specified in customerId dimension
// and seedValue of aggregationDefinition is 0
cube.GetValue("Z", "2007"); // 0

// Returns 0, since year 1999 is not specified in yearDimension dimension
// and seedValue of aggregationDefinition is 0
cube.GetValue("A", "1999"); // 0
```

## Slicing Cubes

The _slice_ operation can be intuitively considered as pinning the indexes for some dimensions. Compare different ways to achieve the same result:

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

In these examples, we apply the _slice_ operation using the index operator `[]`.

More formally, _slice_ is the operation of extracting an _(n-k)_-dimensional cube from an _n_-dimensional cube by picking values for _k_ dimensions (_k = 1_ in the most usual case).

The slice operation can be applied with the indexing operator `[]` of the `CubeResult` class (as in examples above) or the more general `Slice(...)` method. This method allows slicing by multiple dimensions in arbitrary order, whereas the indexing operator `[]` only allows slicing by the first free dimension. The dimension number to slice by can be specified as a value of type [Index](https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/ranges-indexes#language-support-for-indices-and-ranges), which is convertible to _int_ type.

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Returns the cube slice by customer ID "A", the same as cube["A"]
cube.Slice(0, "A");

// Returns the cube slice by year 2007, where 1 means second dimension from beginning
cube.Slice(1, "2007");

// Returns the cube slice by year 2007, where ^1 means first dimension from end
cube.Slice(^1, "2007");
```

To slice by multiple dimensions at once, you can use overloads of the `Slice(...)` method, which take multiple indexes or multiple pairs of dimension numbers and indexes as a [parameter array](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params).

```csharp
// Returns the cube slice by customer ID "A" and year 2007
cube.Slice("A", "2007");
cube.Slice((0, "A"), (1, "2007"));

// Returns the cube slice by year 2007 and customer ID "A"
cube.Slice((1, "2007"), (0, "A"));
```

Applied indexes and corresponding _bound_ dimensions are stored in the result of the _slice_ operation and can be retrieved as a collection of pairs with the method `GetBoundDimensionsAndIndexes()` or separately by methods `GetBoundDimension(...)` and `GetBoundIndex(...)` by the number of the _bound_ dimension.

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Cube slice by customer ID "A"
var sliceByCustomerIdA = cube["A"];
sliceByCustomerIdA.GetBoundDimensionsAndIndexes(); // new[] { (customerIdDimension, "A") }
sliceByCustomerIdA.GetBoundDimension(0); // customerIdDimension
sliceByCustomerIdA.GetBoundIndex(0); // "A"

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

Notice that the result of the `Slice(...)` method is a cube with fewer free dimensions and possibly shifted dimension numbers:

```csharp
// Cube slice by customer ID "A", the same as cube["A"]
cube.Slice(0, "A");

// Slice by dimension number 0, not 1 because of shifting of dimension numbers
sliceByCustomerIdA.Slice(0, "2007");
```

## Breakdown Operations

The _breakdown_ operation allows you to create a collection of slices by all indexes of one or many dimensions.

Returning to the starting example, remember that we need to build a report with customer IDs as rows. Since every row of the report corresponds to a cube slice by a specific customer ID, we need to have a collection of cube slices by all customer IDs. Since we already defined all customer IDs of interest in the dimension definition, we have an option to enumerate them. The breakdown operation is just a combination of enumerating dimension indexes and slicing the cube by each of them.

The _breakdown_ operation is implemented in `BreakdownByDimensions(...)` extension methods of the `CubeResult` class. These methods take one or many free dimension numbers as a [parameter array](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params). There's also an overload that takes a range of dimension numbers as a [Range](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/ranges) instance. As a result, `BreakdownByDimensions(...)` methods return a collection of cube slices in the form of instances of the `CubeResult` class, one per each index definition including totals. More generally, when many dimensions are specified, the result will be a collection of cube slices corresponding to all combinations of index definitions of the specified dimensions.

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

## Generic Cube Operations

Let's return to the initial example in [Getting Started](#getting-started), where we created a tabular report using the cube:

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);
```

Let's look again at probably the most obscure part, which builds the report from the cube:

```csharp
cube
    .BreakdownByDimensions(..^1) // ..^1 - build rows by all dimensions except the last
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

Notice that this code doesn't depend on any specific cube or concrete definitions. It builds a tabular report in which rows correspond to all dimensions except the last, and columns correspond to the last dimension. This is due to the fact that a `CubeResult` instance contains all necessary information and meta-information to create such a report. It illustrates the idea that it's possible to create generic transformations of any cube to particular representations, such as collections, tables, or trees.

Let's create a generic transformation of cube to table similar to what's demonstrated in the example.

For a start, we can build a single table row in the form of a dictionary, where keys correspond to indexes (e.g., in dimension `0` (customerId)) and values contain aggregated values:

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

We can generalize this code snippet to a reusable method by extracting `cubeResult` and `columnDimensionNumber` parameters:

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
GetTableBodyColumns(cube, 0);
```

Now let's outline the rows of our report. Each row will correspond to an index (e.g., in dimension `0` (customerId)) and will be represented by a single-entry dictionary with the aggregated value for the corresponding index:

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

The method `BreakdownByDimensions(...)` takes multiple dimension numbers (as an array or [range](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/ranges)), so it's possible to create a table by multiple dimensions as rows:

```csharp
// Creates collection of total values for all combinations of customerID and year (including totals)
cube
    .BreakdownByDimensions(..) // build rows by all dimensions
    .Select(row => new[] { KeyValuePair.Create("Value", (object)row.GetValue()) }
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```

To add header columns, we can use the method `CubeResult.GetBoundDimensionsAndIndexes(...)`, which returns a collection of pairs of bound dimension and index. Each such pair can be transformed to a `KeyValuePair<string, object>` with the dimension title as key and index title as value:

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

We can similarly generalize the code snippet above to a reusable method by extracting the parameters `cubeResult`, `rowDimensions`, `getHeaderColumns` (function returning header columns from row cube slice), and `getBodyColumns` (function returning body columns from row cube slice) and extract the `GetTableHeaderColumns()` function:

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
    ..^1, // ..^1 - build rows by all dimensions except the last
    GetTableHeaderColumns,
    row => new Dictionary<string, object?> { ["Value"] = (object)row.GetValue() });
```

Now let's put everything together: call the extracted method `GetTable(...)` using the extracted method `GetTableBodyColumns(...)` for generating body columns:

```csharp
GetTable(
    cube,
    ..^1, // ..^1 - build rows by all dimensions except the last
    GetTableHeaderColumns,
    row => GetTableBodyColumns(row, ^1)); // ^1 - build columns by the last dimension
```

| Customers  | 2007 Year | 2008 Year | 2009 Year | Total |
|------------|-----------|-----------|-----------|-------|
| Customer A | 22        | 40        | 10        | 72    |
| Customer B | 20        | 12        | 15        | 47    |
| Customer C | 22        | 14        | 20        | 56    |
| Customer D | 0         | 0         | 60        | 60    |
| Total      | 64        | 66        | 105       | 235   |

If we inline the code of the extracted generic methods, we'll get code similar to the initial example.

As a result, we managed to build the desired report and created reusable generic methods that allow us to create similar reports.
