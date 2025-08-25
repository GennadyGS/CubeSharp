<Query Kind="Statements">
  <Reference Relative="..\..\src\Cube\bin\Debug\netcoreapp3.1\WizNG.Analytics.Cube.dll"/>
  <Namespace>WizNG.Analytics.Cube</Namespace>
  <Namespace>System.Dynamic</Namespace>
</Query>

#load "Utils.linq"

var orders = new [] {
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
    new { OrderDate = new DateTime(2009, 09, 07), Product = (string)null, EmployeeId = 3, CustomerId = "D", Quantity = 30m, Tags = new [] { "Discount", "New", "BestSeller" } },
};

"One-dimensional report (with one row), calculating total quantity for each customer".Dump();
var cube1d = 
    orders
        .BuildCube(
            AggregationDefinition.CreateForCollection(
                orders,
                order => order.Quantity,
                (a, b) => a + b,
                0),
            DimensionDefinition.CreateForCollection(
                orders,
                order => order.CustomerId,
                null,
                IndexDefinition.Create("A"),
                IndexDefinition.Create("B"),
                IndexDefinition.Create("C"),
                IndexDefinition.Create("D")));
new
{
    A = cube1d.GetValue("A"),
    B = cube1d.GetValue("B"),
    C = cube1d.GetValue("C"),
    D = cube1d.GetValue("D"),
}.Dump();

"Two-dimensional report, calculating total quantity for each year for each customer".Dump();
orders
    .BuildCube(
        AggregationDefinition.CreateForCollection(
            orders,
            order => order.Quantity,
            (a, b) => a + b,
            0),
        DimensionDefinition.CreateForCollection(
            orders,
            order => order.CustomerId,
            null,
            IndexDefinition.Create("A"),
            IndexDefinition.Create("B"),
            IndexDefinition.Create("C"),
            IndexDefinition.Create("D"))
                .WithTrailingDefaultIndex("Total"),
        DimensionDefinition.CreateForCollection(
            orders,
            order => order.OrderDate.Year.ToString(),
            null,
            IndexDefinition.Create("2007"),
            IndexDefinition.Create("2008"),
            IndexDefinition.Create("2009")))
    .BreakdownByDimensions(0)
    .Select(row =>
        new
        {
            CustomerId = row.GetBoundIndexDefinition(0).Title ?? row.GetBoundIndexDefinition(0).Value,
            Y2007 = row.GetValue("2007"),
            Y2008 = row.GetValue("2008"),
            Y2009 = row.GetValue("2009"),
            Total = row.GetValue((string)default),
        })
    .Dump();

"Two-dimensional report, calculating total quantity for each tag (with multi-selection) for each year".Dump();
orders
    .BuildCube(
        AggregationDefinition.CreateForCollection(
            orders,
            order => order.Quantity,
            (a, b) => a + b,
            0),
        DimensionDefinition.CreateForCollection(
            orders,
            order => order.OrderDate.Year.ToString(),
            "Year",
            IndexDefinition.Create("2007"),
            IndexDefinition.Create("2008"),
            IndexDefinition.Create("2009"))
            .WithTrailingDefaultIndex("Total"),
        DimensionDefinition.CreateForCollectionWithMultiSelector(
            orders,
            order => order.Tags,
            null,
            IndexDefinition.Create("Discount"),
            IndexDefinition.Create("Retail"),
            IndexDefinition.Create("New"),
            IndexDefinition.Create("Season"),
            IndexDefinition.Create("BestSeller")))
    .BreakdownByDimensions(0)
    .Select(row =>
        new
        {
            Year = row.GetBoundIndexDefinition(0).Title ?? row.GetBoundIndexDefinition(0).Value,
            Discount = row.GetValue("Discount"),
            Retail = row.GetValue("Retail"),
            New = row.GetValue("New"),
            Season = row.GetValue("Season"),
            BestSeller = row.GetValue("BestSeller"),
        })
    .Dump();

"Three-dimensional report".Dump();
orders
    .BuildCube(
        AggregationDefinition.CreateForCollection(
            orders, order => order.Quantity, (a, b) => a + b, 0),
        DimensionDefinition.CreateForCollection(
            orders,
            order => order.EmployeeId.ToString(),
            "EmployeeId",
            IndexDefinition.Create("1"),
            IndexDefinition.Create("2"),
            IndexDefinition.Create("3"))
            .WithTrailingDefaultIndex("Total"),
        DimensionDefinition.CreateForCollection(
            orders,
            order => order.CustomerId,
            "CustomerId",
            IndexDefinition.Create("A"),
            IndexDefinition.Create("B"),
            IndexDefinition.Create("C"),
            IndexDefinition.Create("D"))
            .WithTrailingDefaultIndex("Total"),
        DimensionDefinition.CreateForCollection(
            orders,
            order => order.OrderDate.Year.ToString(),
            "Year",
            IndexDefinition.Create("2007"),
            IndexDefinition.Create("2008"),
            IndexDefinition.Create("2009"))
            .WithTrailingDefaultIndex("Total"))
    .ToTable(new Index[] { 0, 1 }, new Index[] { 2 })
    .Dump();

Array.Empty<string>()
    .BuildCube<string, string, string>(
        AggregationDefinition.Create((string x) => x, (a, b) => default, "Hello world"))
    .GetValue()
    .Dump();
    
Enumerable.Range(1, 10)
    .BuildCube<int, int, int>(
        AggregationDefinition.Create((int i) => i, (a, b) => a + b, 0))
    .GetValue()
    .Dump("Sum of numbers from 1 to 10");
    
Enumerable.Range(0, 10)
    .BuildCube<int, int, int[]>(
        AggregationDefinition.Create(
            (int i) => new[] { i }, 
            (a, b) => a.Concat(b).ToArray(), 
            Array.Empty<int>()))
    .GetValue()
    .Dump("All values as array");

var mod3Cube = 
    Enumerable.Range(0, 10)
        .BuildCube(
            AggregationDefinition.Create(
                (int i) => new[] { i },
                (a, b) => a.Concat(b).ToArray(),
                Array.Empty<int>()),
            DimensionDefinition.Create(
                (int i) => (int?)(i % 3),
                "Mod 3",
                Enumerable.Range(0, 3)
                    .Select(i => IndexDefinition.Create((int?)i)).ToArray()));

mod3Cube.GetValue().Dump("x % 3 = *");
mod3Cube.GetValue(0).Dump("x % 3 = 0");
mod3Cube.GetValue(1).Dump("x % 3 = 1");
mod3Cube.GetValue(2).Dump("x % 3 = 2");

DimensionDefinition<int, int?> CreateModulusDimension(int modulusBase) =>
    DimensionDefinition.Create(
        (int i) => (int?)(i % modulusBase),
        $"Mod {modulusBase}",
        Enumerable.Range(0, modulusBase)
            .Select(i => IndexDefinition.Create((int?)i)).ToArray());

var mod3and5Cube =
    Enumerable.Range(0, 20)
        .BuildCube(
            AggregationDefinition.Create(
                (int i) => new[] { i },
                (a, b) => a.Concat(b).ToArray(),
                Array.Empty<int>()),
            CreateModulusDimension(3),
            CreateModulusDimension(5));

mod3and5Cube.GetValue(0).Dump("x % 3 = 0");
mod3and5Cube.GetValue(null, 0).Dump("x % 5 = 0");
mod3and5Cube.GetValue(0, 0).Dump("(x % 3 = 0) & (x % 5 = 0)");

mod3and5Cube
    .ToTable(new Index[] { 0, 1 }, new Index[] { })
    .Dump();
