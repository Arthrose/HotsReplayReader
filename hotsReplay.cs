using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Heroes.StormReplayParser;
using Heroes.StormReplayParser.Player;

namespace HotsReplayReader
{
    internal class hotsReplay
    {
        public StormReplay stormReplay;
        public IEnumerable<Heroes.StormReplayParser.Player.StormPlayer> stormPlayers;
        public hotsReplay(string path)
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
