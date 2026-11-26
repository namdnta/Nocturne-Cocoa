namespace NocturneCocoa.SaveSystem
{
    /// <summary>
    /// Version handling for save files. Upgrades older saves one version at a time.
    ///
    /// How to change the save format later:
    /// 1. Change SaveData.
    /// 2. Increase CurrentVersion (e.g. to 2).
    /// 3. Add a "case 1:" below that converts version-1 data into version-2 data.
    ///
    /// Note: JsonUtility drops fields it doesn't recognise, so for a rename you may need to read
    /// the old value from the raw JSON. Cross that bridge when a real migration is needed.
    /// </summary>
    public static class SaveMigrator
    {
        public const int CurrentVersion = 1;

        public static bool TryMigrate(SaveData data, out string error)
        {
            if (data.saveVersion > CurrentVersion)
            {
                error = $"Save was made by a newer version of the game (save v{data.saveVersion}, game v{CurrentVersion}).";
                return false;
            }

            while (data.saveVersion < CurrentVersion)
            {
                switch (data.saveVersion)
                {
                    // case 1:
                    //     MigrateV1ToV2(data);
                    //     break;

                    default:
                        error = $"No migration path from save v{data.saveVersion}.";
                        return false;
                }

                // data.saveVersion++;  // uncomment together with the first migration case
            }

            error = null;
            return true;
        }
    }
}
