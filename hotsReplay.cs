using System.Diagnostics;
using Heroes.StormReplayParser;

namespace HotsReplayReader
{
    internal class HotsReplay
    {
        public StormReplay? stormReplay;
        public IEnumerable<Heroes.StormReplayParser.Player.StormPlayer>? stormPlayers;
        public HotsReplay(string path)
        {
            if (!StormReplayParse(path))
                throw new InvalidOperationException($"File {path} could not be analysed.");
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
                    Debug.WriteLine($"Exception parsing replay: {hotsReplayResult.Exception?.Message}");
                }
                return false;
            }
        }

    }
}
