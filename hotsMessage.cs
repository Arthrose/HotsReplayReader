namespace HotsReplayReader
{
    internal class hotsMessage
    {
        public string Message { get; set; }
        public HotsPlayer HotsPlayer { get; set; }
        public double TotalMilliseconds { get; set; }
        public string Hours { get; set; }
        public String Minutes { get; set; }
        public string Seconds { get; set; }
        public string MilliSeconds { get; set; }
        public bool translate { get; set; }
        public hotsMessage(HotsPlayer HotsPlayer, TimeSpan? TimeStamp, string Message, bool translate = true)
        {
            this.HotsPlayer = HotsPlayer;
            this.TotalMilliseconds = TimeStamp.Value.TotalMilliseconds;
            this.Message = Message;
            this.translate = translate;

            this.Hours = (TimeStamp.Value.Hours < 10 ? "0" + TimeStamp.Value.Hours.ToString() : TimeStamp.Value.Hours.ToString());
            this.Minutes = (TimeStamp.Value.Minutes < 10 ? "0" + TimeStamp.Value.Minutes.ToString() : TimeStamp.Value.Minutes.ToString());
            this.Seconds = (TimeStamp.Value.Seconds < 10 ? "0" + TimeStamp.Value.Seconds.ToString() : TimeStamp.Value.Seconds.ToString());

            this.MilliSeconds =
            (
                TimeStamp.Value.Milliseconds < 10 ?
                    TimeStamp.Value.Milliseconds.ToString() + "00"
                    :
                    (
                        TimeStamp.Value.Milliseconds < 100 ?
                            TimeStamp.Value.Milliseconds.ToString() + "0"
                        :
                            TimeStamp.Value.Milliseconds.ToString()
                    )
            );

            /*
                        if (TimeStamp.Value.Milliseconds < 10)
                        {
                            this.MilliSeconds = TimeStamp.Value.Milliseconds.ToString() + "00";
                        }
                        else
                        {
                            if (TimeStamp.Value.Milliseconds < 100)
                            {
                                this.MilliSeconds = TimeStamp.Value.Milliseconds.ToString() + "0";
                            }
                            else
                            {
                                this.MilliSeconds = TimeStamp.Value.Milliseconds.ToString();
                            }
                        }
            */
        }
    }
}
