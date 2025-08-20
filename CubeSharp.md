- [Introduction](#introduction)
- [Motivation](#motivation)
- [Getting started](#getting-started)
- [Building the cubes.](#building-the-cubes)
  - [Aggregation definitions.](#aggregation-definitions)
  - [Dimension definitions.](#dimension-definitions)
    - [Multi-selection](#multi-selection)
- [Querying the cubes.](#querying-the-cubes)
  - [Querying the single cube cells.](#querying-the-single-cube-cells)
  - [Slicing the cubes.](#slicing-the-cubes)
  - [Breakdown operation.](#breakdown-operation)
  - [Generic cube operations.](#generic-cube-operations)

# Introduction

CubeSharp is a small library for building in-memory [data cubes](https://en.wikipedia.org/wiki/Data_cube).  

# Motivation

Imagine you need to build tabular report concerning multiple factors (or dimensions),involving data aggregations and calculations, using totals by one or mode dimensions. 
If you use SQL for this purpose you have limited possibilities to refactor and test your code, which makes difficult to maintain it. 
If you use native .NET capabilities, e.g. LINQ, you need to write a lot of ah-hoc data manipulations, like filtering, mapping and aggregation, which also makes code difficult to maintain.
CubeSharp library allows you to systematize implementation approach to multi-factor (or multi-dimensional) reports by building the in memory data cube and then transforming it in required tabular representation.

# Getting started

Imagine, you have collection of data in the following form:

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

You need to build report with customer IDs as rows versus order years as columns and to calculate total quantity for each cell. Also you have fixed list of rows and columns to display including totals. 

Desired report may look like this:

| Customers  | 2007 Year | 2008 Year | 2009 Year | Total |
|------------|-----------|-----------|-----------|-------|
| Customer A | 22        | 40        | 10        | 72    |
| Customer B | 20        | 12        | 15        | 47    |
| Customer C | 22        | 14        | 20        | 56    |
| Customer D | 0         | 0         | 60        | 60    |
| Total      | 64        | 66        | 105       | 235   |

In order to build such report with CubeSharp library, you need to write the following code:

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
            dimensionAndIndex.dimension.Title,
            (object)dimensionAndIndex.dimension[dimensionAndIndex.index].Title))
        .Concat(row
            .BreakdownByDimensions(^1) // ^1 - build columns by last dimension
            .Select(column => KeyValuePair.Create(
                column.GetBoundIndexDefinition(^1).Title,
                (object)column.GetValue())))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```
For now it is enough to understand how requirements are reflected in this code, all details will be explained in following sections. 
Result will be collection of dictionaries, which in table form equivalent to desired report.

# Building the cubes.

In order to build the cube you need to use generic extension method `BuildCube(...)` for any collection, additionally providing aggregation definition and any number of dimension definitions (including zero) in form of [parameter array](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params).

```csharp
orders.BuildCube(aggregationDefinition, customerIdDimension);

orders.BuildCube(aggregationDefinition, yearDimension, customerIdDimension);

orders.BuildCube(aggregationDefinition, yearDimension, customerIdDimension, productDimension);
```

`BuildCube<TSource, TIndex, T>(...)` is generic method with following type arguments:
- `TSource` - the type of source collection's item;
- `TIndex` - the type of dimension index, in previous example type of column and row indexes: `order.CustomerId` and `order.OrderDate.Year.ToString()`. The type should be the same for all dimensions. It means that if types of data for rows and columns are different, you need to bring them to the common type - in out case `string`. That is why `.ToString()` is used for years;
- `T` - the type of aggregation result, that is basically type of cube cell. In our example it is type of `order.Quantity`, which is inferred as integer.
Usually type arguments are inferred automatically, but in case when it is impossible, e.g. in absence of dimensions, it is required to specify them explicitly:

```csharp
Enumerable.Range(1, 10)
    .BuildCube<int, int, int>(
        AggregationDefinition.Create((int i) => i, (a, b) => a + b, 0));
```

Order of dimension definition argument is arbitrary. Method `BuildCube(...)` treats all dimension definitions equally. However notice that order of dimensions in result cube will be the same as order of dimension definition arguments.
Method `BuildCube(...)` returns data cube in form of `CubeResult` instance, see [Querying the cubes](#querying-the-cubes) section for details.

## Aggregation definitions.

Aggregation (alternatively called measure) specifies how to calculate cube cells. 
Aggregation definitions can be created with static method `AggregationDefinition.Create(...)` (or its modifications) in following way:

```csharp
AggregationDefinition.Create(
    order => order.Quantity,
    (a, b) => a + b,
    seedValue: 0);
```

Method `AggregationDefinition.Create<TSource, T>(...)` is generic with following type arguments:
- `TSource` - the type of source source collection's item;
- `T` - the type of aggregation result, that is basically type of cube cell; in our example it is type of `order.Quantity`, which is inferred as integer.

Method `AggregationDefinition.Create(...)` has the following parameters:
- `valueSelector` - lambda function (more precisely expression), specifying basically what field of source collection will be used for aggregation, in our example `order => order.Quantity`; in cause your collection directly contains values of interest, specify just `x => x`; expression can also include multiple fields, e.g. `order => order.Quantity * order.Amount`;
- `aggregationFunction` - lambda function with signature `Func<T, T, T>`, specifying how data should be aggregated, e.g. sum, min, max, etc, for our case it is `(a, b) => a + b`;
- `seedValue` - specifies seed value for aggregation, it also will define value for cells, which does not have matching data.

Last two arguments corresponds to similar arguments of [LINQ Aggregate method](https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.aggregate?view=netcore-3.1).

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

In case you need co calculate multiple values for each cell you can create composite type and use it as type of aggregation result `T`, for instance:

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

There are several modifications of `AggregationDefinition.Create(...)` method.

Method `AggregationDefinition.CreateForDictionaryCollection(...)` is shortcut fixing type of collection elements to `IDictionary<string, object>`, which frees from specifying this type argument explicitly.

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

## Dimension definitions.

Dimension definition basically specify which field of source collection to use and what values of this field we are interested in.
Dimension definition can be created with static method `DimensionDefinition.Create(...)` (or its modifications) in following way:

```csharp
DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"),
    IndexDefinition.Create("C", "Customer C"));
```

Method `DimensionDefinition.Create<TSource, TIndex>(...)` is generic with following type arguments:
- `TSource` - the type of source source collection's item;
- `TIndex` - the type of index, that is basically type of field, specified in `indexSelector` parameter; in our example it is the type of `order.CustomerId`, which is inferred as string.

Method `DimensionDefinition.Create(...)` has the following parameters:
- `indexSelector` - lambda function (more precisely expression), basically specifying which field of source collection will be used as index, in our example `order => order.CustomerId`; more generally, `indexSelector` calculates index value on base of arbitrary expression:

```csharp
DimensionDefinition.Create(
    order => order.OrderDate.Year % 4 == 0 ? "leap" : "nonLeap",
    title: "Years",
    IndexDefinition.Create("leap", "Leap years"),
    IndexDefinition.Create("nonLeap", "Non-leap years"));
```

- `title` - the title of dimension, which can be used for data representation in tabular form;
- `indexDefinitions` - [variable size parameter](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params) for specifying collection of index definitions.

Index definitions are created with static method `IndexDefinition.Create(...)` in following way: 

```csharp
IndexDefinition.Create("A", "Customer A");
```

Index values should be unique in scope of dimension.

Indexes can be organized in hierarchy, where parent index corresponds to subtotal value of its children. Child indexes can be specified as additional [variable size parameter](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params) of `IndexDefinition.Create(...)` method:

```csharp
IndexDefinition.Create(
    "A",
    title: "Product category A",
    IndexDefinition.Create("A1", "Product A1"),
    IndexDefinition.Create("A2", "Product A2"));
```

Dimension definition implements [IEnumerable<>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1?view=netcore-3.1) interface of `IndexDefinition<TIndex>`, which allows to traverse incorporated index definitions in specified order. It is useful for building table representations of cubes.
 
 For child indexes they are usually follow after the parent index as in previous example. However parent index can be placed after the children with overload of `IndexDefinition.Create(...)` method with reversed order of arguments:

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

Value `default` (which is the same as `null` for reference and nullable types) of index is reserved for so-called _Default index_ which contains total value over the whole dimension and which usually represent total row or column in tabular representation.

> **_NOTE:_**  In case `default` value is conflicting with usual index value (e.g. `0` value for `integer` type, you can consider converting index type to nullable (`int?`) or map `default` value to some other value in `indexSelector` lambda function.

_Default index_ always must be the only root index of dimension and can have other indexes as children:

```csharp
/// Correct
DimensionDefinition.Create(
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create(
        (string)default,
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

There are shortcuts for creating default index in existing dimension in form of `DimensionDefinition`'s extension methods `.WithLeadingDefaultIndex(...)` and `.WithTrailingDefaultIndex(...)`, which create new instance of `DimensionDefinition` with default index and nest all existing indexes as its children:

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

Sometimes it is useful to have so-called `Default dimensions`, which are just dimensions with the only default index. It is degenerate form of dimension which can be used for instance as placeholder to keep constant number of dimensions across the code in cases where less number of dimensions is required. Since `Default dimensions` does not have actual index values, parameter `indexSelector` (lambda expression) is redundant. There is shortcut method `DimensionDefinition.CreateDefault(...)` for creating default dimensions:

```csharp
DimensionDefinition.CreateDefault<string, string>(
    title: "Customers",
    indexTitle: "All customers");
```

There are several modifications of `DimensionDefinition.Create(...)` method.

Method `DimensionDefinition.CreateForDictionaryCollection(...)` is shortcut fixing type of collection elements to `IDictionary<string, object>`, which frees from specifying this type argument explicitly:

```csharp
DimensionDefinition.CreateForDictionaryCollection(
    dict => (string)dict["CustomerId"],
    title: "Customers",
    IndexDefinition.Create(
        (string)default,
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
        (string)default,
        title: "Total",
        IndexDefinition.Create("A", "Customer A"),
        IndexDefinition.Create("B", "Customer B")));
```

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
        (string)default,
        title: "Total",
        IndexDefinition.Create("Bestseller", "Bestseller"),
        IndexDefinition.Create("Discount", "Discount")));
```

# Querying the cubes.

## Querying the single cube cells.

The basic operation for querying the cubes is method `GetValue(...)` of `CubeResult` class. It returns aggregated data for specified cube cell. This method takes any number of index values in form of [parameter array](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params) each corresponding to cube's dimension in order of cube's dimensions specified in `BuildCube(...)` method.

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Get aggregated value for customer "A" and year 2007
cube.GetValue("A", "2007");
```

In order to get total by some dimension you need to specify `default` (which is the same as `null` for reference and nullable types) in corresponding position.

```csharp
// Get aggregated value for all customers and year 2007
cube.GetValue(default, "2007");

// Get aggregated value for customer "A" and all years
cube.GetValue("A", default);

// Get aggregated value for all records
cube.GetValue(default, default);
```

As shorthand you can also bypass trailing `default` index values:

```csharp
// The same as cube.GetValue("A", default)
cube.GetValue("A");

// The same as cube.GetValue(default, default) or cube.GetValue(default)
cube.GetValue();
```

> **_NOTE:_**  Bypassing trailing default indexes allows you to write generic code which does not depend on actual number of dimensions, for instance `cube.GetValue()` gets total value for any cube, `cube.GetValue(index)` gets specific value by index in first dimension provided only that cube has at least one dimension. See more details in section [Generic cube operations](#generic-cube-operations).

Method `GetValue(...)` always return the result, unless the number or specified parameters is bigger then number of cube's dimensions which can be retrieved by property `CubeResult.FreeDimensionCount`. In case when one or more indexes are not included in Dimensions' index definitions, result will be the `seedValue` specified in `Aggregation definition`.

```csharp
// Returns 0, since customer id "Z" is not specified in customerId dimension
// and seedValue of aggregationDefinition is 0
cube.GetValue("Z", "2007"); // 0

// Returns 0, since year 1999 is not specified in yearDimension dimension
// and seedValue of aggregationDefinition is 0
cube.GetValue("A", "1999"); // 0
```

## Slicing the cubes.

_Slice_ operation intuitively can be considered as pinning the indexes for some dimensions.
Compare different ways to achieve the same result:

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

In these examples we apply _slice_ operation with use index operator `[]`.

More formally, _slice_ is basically the operation of extracting the _(n-k)_-dimension cube from the _n_-dimension cube by picking the values for _k_ of dimensions (_k = 1_ in most usual case).

Slice operation can be applied with indexing operator `[]` of class `CubeResult` (as in examples above) or more general `Slice(...)` method. This method allows to slice by multiple dimensions in arbitrary order, whereas indexing operator `[]` allows to slice only by the first free dimension.
Dimension number to slice by can be specified as value of type [Index](https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/ranges-indexes#language-support-for-indices-and-ranges), which is convertible to _int_ type.

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);

// Returns the cube slice by customer ID "A", the same as cube["A"]
cube.Slice(0, "A");

// Returns the cube slice by year 2007, where 1 means second dimension from beginning
cube.Slice(1, "2007");

// Returns the cube slice by year 2007, where ^1 means first dimension from end
cube.Slice(^1, "2007");
```

In order to slice by multiple dimensions at once you can use overloads of `Slice(...)` method, which take multiple indexes or multiple pairs or dimension numbers and indexes in form of [parameter array](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params).

```csharp
// Returns the cube slice by customer ID "A" and year 2007
cube.Slice("A", "2007");
cube.Slice((0, "A"), (1, "2007"));

// Returns the cube slice by year 2007 and customer ID "A"
cube.Slice((1, "2007"), (0, "A"));
```

Applied indexes and corresponding _bound_ dimensions are stored in result of _slice_ operation and can be retrieved as collection of pairs with method `GetBoundDimensionsAndIndexes()` or or separately by methods `GetBoundDimension(...)` and `GetBoundIndex(...)` by number or _bound_ dimension.

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

Notice that result of `Slice(...)` method is cube with less number of free dimensions and dimension numbers possibly shifted:

```csharp
// Cube slice by customer ID "A", the same as cube["A"]
cube.Slice(0, "A");

// Slice by dimension number 0, not 1 because of shifting of dimension numbers
sliceByCustomerIdA.Slice(0, "2007");
```

## Breakdown operation.

_Breakdown_ operation allows to create collection of slices by all indexes of one or many dimensions. 

Returning back to starting example, remember that we need to build report with customer IDs as rows. Since every row of report corresponds to cube slice by specific customer ID, we need to have collection of cube slices by all customer IDs. Since we already defined all customer IDs of interest in dimension definition, we have an option to enumerate them. Breakdown operation is just combination of enumerating dimension indexes and slicing cube by each of them. 

_Breakdown_ operation is implemented in `BreakdownByDimensions(...)` extension methods of `CubeResult` class. These methods take one or many free dimension numbers in form of [parameter array](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params). There is also overload taking range of dimension numbers as [Range](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/ranges) instance. As result `BreakdownByDimensions(...)` methods return collection of cube slices in form of instances of `CubeResult` class one per each index definition including totals. More generally, when many dimensions are specified result will be collection of cube slices corresponding to all combinations of index definitions of specified dimensions.

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

## Generic cube operations.

Let's return to initial example in [Getting started](#getting-started), where we created tabular report using the cube:

```csharp
var cube = orders.BuildCube(aggregationDefinition, customerIdDimension, yearDimension);
```

Let's look again to probably the most obscure part, which builds the report from the cube:

```csharp
cube
    .BreakdownByDimensions(..^1) // ..^1 - build rows by range of all dimensions except last
    .Select(row => row
        .GetBoundDimensionsAndIndexes()
        .Select(dimensionAndIndex => KeyValuePair.Create(
            dimensionAndIndex.dimension.Title,
            (object)dimensionAndIndex.dimension[dimensionAndIndex.index].Title))
        .Concat(row
            .BreakdownByDimensions(^1) // ^1 - first from end
            .Select(column => KeyValuePair.Create(
                column.GetBoundIndexDefinition(^1).Title,
                (object)column.GetValue())))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```

Notice, that this code does not depend neither on specific cube nor on any concrete definitions. It builds the tabular report, in which rows correspond to all dimensions but last and columns correspond to the last dimension. It is due to the fact, that `CubeResult` instance contains all necessary information and meta-information to create such report. It illustrates the idea that it is possible to create generic transformation of any cube to particular representation, like collection, table or tree.

Let's create generic transformation of cube to table similar to demonstrated in example. 

For beginning, we can build the single table row in form of dictionary, which keys correspond to indexes e.g. in dimension `0` (customerId) and which values contain aggregated values:

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

We can generalize this code snippet to reusable method by extracting `cubeResult` and `columnDimensionNumber` parameters:

```csharp
IDictionary<string, object> GetTableColumns<TIndex, T>(
    CubeResult<TIndex, T> cubeResult,
    Index columnDimensionNumber) =>
cubeResult
    .BreakdownByDimensions(columnDimensionNumber)
    .Select(column => KeyValuePair.Create(
        column.GetBoundIndexDefinition(^1).Title,
        (object)column.GetValue()))
    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

// Example of call
GetTableColumns(cube, 0);
```

Now let's outline the rows of our report. Each row will correspond to index e.g. in dimension `0` (customerId) and will be represented by single-entry dictionary with aggregated value for corresponding index:

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

Method `BreakdownByDimensions(...)` takes multiple dimension numbers (as array or [range](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/ranges)), so it is possible to create table by multiple dimensions as rows:

```csharp
// Creates collection of total values for all combinations of customerID and year (including totals)
cube
    .BreakdownByDimensions(..) // build rows by all dimensions
    .Select(row => new[] { KeyValuePair.Create("Value", (object)row.GetValue()) }
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```

In order to add header columns, we can use method `CubeResult.GetBoundDimensionsAndIndexes(...)`, which returns collection of pairs of bound dimension and index. Each such pair can be transformed to `KeyValuePair<string, object>` with dimension title as key and index title as value:

```csharp
cube
    .BreakdownByDimensions(0)
    .Select(row => row
        .GetBoundDimensionsAndIndexes() // get collection of pairs of bound dimension and index
        .Select(dimensionAndIndex => KeyValuePair.Create( // create header column
            dimensionAndIndex.dimension.Title, // use dimension title for key
            (object)dimensionAndIndex.dimension[dimensionAndIndex.index].Title)) // use index title for value
        .Concat(new[] { KeyValuePair.Create("Value", (object)row.GetValue()) })
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
```

| Customers  | Value |
|------------|-------|
| Customer A | 72    |
| Customer B | 47    |
| Customer C | 56    |
| Customer D | 60    |
| Total      | 235   |

We can similarly generalize code snippet above to reusable method by extracting the parameters `cubeResult`, `rowDimensions`, `getHeaderColumns` (function returning columns of header from row cube slice) and `getBodyColumns` (function returning columns of body from row cube slice) and extract `GetTableHeaderColumns()` function:

```csharp
IEnumerable<IDictionary<string, object>> GetTable<TIndex, T>(
    CubeResult<TIndex, T> cubeResult,
    Range rowDimensions,
    Func<CubeResult<TIndex, T>, IDictionary<string, object>> getHeaderColumns,
    Func<CubeResult<TIndex, T>, IDictionary<string, object>> getBodyColumns) =>
    from row in cubeResult.BreakdownByDimensions(rowDimensions)
    select getHeaderColumns(row)
        .Concat(getBodyColumns(row)) // get body columns
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

IDictionary<string, object> GetTableHeaderColumns<TIndex, T>(
    CubeResult<TIndex, T> row) =>
    row.GetBoundDimensionsAndIndexes()
        .Select(dimensionAndIndex => KeyValuePair.Create(
            dimensionAndIndex.dimension.Title,
            (object)dimensionAndIndex.dimension[dimensionAndIndex.index].Title))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

// Example of call
GetTable(
    cube,
    ..^1, // ..^1 - build rows by range of all dimensions except last
    GetTableHeaderColumns,
    row => new Dictionary<string, object> { ["Value"] = (object)row.GetValue() });
```

Now let's put everything together: call extracted method `GetTable(...)` using extracted method `GetTableColumns(...)` for generating body columns:

```csharp
GetTable(
    cube,
    ..^1, // ..^1 - build rows by range of all dimensions except last
    GetTableHeaderColumns,
    row => GetTableColumns(row, ^1)); // ^1 - build columns by last dimension
```

| Customers  | 2007 Year | 2008 Year | 2009 Year | Total |
|------------|-----------|-----------|-----------|-------|
| Customer A | 22        | 40        | 10        | 72    |
| Customer B | 20        | 12        | 15        | 47    |
| Customer C | 22        | 14        | 20        | 56    |
| Customer D | 0         | 0         | 60        | 60    |
| Total      | 64        | 66        | 105       | 235   |

If we inline code of extracted generic methods, we will get the code similar to initial example. 

As result, we managed to build desired report and created reusable generic methods, which allow to create similar reports.
