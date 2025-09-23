using Heroes.StormReplayParser;

namespace HotsReplayReader
{
    internal class HotsReplay
    {
        public StormReplay stormReplay;
        public IEnumerable<Heroes.StormReplayParser.Player.StormPlayer> stormPlayers;
        public HotsReplay(string path)
        {
            StormReplayParse(path);
        }
        private bool StormReplayParse(string hotsReplayFilePath)
        {
            StormReplayResult? hotsReplayResult = StormReplay.Parse(hotsReplayFilePath);
            StormReplayParseStatus hotsReplayStatus = hotsReplayResult.Status;

            if (hotsReplayStatus == StormReplayParseStatus.Success)
            {
                stormReplay = hotsReplayResult.Replay;
                stormPlayers = stormReplay.StormPlayers;
                return true;
            }
            else
            {
                if (hotsReplayStatus == StormReplayParseStatus.Exception)
                {
                    StormParseException? hotsParseException = hotsReplayResult.Exception;
                }
                return false;
            }
        }

    }
}
