<Query Kind="Statements">
  <NuGetReference Prerelease="true">CubeSharp</NuGetReference>
  <Namespace>CubeSharp</Namespace>
</Query>

#load "Utils.linq"

// Section "Getting started"

var orders = new[] {
    new { OrderDate = new DateTime(2007, 08, 02), Product = "X", EmployeeId = 3, CustomerId = "A", Quantity = 10m, Tags = new [] { "Discount", "Retail" } },
    new { OrderDate = new DateTime(2007, 12, 24), Product = "X", EmployeeId = 3, CustomerId = "A", Quantity = 12m, Tags = new [] { "Retail", "BestSeller" } },
    new { OrderDate = new DateTime(2007, 12, 24), Product = "Y", EmployeeId = 1, CustomerId = "B", Quantity = 20m, Tags = new [] { "Discount", "Retail", "New", "Season", "BestSeller" } },
    new { OrderDate = new DateTime(2008, 01, 09), Product = "Z", EmployeeId = 2, CustomerId = "A", Quantity = 40m, Tags = new [] { "BestSeller" } },
    new { OrderDate = new DateTime(2008, 01, 18), Product = "Z", EmployeeId = 1, CustomerId = "C", Quantity = 14m, Tags = new [] { "Discount", "New", "BestSeller" } },
    new { OrderDate = new DateTime(2008, 02, 12), Product = "Z", EmployeeId = 2, CustomerId = "B", Quantity = 12m, Tags = new [] { "Retail" } },
    new { OrderDate = new DateTime(2009, 02, 12), Product = "X", EmployeeId = 3, CustomerId = "A", Quantity = 10m, Tags = new string[] {} },
    new { OrderDate = new DateTime(2009, 02, 16), Product = "X", EmployeeId = 1, CustomerId = "C", Quantity = 20m, Tags = new [] { "New" } },
    new { OrderDate = new DateTime(2009, 04, 18), Product = "Z", EmployeeId = 2, CustomerId = "B", Quantity = 15m, Tags = new [] { "Discount", "BestSeller" } },
    new { OrderDate = new DateTime(2007, 04, 18), Product = "X", EmployeeId = 3, CustomerId = "C", Quantity = 22m, Tags = new [] { "Discount", "Retail", "New", "Season" } },
    new { OrderDate = new DateTime(2009, 09, 07), Product = "Y", EmployeeId = 3, CustomerId = "D", Quantity = 30m, Tags = new [] { "Retail", "New", "Season", "BestSeller" } },
    new { OrderDate = new DateTime(2009, 09, 07), Product = (string?)null!, EmployeeId = 3, CustomerId = "D", Quantity = 30m, Tags = new [] { "Discount", "New", "BestSeller" } },
};

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
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        .ToExpando())
    .Dump();

// Section "Building the cubes"

var productDimension = DimensionDefinition.CreateForCollection(
    orders,
    order => order.Product,
    title: "Products",
    IndexDefinition.Create("X", "Product X"),
    IndexDefinition.Create("Y", "Product Y"),
    IndexDefinition.Create("Z", "Product Z"))
        .WithTrailingDefaultIndex("Total");

orders.BuildCube(aggregationDefinition, customerIdDimension);

orders.BuildCube(aggregationDefinition, yearDimension, customerIdDimension);

orders.BuildCube(aggregationDefinition, yearDimension, customerIdDimension, productDimension);

Enumerable.Range(1, 10)
    .BuildCube<int, int, int>(
        AggregationDefinition.Create((int i) => i, (a, b) => a + b, 0));

// Section "Aggregation definitions"

AggregationDefinition.CreateForCollection(
    orders,
    order => order.Quantity,
    (a, b) => a + b,
    seedValue: 0);

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

// Create aggregation definition for calculating count amd sum
AggregationDefinition.Create(
    (int i) => new CountAndSum(1, i),
    CountAndSum.Combine,
    CountAndSum.Zero);

// Take sum of field Quantity
AggregationDefinition.CreateForDictionaryCollection(
    dict => (decimal)dict["Quantity"], (a, b) => a + b, 0);

// Take sum of field Quantity
AggregationDefinition.CreateForCollection(
    orders,
    order => order.Quantity,
    (a, b) => a + b,
    0);

// Section "Dimension definitions"

DimensionDefinition.CreateForCollection(
    orders,
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"),
    IndexDefinition.Create("C", "Customer C"));

DimensionDefinition.CreateForCollection(
    orders,
    order => order.OrderDate.Year % 4 == 0 ? "leap" : "nonLeap",
    title: "Years",
    IndexDefinition.Create("leap", "Leap years"),
    IndexDefinition.Create("nonLeap", "Non-leap years"));

IndexDefinition.Create("A", "Customer A");

IndexDefinition.Create(
    "A",
    title: "Product category A",
    IndexDefinition.Create("A1", "Product A1"),
    IndexDefinition.Create("A2", "Product A2"));

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

/// Correct
DimensionDefinition.CreateForCollection(
    orders,
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create(
        (string?)default,
        title: "Total",
        IndexDefinition.Create("A", "Customer A"),
        IndexDefinition.Create("B", "Customer B")));


