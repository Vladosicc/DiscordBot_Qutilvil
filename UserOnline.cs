using DSharpPlus;
using DSharpPlus.Entities;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace DisBot
{
    [BsonIgnoreExtraElements]
    public class UserOnline
    {
        public ulong UserId { get; set; }
        public DateTime Entry { get; set; }
        public List<EventInGuild> inGuilds { get; set; }

        public UserOnline() { inGuilds = new List<EventInGuild>(); }
    }

    [BsonIgnoreExtraElements]
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
            if (TimeInOnline.HasValue)
                return (int)(TimeInOnline.Value.TotalMinutes * 1.2);
            else
                return 0;
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

    public class _guildforapi
    {
        public ulong GuildId { get; set; }
        public string BannerUrl { get; set; }
        public string Name { get; set; }
        public int CountMembers { get; set; }
        public _forApiLabHelp_deletethis Owner { get; set; }
        public List<_forApiLabHelp_deletethis> Users { get; set; }
        public List<_roleforapi> Roles { get; set; }
    }

    public class _roleforapi
    {
        public string Name { get; set; }
        public DiscordColor Color { get; set; }
    }

    public class _forApiLabHelp_deletethis2
    {
        public int CountGuilds { get; set; }
        public List<ulong> IdGuilds { get; set; }
        public List<_guildforapi> Guilds { get; set; }

        public static _forApiLabHelp_deletethis2 Create(DiscordClient client)
        {
            _forApiLabHelp_deletethis2 res = new _forApiLabHelp_deletethis2();
            res.Guilds = new List<_guildforapi>();
            res.IdGuilds = new List<ulong>();
            foreach(var id in client.Guilds.Keys)
            {
                res.IdGuilds.Add(id);
            }
            res.CountGuilds = client.Guilds.Count;
            int i = 0;
            foreach(var guild in client.Guilds.Values)
            {
                _guildforapi g = new _guildforapi();
                g.BannerUrl = guild.IconUrl;
                g.CountMembers = guild.MemberCount;
                g.Name = guild.Name;
                g.GuildId = guild.Id;
                g.Roles = new List<_roleforapi>();
                g.Users = new List<_forApiLabHelp_deletethis>();

                _forApiLabHelp_deletethis own = new _forApiLabHelp_deletethis();
                own.AvatarUrl = guild.Owner.AvatarUrl;
                own.GuildName = guild.Owner.DisplayName;
                own.IsBot = guild.Owner.IsBot;
                own.NickName = guild.Owner.Nickname;
                own.UserId = guild.Owner.Id;
                g.Owner = own;
                res.Guilds.Add(g);

                foreach (var user in guild.Members.Values)
                {
                    _forApiLabHelp_deletethis tmp = new _forApiLabHelp_deletethis();
                    tmp.AvatarUrl = user.AvatarUrl;
                    tmp.GuildName = user.DisplayName;
                    tmp.NickName = user.Nickname;
                    tmp.IsBot = user.IsBot;
                    tmp.UserId = user.Id;
                    res.Guilds[i].Users.Add(tmp);
                }
                foreach (var role in guild.Roles.Values)
                {
                    _roleforapi tmp = new _roleforapi();
                    tmp.Color = role.Color;
                    tmp.Name = role.Name;
                    res.Guilds[i].Roles.Add(tmp);
                }
                i++;
            }
            return res;
        }
    }
}
