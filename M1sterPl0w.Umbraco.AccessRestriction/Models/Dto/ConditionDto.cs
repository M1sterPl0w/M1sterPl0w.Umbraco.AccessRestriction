namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class ConditionDto
    {
        public int Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public List<string> Values { get; set; } = [];

        public bool CanDelete { get; set; } = true;
    }
}
