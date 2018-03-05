using System;
using System.Collections.Generic;
using System.Linq;
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
using VkNet.Enums.Filters;

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
    public class RML_Methods
    {
        public static DispatcherTimer timer_autopost;
        public static DispatcherTimer timer_friend_add;
        public TimeSpan tm;
        public DateTime st_dt;
        public DateTime st_tick;
        List<VkNet.Model.RequestParams.WallPostParams> prms;
       

        private string[] str;
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
        public void RmlWriteResults()
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

        }
        public void RmlStartPop()
        {
            
           MainWindow.vkApi.Friends.Add(users[users_done].Id);
           users_done++; 
        }
        public void RmlStartAutoPost()
        {
            timer_autopost = new System.Windows.Threading.DispatcherTimer();
            timer_autopost.Tick += new EventHandler(timer_autopost_ticker);
            timer_autopost.Interval = new TimeSpan(0, 0, 1);
            tm = new TimeSpan(0, 5, 0);
            st_dt = DateTime.Now;

            timer_friend_add = new System.Windows.Threading.DispatcherTimer();
            timer_friend_add.Tick += new EventHandler(timer_friend_add_tick);
            timer_friend_add.Interval = new TimeSpan(0, 0, 1);

            prms = new List<VkNet.Model.RequestParams.WallPostParams>();

            if (str == null)
                RmlReadGroups(@"Data/groups.dat");

            for (int i = 0; i < str.Length; i++)
            {
                prms.Add(new VkNet.Model.RequestParams.WallPostParams
                {
                    OwnerId = -int.Parse(str[i].Split(' ')[0]),
                    Message = "Добавляйтесь в друзья!\nВ подписчиках не оставляю!\nЗаявку одобряю моментально!"
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
            

            timer_autopost.Interval = new TimeSpan(0, 5, 0);
            foreach (var prm in prms)
            {
                try
                {
                    MainWindow.vkApi.Wall.Post(prm);
                    System.Threading.Thread.Sleep(350);
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

            rml_methods.tm = new TimeSpan(0,5,0) - (DateTime.Now - rml_methods.st_dt);
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
            rml_methods.RmlStartAutoPost();
            timer2.Start();
            MessageBox.Show("Накрутка запущена!");
        }

        private void Button_auto_end_Click(object sender, RoutedEventArgs e)
        {
            rml_methods.RmlStopAutoPost();
            MessageBox.Show("Накрутка завершена!");
        }
    }

   
}
