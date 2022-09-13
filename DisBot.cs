using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YandexDisk.Client;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;

using ParseInstagram;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DisBot
{
    class DisBotMain
    {
        static ulong ChannelLogId = 799287181508083724; //В какой текстовый канал пишутся логи
        private static SignalHandler signalHandler;
        static Config conf = new Config();

        static List<UserOnline> UsersOnline = new List<UserOnline>(); //Все пользователи, находящиеся online
        static List<IdUserIdGuild> IdUsersRainbow = new List<IdUserIdGuild>(); //Пользователи с градиентным цветом роли
        static MemoryCache InstagramCache = new MemoryCache(new MemoryCacheOptions());

        static DiscordClient discord;
        static IDiskApi disk;
        static YouTubeService youtubeService;
        public static IMongoDBWorker mongoDB;

        static int CountGuild = 0;

        static readonly string SERVER_USER_ONLINE = "!!serveronline";
        static readonly string USER_ONLINE = "!!online";
        static readonly string CREATE_PLAYLIST = "!!clist";
        static readonly string ADD_PLAYLIST = "!!alist";
        static readonly string PLAY_PLAYLIST = "!!plist";
        static readonly string HELP = "!!help";
        static readonly string RESTART = "!!restart";
        static readonly string RAINBOW = "!!rainbow";
        static readonly string CHANGEROLE = "!!changerole";
        static readonly string CHANGECOLOR = "!!changecolor";
        static readonly string START_DATA = "#date_bot";
        static readonly string DONTWORK = "(Не работает)";

        static readonly Random rand = new Random((int)DateTime.UtcNow.Ticks);

        static void Main(string[] args)
        {
            MainTask(args).ConfigureAwait(false).GetAwaiter().GetResult();

            int i = 0;
            Console.WriteLine(i.ToString());
        }

        private static void HandleConsoleSignal(ConsoleSignal consoleSignal)
        {
            //using (Stream SourceStream = (JsonConvert.SerializeObject(UsersOnline)).ToStream())
            //{
            //    using (IDiskApi disk = new DiskHttpApi(ydtoken))
            //    {
            //        await disk.Files.UploadFileAsync("/DisBot/" + "BackUpProfile.json", false, SourceStream);
            //        Console.Write("-BackUp-");
            //    }
            //}

            //using (StreamWriter fs = new StreamWriter("BackUpProfile.json", false, System.Text.Encoding.Default))
            //{            
            //    fs.WriteLine(JsonConvert.SerializeObject(UsersOnline));
            //    Console.Write("-BackUp-");
            //}

            try
            {
                IMongoDBWorker IMongoDBWorker = new MongoDBWorker(conf.MongoDBConnectString);
                IMongoDBWorker.AddData<DateTimeClass>(new DateTimeClass() { dateTime = DateTime.UtcNow }, "DisBot", "DateTimeBackUp");
                IMongoDBWorker.AddData<UserOnline>(UsersOnline, "DisBot", "BackUp");
                Console.WriteLine("xfcgvhjzdgfxgfghij");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //string connection = @"Data Source=.\SQLEXPRESS; Initial catalog=test1; Integrated Security=True";
            //try
            //{
            //    using (SqlConnection sqlConnection = new SqlConnection(connection))
            //    {
            //        using (var stringwriter = new System.IO.StringWriter())
            //        {
            //            var serializer = new XmlSerializer(UsersOnline.GetType());
            //            serializer.Serialize(stringwriter, UsersOnline);
            //            sqlConnection.Open();
            //            string command = "INSERT INTO " + "BackUpDB" + " (DateExit, xml_file) " + "VALUES (" + DateTime.UtcNow + ", " + stringwriter.ToString() + ")";
            //            using (SqlCommand sqlCommand = new SqlCommand(command, sqlConnection))
            //            {
            //                sqlCommand.ExecuteNonQuery();
            //            }
            //            sqlConnection.Close();
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
            //return;

        }

        static async Task MainTask(string[] args)
        {
            Authorization();
            mongoDB = new MongoDBWorker(conf.MongoDBConnectString);
            ExitBackup();

            //Сигнал закрытия консоли
            signalHandler += HandleConsoleSignal;
            ConsoleHelper.SetSignalHandler(signalHandler, true);

            await discord.ConnectAsync();

            discord.Heartbeated += Heartbeated;
            discord.Heartbeated += ChangeRoleColor;

            discord.SocketClosed += Discord_SocketClosed;

            discord.Resumed += Discord_Resumed;

            discord.SocketErrored += Discord_SocketErrored;

            discord.VoiceStateUpdated += Discord_VoiceStateUpdated;

            discord.MessageReactionAdded += Discord_MessageReactionAdded;

            discord.MessageReactionRemoved += Discord_MessageReactionRemoved;

            discord.GuildDownloadCompleted += Discord_GuildDownloadCompleted;

            discord.MessageCreated += async (sender, e) =>
            {
                string message = e.Message.Content;

                if (e.Author.Id == e.Guild.OwnerId) //админские команды
                {
                    if (message.ToLower().StartsWith(RESTART))
                    {
                        await e.Message.RespondAsync("```css\nDisconnect" + "```");
                        var UserClone = new UserOnline[UsersOnline.Count];
                        UsersOnline.CopyTo(UserClone);
                        await Backup(UserClone);
                        UsersOnline.Clear();
                        CountGuild = 0;
                        return;
                        //await e.Message.RespondAsync("```css\nYou don't have permission" + "```");
                    }

                    if (message.ToLower().StartsWith("!!adminhelp"))
                    {
                        string helpMsg = e.Author.Mention + "```css\n" +
                                "!!restart - выключить бота\n" +
                                "!!privacyrole [@role] - запретить изменение этой роли\n" +
                                "!!openrole [@role] - разрешить изменение этой роли\n" +
                                "!!commonrole [@role] - Запретить упоминать роль не имея роли\n" +
                                "```";
                        await e.Message.RespondAsync(helpMsg);
                        return;
                    }

                    if (message.ToLower().StartsWith("!!privacyrole"))
                    {
                        ulong RoleId;
                        foreach (var item in (await discord.GetGuildAsync(e.Guild.Id)).Roles)
                        {
                            if (item.Value.Mention == message[14..])
                            {
                                RoleId = item.Value.Id;
                                await e.Message.RespondAsync("```css\nprivacy = " + item.Value.Name + "```");
                                await PushPrivacyRoles(e.Guild.Id, RoleId);
                            }
                        }
                        return;
                    }

                    if (message.ToLower().StartsWith("!!openrole"))
                    {
                        ulong RoleId;
                        foreach (var item in (await discord.GetGuildAsync(e.Guild.Id)).Roles)
                        {
                            if (item.Value.Mention == message[11..])
                            {
                                RoleId = item.Value.Id;
                                await e.Message.RespondAsync("```css\nopen = " + item.Value.Name + "```");
                                await OpenRole(e.Guild.Id, RoleId);
                            }
                        }
                        return;
                    }

                    if (message.ToLower().StartsWith("!!start"))
                    {
                        await discord.ConnectAsync();
                    }

                    if (message.ToLower().StartsWith("!!commonrole"))
                    {
                        ulong RoleId;
                        foreach (var item in (await discord.GetGuildAsync(e.Guild.Id)).Roles)
                        {
                            if (item.Value.Mention == message[13..])
                            {
                                RoleId = item.Value.Id;
                                await e.Message.RespondAsync("```css\ncommon = " + item.Value.Name + "```");
                                await PushCommonRoles(e.Guild.Id, RoleId);
                            }
                        }
                        return;
                    }

                    if (message.ToLower().StartsWith("!!list"))
                    {
                        string Out = "";
                        int count = 0;
                        foreach (var item in UsersOnline)
                        {
                            count++;
                            Out += count.ToString() + ". " + (await e.Guild.GetMemberAsync(item.UserId)).DisplayName + " - " + item.Entry + "\n";
                        }
                        await e.Message.RespondAsync(Out);
                        return;
                    }

                    if (message.ToLower().StartsWith("!!stankin"))
                    {
                        foreach (var item in sender.Guilds.Values)
                        {
                            _forApiLabHelp_deletethis[] _ForApis = new _forApiLabHelp_deletethis[item.Members.Count];
                            int i = 0;
                            foreach (var us in item.Members.Values)
                            {
                                _ForApis[i] = new _forApiLabHelp_deletethis() { UserId = us.Id, AvatarUrl = us.AvatarUrl, GuildName = us.DisplayName, NickName = us.Username, IsBot = us.IsBot };
                                i++;
                            }
                            using (StreamWriter fs = new StreamWriter("fromBotforLab" + item.Id + ".json", false, System.Text.Encoding.Default))
                            {
                                await fs.WriteLineAsync(JsonConvert.SerializeObject(_ForApis));
                            }
                        }
                    }

                    if (message.ToLower().StartsWith("!!stankin2"))
                    {
                        var EntityForJson = _forApiLabHelp_deletethis2.Create(sender);
                        using (StreamWriter fs = new StreamWriter("ForONIT.json", false, System.Text.Encoding.Default))
                        {
                            await fs.WriteLineAsync(JsonConvert.SerializeObject(EntityForJson));
                        }
                    }
                }

                if (message.StartsWith("!!"))
                {
                    if (message.ToLower().StartsWith(HELP))
                    {
                        string helpMsg = e.Author.Mention + "```css\n[] - обязательный параметр\n" + START_DATA +
                                "!!serveronline (so) - показывает ваш онлайн на сервере с ...\n" +
                                "!!online (o) - показывает ваш онлайн в дискорде с ...\n" +
                                DONTWORK + "!!clist [name_playlist] - создать плейлист\n" +
                                DONTWORK + "!!alist [name_playlist] [source] - добавить в плейлист\n" +
                                DONTWORK + "!!plist [name_playlist] - воспроизведение плейлиста\n" +
                                "!!changerole [new_name] - изменить роль\n" +
                                "!!changecolor [#color] - изменить цвет\n" +
                                "!!search [search] - поиск в Youtube\n" +
                                "!!playlist [path] - плейлист в Youtube (ссылка)\n" +
                                "!!dateentry - первое появление на сервере\n" +
                                "!!ava - аватарка?\n" +
                                "!!gif [req] - гифка\n" +
                                "!!inst [username] - рандомная из 12 первых фоток\n" +
                                "!!stats - информация" +
                                "```";
                        await e.Message.RespondAsync(helpMsg);
                        return;
                    }

                    if (message.ToLower().StartsWith("!!stats"))
                    {
                        RespondBitmap(e.Message);
                        return;
                    }

                    if (message.ToLower().StartsWith("!!bibametr"))
                    {
                        await e.Message.RespondAsync(e.Author.Mention + "```css\nТвоя биба " + rand.Next(0, 100) + " см" + "```");
                        return;
                    }

                    if (message.ToLower().StartsWith(USER_ONLINE) || message.ToLower().StartsWith("!!o"))
                    {
                        UserProp User;
                        using (StreamReader fs = new StreamReader("user" + e.Author.Id.ToString() + ".json"))
                        {
                            User = JsonConvert.DeserializeObject<UserProp>(await fs.ReadToEndAsync());
                            var tmp = UsersOnline.Find(u => u.UserId == e.Author.Id);
                            if (tmp != null)
                            {
                                User.ReCalculate(tmp.Entry, DateTime.UtcNow, tmp.inGuilds.Find(g => g.GuildId == e.Guild.Id).VoiceEntry, e.Guild.Id);
                            }
                        }
                        string str = e.Author.Mention + "```css\nТвой онлайн в discord с " + START_DATA + " уже " + User.ToString() + "```";
                        await e.Message.RespondAsync(str);
                        return;
                    }

                    if (message.ToLower().StartsWith(SERVER_USER_ONLINE) || message.ToLower().StartsWith("!!so"))
                    {
                        UserProp User;
                        using (StreamReader fs = new StreamReader("user" + e.Author.Id.ToString() + ".json"))
                        {
                            User = JsonConvert.DeserializeObject<UserProp>(await fs.ReadToEndAsync());
                            var tmp = UsersOnline.Find(u => u.UserId == e.Author.Id);
                            if (tmp != null)
                            {
                                User.ReCalculate(tmp.Entry, DateTime.UtcNow, tmp.inGuilds.Find(g => g.GuildId == e.Guild.Id).VoiceEntry, e.Guild.Id);
                            }
                        }
                        string str = e.Author.Mention + "```css\nТвой онлайн на сервере [" + e.Guild.Name + "] с " + START_DATA + " уже " + User.ReturnYourVoiceOnline(e.Guild.Id) + "```";
                        await e.Message.RespondAsync(str);
                        return;
                    }

                    if (message.ToLower().StartsWith(RAINBOW))
                    {
                        if (e.Author.Id == e.Guild.Owner.Id)
                        {
                            await e.Message.RespondAsync("```css\nMAGICCCC!!!!" + "```");
                            IdUsersRainbow.Add(new IdUserIdGuild() { IdUser = e.Author.Id, IdGuild = e.Guild.Id });
                        }
                        else
                        {
                            await e.Message.RespondAsync("```css\nYou don't have permission" + "```");
                        }
                        return;
                    }

                    if (message.ToLower().StartsWith(CHANGEROLE))
                    {
                        var role = GetUpperRole((await (await discord.GetGuildAsync(e.Guild.Id)).GetMemberAsync(e.Author.Id)).Roles);
                        var roledas = (await (await discord.GetGuildAsync(e.Guild.Id)).GetMemberAsync(e.Author.Id)).Roles.ToList();
                        List<ulong> PrivacyRoles = await GetPrivacyRoles(e.Guild.Id);
                        var tmp = PrivacyRoles.Find(Id => Id == role.Id);
                        if (tmp != 0) //Первая роль защищена, пытаемся найти первую не приватную
                        {
                            var roles = (await (await discord.GetGuildAsync(e.Guild.Id)).GetMemberAsync(e.Author.Id)).Roles;
                            foreach (var item in roles)
                            {
                                if (PrivacyRoles.Find(Id => Id == item.Id) == 0) //Если найдем меняем ее
                                {
                                    role = item;
                                    await e.Message.RespondAsync("```css\n" + role.Name + " -> " + message[(CHANGEROLE.Length + 1)..] + "```");
                                    try
                                    {
                                        await role.ModifyAsync(message[(CHANGEROLE.Length + 1)..], role.Permissions, role.Color, role.IsHoisted, role.IsMentionable);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                    return;
                                }
                            }
                        }
                        else //если первая роль устраивает
                        {
                            await e.Message.RespondAsync("```css\n" + role.Name + " -> " + message[(CHANGEROLE.Length + 1)..] + "```");
                            try
                            {
                                await role.ModifyAsync(message[(CHANGEROLE.Length + 1)..], role.Permissions, role.Color, role.IsHoisted, role.IsMentionable);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            return;
                        }
                    }

                    if (message.ToLower().StartsWith(CHANGECOLOR))
                    {
                        var role = (await (await discord.GetGuildAsync(e.Guild.Id)).GetMemberAsync(e.Author.Id)).Roles.First();
                        List<ulong> PrivacyRoles = await GetPrivacyRoles(e.Guild.Id);
                        var tmp = PrivacyRoles.Find(Id => Id == role.Id);
                        if (tmp != 0) //Первая роль защищена, пытаемся найти первую не приватную
                        {
                            var roles = (await (await discord.GetGuildAsync(e.Guild.Id)).GetMemberAsync(e.Author.Id)).Roles;
                            foreach (var item in roles)
                            {
                                if (PrivacyRoles.Find(Id => Id == item.Id) == 0) //Если найдем меняем ее
                                {
                                    role = item;
                                    await e.Message.RespondAsync("```css\n" + role.Color + " -> " + message[(CHANGECOLOR.Length + 1)..] + "```");
                                    try
                                    {
                                        await role.ModifyAsync(role.Name, role.Permissions, new DiscordColor(message[(CHANGECOLOR.Length + 1)..]), role.IsHoisted, role.IsMentionable);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                    return;
                                }
                            }
                        }
                        else //если первая роль устраивает
                        {
                            await e.Message.RespondAsync("```css\n" + role.Color + " -> " + message[(CHANGECOLOR.Length + 1)..] + "```");
                            try
                            {
                                await role.ModifyAsync(role.Name, role.Permissions, new DiscordColor(message[(CHANGECOLOR.Length + 1)..]), role.IsHoisted, role.IsMentionable);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            return;
                        }
                    }

                    if (message.ToLower().StartsWith("!!search"))
                    {
                        string req = e.Message.Content[("!!search".Length + 1)..];
                        try
                        {
                            //youtubeService.Playlists.List("path");
                            var searchListRequest = youtubeService.Search.List("snippet");
                            searchListRequest.Q = req;
                            searchListRequest.MaxResults = 15;

                            // Call the search.list method to retrieve results matching the specified query term.
                            var searchListResponse = await searchListRequest.ExecuteAsync();
                            string res = e.Author.Mention + "```css\n";
                            for (int i = 0; i < searchListResponse.Items.Count; i++)
                            {
                                if (searchListResponse.Items[i].Id.VideoId != null) //Условие, что результат это видео
                                {
                                    res += i.ToString() + ") " + searchListResponse.Items[i].Snippet.Title.Replace("&amp;", "&").Replace("&quot;", "\"") + "\n\n";
                                }
                            }
                            res += "```";
                            await e.Message.RespondAsync(res);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }//Поиск в ютубе

                    if (message.ToLower().StartsWith("!!playlist")) //Плeйлист
                    {
                        string req = e.Message.Content.Substring(e.Message.Content.IndexOf("list=") + 5);
                        try
                        {
                            //youtubeService.Playlists.List("path");
                            var searchListRequest = youtubeService.PlaylistItems.List("snippet");
                            searchListRequest.PlaylistId = req;
                            searchListRequest.MaxResults = 20;

                            // Call the search.list method to retrieve results matching the specified query term.
                            var searchListResponse = await searchListRequest.ExecuteAsync();
                            string res = e.Author.Mention + "```css\n";
                            for (int i = 0; i < searchListResponse.Items.Count; i++)
                            {
                                res += i.ToString() + ") " + searchListResponse.Items[i].Snippet.Title.Replace("&amp;", "&").Replace("&quot;", "\"") + "\n\n";
                            }
                            res += "```";
                            await e.Message.RespondAsync(res);
                            //var tmp = discord.UseVoiceNext();
                            //VoiceNextConnection voiceNext = tmp.GetConnection(await discord.GetGuildAsync(e.Guild.Id));
                            //tmp.ConnectAsync((await (await discord.GetGuildAsync(e.Guild.Id)).GetMemberAsync(e.Author.Id)).VoiceState.Channel);
                            //voiceNext.Disconnect();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (message.ToLower().StartsWith("!!dateentry"))
                    {
                        var Mem = await e.Guild.GetMemberAsync(e.Author.Id);
                        await e.Message.RespondAsync("На сервере с: " + Mem.JoinedAt.UtcDateTime.ToString());
                    }

                    if (message.ToLower().StartsWith("!!ava"))
                    {
                        await e.Message.RespondAsync(e.Author.AvatarUrl);
                    }

                    if (message.ToLower().StartsWith("!!gif"))
                    {
                        await e.Message.RespondAsync(await GetGif(message[("!!gif".Length + 1)..]));
                        return;
                    }

                    if (message.ToLower().StartsWith("!!inst"))
                    {
                        RespondPhotoInst(e);
                        return;
                    }

                    if (message.ToLower().StartsWith("!!myexp"))
                    {
                        var info = await _getData.GetFromMongoUserVoiceTimeAsync(e.Author.Id, e.Guild.Id, mongoDB);
                        var tmp = UsersOnline.Find(u => u.UserId == e.Author.Id).inGuilds.FirstOrDefault(i => i.GuildId == e.Guild.Id);
                        if (tmp != null)
                        {
                            info.Exp += (uint)tmp.Exp();
                        }
                        string str = e.Author.Mention + "```css\nТвой опыт на сервере [" + e.Guild.Name + "] - " + info.Exp + "```";
                        await e.Message.RespondAsync(str);
                        return;
                    }

                    if (message.ToLower().StartsWith("!!send"))
                    {
                        string mes = message.Replace("!!send ", "").Replace(e.MentionedUsers.First().Mention, "");
                        await e.Message.DeleteAsync();
                        await e.Message.RespondAsync("Мистер " + e.MentionedUsers.First().Mention + ". Мистер " + (await e.Guild.GetMemberAsync(e.Author.Id)).DisplayName + " передает вам" + mes);
                    }
                }

                if (e.MentionedRoles.Count > 0)
                {
                    bool fl = true;
                    List<ulong> roles = await GetCommonRoles(e.Guild.Id);

                    foreach (var commonrole in roles)
                    {
                        foreach (var role in e.MentionedRoles)
                        {
                            if (role.Id == commonrole)
                            {
                                fl = false;
                                foreach (var roleUser in (await e.Guild.GetMemberAsync(e.Author.Id)).Roles)
                                {
                                    if (roleUser == role)
                                    {
                                        fl = true;
                                    }
                                }
                            }
                        }
                    }
                    if (!fl)
                    {
                        await e.Message.RespondAsync(e.Author.Mention + " хуй соси");
                        //Jail(e);
                        await e.Message.DeleteAsync();
                    }
                }
            };

            discord.PresenceUpdated += async (sender, e) =>
            {
                if (CountGuild == 0)
                    return;

                var StatusNow = e.Status;
                UserStatus StatusBefore;

                if (e.PresenceBefore != null)
                    StatusBefore = e.PresenceBefore.Status;
                else
                    StatusBefore = UserStatus.Offline;

                if ((StatusNow == UserStatus.Online || StatusNow == UserStatus.DoNotDisturb || StatusNow == UserStatus.Idle || StatusNow == UserStatus.Invisible) && StatusBefore == UserStatus.Offline)
                {
                    if (UsersOnline.Find(i => i.UserId == e.User.Id) == null)
                        UsersOnline.Add(new UserOnline() { UserId = e.User.Id, Entry = DateTime.UtcNow });
                    await discord.SendMessageAsync(await discord.GetChannelAsync(ChannelLogId), e.User.Mention + " Зашел в дискорд в " + DateTime.UtcNow.ToString());
                }
                if ((StatusBefore == UserStatus.Online || StatusBefore == UserStatus.DoNotDisturb || StatusBefore == UserStatus.Idle || StatusBefore == UserStatus.Invisible) && StatusNow == UserStatus.Offline)
                {
                    var tmp = UsersOnline.Find(u => u.UserId == e.User.Id);
                    if (tmp != null)
                    {
                        //tmp.PushToJsonUserOnlineTimeAsync();
                        //tmp.BackupToDiskAsync(disk);
                        tmp.PushToDBUserOnlineTimeAsync();
                        tmp.PushToMongoUserOnlineTimeAsync(mongoDB);
                        UsersOnline.Remove(tmp);
                    }
                    await discord.SendMessageAsync(await discord.GetChannelAsync(ChannelLogId), e.User.Mention + " Покинул дискорд в " + DateTime.UtcNow.ToString());
                }
            };

            await Task.Delay(-1);
        }

        private static async Task Discord_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
        {
            var Guilds = e.Guilds;
            if (CountGuild < Guilds.Count)
            {
                if (Guilds.Count > 1)
                {
                    GetAllUsersOnline(Guilds);
                    CountGuild = Guilds.Count;
                }
            }
            Console.WriteLine("---Guild download---");
        }

        private static async Task Discord_MessageReactionAdded(DiscordClient sender, DSharpPlus.EventArgs.MessageReactionAddEventArgs e)
        {
            //Если бот создал эмоджи
            if (e.User.Id == 799222619248656405)
                return;

            var mes = await e.Channel.GetMessageAsync(e.Message.Id);
            //Если сообщение в ивенте от бота
            if (mes.Author.Id == 799222619248656405)
            {
                MyInst inst;
                //Предыдущее
                if (e.Emoji == DiscordEmoji.FromName(discord, ":arrow_left:").Name)
                {
                    if (InstagramCache.TryGetValue(mes.Embeds[0].Footer.Text[("https://www.instagram.com/".Length)..], out inst))
                    {
                        await mes.ModifyAsync(Optional.FromValue(CreateEmbed(inst.InstagramProfile, inst.ToPrevPost(), e.Message.Timestamp)));
                    }
                    //Если в кеше нет такого, то закидываем
                    else
                    {
                        InstagramParser parser = InstagramParser.Create(mes.Embeds[0].Footer.Text[("https://www.instagram.com/".Length)..]);
                        parser = await parser.ParseAsync();
                        inst = new MyInst(parser, 0);
                        InstagramCache.Set(parser.UserName, inst, TimeSpan.FromMinutes(15));
                        await mes.ModifyAsync(Optional.FromValue(CreateEmbed(inst.InstagramProfile, inst.ToPrevPost(), e.Message.Timestamp)));
                    }
                    return;
                }
                //Следующее
                if (e.Emoji.Name == DiscordEmoji.FromName(discord, ":arrow_right:").Name)
                {
                    if (InstagramCache.TryGetValue(mes.Embeds[0].Footer.Text[("https://www.instagram.com/".Length)..], out inst))
                    {
                        await mes.ModifyAsync(Optional.FromValue(CreateEmbed(inst.InstagramProfile, inst.ToPrevPost(), e.Message.Timestamp)));
                    }
                    //Если в кеше нет такого, то закидываем
                    else
                    {
                        InstagramParser parser = InstagramParser.Create(mes.Embeds[0].Footer.Text[("https://www.instagram.com/".Length)..]);
                        parser = await parser.ParseAsync();
                        inst = new MyInst(parser, 0);
                        InstagramCache.Set(parser.UserName, inst, TimeSpan.FromMinutes(15));
                        await mes.ModifyAsync(Optional.FromValue(CreateEmbed(inst.InstagramProfile, inst.ToPrevPost(), e.Message.Timestamp)));
                    }
                    return;
                }
            }
        }

        private static async Task Discord_MessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
        {
            //Если бот создал эмоджи
            if (e.User.Id == 799222619248656405)
                return;

            var mes = await e.Channel.GetMessageAsync(e.Message.Id);
            //Если сообщение в ивенте от бота
            if (mes.Author.Id == 799222619248656405)
            {
                MyInst inst;
                //Предыдущее
                if (e.Emoji == DiscordEmoji.FromName(discord, ":arrow_left:").Name)
                {
                    if (InstagramCache.TryGetValue(mes.Embeds[0].Footer.Text[("https://www.instagram.com/".Length)..], out inst))
                    {
                        await mes.ModifyAsync(Optional.FromValue(CreateEmbed(inst.InstagramProfile, inst.ToPrevPost(), e.Message.Timestamp)));
                    }
                    //Если в кеше нет такого, то закидываем
                    else
                    {
                        InstagramParser parser = InstagramParser.Create(mes.Embeds[0].Footer.Text[("https://www.instagram.com/".Length)..]);
                        parser = await parser.ParseAsync();
                        inst = new MyInst(parser, 0);
                        InstagramCache.Set(parser.UserName, inst, TimeSpan.FromMinutes(15));
                        await mes.ModifyAsync(Optional.FromValue(CreateEmbed(inst.InstagramProfile, inst.ToPrevPost(), e.Message.Timestamp)));
                    }
                    return;
                }
                //Следующее
                if (e.Emoji.Name == DiscordEmoji.FromName(discord, ":arrow_right:").Name)
                {
                    if (InstagramCache.TryGetValue(mes.Embeds[0].Footer.Text[("https://www.instagram.com/".Length)..], out inst))
                    {
                        await mes.ModifyAsync(Optional.FromValue(CreateEmbed(inst.InstagramProfile, inst.ToPrevPost(), e.Message.Timestamp)));
                    }
                    //Если в кеше нет такого, то закидываем
                    else
                    {
                        InstagramParser parser = InstagramParser.Create(mes.Embeds[0].Footer.Text[("https://www.instagram.com/".Length)..]);
                        parser = await parser.ParseAsync();
                        inst = new MyInst(parser, 0);
                        InstagramCache.Set(parser.UserName, inst, TimeSpan.FromMinutes(15));
                        await mes.ModifyAsync(Optional.FromValue(CreateEmbed(inst.InstagramProfile, inst.ToPrevPost(), e.Message.Timestamp)));
                    }
                    return;
                }
            }
        }

        private static async Task ChangeRoleColor(DiscordClient sender, DSharpPlus.EventArgs.HeartbeatEventArgs e)
        {
            foreach (var item in IdUsersRainbow)
            {
                await RainbowColor(item);
            }
        }

        private static async Task Discord_SocketErrored(DiscordClient sender, DSharpPlus.EventArgs.SocketErrorEventArgs e)
        {
            var UserClone = new UserOnline[UsersOnline.Count];
            UsersOnline.CopyTo(UserClone);
            Backup(UserClone);
            UsersOnline.Clear();
            CountGuild = 0;
        }

        private static async Task Discord_VoiceStateUpdated(DiscordClient sender, DSharpPlus.EventArgs.VoiceStateUpdateEventArgs e)
        {
            try
            {
                if (e.Channel == null) //вышел с войса
                {
                    var tmp = UsersOnline.Find(i => i.UserId == e.User.Id);
                    if (tmp != null)
                    {
                        //LeaveVoiceToJsonAsync(e, tmp);
                        //LeaveVoiceToDiskAsync(e, tmp);
                        UsersOnline.Remove(tmp);
                        var gld = tmp.inGuilds.Find(g => g.GuildId == e.Guild.Id);
                        if (gld == null)
                            return;
                        gld.ReCalc();
                        gld.VoiceEntry = null;
                        UsersOnline.Add(tmp);
                        Console.WriteLine("User " + e.User.Username + " Leave voice in " + e.Guild.Name);
                        await discord.SendMessageAsync(await discord.GetChannelAsync(ChannelLogId), e.User.Mention + " Покинул voice в " + DateTime.UtcNow.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            try
            {
                if (e.Channel != null) //зашел в войс
                {
                    var user = UsersOnline.Find(i => i.UserId == e.User.Id);
                    if (user == null)
                        return;
                    var tmp = user.inGuilds.Find(g => g.GuildId == e.Guild.Id);
                    if (tmp == null)
                        return;
                    if (tmp.VoiceEntry == null)
                    {
                        UsersOnline.Remove(user);
                        tmp.VoiceEntry = DateTime.UtcNow;
                        UsersOnline.Add(user);
                        Console.WriteLine("User " + e.User.Username + " Join voice in " + e.Guild.Name);
                        await discord.SendMessageAsync(await discord.GetChannelAsync(ChannelLogId), e.User.Mention + " Зашел в voice в " + DateTime.UtcNow.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async Task Discord_Resumed(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            //CountGuild = 0;
        }

        private static async Task Discord_SocketClosed(DiscordClient sender, DSharpPlus.EventArgs.SocketCloseEventArgs e) //При потере соединения
        {
            var UserClone = new UserOnline[UsersOnline.Count];
            UsersOnline.CopyTo(UserClone);
            Backup(UserClone);
            UsersOnline.Clear();
            CountGuild = 0;
        }

        private static async Task Heartbeated(DiscordClient sender, DSharpPlus.EventArgs.HeartbeatEventArgs e) //Ping
        {
            var Guilds = sender.Guilds;
            if (CountGuild < Guilds.Count)
            {
                if (Guilds.Count > 1)
                {
                    GetAllUsersOnline(Guilds);
                    CountGuild = Guilds.Count;
                }
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Ping: " + e.Ping + "\nUsers alive " + UsersOnline.Count.ToString() + "\nGuilds alive " + sender.Guilds.Count);
            Console.ResetColor();
        }

        private static async Task Backup(UserOnline[] Users)
        {
            foreach (var item in Users)
            {
                //item.PushToJsonUserOnlineTimeAsync();
                //item.PushToDBUserOnlineTimeAsync();
                item.PushToMongoUserOnlineTimeAsync(mongoDB);
                //item.BackupToDiskAsync(disk);
            }
        }

        private static async Task BackupFromLastExit(UserOnline[] Users, DateTime leave)
        {
            foreach (var item in Users)
            {
                //item.PushToJsonUserOnlineTimeAsync();
                //item.PushToDBUserOnlineTimeAsync();
                item.PushToMongoUserOnlineTimeAsync(mongoDB, leave);
                //item.BackupToDiskAsync(disk);
            }
        }
        private static async Task PushPrivacyRoles(ulong GuildId, ulong RoleId)
        {
            List<ulong> Roles;
            try
            {
                using (StreamReader fs = new StreamReader("privacyrole" + GuildId + ".json"))
                {
                    Roles = JsonConvert.DeserializeObject<List<ulong>>(await fs.ReadToEndAsync());
                    var tmp = Roles.Find(Id => Id == RoleId);
                    if (tmp == 0)
                    {
                        Roles.Add(RoleId);
                    }
                }
                using (StreamWriter fss = new StreamWriter("privacyrole" + GuildId + ".json", false, System.Text.Encoding.Default))
                {
                    string str = JsonConvert.SerializeObject(Roles);
                    await fss.WriteLineAsync(str);
                    Console.WriteLine("PrivacyRole " + GuildId + " ReWrite");
                }
            }
            catch (FileNotFoundException)
            {
                Roles = new List<ulong>();
                Roles.Add(RoleId);
                using (StreamWriter fss = new StreamWriter("privacyrole" + GuildId + ".json", false, System.Text.Encoding.Default))
                {
                    string str = JsonConvert.SerializeObject(Roles);
                    await fss.WriteLineAsync(str);
                    Console.WriteLine("PrivacyRole " + GuildId + " ReWrite");
                }
            }
            catch (AggregateException ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
            }
        }
        private static async Task PushCommonRoles(ulong GuildId, ulong RoleId)
        {
            List<ulong> Roles;
            try
            {
                using (StreamReader fs = new StreamReader("commonrole" + GuildId + ".json"))
                {
                    Roles = JsonConvert.DeserializeObject<List<ulong>>(await fs.ReadToEndAsync());
                    var tmp = Roles.Find(Id => Id == RoleId);
                    if (tmp == 0)
                    {
                        Roles.Add(RoleId);
                    }
                }
                using (StreamWriter fss = new StreamWriter("commonrole" + GuildId + ".json", false, System.Text.Encoding.Default))
                {
                    string str = JsonConvert.SerializeObject(Roles);
                    await fss.WriteLineAsync(str);
                    Console.WriteLine("commonrole " + GuildId + " ReWrite");
                }
            }
            catch (FileNotFoundException)
            {
                Roles = new List<ulong>();
                Roles.Add(RoleId);
                using (StreamWriter fss = new StreamWriter("commonrole" + GuildId + ".json", false, System.Text.Encoding.Default))
                {
                    string str = JsonConvert.SerializeObject(Roles);
                    await fss.WriteLineAsync(str);
                    Console.WriteLine("commonrole " + GuildId + " ReWrite");
                }
            }
            catch (AggregateException ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
            }
        }
        private static async Task OpenRole(ulong GuildId, ulong RoleId)
        {
            List<ulong> Roles;
            try
            {
                using (StreamReader fs = new StreamReader("privacyrole" + GuildId + ".json"))
                {
                    Roles = JsonConvert.DeserializeObject<List<ulong>>(await fs.ReadToEndAsync());
                    var tmp = Roles.Find(Id => Id == RoleId);
                    if (tmp != 0)
                    {
                        Roles.Remove(RoleId);
                    }
                }
                using (StreamWriter fss = new StreamWriter("privacyrole" + GuildId + ".json", false, System.Text.Encoding.Default))
                {
                    string str = JsonConvert.SerializeObject(Roles);
                    await fss.WriteLineAsync(str);
                    Console.WriteLine("OpenRole " + GuildId + " ReWrite");
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("FileNotFoundException");
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
            }
        }
        private static async Task<List<ulong>> GetPrivacyRoles(ulong GuildId)
        {
            using (StreamReader fs = new StreamReader("privacyrole" + GuildId + ".json"))
            {
                return JsonConvert.DeserializeObject<List<ulong>>(await fs.ReadToEndAsync());
            }
        } //Возвращает приватные роли гильдии
        private static async Task<List<ulong>> GetCommonRoles(ulong GuildId)
        {
            using (StreamReader fs = new StreamReader("commonrole" + GuildId + ".json"))
            {
                return JsonConvert.DeserializeObject<List<ulong>>(await fs.ReadToEndAsync());
            }
        }
        private static async Task PushToBinaryUserOnlineTime(DSharpPlus.EventArgs.PresenceUpdateEventArgs e)
        {
            // Перезапись пользователя
            try
            {
                UserProp User;
                using (FileStream fs = new FileStream("user" + e.User.Id.ToString(), FileMode.Open))
                {
                    User = (UserProp)new BinaryFormatter().Deserialize(fs);
                    var tmp = UsersOnline.Find(u => u.UserId == e.User.Id);
                    if (tmp != null)
                    {
                        foreach (var item in tmp.inGuilds)
                        {
                            User.ReCalculate(tmp.Entry, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                        }
                        Console.Write("User " + e.User.Username + " leave. " + "Session time:" + (DateTime.UtcNow - tmp.Entry).ToString());
                    }
                }
                using (FileStream fs = new FileStream("user" + e.User.Id.ToString(), FileMode.Open))
                {
                    BinaryFormatter binary = new BinaryFormatter();
                    binary.Serialize(fs, User);
                    Console.WriteLine("  UserData " + e.User.Username + " ReWrite");
                }
            }
            catch (FileNotFoundException)
            {
                UserProp User;
                using (FileStream fs = new FileStream("user" + e.User.Id.ToString(), FileMode.Create))
                {
                    User = new UserProp(0, 0, 0, e.User.Id);
                    BinaryFormatter binary = new BinaryFormatter();
                    binary.Serialize(fs, new UserProp(0, 0, 0, e.User.Id));
                    var tmp = UsersOnline.Find(u => u.UserId == e.User.Id);
                    if (tmp != null)
                    {
                        foreach (var item in tmp.inGuilds)
                        {
                            User.ReCalculate(tmp.Entry, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                        }
                        Console.Write("User " + e.User.Username + " leave. " + "Session time:" + (DateTime.UtcNow - tmp.Entry).ToString());
                    }
                }
                using (FileStream fs = new FileStream("user" + e.User.Id.ToString(), FileMode.Create))
                {
                    BinaryFormatter binary = new BinaryFormatter();
                    binary.Serialize(fs, User);
                    Console.WriteLine("  UserData " + e.User.Username + " ReWrite");
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
            }
        }
        private static DateTime? CheckUserInVoice(IEnumerable<DiscordChannel> discordChannels, DiscordMember discordMember)
        {
            foreach (var item in discordChannels)
            {
                if (discordMember.VoiceState == null)
                    return null;
                if (discordMember.VoiceState.Channel == null)
                    return null;
                if (discordMember.VoiceState.Channel.Id == item.Id) //тут
                    return DateTime.UtcNow;
            }
            return null;
        }
        private static async Task RainbowColor(IdUserIdGuild item)
        {
            Random random = new Random();
            byte RS = (byte)random.Next(0, 1);
            byte GS = (byte)random.Next(0, 1);
            byte BS = (byte)random.Next(0, 1);
            byte delta = 10;
            var role = (await (await discord.GetGuildAsync(item.IdGuild)).GetMemberAsync(item.IdUser)).Roles.First();
            byte R = role.Color.R;
            byte G = role.Color.G;
            byte B = role.Color.B;
            #region Color
            if (RS == 0)
            {
                if (R - delta < 0)
                    R = 0;
                else
                    R -= delta;
            }
            else
            {
                if (R + delta > 255)
                    R = 255;
                else
                    R += delta;
            }

            if (GS == 0)
            {
                if (G - delta < 0)
                    G = 0;
                else
                    G -= delta;
            }
            else
            {
                if (G + delta > 255)
                    G = 255;
                else
                    G += delta;
            }

            if (BS == 0)
            {
                if (B - delta < 0)
                    B = 0;
                else
                    B -= delta;
            }
            else
            {
                if (B + delta > 255)
                    B = 255;
                else
                    B += delta;
            }
            #endregion
            try
            {
                await role.ModifyAsync(role.Name, role.Permissions, new DiscordColor(R, G, B), role.IsHoisted, role.IsMentionable);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private static async void LeaveVoiceToJsonAsync(DSharpPlus.EventArgs.VoiceStateUpdateEventArgs e, UserOnline tmp)
        {
            try
            {
                UserProp User;
                using (StreamReader fs = new StreamReader("user" + e.User.Id.ToString() + ".json"))
                {
                    User = JsonConvert.DeserializeObject<UserProp>(await fs.ReadToEndAsync());
                    foreach (var item in tmp.inGuilds)
                    {
                        User.ReCalculate(DateTime.UtcNow, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                    }
                    Console.WriteLine("User " + e.User.Username + " Leave " + "Voice");
                    await discord.SendMessageAsync(await discord.GetChannelAsync(ChannelLogId), e.User.Mention + " Покинул voice в " + DateTime.UtcNow.ToString());
                }
                using (StreamWriter fs = new StreamWriter("user" + User.UserId.ToString() + ".json", false, System.Text.Encoding.Default))
                {
                    string str = JsonConvert.SerializeObject(User);
                    await fs.WriteLineAsync(str);
                    Console.WriteLine("UserData " + e.User.Username + " ReWrite");
                }
            }
            catch (FileNotFoundException)
            {
                UserProp User;
                using (StreamWriter fs = new StreamWriter("user" + e.User.Id.ToString() + ".json", false, System.Text.Encoding.Default))
                {
                    User = new UserProp(0, 0, 0, e.User.Id);
                    var TimeNow = DateTime.UtcNow;
                    foreach (var item in tmp.inGuilds)
                    {
                        User.ReCalculate(DateTime.UtcNow, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                    }
                    Console.WriteLine("User " + e.User.Username + " Leave " + "Voice");
                    await fs.WriteLineAsync(JsonConvert.SerializeObject(User));
                    await discord.SendMessageAsync(await discord.GetChannelAsync(ChannelLogId), e.User.Mention + " Покинул voice в " + DateTime.UtcNow.ToString());
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
            }
        }
        private static async void LeaveVoiceToDiskAsync(DSharpPlus.EventArgs.VoiceStateUpdateEventArgs e, UserOnline tmp)
        {
            try
            {
                UserProp User;
                using (StreamReader fs = new StreamReader(await disk.Files.DownloadFileAsync(("/DisBot/" + "user" + e.User.Id.ToString() + ".json"))))
                {
                    User = JsonConvert.DeserializeObject<UserProp>(await fs.ReadToEndAsync());
                    var TimeNow = DateTime.UtcNow;
                    foreach (var item in tmp.inGuilds)
                    {
                        User.ReCalculate(DateTime.UtcNow, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                    }
                }
                using (Stream SourceStream = (JsonConvert.SerializeObject(User)).ToStream())
                {
                    await disk.Files.UploadFileAsync(("/DisBot/" + "user" + e.User.Id.ToString() + ".json"), true, SourceStream);
                }
            }
            catch (Exception ex)
            {
                UserProp User;
                User = new UserProp(0, 0, 0, e.User.Id);
                var TimeNow = DateTime.UtcNow;
                foreach (var item in tmp.inGuilds)
                {
                    User.ReCalculate(DateTime.UtcNow, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                }
                try
                {
                    using (Stream SourceStream = (JsonConvert.SerializeObject(User)).ToStream())
                    {
                        await disk.Files.UploadFileAsync(("/DisBot/" + "user" + e.User.Id.ToString() + ".json"), false, SourceStream);
                    }
                }
                catch
                {
                    User = new UserProp(0, 0, 0, e.User.Id);
                    foreach (var item in tmp.inGuilds)
                    {
                        User.ReCalculate(DateTime.UtcNow, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                    }
                    await disk.Commands.CreateDictionaryAsync("/DisBot");
                    using (Stream SourceStream = (JsonConvert.SerializeObject(User)).ToStream())
                    {
                        await disk.Files.UploadFileAsync(("/DisBot/" + "user" + e.User.Id.ToString() + ".json"), false, SourceStream);
                    }
                }
            }
        }
        private static async void GetAllUsersOnline(IReadOnlyDictionary<ulong, DiscordGuild> a)
        {
            foreach (var item in a)
            {
                //var Members1 = await item.Value.GetAllMembersAsync();
                //foreach(var i in Members1)
                //{
                //    var aaaa = await item.Value.GetMemberAsync(i.Id);
                //    var bbbb = await discord.GetUserAsync(i.Id);
                //}
                var Members = (await item.Value.GetAllMembersAsync()).Where(i => i.Presence != null && i.Presence.Status != UserStatus.Offline);
                var VoiceChannels = item.Value.Channels.Where(e => e.Value.Type == ChannelType.Voice);
                foreach (var Member in Members)
                {
                    var User = UsersOnline.Find(i => i.UserId == Member.Id);
                    if (User == null)
                    {
                        var us = new UserOnline() { UserId = Member.Id, Entry = DateTime.UtcNow };
                        us.inGuilds.Add(new EventInGuild() { GuildId = item.Value.Id, VoiceEntry = CheckUserInVoice(VoiceChannels.Select(i => i.Value), Member) });
                        UsersOnline.Add(us);
                    }
                    else
                    {
                        User.inGuilds.Add(new EventInGuild() { GuildId = item.Value.Id, VoiceEntry = CheckUserInVoice(VoiceChannels.Select(i => i.Value), Member) });
                    }
                }
            }
        }
        private static DiscordRole GetUpperRole(IEnumerable<DiscordRole> roles)
        {
            int pos = 0;
            DiscordRole ret;
            ret = roles.First();
            foreach (var item in roles)
            {
                if (item.Position > pos)
                {
                    pos = item.Position;
                    ret = item;
                }
            }
            return ret;
        }
        public static async Task<string> GetGif(string search)
        {
            try
            {
                Random random = new Random();
                System.Net.WebRequest req = System.Net.WebRequest.Create("https://api.giphy.com/v1/gifs/search?api_key=frT9CU8K1SO8FAInTXlIUtV6EfHGI9dA&q=" + search + "&limit=1&offset=" + random.Next(0, 50) + "&rating=g&lang=eu");
                System.Net.WebResponse resp = await req.GetResponseAsync();
                Stream stream = resp.GetResponseStream();
                StreamReader sr = new StreamReader(stream);
                string Out = sr.ReadToEnd();
                sr.Close();
                string down = Out.Substring(Out.IndexOf("downsized\":{") + "downsized\":{".Length);
                string url1 = down.Substring(down.IndexOf("https://"));
                string url = url1.Substring(0, url1.IndexOf("\"}"));
                return url;
            }
            catch
            {
                try
                {
                    Random random = new Random();
                    System.Net.WebRequest req = System.Net.WebRequest.Create("https://api.giphy.com/v1/gifs/search?api_key=frT9CU8K1SO8FAInTXlIUtV6EfHGI9dA&q=" + search + "&limit=1&offset=" + random.Next(0, 10) + "&rating=g&lang=ru");
                    System.Net.WebResponse resp = await req.GetResponseAsync();
                    Stream stream = resp.GetResponseStream();
                    StreamReader sr = new StreamReader(stream);
                    string Out = sr.ReadToEnd();
                    sr.Close();
                    string down = Out.Substring(Out.IndexOf("downsized\":{") + "downsized\":{".Length);
                    string url1 = down.Substring(down.IndexOf("https://"));
                    string url = url1.Substring(0, url1.IndexOf("\"}"));
                    return url;
                }
                catch
                {
                    return "Not Found";
                }
            }


        }
        public static async void RespondPhotoInst(MessageCreateEventArgs e)
        {
            try
            {
                //await e.Message.RespondAsync(await ParseInst(e.Message.Content[("!!inst".Length + 1)..]));
                //Random random = new Random((int)DateTime.Now.Ticks);

                var inst = InstagramParser.Create(e.Message.Content[("!!inst".Length + 1)..]);
                inst = await inst.ParseAsync();
                var post = inst.Posts.ElementAt(0);
                var embBuilder = new DiscordEmbedBuilder();
                embBuilder.Color = Optional.FromValue(DiscordColor.Purple);
                embBuilder.ImageUrl = post.UrlMainImage;
                var footer = new DiscordEmbedBuilder.EmbedFooter();
                footer.Text = inst.CreateUrlOnInstagram() + "\n";
                footer.IconUrl = inst.ProfileInfo.ProfileImageUrl;
                embBuilder.Footer = footer;
                embBuilder.Description = $"{inst.ProfileInfo.CountPosts} Posts\n {inst.ProfileInfo.CountFollowers} Followers\n {inst.ProfileInfo.CountFollowing} Following";
                var thumbnail = new DiscordEmbedBuilder.EmbedThumbnail();
                thumbnail.Url = inst.ProfileInfo.ProfileImageUrl;
                thumbnail.Height = 64;
                thumbnail.Width = 64;
                embBuilder.Thumbnail = thumbnail;
                embBuilder.Title = inst.ProfileInfo.ProfileName;
                embBuilder.Timestamp = e.Message.Timestamp;
                if (inst.ProfileInfo.ProfileStatus != "")
                {
                    embBuilder.AddField("Status", inst.ProfileInfo.ProfileStatus);
                }
                embBuilder.AddField(":thumbsup: " + post.CountLikes + "   :pencil: " + post.CountComments + "   :clock1: " + post.Date, ":heart:");
                var mes = await e.Message.RespondAsync(embBuilder.Build());
                await mes.CreateReactionAsync(DiscordEmoji.FromName(discord, ":arrow_left:"));
                await mes.CreateReactionAsync(DiscordEmoji.FromName(discord, ":arrow_right:"));
                InstagramCache.Set(inst.UserName, new MyInst(inst, 0), TimeSpan.FromMinutes(15));
            }
            catch (Exception ex)
            {
                await e.Message.RespondAsync(ex.Message);
            }
        }
        public static DiscordEmbed CreateEmbed(InstagramParser instagram, PostInstagramInfo post, DateTimeOffset Timestamp)
        {
            var embBuilder = new DiscordEmbedBuilder();
            embBuilder.Color = Optional.FromValue(DiscordColor.Purple);
            embBuilder.ImageUrl = post.UrlMainImage;
            var footer = new DiscordEmbedBuilder.EmbedFooter();
            footer.Text = instagram.CreateUrlOnInstagram() + "\n";
            footer.IconUrl = instagram.ProfileInfo.ProfileImageUrl;
            embBuilder.Footer = footer;
            embBuilder.Description = $"{instagram.ProfileInfo.CountPosts} Posts\n {instagram.ProfileInfo.CountFollowers} Followers\n {instagram.ProfileInfo.CountFollowing} Following";
            var thumbnail = new DiscordEmbedBuilder.EmbedThumbnail();
            thumbnail.Url = instagram.ProfileInfo.ProfileImageUrl;
            thumbnail.Height = 64;
            thumbnail.Width = 64;
            embBuilder.Thumbnail = thumbnail;
            embBuilder.Title = instagram.ProfileInfo.ProfileName;
            embBuilder.Timestamp = Timestamp;
            if (instagram.ProfileInfo.ProfileStatus != "")
            {
                embBuilder.AddField("Status", instagram.ProfileInfo.ProfileStatus);
            }
            embBuilder.AddField(":thumbsup: " + post.CountLikes + "   :pencil: " + post.CountComments + "   :clock1: " + post.Date, ":heart:");
            return embBuilder.Build();
        }
        public static async void RespondBitmap(DiscordMessage message)
        {
            try
            {
                //FileStream fileStream = new FileStream("tmpaw1.jpg", FileMode.Create);
                //MemoryStream memoryStream = new MemoryStream();
                //Bitmap image = await CreateImage.GetStatsImageB(message);
                //image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                //byte[] array = memoryStream.ToArray();
                //fileStream.Write(array, 0, array.Length);
                //fileStream.Close();
                int exp;
                using (StreamReader fs = new StreamReader("user" + message.Author.Id.ToString() + ".json"))
                {
                    UserProp User;
                    User = JsonConvert.DeserializeObject<UserProp>(await fs.ReadToEndAsync());
                    User.ReCalculate(DateTime.UtcNow, DateTime.UtcNow, UsersOnline.Find(u => u.UserId == message.Author.Id).inGuilds.Find(g => g.GuildId == message.Channel.Guild.Id).VoiceEntry, message.Channel.Guild.Id);
                    exp = (int)User.GuildsInfos.Find(i => i.GuildId == message.Channel.Guild.Id).Exp;
                }
                string path = await CreateImage.GetStatsImage(message, exp);
                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    var d = new DiscordMessageBuilder();
                    await message.RespondAsync(d.WithFile(stream));
                }
                File.Delete(path);
                //memoryStream.Dispose();
                //image.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async void ExitBackup()
        {
            //Обработка старого бэкапа
            try
            {
                IMongoDBWorker IMongoDBWorker = new MongoDBWorker(conf.MongoDBConnectString);
                IEnumerable<UserOnline> usersback = await IMongoDBWorker.ReadDataAsync<UserOnline>("DisBot", "BackUp");
                var dateTimeleave = (await IMongoDBWorker.ReadDataAsync<DateTimeClass>("DisBot", "DateTimeBackUp")).FirstOrDefault();
                if(dateTimeleave == null)
                {
                    return;
                }
                await BackupFromLastExit(usersback.ToArray(), dateTimeleave.dateTime);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Backup load");
                Console.ForegroundColor = ConsoleColor.White;
                await IMongoDBWorker.DropCollectionAsync("DisBot", "BackUp");
                await IMongoDBWorker.DropCollectionAsync("DisBot", "DateTimeBackUp");
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine("------------Backup not founded------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private static bool Authorization()
        {
            try
            {
                using (StreamReader fs = new StreamReader("Tokens.config"))
                {
                    conf = JsonConvert.DeserializeObject<Config>(fs.ReadToEnd());
                }
            }
            catch (FileNotFoundException)
            {
                Console.Write("DiskordToken (require) = ");
                conf.DiskordToken = Console.ReadLine();
                Console.Write("YandexDiskToken (can skip) = ");
                conf.YandexDiskToken = Console.ReadLine();
                Console.Write("YoutubeToken (can skip) = ");
                conf.YandexDiskToken = Console.ReadLine();

                using (StreamWriter fs = new StreamWriter("Tokens.config", false, System.Text.Encoding.Default))
                {
                    string str = JsonConvert.SerializeObject(conf);
                    fs.WriteLine(str);
                }
            }
            //Инициализации
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = conf.DiskordToken,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                AlwaysCacheMembers = true,
                Intents = DiscordIntents.All
            });
            disk = new DiskHttpApi(conf.YandexDiskToken);
            youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = conf.YoutubeToken,
                ApplicationName = "Search"
            });
            return true;
        }
    }

    struct IdUserIdGuild
    {
        public ulong IdUser { get; set; }
        public ulong IdGuild { get; set; }
    }

    public static class _saveData
    {
        public static async void PushToJsonUserOnlineTimeAsync(this UserOnline userOnline)
        {
            // Перезапись пользователя
            try
            {
                UserProp User;
                using (StreamReader fs = new StreamReader("user" + userOnline.UserId.ToString() + ".json"))
                {
                    User = JsonConvert.DeserializeObject<UserProp>(await fs.ReadToEndAsync());
                    foreach (var item in userOnline.inGuilds)
                    {
                        User.ReCalculate(userOnline.Entry, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                    }
                }
                using (StreamWriter fs = new StreamWriter("user" + User.UserId.ToString() + ".json", false, System.Text.Encoding.Default))
                {
                    string str = JsonConvert.SerializeObject(User);
                    await fs.WriteLineAsync(str);
                    //Console.WriteLine("  UserData " + userOnline.UserId.ToString() + " ReWrite");
                }
            }
            catch (FileNotFoundException)
            {
                UserProp User;
                using (StreamWriter fs = new StreamWriter("user" + userOnline.UserId.ToString() + ".json", false, System.Text.Encoding.Default))
                {
                    User = new UserProp(0, 0, 0, userOnline.UserId);
                    await fs.WriteLineAsync(JsonConvert.SerializeObject(User));
                    foreach (var item in userOnline.inGuilds)
                    {
                        User.ReCalculate(userOnline.Entry, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                    }
                }
                using (StreamWriter fs = new StreamWriter("user" + User.UserId.ToString() + ".json", false, System.Text.Encoding.Default))
                {
                    string str = JsonConvert.SerializeObject(User);
                    await fs.WriteLineAsync(str);
                    //Console.WriteLine("  UserData " + userOnline.UserId.ToString() + " ReWrite");
                }
            }
            catch (AggregateException ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
            }
        }

        public static async void BackupToDiskAsync(this UserOnline userOnline, IDiskApi disk)
        {
            UserProp User;
            try
            {
                using (StreamReader stream = new StreamReader(await disk.Files.DownloadFileAsync(("/DisBot/" + "user" + userOnline.UserId.ToString() + ".json"))))
                {
                    User = JsonConvert.DeserializeObject<UserProp>(await stream.ReadToEndAsync());
                    foreach (var item in userOnline.inGuilds)
                    {
                        User.ReCalculate(userOnline.Entry, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                    }
                }
                //await disk.Commands.CreateDictionaryAsync("/DisBot");
                using (Stream SourceStream = (JsonConvert.SerializeObject(User)).ToStream())
                {
                    await disk.Files.UploadFileAsync(("/DisBot/" + "user" + userOnline.UserId.ToString() + ".json"), true, SourceStream);
                }
            }
            catch (Exception ex)
            {
                User = new UserProp(0, 0, 0, userOnline.UserId);
                foreach (var item in userOnline.inGuilds)
                {
                    User.ReCalculate(userOnline.Entry, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                }
                try
                {
                    using (Stream SourceStream = (JsonConvert.SerializeObject(User)).ToStream())
                    {
                        await disk.Files.UploadFileAsync(("/DisBot/" + "user" + userOnline.UserId.ToString() + ".json"), false, SourceStream);
                    }
                }
                catch
                {
                    try
                    {
                        User = new UserProp(0, 0, 0, userOnline.UserId);
                        foreach (var item in userOnline.inGuilds)
                        {
                            User.ReCalculate(userOnline.Entry, DateTime.UtcNow, item.VoiceEntry, item.GuildId);
                        }
                        await disk.Commands.CreateDictionaryAsync("/DisBot");
                        using (Stream SourceStream = (JsonConvert.SerializeObject(User)).ToStream())
                        {
                            await disk.Files.UploadFileAsync(("/DisBot/" + "user" + userOnline.UserId.ToString() + ".json"), false, SourceStream);
                        }
                    }
                    catch (Exception exep)
                    {
                        Console.WriteLine(exep.Message);
                    }
                }
            }
        }

        public static async void PushToDBUserOnlineTimeAsync(this UserOnline tmp)
        {
            // Перезапись пользователя
            try
            {
                string connection = @"Data Source=.\SQLEXPRESS; Initial catalog=test1; Integrated Security=True";
                using (SqlConnection sqlConnection = new SqlConnection(connection))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCommand = new SqlCommand(null, sqlConnection);
                    foreach (var item in tmp.inGuilds)
                    {
                        //(UserId, HoursInVoice, MinutesInVoice, SecondsInVoice, Exp, Coins)
                        string getuser = "SELECT * FROM Guild" + item.GuildId + " WHERE UserId=" + tmp.UserId;
                        sqlCommand.CommandText = getuser;
                        using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                        {
                            sqlDataReader.Read();
                            int hours = (int)sqlDataReader.GetValue(1);
                            int min = (int)sqlDataReader.GetValue(2);
                            int sec = (int)sqlDataReader.GetValue(3);
                            int exp = (int)sqlDataReader.GetValue(4);
                            int coins = (int)sqlDataReader.GetValue(5);
                            item.ReCalc();
                            string update = "UPDATE " + "Guild" + item.GuildId + " SET HoursInVoice=" + (hours + item.Hours) + ",MinutesInVoice=" + (min + item.Minutes) + ",SecondsInVoice=" + (sec + item.Seconds) + ",Exp=" + (exp + Exp.ConvertMinutesInExp((int)item.Minutes)) + ",Coins=" + 0 + " WHERE UserId=" + tmp.UserId;
                            //string command = "CREATE DATABASE " + "test1";
                            sqlCommand.CommandText = update;
                            sqlDataReader.Close();
                            await sqlCommand.ExecuteNonQueryAsync();
                        }
                    }
                    sqlConnection.Close();
                    sqlCommand.Dispose();
                    Console.WriteLine("User " + tmp.UserId + " updated");
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
            }
        }

        public static async void PushToMongoUserOnlineTimeAsync(this UserOnline tmp, IMongoDBWorker mongoDBWorker)
        {
            // Перезапись пользователя
            try
            {
                var user = (await mongoDBWorker.ReadDataAsync<UserProp>((item) => item.UserId == tmp.UserId, "DisBot", "Users")).FirstOrDefault();
                //Если пользователя еще нет в бд
                if (user == null)
                {
                    UserProp newuser = new UserProp() { UserId = tmp.UserId, GuildsInfos = new List<GuildsInfo>() };
                    newuser.ReCalculate(tmp.Entry, DateTime.UtcNow, tmp.inGuilds);
                    await mongoDBWorker.AddDataAsync(newuser, "DisBot", "Users");
                }
                //Получим и обновим пользователя
                else
                {
                    user.ReCalculate(tmp.Entry, DateTime.UtcNow, tmp.inGuilds);
                    await mongoDBWorker.UpdateDataAsync(user, "DisBot", "Users", "UserId");
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
            }
        }

        public static async void PushToMongoUserOnlineTimeAsync(this UserOnline tmp, IMongoDBWorker mongoDBWorker, DateTime leave)
        {
            // Перезапись пользователя
            try
            {
                var user = (await mongoDBWorker.ReadDataAsync<UserProp>((item) => item.UserId == tmp.UserId, "DisBot", "Users")).FirstOrDefault();
                //Если пользователя еще нет в бд
                if (user == null)
                {
                    UserProp newuser = new UserProp() { UserId = tmp.UserId, GuildsInfos = new List<GuildsInfo>() };
                    newuser.ReCalculate(tmp.Entry, leave, tmp.inGuilds);
                    await mongoDBWorker.AddDataAsync<UserProp>(newuser, "DisBot", "Users");
                }
                //Получим и обновим пользователя
                else
                {
                    user.ReCalculate(tmp.Entry, leave, tmp.inGuilds);
                    await mongoDBWorker.UpdateDataAsync(user, "DisBot", "Users", "UserId");
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
            }
        }
    }

    public static class _getData
    {
        public static async Task<GuildsInfo> GetFromJsonUserVoiceTimeAsync(ulong userId, ulong guildId)
        {
            // Перезапись пользователя
            try
            {
                UserProp User;
                using (StreamReader fs = new StreamReader("user" + userId.ToString() + ".json"))
                {
                    User = JsonConvert.DeserializeObject<UserProp>(await fs.ReadToEndAsync());
                }
                return User.GuildsInfos.FirstOrDefault(g => g.GuildId == guildId);
            }
            catch (FileNotFoundException)
            {
                return new GuildsInfo() { GuildId = guildId };
            }
            catch (AggregateException ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
                return null;
            }
        }

        public static async Task<GuildsInfo> GetFromDiskVoiceTimeAsync(ulong userId, ulong guildId, IDiskApi disk)
        {
            UserProp User;
            try
            {
                using (StreamReader stream = new StreamReader(await disk.Files.DownloadFileAsync(("/DisBot/" + "user" + userId.ToString() + ".json"))))
                {
                    User = JsonConvert.DeserializeObject<UserProp>(await stream.ReadToEndAsync());
                }
                return User.GuildsInfos.FirstOrDefault(g => g.GuildId == guildId);
                //await disk.Commands.CreateDictionaryAsync("/DisBot");
            }
            catch (Exception ex)
            {
                User = new UserProp(0, 0, 0, userId);
                try
                {
                    await disk.Commands.CreateDictionaryAsync("/DisBot");
                    using (Stream SourceStream = (JsonConvert.SerializeObject(User)).ToStream())
                    {
                        await disk.Files.UploadFileAsync(("/DisBot/" + "user" + userId.ToString() + ".json"), false, SourceStream);
                    }
                    return new GuildsInfo() { GuildId = guildId };
                }
                catch (Exception exep)
                {
                    Console.WriteLine(exep.Message);
                    return new GuildsInfo() { GuildId = guildId };
                }
            }
        }

        public static async Task<GuildsInfo> GetFromDBUserVoiceTimeAsync(ulong userId, ulong guildId)
        {
            // Перезапись пользователя
            try
            {
                string connection = @"Data Source=.\SQLEXPRESS; Initial catalog=test1; Integrated Security=True";
                using (SqlConnection sqlConnection = new SqlConnection(connection))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCommand = new SqlCommand(null, sqlConnection);
                    GuildsInfo info = new GuildsInfo();
                    //(UserId, HoursInVoice, MinutesInVoice, SecondsInVoice, Exp, Coins)
                    string getuser = "SELECT * FROM Guild" + guildId + " WHERE UserId=" + userId;
                    sqlCommand.CommandText = getuser;
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        sqlDataReader.Read();
                        info.GuildId = (ulong)(Int64)sqlDataReader.GetValue(0);
                        info.HoursOnServer = (uint)((int)sqlDataReader.GetValue(1));
                        info.MinutesOnServer = (uint)(int)sqlDataReader.GetValue(2);
                        info.SecondsOnServer = (uint)(int)sqlDataReader.GetValue(3);
                        info.Exp = (uint)(int)sqlDataReader.GetValue(4);
                        info.Coin = (uint)(int)sqlDataReader.GetValue(5);
                        sqlDataReader.Close();
                    }
                    sqlConnection.Close();
                    sqlCommand.Dispose();
                    Console.WriteLine("User " + userId + " download");
                    return info;
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
                return null;
            }
        }

        public static async Task<GuildsInfo> GetFromMongoUserVoiceTimeAsync(ulong userId, ulong guildId, IMongoDBWorker mongoDBWorker)
        {
            // Перезапись пользователя
            try
            {
                var user = (await mongoDBWorker.ReadDataAsync<UserProp>((item) => item.UserId == userId, "DisBot", "Users")).FirstOrDefault();
                if (user != null)
                {
                    return user.GuildsInfos.Find((g) => g.GuildId == guildId);
                }
                return null;
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                Console.WriteLine(s);
                return null;
            }
        }
    }

    public class MyInst
    {
        public InstagramParser InstagramProfile { get; set; }
        public int NumberSelectedImage { get; set; }
        public MyInst() { }
        public MyInst(InstagramParser instagram, int num)
        {
            InstagramProfile = instagram;
            NumberSelectedImage = num;
        }

        public PostInstagramInfo ToNextPost()
        {
            NumberSelectedImage++;
            if(NumberSelectedImage >= InstagramProfile.Posts.Count)
            {
                NumberSelectedImage = 0;
            }
            return InstagramProfile.Posts.ElementAt(NumberSelectedImage);
        }

        public PostInstagramInfo ToPrevPost()
        {
            NumberSelectedImage--;
            if (NumberSelectedImage < 0)
            {
                NumberSelectedImage = InstagramProfile.Posts.Count - 1;
            }
            return InstagramProfile.Posts.ElementAt(NumberSelectedImage);
        }
    }

    public static class Exp
    {
        public static int ConvertMinutesInExp(int min)
        {
            //exp за минуту
            double expPerMin = 1.2;
            return (int)(min * expPerMin);
        }
    }

    public static class StrToStream
    {
        public static Stream ToStream(this string str)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(str));
        }
    }

    public static class CreateImage
    {
        private static int GetLevel(int exp)
        {
            if (exp < 1000)
            {
                return exp / 100;
            }
            return 10 + ((exp - 1000) / 1000);
        }

        private static int GetProcent(int exp)
        {
            if (exp < 1000)
            {
                int lev = GetLevel(exp);
                return exp - lev * 100;
            }
            int lvl = GetLevel(exp);
            return (exp - 1000 - (lvl - 10) * 1000) / 10;
        }

        /// <summary>
        /// Создает картинку со статистикой
        /// </summary>
        /// <param name="message">DiscordMessage</param>
        /// <returns>Возвращает название файла *.jpg</returns>
        public static async Task<string> GetStatsImage(DiscordMessage message, int exp)
        {
            Random random = new Random((int)DateTime.UtcNow.Ticks);
            int lvl = GetLevel(exp);
            int proc = GetProcent(exp);
            int oneProc = 6;
            Bitmap image = new Bitmap("back.jpg");
            Bitmap loadedBitmap = null;
            Bitmap resizeAvatar = null;
            //
            try
            {
                var request = System.Net.WebRequest.Create(message.Author.AvatarUrl);
                var response = await request.GetResponseAsync();
                using (var responseStream = response.GetResponseStream())
                {
                    loadedBitmap = new Bitmap(responseStream);
                }
                resizeAvatar = new Bitmap(loadedBitmap, 115, 115);
                loadedBitmap.Dispose();
                Console.WriteLine("Загружена ава");
            }
            catch (System.Net.WebException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            using (var g = Graphics.FromImage(image))
            {
                g.DrawString("Lvl = " + lvl.ToString(), new Font("Lucida Sans", 20, FontStyle.Bold), Brushes.White, 600, 20);
                g.DrawString("Coins = " + random.Next(-10000, 1000), new Font("Lucida Sans", 20, FontStyle.Bold), Brushes.White, 600, 100);
                g.DrawString(message.Author.Username + " #" + message.Author.Discriminator, new Font("Lucida Sans", 26, FontStyle.Bold), Brushes.White, 300, 180);
                g.DrawString(exp.ToString() + " exp", new Font("Lucida Sans", 20, FontStyle.Bold), Brushes.White, 800, 180);
                g.DrawImage(resizeAvatar, new Point(52, 144));
                using (Brush brush = new LinearGradientBrush(new Point(303, 0), new Point(903, 0), Color.Aquamarine, Color.DeepPink))
                {
                    var p = new Pen(brush, 37);
                    g.DrawLine(p, 303, 244, 303 + proc * oneProc, 244);
                    p.Dispose();
                }
            }
            //
            string str = "tmp" + Guid.NewGuid().ToString() + ".jpg";
            image.Save(str, System.Drawing.Imaging.ImageFormat.Jpeg);
            image.Dispose();
            resizeAvatar.Dispose();
            return str;
        }

        public static async Task<Bitmap> GetStatsImageB(DiscordMessage message)
        {
            Random random = new Random((int)DateTime.UtcNow.Ticks);
            int exp = random.Next(0, 10000);
            int lvl = GetLevel(exp);
            int proc = GetProcent(exp);
            int oneProc = 6;
            Bitmap image = new Bitmap("back.jpg");
            Bitmap loadedBitmap = null;
            Bitmap resizeAvatar = null;
            //
            try
            {
                var request = System.Net.WebRequest.Create(message.Author.AvatarUrl);
                var response = request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                {
                    loadedBitmap = new Bitmap(responseStream);
                }
                resizeAvatar = new Bitmap(loadedBitmap, 115, 115);
                loadedBitmap.Dispose();
                Console.WriteLine("Загружена ава");
            }
            catch (System.Net.WebException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            using (var g = Graphics.FromImage(image))
            {
                g.DrawString("Пошел нахуй / Lvl = " + lvl.ToString(), new Font("Lucida Sans", 20, FontStyle.Bold), Brushes.White, 600, 20);
                g.DrawString("Пошел нахуй Coins = " + random.Next(-10000, 1000), new Font("Lucida Sans", 20, FontStyle.Bold), Brushes.White, 600, 100);
                g.DrawString(message.Author.Username + " #" + message.Author.Discriminator, new Font("Lucida Sans", 26, FontStyle.Bold), Brushes.White, 300, 180);
                g.DrawString(exp.ToString() + " exp", new Font("Lucida Sans", 20, FontStyle.Bold), Brushes.White, 800, 180);
                g.DrawImage(resizeAvatar, new Point(52, 144));
                using (Brush brush = new LinearGradientBrush(new Point(303, 0), new Point(903, 0), Color.Aquamarine, Color.DeepPink))
                {
                    var p = new Pen(brush, 37);
                    g.DrawLine(p, 303, 244, 303 + proc * oneProc, 244);
                    p.Dispose();
                }
            }
            //
            string str = "tmp" + Guid.NewGuid().ToString() + ".jpg";
            image.Save(str, System.Drawing.Imaging.ImageFormat.Jpeg);
            resizeAvatar.Dispose();
            return image;
        }
    }
}
