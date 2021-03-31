using System;
using System.Collections.Generic;
using System.Text;

namespace DisBot
{
    public class UserOnline
    {
        public ulong UserId { get; set; }
        public DateTime Entry { get; set; }
        public List<EventInGuild> inGuilds { get; set; }

        public UserOnline() { inGuilds = new List<EventInGuild>(); }
    }

    public class EventInGuild
    {
        public ulong GuildId { get; set; }
        public DateTime? VoiceEntry { get; set; }
        public uint Seconds { get; set; }
        public uint Minutes { get; set; }
        public uint Hours { get; set; }

        public int Exp()
        {
            var TimeInOnline = DateTime.UtcNow - VoiceEntry; 
            return (int)(TimeInOnline.Value.TotalMinutes * 1.2);
        }

        public void ReCalc()
        {
            if (VoiceEntry.HasValue == false)
                return;
            var TimeInOnline = DateTime.UtcNow - VoiceEntry;
            Hours += (uint)TimeInOnline.Value.Hours + (uint)TimeInOnline.Value.Days * 24;
            Minutes += (uint)TimeInOnline.Value.Minutes;
            Seconds += (uint)TimeInOnline.Value.Seconds;
            while (Seconds >= 60)
            {
                Minutes += 1;
                Seconds -= 60;
            }
            while (Minutes >= 60)
            {
                Hours += 1;
                Minutes -= 60;
            }
        }
    }

    public class _forApiLabHelp_deletethis
    {
        public ulong UserId { get; set; }
        public string NickName { get; set; }
        public string GuildName { get; set; }
        public bool IsBot { get; set; }
        public string AvatarUrl { get; set; }

    }
}
