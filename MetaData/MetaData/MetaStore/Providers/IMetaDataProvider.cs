using System;
using System.Collections.Generic;
using MetaData.Settings;

namespace MetaData.MetaData.MetaStore.Providers
{
    /// <summary>
    /// Interfaced used to implement providers for MetadataSnapshots
    /// </summary>
    public interface IMetaDataProvider
    {
        // Provide latest version of snapshot
        MetaDataSnapshot GetLatest(EDSSettings eds);

        // Provide snapshot at dateTime
        // TODO: We might want to make it fuzzy to provide better retrieval option
        MetaDataSnapshot GetSnapshot(EDSSettings eds, DateTime dateTime);

        // Provide all snapshots
        List<MetaDataSnapshot> GetAllSnapshots(EDSSettings eds);

        List<MetaDataSnapshot> GetSnapshotsBefore(EDSSettings eds, DateTime dateTime);

        void SaveSnapshot(EDSSettings eds, MetaDataSnapshot metaDataSnapshot);
    }
}
