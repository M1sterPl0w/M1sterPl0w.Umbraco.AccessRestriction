namespace M1sterPl0w.Umbraco.AccessRestriction
{
    public class Constants
    {
        public const string ApiName = "m1sterpl0wumbracoaccessrestriction";

        /// <summary>HttpContext.Items key under which the resolved client IP is stored by the middleware.</summary>
        public const string ClientIpItemKey = "AccessRestriction.ClientIp";

        public static class CacheKeys
        {
            public const string Settings = "AccessRestriction.Settings";
            public const string Rules    = "AccessRestriction.Rules";
        }
    }
}
