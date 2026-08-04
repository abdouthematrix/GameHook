using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace GameHook
{
    public static class Config
    {
        public static GameConfig Load()
        {
            string moduleDir = Path.GetDirectoryName(typeof(Config).Assembly.Location);
            string path = Path.Combine(moduleDir ?? ".", "ServerGameConfig.json");

            var cfg = new GameConfig();

            if (!File.Exists(path))
            {
                Log.Info("[Config] No config file found, using defaults.");
                return cfg;
            }

            try
            {
                JObject json = JObject.Parse(File.ReadAllText(path));

                cfg.ExpRewardTime = (int?)json["SlotMachine"]?["ExpRewardTime"] ?? cfg.ExpRewardTime;
                cfg.PrizeRewardMultiply = (int?)json["SlotMachine"]?["PrizeRewardMultiply"] ?? cfg.PrizeRewardMultiply;

                cfg.LockAlwaysSucceeds = (bool?)json["Items"]?["LockAlwaysSucceeds"] ?? cfg.LockAlwaysSucceeds;

                cfg.MeteorFirstSocket = (int?)json["Upgrade"]?["MeteorFirstSocket"] ?? cfg.MeteorFirstSocket;
                cfg.MeteorSecondSocket = (int?)json["Upgrade"]?["MeteorSecondSocket"] ?? cfg.MeteorSecondSocket;
                cfg.QualitySocket = (int?)json["Upgrade"]?["QualitySocket"] ?? cfg.QualitySocket;

                cfg.ForceMerchant = (bool?)json["Player"]?["ForceMerchant"] ?? cfg.ForceMerchant;

                cfg.BlowfishKey = (string?)json["Security"]?["BlowfishKey"] ?? cfg.BlowfishKey;

                cfg.IdentityCheckEnabled = (bool?)json["Identity"]?["EnableCheck"] ?? false;
                cfg.AllowedHwid = (string?)json["Identity"]?["Hwid"] ?? string.Empty;
                cfg.AllowedIp = (string?)json["Identity"]?["Ip"] ?? string.Empty;

                Log.Info(" ---- Game Server Config ----");
                Log.Info($"SlotMachine::ExpRewardTime = {cfg.ExpRewardTime}");
                Log.Info($"SlotMachine::PrizeRewardMultiply = {cfg.PrizeRewardMultiply}");
                Log.Info($"Upgrade::MeteorFirstSocket = {cfg.MeteorFirstSocket}");
                Log.Info($"Upgrade::MeteorSecondSocket = {cfg.MeteorSecondSocket}");
                Log.Info($"Upgrade::QualitySocket = {cfg.QualitySocket}");
                Log.Info($"Player::ForceMerchant = {cfg.ForceMerchant}");
                Log.Info($"Items::LockAlwaysSucceeds = {cfg.LockAlwaysSucceeds}");
                Log.Info($"Security::BlowfishKey = {cfg.BlowfishKey}");
                Log.Info($"Identity::EnableCheck = {cfg.IdentityCheckEnabled}");
                Log.Info(" ----------------------------");
            }
            catch (Exception ex)
            {
                Log.Info($"[Config] Failed to parse config: {ex.Message}");
            }

            return cfg;
        }
    }
}
