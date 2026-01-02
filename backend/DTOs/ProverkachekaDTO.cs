
public class ProverkachekaRoot
{
    public int Code { get; set; }
    public ProverkachekaData Data { get; set; } = null!;
}

public class ProverkachekaData
{
    public ProverkachekaJson Json { get; set; } = null!;
}

public class ProverkachekaJson
{
    public string User { get; set; } = null!;
    public List<ProverkachekaItem> Items { get; set; } = new();

    public long TotalSum { get; set; }               // 34993
    public string DateTime { get; set; } = null!;    // "2020-09-24T18:37:00"
    public string RetailPlaceAddress { get; set; } = null!;
}

public class ProverkachekaItem
{
    public long Sum { get; set; }        // копейки
    public string Name { get; set; } = null!;
    public long Price { get; set; }      // копейки
    public decimal Quantity { get; set; }
}
