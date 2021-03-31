using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
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
using System.Threading.Tasks;
using YandexDisk.Client;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;

namespace DisBot
{
    class DisBotMain
    {
        static ulong ChannelLogId = 799287181508083724; //В какой текстовый канал пишутся логи
        private static SignalHandler signalHandler;
        static Config conf = new Config();

        static List<UserOnline> UsersOnline = new List<UserOnline>(); //Все пользователи, находящиеся online
        static List<IdUserIdGuild> IdUsersRainbow = new List<IdUserIdGuild>(); //Пользователи с градиентным цветом роли

        static DiscordClient discord;
        static IDiskApi disk;
        static YouTubeService youtubeService;

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
        static readonly string START_DATA = "[22.8.1488]";
        static readonly string DONTWORK = "(Не работает)";

        static readonly Random rand = new Random((int)DateTime.UtcNow.Ticks);

        static void Main(string[] args)
        {
            //token = Console.ReadLine();
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

            using (StreamWriter fs = new StreamWriter("BackUpProfile.json", false, System.Text.Encoding.Default))
            {            
                fs.WriteLine(JsonConvert.SerializeObject(UsersOnline));
                Console.Write("-BackUp-");
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

            //Сигнал закрытия консоли
            signalHandler += HandleConsoleSignal;
            ConsoleHelper.SetSignalHandler(signalHandler, true);

            //Обработка старого бэкапа
            try
            {
                using (StreamReader fs = new StreamReader("BackUpProfile.json"))
                {
                    UsersOnline = JsonConvert.DeserializeObject<List<UserOnline>>(await fs.ReadToEndAsync());
                    if (UsersOnline != null)
                    {
                        await BackupFromLastExit(UsersOnline.ToArray());
                        Console.Clear();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("------------Backup successfully uploaded------------");
                        Console.ForegroundColor = ConsoleColor.White;
                        UsersOnline.Clear();
                    }
                    else
                    {
                        UsersOnline = new List<UserOnline>();
                        Console.WriteLine("------------Backup not founded------------");
                    }
                }
                File.Delete("BackUpProfile.json");
                //using (StreamReader stream = new StreamReader(await disk.Files.DownloadFileAsync(("/DisBot/" + "BackUpProfile.json"))))
                //{
                //    UsersOnline = JsonConvert.DeserializeObject<List<UserOnline>>(await stream.ReadToEndAsync());
                //    Backup(UsersOnline.ToArray());
                //    UsersOnline.Clear();
                //}

            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("------------Backup not founded------------");
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine("------------Backup not founded------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            await discord.ConnectAsync();

            discord.Heartbeated += Heartbeated;
            discord.Heartbeated += ChangeRoleColor;

            discord.SocketClosed += Discord_SocketClosed;

            discord.Resumed += Discord_Resumed;

            discord.SocketErrored += Discord_SocketErrored;

            discord.VoiceStateUpdated += Discord_VoiceStateUpdated;

            discord.MessageReactionAdded += Discord_MessageReactionAdded;

            discord.GuildDownloadCompleted += Discord_GuildDownloadCompleted;

            discord.MessageCreated += async (sender, e) =>
            {
                string message = e.Message.Content;

                if (e.Author.Id == e.Guild.Owner.Id) //админские команды
                {
                    if (message.ToLower().StartsWith(RESTART))
                    {
                        await e.Message.RespondAsync("```css\nDisconnect" + "```");
                        var UserClone = new UserOnline[UsersOnline.Count];
                        UsersOnline.CopyTo(UserClone);
                        Backup(UserClone);
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

                    if (message.ToLower().StartsWith("!!createdatatable1"))
                    {
                        string connection = @"Data Source=.\SQLEXPRESS; Initial catalog=test1; Integrated Security=True";
                        try
                        {
                            using (SqlConnection sqlConnection = new SqlConnection(connection))
                            {
                                sqlConnection.Open();
                                string command = "CREATE TABLE " + "Guild" + e.Guild.Id.ToString() + " (UserId BIGINT PRIMARY KEY, HoursInVoice INT, MinutesInVoice INT, SecondsInVoice INT, Exp INT, Coins INT)";
                                //string command = "CREATE DATABASE " + "test1";
                                using (SqlCommand sqlCommand = new SqlCommand(command, sqlConnection))
                                {
                                    sqlCommand.ExecuteNonQuery();
                                    sqlConnection.Close();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            string qsdawd = ex.Message;
                        }
                        return;
                    }

                    if (message.ToLower().StartsWith("!!createdatatable2"))
                    {
                        string connection = @"Data Source=.\SQLEXPRESS; Initial catalog=test1; Integrated Security=True";
                        try
                        {
                            using (SqlConnection sqlConnection = new SqlConnection(connection))
                            {
                                sqlConnection.Open();
                                string command = "CREATE TABLE " + "BackUpDB" + " (DateExit DATETIME PRIMARY KEY, xml_file XML)";
                                //string command = "CREATE DATABASE " + "BackupExit";
                                using (SqlCommand sqlCommand = new SqlCommand(command, sqlConnection))
                                {
                                    sqlCommand.ExecuteNonQuery();
                                    sqlConnection.Close();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await e.Message.RespondAsync(ex.Message);
                        }
                        return;
                    }

                    if (message.ToLower().StartsWith("!!fromfiletodatabase"))
                    {
                        string connection = @"Data Source=.\SQLEXPRESS; Initial catalog=test1; Integrated Security=True";
                        try
                        {
                            using (SqlConnection sqlConnection = new SqlConnection(connection))
                            {
                                sqlConnection.Open();
                                string command = "INSERT INTO " + "Guild" + e.Guild.Id.ToString() + " (UserId, HoursInVoice, MinutesInVoice, SecondsInVoice, Exp, Coins) " + "VALUES ";
                                foreach (var memb in e.Guild.Members.Values)
                                {
                                    try
                                    {
                                        using (StreamReader fs = new StreamReader("user" + memb.Id + ".json"))
                                        {
                                            UserProp User;
                                            User = JsonConvert.DeserializeObject<UserProp>(await fs.ReadToEndAsync());
                                            var guildinfo = User.GuildsInfos.Find(g => g.GuildId == e.Guild.Id);
                                            if (guildinfo != null)
                                                command += "(" + User.UserId + "," + guildinfo.HoursOnServer + "," + guildinfo.MinutesOnServer + "," + guildinfo.SecondsOnServer + "," + guildinfo.Exp + "," + guildinfo.Coin + "),";
                                            else
                                                command += "(" + User.UserId + "," + 0 + "," + 0 + "," + 0 + "," + 0 + "," + 0 + "),";
                                        }
                                    }
                                    catch (FileNotFoundException)
                                    {
                                        command += "(" + memb.Id + "," + 0 + "," + 0 + "," + 0 + "," + 0 + "," + 0 + "),";
                                    }
                                }
                                using (SqlCommand sqlCommand = new SqlCommand(command.Substring(0, command.Length - 1), sqlConnection))
                                {
                                    sqlCommand.ExecuteNonQuery();
                                }
                                sqlConnection.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            string qsdawd = ex.Message;
                        }
                        return;
                    }

                    if (message.ToLower().StartsWith("!!selectfromtableall"))
                    {
                        string connection = @"Data Source=.\SQLEXPRESS; Initial catalog=test1; Integrated Security=True";
                        try
                        {
                            using (SqlConnection sqlConnection = new SqlConnection(connection))
                            {
                                sqlConnection.Open();
                                string command = "SELECT * FROM " + "Guild" + e.Guild.Id.ToString();

                                using (SqlCommand sqlCommand = new SqlCommand(command, sqlConnection))
                                {
                                    using (var sqlReader = sqlCommand.ExecuteReader())
                                    {
                                        string response = "";
                                        if (sqlReader.HasRows) // если есть данные
                                        {
                                            // выводим названия столбцов
                                            response += sqlReader.GetName(0) + "\t" + sqlReader.GetName(1) + "\t" + sqlReader.GetName(2) + "\n";

                                            while (sqlReader.Read()) // построчно считываем данные
                                            {
                                                object id = sqlReader.GetValue(0);
                                                object hours = sqlReader.GetValue(1);
                                                object minutes = sqlReader.GetValue(2);

                                                response += id + "\t" + hours + "\t" + minutes + "\n";
                                            }
                                            await e.Message.RespondAsync(response);
                                        }
                                    }
                                }
                                sqlConnection.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            string qsdawd = ex.Message;
                        }
                        return;
                    }

                    if (message.ToLower().StartsWith("!!selectfromtable"))
                    {
                        string connection = @"Data Source=.\SQLEXPRESS; Initial catalog=test1; Integrated Security=True";
                        try
                        {
                            using (SqlConnection sqlConnection = new SqlConnection(connection))
                            {
                                sqlConnection.Open();
                                string command = "SELECT * FROM " + "Guild" + e.Guild.Id.ToString() + " WHERE UserId=" + e.Author.Id;
                                using (SqlCommand sqlCommand = new SqlCommand(command, sqlConnection))
                                {
                                    using (var sqlReader = sqlCommand.ExecuteReader())
                                    {
                                        string response = "";
                                        if (sqlReader.HasRows) // если есть данные
                                        {
                                            // выводим названия столбцов
                                            response += sqlReader.GetName(0) + "\t" + sqlReader.GetName(1) + "\t" + sqlReader.GetName(2) + "\t" + sqlReader.GetName(3) + "\t" + sqlReader.GetName(4) + "\t" + sqlReader.GetName(5) + "\n";

                                            while (sqlReader.Read()) // построчно считываем данные
                                            {
                                                response += sqlReader.GetValue(0) + "\t" + sqlReader.GetValue(1) + "\t" + sqlReader.GetValue(2) + "\t" + sqlReader.GetValue(3) + "\t" + sqlReader.GetValue(4) + "\t" + sqlReader.GetValue(5) + "\n";
                                            }
                                            await e.Message.RespondAsync(response);
                                        }
                                    }
                                }
                                sqlConnection.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            string qsdawd = ex.Message;
                        }
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
                        var info = await _getData.GetFromDBUserVoiceTimeAsync(e.Author.Id, e.Guild.Id);
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
            return;
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

        private static async void Backup(UserOnline[] Users)
        {
            foreach (var item in Users)
            {
                //item.PushToJsonUserOnlineTimeAsync();
                item.PushToDBUserOnlineTimeAsync();
                //item.BackupToDiskAsync(disk);
            }
        }
        private static async Task BackupFromLastExit(UserOnline[] Users)
        {
            foreach (var item in Users)
            {
                //item.PushToJsonUserOnlineTimeAsync();
                item.PushToDBUserOnlineTimeAsync();
                //item.BackupToDiskAsync(disk);
            }
            int i = 0;
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
                //var Members = item.Value.Members.Where(i => i.Value.Presence != null);
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
                await e.Message.RespondAsync(await ParseInst(e.Message.Content[("!!inst".Length + 1)..]));
            }
            catch (Exception ex)
            {
                await e.Message.RespondAsync(ex.Message);
            }
        }
        public static async Task<string> ParseInst(string user)
        {
            var Urls = await ParseInstMethods.ParseMethod2(user);
            Random random = new Random((int)DateTime.Now.Ticks);
            if (Urls.Count > 0)
            {
                return Urls[random.Next(0, Urls.Count)];
            }
            else
            {
                return "Bad request";
            }
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
                await message.RespondWithFileAsync(path);
                File.Delete(path);
                //memoryStream.Dispose();
                //image.Dispose();
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

    public static class ParseInstMethods
    {
        /// <summary>
        /// //Ищет через сурсы. Не находит все фото в коллекциях (640x640 only)
        /// Возвращает список ссылок на фотографии или null
        /// <param name="user"></param>
        /// </summary>
        public static async Task<List<string>> Parse(string user) //Ищет через сурсы (640x640 only)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.instagram.com/" + user + "/");
            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
            string str;

            using (Stream resStream = response.GetResponseStream())
            {
                int count = 0;
                do
                {
                    count = resStream.Read(buf, 0, buf.Length);
                    if (count != 0)
                    {
                        sb.Append(Encoding.Default.GetString(buf, 0, count));
                    }
                }
                while (count > 0);
                str = sb.ToString();
            }

            List<string> Urls = new List<string>();
            str = str.Substring(str.IndexOf("body"));
            while (true)
            {
                try
                {
                    str = str.Substring(str.IndexOf("src") + "src\"=\"".Length);
                    string url1 = str.Substring(0, str.IndexOf("\""));
                    if (url1.StartsWith("http") && (str.IndexOf("config_width\":640") - url1.Length) < 10)
                    {
                        string url = url1.Replace("\\u0026", "&");
                        Urls.Add(url);
                    }
                }
                catch
                {
                    break;
                }
            }
            Random random = new Random((int)DateTime.Now.Ticks);
            if (Urls.Count > 0)
            {
                return Urls;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Находит фотки в оригинальном разрешении. 
        /// Берет все фотки из групп фотографий (Ищет через display_url) (Работает дольше, чем обычный Parse)
        /// Возвращает список ссылок на фотографии или null
        /// <param name="user"></param>
        /// </summary>
        public static async Task<List<string>> ParseMethod2(string user) //Находит фотки в оригинальном разрешении (Ищет через display_url) (Работает дольше, чем обычный Parse)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.instagram.com/" + user + "/");
            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
            string str;

            using (Stream resStream = response.GetResponseStream())
            {
                int count = 0;
                do
                {
                    count = resStream.Read(buf, 0, buf.Length);
                    if (count != 0)
                    {
                        sb.Append(Encoding.Default.GetString(buf, 0, count));
                    }
                }
                while (count > 0);
                str = sb.ToString();
            }

            List<string> Urls = new List<string>();
            str = str.Substring(str.IndexOf("body"));
            while (true)
            {
                try
                {
                    str = str.Substring(str.IndexOf("display_url\":\"") + "display_url\":\"".Length);
                    string url1 = str.Substring(0, str.IndexOf("\""));
                    if (url1.StartsWith("http"))
                    {
                        string url = url1.Replace("\\u0026", "&");
                        Urls.Add(url);
                    }
                }
                catch
                {
                    break;
                }
            }
            if (Urls.Count > 0)
            {
                //Убираем все повторы
                var UrldWithoutR = new List<String>();
                foreach (var ur in Urls)
                {
                    var item = UrldWithoutR.FirstOrDefault(i => i == ur);
                    if (item == null)
                    {
                        UrldWithoutR.Add(ur);
                    }
                }
                //
                return UrldWithoutR;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Ищет по шорткодам (надо тестить, очень странно работает)
        /// Возвращает список ссылок на фотографии или null
        /// <param name="user"></param>
        /// </summary>
        public static async Task<List<string>> ParseMethodTest(string user) //Находит скрытые? фотки, ищет по шорткодам (надо тестить, очень странно работает)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.instagram.com/" + user + "/");
            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
            string str;

            using (Stream resStream = response.GetResponseStream())
            {
                int count = 0;
                do
                {
                    count = resStream.Read(buf, 0, buf.Length);
                    if (count != 0)
                    {
                        sb.Append(Encoding.Default.GetString(buf, 0, count));
                    }
                }
                while (count > 0);
                str = sb.ToString();
            }

            List<string> Urls = new List<string>();
            str = str.Substring(str.IndexOf("body"));
            while (true)
            {
                try
                {
                    str = str.Substring(str.IndexOf("shortcode\":\"") + "shortcode\":\"".Length);
                    string shortcode = str.Substring(0, str.IndexOf('"'));
                    string url1 = str.Substring(str.IndexOf("display_url\":\"") + "display_url\":\"".Length);
                    string url2 = url1.Substring(0, url1.IndexOf('"'));
                    if (url2.StartsWith("http"))
                    {
                        string url = url2.Replace("\\u0026", "&");
                        Urls.Add(url);
                    }
                }
                catch
                {
                    break;
                }
            }
            Random random = new Random((int)DateTime.Now.Ticks);
            if (Urls.Count > 0)
            {
                return Urls;
            }
            else
            {
                return null;
            }
        }
    }
}
