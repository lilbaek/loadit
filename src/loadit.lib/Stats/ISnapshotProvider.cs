namespace Loadit.Stats
{
    public interface ISnapshotProvider
    {
        /// <summary>
        /// Should return all snapshots collected since the last call
        /// </summary>
        /// <returns></returns>
        internal Snapshot[] DeltaSnapshot();
    }
}