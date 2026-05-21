namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class CreateConditionRequest
    {
        public string Type { get; set; } = string.Empty;
     
        public List<string> Values { get; set; } = [];
    }
}
