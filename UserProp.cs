using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Text.Json;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace DisBot
{
    [Serializable]
    [BsonIgnoreExtraElements]
    public class UserProp
    {
        public ulong UserId { get; set; }
        public uint Seconds { get; set; }
        public uint Minutes { get; set; }
        public uint Hours { get; set; }
        public List<GuildsInfo> GuildsInfos { get; set; }

        public UserProp (uint s, uint m, uint h, ulong u) { Seconds = s; Minutes = m; Hours = h; UserId = u; GuildsInfos = new List<GuildsInfo>(); }

        public UserProp() { }

        public void ReCalculate(DateTime Entry, DateTime Leave, DateTime? EntryServer, ulong guildId)
        {
            var Guild = GuildsInfos.Find(g => g.GuildId == guildId);
            if(Guild == null)
            {
                Guild = new GuildsInfo() { GuildId = guildId };
                GuildsInfos.Add(Guild);
            }
            var TimeInOnline = Leave - Entry;
            Hours += (uint)TimeInOnline.Hours + (uint)TimeInOnline.Days * 24;
            Minutes += (uint)TimeInOnline.Minutes;
            Seconds += (uint)TimeInOnline.Seconds;
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
            if (EntryServer != null && Guild != null)
            {
                var TimeInVoiceOnline = Leave - EntryServer;
                Guild.HoursOnServer += (uint)TimeInVoiceOnline.Value.Hours + (uint)TimeInOnline.Days * 24;
                Guild.MinutesOnServer += (uint)TimeInVoiceOnline.Value.Minutes;
                Guild.SecondsOnServer += (uint)TimeInVoiceOnline.Value.Seconds;
                while (Guild.SecondsOnServer >= 60)
                {
                    Guild.MinutesOnServer += 1;
                    Guild.SecondsOnServer -= 60;
                }
                while (Guild.MinutesOnServer >= 60)
                {
                    Guild.HoursOnServer += 1;
                    Guild.MinutesOnServer -= 60;
                }
                Guild.Exp += getExp(TimeInVoiceOnline.Value);
            }
        }

        public void ReCalculate(DateTime Entry, DateTime Leave, List<EventInGuild> inGuilds)
        {
            var TimeInOnline = Leave - Entry;
            Hours += (uint)TimeInOnline.Hours + (uint)TimeInOnline.Days * 24;
            Minutes += (uint)TimeInOnline.Minutes;
            Seconds += (uint)TimeInOnline.Seconds;
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
            foreach (var inGuild in inGuilds)
            {
                var Guild = GuildsInfos.Find((i) => i.GuildId == inGuild.GuildId);
                var EntryServer = inGuild.VoiceEntry;
                if (EntryServer != null && Guild != null)
                {
                    var TimeInVoiceOnline = Leave - EntryServer;
                    Guild.HoursOnServer += (uint)TimeInVoiceOnline.Value.Hours + (uint)TimeInOnline.Days * 24;
                    Guild.MinutesOnServer += (uint)TimeInVoiceOnline.Value.Minutes;
                    Guild.SecondsOnServer += (uint)TimeInVoiceOnline.Value.Seconds;
                    while (Guild.SecondsOnServer >= 60)
                    {
                        Guild.MinutesOnServer += 1;
                        Guild.SecondsOnServer -= 60;
                    }
                    while (Guild.MinutesOnServer >= 60)
                    {
                        Guild.HoursOnServer += 1;
                        Guild.MinutesOnServer -= 60;
                    }
                    Guild.Exp += getExp(TimeInVoiceOnline.Value);
                    Guild.Coin += getCoin(TimeInVoiceOnline.Value);
                }
                if (Guild == null)
                {
                    Guild = new GuildsInfo() { GuildId = inGuild.GuildId };
                    if (EntryServer != null)
                    {
                        var TimeInVoiceOnline = Leave - EntryServer;
                        Guild.HoursOnServer += (uint)TimeInVoiceOnline.Value.Hours + (uint)TimeInOnline.Days * 24;
                        Guild.MinutesOnServer += (uint)TimeInVoiceOnline.Value.Minutes;
                        Guild.SecondsOnServer += (uint)TimeInVoiceOnline.Value.Seconds;
                        while (Guild.SecondsOnServer >= 60)
                        {
                            Guild.MinutesOnServer += 1;
                            Guild.SecondsOnServer -= 60;
                        }
                        while (Guild.MinutesOnServer >= 60)
                        {
                            Guild.HoursOnServer += 1;
                            Guild.MinutesOnServer -= 60;
                        }
                        Guild.Exp += getExp(TimeInVoiceOnline.Value);
                        Guild.Coin += getCoin(TimeInVoiceOnline.Value);
                    }
                    GuildsInfos.Add(Guild);
                }
            }
        }

        private double getCoin(TimeSpan time)
        {
            //coin за минуту
            double coinPerMin = 0.1;
            return time.TotalMinutes * coinPerMin;
        }

        private uint getExp(TimeSpan time)
        {
            //exp за минуту
            double expPerMin = 1.2;
            return (uint)(time.TotalMinutes * expPerMin);
        }

        public override string ToString()
        {
            return Hours.ToString() + " часов, " + Minutes.ToString() + " минут, " + Seconds.ToString() + " секунд.";
        }

        public string ReturnYourOnline()
        {
            return Hours.ToString() + " часов, " + Minutes.ToString() + " минут, " + Seconds.ToString() + " секунд.";
        }

        public string ReturnYourVoiceOnline(ulong GuildId)
        {
            var Guild = GuildsInfos.Find(g => g.GuildId == GuildId);
            return Guild.HoursOnServer.ToString() + " часов, " + Guild.MinutesOnServer.ToString() + " минут, " + Guild.SecondsOnServer.ToString() + " секунд.";
        }
    }

    [Serializable]
    /// <summary>
    /// Информация по всем гильдиям, в которых состоит пользователь
    /// </summary>
    public class GuildsInfo
    {
        public ulong GuildId { get; set; }
        public uint HoursOnServer { get; set; }
        public uint MinutesOnServer { get; set; }
        public uint SecondsOnServer { get; set; }
        public uint Exp { get; set; }
        public double Coin { get; set; }
    }
}