// Order of indexes:
// Total
// Customer A
// Customer B
DimensionDefinition.CreateForCollection(
    orders,
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"))
        .WithLeadingDefaultIndex("Total");

// Order of indexes:
// Customer A
// Customer B
// Total
DimensionDefinition.CreateForCollection(
    orders,
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create("A", "Customer A"),
    IndexDefinition.Create("B", "Customer B"))
        .WithTrailingDefaultIndex("Total");

DimensionDefinition.CreateDefault<string, string>(
    title: "Customers",
    indexTitle: "All customers");

DimensionDefinition.CreateForDictionaryCollection(
    dict => (string?)dict["CustomerId"],
    title: "Customers",
    IndexDefinition.Create(
        (string?)default,
        title: "Total",
        IndexDefinition.Create("A", "Customer A"),
        IndexDefinition.Create("B", "Customer B")));

DimensionDefinition.CreateForCollection(
    orders,
    order => order.CustomerId,
    title: "Customers",
    IndexDefinition.Create(
        (string?)default,
        title: "Total",
        IndexDefinition.Create("A", "Customer A"),
        IndexDefinition.Create("B", "Customer B")));

DimensionDefinition.CreateForCollectionWithMultiSelector(
    orders,
    order => order.Tags,
    title: "Tags",
    IndexDefinition.Create(
        (string?)default,
        title: "Total",
        IndexDefinition.Create("Bestseller", "Bestseller"),
        IndexDefinition.Create("Discount", "Discount")));

// Section "Querying the cubes"

// Section "Querying the single cube cells"

cube.GetValue("A", "2007");

// Get aggregated value for all customers and year 2007
cube.GetValue(default, "2007");

// Get aggregated value for customer "A" and all years
cube.GetValue("A", default);

// Get aggregated value for all records
cube.GetValue(default, default);

// The same as cube.GetValue("A", default)
cube.GetValue("A");

// The same as cube.GetValue(default, default) or cube.GetValue(default)
cube.GetValue();

// Returns 0, since customer id "Z" is not specified in customerId dimension
// and seedValue of aggregationDefinition is 0
cube.GetValue("Z", "2007"); // 0

// Returns 0, since year 1999 is not specified in yearDimension dimension
// and seedValue of aggregationDefinition is 0
cube.GetValue("A", "1999"); // 0

// Section "Slicing the cubes"

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

// Returns the cube slice by customer ID "A", the same as cube["A"]
cube.Slice(0, "A");

// Returns the cube slice by year 2007, where 1 means second dimension from beginning
cube.Slice(1, "2007");

// Returns the cube slice by year 2007, where ^1 means first dimension from end
cube.Slice(^1, "2007");

// Returns the cube slice by customer ID "A" and year 2007
cube.Slice("A", "2007");
cube.Slice((0, "A"), (1, "2007"));

// Returns the cube slice by year 2007 and customer ID "A"
cube.Slice((1, "2007"), (0, "A"));

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

// Cube slice by customer ID "A", the same as cube["A"]
cube.Slice(0, "A");

// Slice by dimension number 0, not 1 because of shifting of dimension numbers
sliceByCustomerIdA.Slice(0, "2007");

// Section "Breakdown operation"

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

// Section "Generic cube operations"

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
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        .ToExpando())
    .Dump();

cube
    .BreakdownByDimensions(0) // use dimension 0 (customerId) for columns
    .Select(column => KeyValuePair.Create( // create KeyValuePair instance for each column
        column.GetBoundIndexDefinition(^1).Title!, // use title of last bound index for key
        column.GetValue())) // use value for dictionary value
    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
    .ToExpando()
    .Dump();

IDictionary<string, object?> GetTableBodyColumns<TIndex, T>(
    CubeResult<TIndex, T> cubeResult, Index columnDimensionNumber)
    where TIndex : notnull =>
cubeResult
    .BreakdownByDimensions(columnDimensionNumber)
    .Select(column => KeyValuePair.Create(
        column.GetBoundIndexDefinition(^1).Title!,
        (object?)column.GetValue()))
    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

GetTableBodyColumns(cube, 0).ToExpando().Dump();

cube
    .BreakdownByDimensions(0)
    .Select(row => 
        new[] { KeyValuePair.Create("Value", (object)row.GetValue()) }
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            .ToExpando())
    .Dump();

// Creates collection of total values for all combinations of customerID and year (including totals)
cube
    .BreakdownByDimensions(..) // build rows by all dimensions
    .Select(row => new[] { KeyValuePair.Create("Value", (object)row.GetValue()) }
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value).ToExpando())
    .Dump();

cube
    .BreakdownByDimensions(0)
    .Select(row => row
        .GetBoundDimensionsAndIndexes() // get collection of pairs of bound dimension and index
        .Select(dimensionAndIndex => KeyValuePair.Create( // create header column
            dimensionAndIndex.dimension.Title!, // use dimension title for key
            (object?)dimensionAndIndex.dimension[dimensionAndIndex.index].Title)) // use index title for value
        .Concat(new[] { KeyValuePair.Create("Value", (object?)row.GetValue()) })
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        .ToExpando())
    .Dump();

IEnumerable<IDictionary<string, object?>> GetTable<TIndex, T>(
    CubeResult<TIndex, T> cubeResult,
    Range rowDimensions,
    Func<CubeResult<TIndex, T>, IDictionary<string, object?>> getHeaderColumns,
    Func<CubeResult<TIndex, T>, IDictionary<string, object?>> getBodyColumns)
    where TIndex : notnull =>
    from row in cubeResult.BreakdownByDimensions(rowDimensions)
    select getHeaderColumns(row)
        .Concat(getBodyColumns(row)) // get body columns
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        .ToExpando();

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
    row => new Dictionary<string, object?> { ["Value"] = (object)row.GetValue() }).Dump();

GetTable(
    cube,
    ..^1, // ..^1 - build rows by range of all dimensions except last
    GetTableHeaderColumns,
    row => GetTableBodyColumns(row, ^1)).Dump(); // ^1 - build columns by last dimension
