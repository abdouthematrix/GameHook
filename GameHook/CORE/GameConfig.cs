namespace GameHook
{
    public class GameConfig
    {
        // [SlotMachine]
        public int ExpRewardTime { get; set; } = 100;
        public int PrizeRewardMultiply { get; set; } = 300;

        // [Items]
        public bool LockAlwaysSucceeds { get; set; } = false;

        // [Upgrade]
        public int MeteorFirstSocket { get; set; } = 1200;
        public int MeteorSecondSocket { get; set; } = 2400;
        public int QualitySocket { get; set; } = 300;

        // [Player]
        public bool ForceMerchant { get; set; } = false;

        // [Security]
        public string BlowfishKey { get; set; } = "C238xs65pjy7HU9Q";

        // [Identity]
        public bool IdentityCheckEnabled { get; set; } = false;
        public string AllowedHwid { get; set; } = string.Empty;
        public string AllowedIp { get; set; } = string.Empty;
    }
}
