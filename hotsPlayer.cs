using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotsReplayReader
{
    internal class hotsPlayer
    {
        public string BattleTag { get; set; }
        public string Party { get; set; }
        public string teamColor { get; set; }
        public double mvpScore { get; set; }
        public hotsTeam Team { get; set; }
        public hotsTeam otherTeam { get; set; }
    }
}
