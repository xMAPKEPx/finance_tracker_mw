

public class UserCategory
{
        public int Id { get; set; }
        public int UserId { get; set; }      // внешний ключ на User
        public string Name { get; set; } = "";
        public bool IsDefault { get; set; }      // системная/обязательная (например, "другое")
        public string? Color { get; set; }   // опционально для фронта
}

public class DefaultCategories
{
        public static readonly string[] DefaultCategoryNames =
        {
                "еда", "транспорт", "развлечения", "одежда", "медицина", "быт", "другое"
        };
}
