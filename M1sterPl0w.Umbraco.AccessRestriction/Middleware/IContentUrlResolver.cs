namespace M1sterPl0w.Umbraco.AccessRestriction.Middleware
{
    public interface IContentUrlResolver
    {
        string? GetUrl(Guid contentKey);
    }
}
