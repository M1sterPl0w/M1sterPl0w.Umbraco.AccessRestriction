namespace M1sterPl0w.Umbraco.AccessRestriction.Middleware
{
    /// <summary>Resolves the public URL for an Umbraco content node by its key.</summary>
    public interface IContentUrlResolver
    {
        /// <summary>Returns the URL for the given content node key, or <c>null</c> if it cannot be resolved.</summary>
        string? GetUrl(Guid contentKey);
    }
}
