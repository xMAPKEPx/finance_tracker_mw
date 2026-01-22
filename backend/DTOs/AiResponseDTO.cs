namespace backend;

public class OllamaApiResponse
{
     public string Response { get; set; } = "";
}

public class CategorizationResult
{
     public List<CategorizedItem> Products { get; set; } = new();
}

public class CategorizedItem
{
     public string Name { get; set; } = "";
     public string Category { get; set; } = "";
}
