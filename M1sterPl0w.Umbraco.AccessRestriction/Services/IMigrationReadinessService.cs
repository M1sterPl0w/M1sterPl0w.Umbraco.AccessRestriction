namespace M1sterPl0w.Umbraco.AccessRestriction.Services
{
    public interface IMigrationReadinessService
    {
        bool IsReady { get; }
        void MarkReady();
    }

    public class MigrationReadinessService : IMigrationReadinessService
    {
        private volatile bool _isReady;

        public bool IsReady => _isReady;

        public void MarkReady() => _isReady = true;
    }
}
