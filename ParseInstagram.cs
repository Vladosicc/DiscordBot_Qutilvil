using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ParseInstagram
{
    public class InstagramParser
    {
        public IReadOnlyCollection<PostInstagramInfo> Posts { get; }
        public ProfileInstagramInfo ProfileInfo { get; }
        public string UserName { get; }

        private InstagramParser(string us) { UserName = us; }
        private InstagramParser(string us, ProfileInstagramInfo pr, IEnumerable<PostInstagramInfo> posts) { UserName = us; ProfileInfo = pr; Posts = posts.ToList(); }
        public static InstagramParser Create(string UserName)
        {
            return new InstagramParser(UserName);
        }

        public string CreateUrlOnInstagram()
        {
            return "https://www.instagram.com/" + UserName;
        }

        public async Task<InstagramParser> ParseAsync()
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://dumpor.com/v/" + UserName + "/");
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

            List<PostInstagramInfo> posts = new List<PostInstagramInfo>();
            return await Task.Run(() =>
            {
                var imgItemsAll = str.Split("content__item");
                List<string> reallyCards = new List<string>();
                //Находим информацию о пользователе
                str = imgItemsAll[0].Substring(imgItemsAll[0].IndexOf("background-image: url(&#39;") + "background-image: url(&#39;".Length);
                string urlAvatar = str.Substring(0, str.IndexOf("&#39"));
                var li = imgItemsAll[0].Split("list__item");
                //Кол-во постов
                int postsCount = int.Parse(li[1].Substring(li[1].IndexOf(">") + 1, li[1].IndexOf(" P") - li[1].IndexOf(">")));
                //Кол-во фоловеров
                int folCount = int.Parse(li[2].Substring(li[2].IndexOf(">") + 1, li[2].IndexOf(" F") - li[2].IndexOf(">")));
                //Кол-во фоловингов
                int folingCount = int.Parse(li[3].Substring(li[3].IndexOf(">") + 1, li[3].IndexOf(" F") - li[3].IndexOf(">")));
                //Если ли статус у аккаунта
                int brIndex = li[3].IndexOf("<br />");
                string statusProfile = "";
                if (brIndex > 0)
                {
                    str = li[3].Substring(brIndex + "<br />".Length);
                    statusProfile = str.Remove(str.IndexOf("<"));
                }
                //Находим имя аккаунта (не тег)
                str = li[0].Substring(li[0].IndexOf("<h1>") + "<h1>".Length);
                string profileName = str.Remove(str.IndexOf("<"));

                var profileInfo = new ProfileInstagramInfo(urlAvatar, profileName, statusProfile, postsCount, folCount, folingCount);

                //Убираем первую и рекламные
                for (int i = 1; i < imgItemsAll.Length; i++)
                {
                    if (!imgItemsAll[i].Contains("ads"))
                    {
                        reallyCards.Add(imgItemsAll[i]);
                    }
                }
                foreach (var card in reallyCards)
                {
                    //Находим ссылку на изображение
                    string tmp = card.Substring(card.IndexOf("http"));
                    tmp = tmp.Substring(0, tmp.IndexOf("\""));
                    if (tmp.EndsWith('\\'))
                        tmp.Replace("\\", string.Empty);
                    string Url = tmp;
                    //Находим количество лайков комментов и дату
                    tmp = card.Substring(card.IndexOf("bx-like"));
                    tmp = tmp.Substring(tmp.IndexOf("ml-1") + 6); //7 спец символов после ml-1 скипаем
                    int like = int.Parse(tmp.Remove(tmp.IndexOf("<")));
                    tmp = tmp.Substring(tmp.IndexOf("ml-1") + 6);
                    int comm = int.Parse(tmp.Remove(tmp.IndexOf("<")));
                    tmp = tmp.Substring(tmp.IndexOf("ml-1") + 6);
                    string date = tmp.Remove(tmp.IndexOf("<"));

                    //Проверяем, есть коммент или нет
                    int c = card.IndexOf("<p><br /> ");
                    string desc = "";
                    if (c > 0)
                    {
                        tmp = card.Substring(c);
                        desc = tmp.Remove(tmp.IndexOf(" <br />") + " <br />".Length - 1);
                    }

                    posts.Add(new PostInstagramInfo(Url, desc, like, comm, date, null));
                }

                return new InstagramParser(UserName, profileInfo, posts);
            });
        }

        /// <summary>
        /// Работа без обращения к домену instagram.com, через https://dumpoir.com
        /// Для обхода требования авторизации
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<string>> ParseWithDumpoir(string user)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://dumpoir.com/v/" + user + "/");
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

            var imgItemsAll = str.Split("content__item");
            List<string> reallyCards = new List<string>();
            //Убираем первую и рекламные
            for (int i = 1; i < imgItemsAll.Length; i++)
            {
                if (!imgItemsAll[i].Contains("ads"))
                {
                    reallyCards.Add(imgItemsAll[i]);
                }
            }
            foreach (var card in reallyCards)
            {
                string tmp = card.Substring(card.IndexOf("http"));
                tmp = tmp.Substring(0, tmp.IndexOf("\""));
                if (tmp.EndsWith('\\'))
                    tmp.Replace("\\", string.Empty);
                Urls.Add(tmp);
            }
            return Urls;
        }

        /// <summary>
        /// Ищет через сурсы. Не находит все фото в коллекциях (640x640 only)
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

    public class ProfileInstagramInfo
    {
        public string ProfileImageUrl { get; }
        public string ProfileName { get; }
        public string ProfileStatus { get; }
        public int CountPosts { get; }
        public int CountFollowers { get; }
        public int CountFollowing { get; }

        public ProfileInstagramInfo(string ProfileImageUrl, string ProfileName, string ProfileStatus, int CountPosts, int CountFollowers, int CountFollowing)
        {
            this.ProfileImageUrl = ProfileImageUrl;
            this.ProfileName = ProfileName;
            this.ProfileStatus = ProfileStatus;
            this.CountPosts = CountPosts;
            this.CountFollowers = CountFollowers;
            this.CountFollowing = CountFollowing;
        }
    }

    public class PostInstagramInfo
    {
        public string UrlMainImage { get; }
        public string Description { get; }
        public int CountLikes { get; }
        public int CountComments { get; }
        public string Date { get; }
        public IEnumerable<string> UrlImages { get; }

        public PostInstagramInfo(string UrlMainImage, string Description, int CountLikes, int CountComments, string Date, IEnumerable<string> UrlImages)
        {
            this.UrlMainImage = UrlMainImage;
            this.Description = Description;
            this.CountComments = CountComments;
            this.CountLikes = CountLikes;
            this.Date = Date;
            this.UrlImages = UrlImages;
        }
    }
}
