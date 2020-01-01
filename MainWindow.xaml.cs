using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Scanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }



        public void Start_scan_tcp_connect()                            //TCP连接扫描线程
        {
            m_textbox_result_one.Dispatcher.Invoke(new Action(() => m_textbox_result_one.Text=""));
            Socket test = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress target_ip = IPAddress.Parse(m_textbox_target_ip_one.Dispatcher.Invoke(new Func<string>(()=> m_textbox_target_ip_one.Text.Trim())));
            int port_min = int.Parse(m_textbox_target_port_min_one.Dispatcher.Invoke(new Func<string>(()=> m_textbox_target_port_min_one.Text.Trim())));
            int port_max = int.Parse(m_textbox_target_port_max_one.Dispatcher.Invoke(new Func<string>(() => m_textbox_target_port_max_one.Text.Trim())));

            int i;
            for (i = port_min; i <= port_max; i++)
            {
                try
                {
                    test.Connect(target_ip, i);
                }
                catch (Exception)
                {
                    m_textbox_result_one.Dispatcher.Invoke(new Action(() => m_textbox_result_one.AppendText(target_ip.ToString() + ":" + i + "    |inactive\n")));
                    m_textbox_result_one.Dispatcher.Invoke(new Action(() => m_textbox_result_one.ScrollToEnd()));
                    continue;
                }
                m_textbox_result_one.Dispatcher.Invoke(new Action(() => m_textbox_result_one.AppendText(target_ip.ToString() + ":" + i + "    |active\n")));
                m_textbox_result_one.Dispatcher.Invoke(new Action(() => m_textbox_result_one.ScrollToEnd()));
                test.Dispose();
                test = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            }
            m_textbox_result_one.Dispatcher.Invoke(new Action(() => m_textbox_result_one.AppendText("==================================\n扫描完成\n")));
            m_textbox_result_one.Dispatcher.Invoke(new Action(() => m_textbox_result_one.ScrollToEnd()));
            m_button_start_one.Dispatcher.Invoke(new Action(()=>m_button_start_one.IsEnabled=true));
            m_textbox_target_ip_one.Dispatcher.Invoke(new Action(() => m_textbox_target_ip_one.IsEnabled = true));
            m_textbox_target_port_max_one.Dispatcher.Invoke(new Action(() => m_textbox_target_port_max_one.IsEnabled = true));
            m_textbox_target_port_min_one.Dispatcher.Invoke(new Action(() => m_textbox_target_port_min_one.IsEnabled = true));
        }

        protected override void OnClosed(EventArgs e)                   //关闭所有线程并关闭程序
        {
            System.Environment.Exit(0);
            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)            //判断是否结束程序
        {
            if (!m_button_start_one.IsEnabled||!m_button_start_two.IsEnabled||!m_button_start_three.IsEnabled)
            {
                if(MessageBox.Show("扫描正在运行，确定要关闭程序嘛？","警告",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes)
                {
                    e.Cancel = false;
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = false;
            }
        }

        private void Button_start_one_Click(object sender, RoutedEventArgs e)             //TCP连接 开始扫描案件事件
        {
            m_textbox_result_one.Text = "";
            Thread start = new Thread(new ThreadStart(Start_scan_tcp_connect));
            start.Start();
            m_button_start_one.IsEnabled = false;
            m_textbox_target_ip_one.IsEnabled = false;
            m_textbox_target_port_max_one.IsEnabled = false;
            m_textbox_target_port_min_one.IsEnabled = false;
        }


        //
        //多线程扫描,方案一
        //

        public bool[] is_active_two=new bool[65535];
        public int finish_scan_num_two;
        public int port_all_num_two;
        public Mutex mutex_two = new Mutex();

        public void Start_multi_thread_scan_two(object target_port)
        {
            int target_port_num = int.Parse(target_port.ToString());
            Socket test = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress target_ip = IPAddress.Parse(m_textbox_target_ip_two.Dispatcher.Invoke(new Func<string>(() => m_textbox_target_ip_two.Text.Trim())));

            try
            {
                test.Connect(target_ip, target_port_num);
            }
            catch (Exception)
            {
                mutex_two.WaitOne();
                finish_scan_num_two += 1;
                m_textbox_result_two.Dispatcher.Invoke(new Action(()=>m_textbox_result_two.Text=string.Format("{0}/{1} 已完成",finish_scan_num_two,port_all_num_two)));
                mutex_two.ReleaseMutex();
                test.Dispose();
                return;
            }
            mutex_two.WaitOne();
            finish_scan_num_two += 1;
            is_active_two[target_port_num-1] = true;
            m_textbox_result_two.Dispatcher.Invoke(new Action(() => m_textbox_result_two.Text = string.Format("{0}/{1} 已完成", finish_scan_num_two, port_all_num_two)));
            mutex_two.ReleaseMutex();
            test.Dispose();

        }

        public void Thread_control_two()
        {
            //初始化
            int i;
            int port_min = int.Parse(m_textbox_target_port_min_two.Dispatcher.Invoke(new Func<string>(() => m_textbox_target_port_min_two.Text.Trim())));
            int port_max = int.Parse(m_textbox_target_port_min_two.Dispatcher.Invoke(new Func<string>(() => m_textbox_target_port_max_two.Text.Trim())));
            port_all_num_two = port_max - port_min + 1;
            finish_scan_num_two = 0;
            for (i = 0; i < 65535; i++) is_active_two [i]= false;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            //开始扫描
            for (i = port_min; i <= port_max; i++)
            {
                new Thread(new ParameterizedThreadStart(Start_multi_thread_scan_two)).Start(i);
            }

            //判断扫描是否结束
            bool is_finish = false;
            while (true)
            {
                mutex_two.WaitOne();
                if (finish_scan_num_two == port_all_num_two) is_finish = true;
                mutex_two.ReleaseMutex();

                if (is_finish)
                {
                    sw.Stop();
                    m_textbox_result_two.Dispatcher.Invoke(new Action(() => m_textbox_result_two.Text = string.Format("扫描已完成,用时{0}ms，结果如下\n",sw.ElapsedMilliseconds)));
                    Print_result_two(port_min,port_max);
                    m_button_start_two.Dispatcher.Invoke(new Action(() => m_button_start_two.IsEnabled = true));
                    m_textbox_target_ip_two.Dispatcher.Invoke(new Action(() => m_textbox_target_ip_two.IsEnabled = true));
                    m_textbox_target_port_max_two.Dispatcher.Invoke(new Action(() => m_textbox_target_port_max_two.IsEnabled = true));
                    m_textbox_target_port_min_two.Dispatcher.Invoke(new Action(() => m_textbox_target_port_min_two.IsEnabled = true));
                    return;
                }

            }
        }

        private void Print_result_two(int min,int max)
        {
            m_textbox_result_two.Dispatcher.Invoke(new Action(() => m_textbox_result_two.AppendText("============================================\n")));
            m_textbox_result_two.Dispatcher.Invoke(new Action(() => m_textbox_result_two.AppendText("开放端口：\n")));
            for (int i = min; i <= max; i++)
            {
                if(is_active_two[i-1]) m_textbox_result_two.Dispatcher.Invoke(new Action(() => m_textbox_result_two.AppendText(string.Format("{0}\n",i))));
            }
            m_textbox_result_two.Dispatcher.Invoke(new Action(() => m_textbox_result_two.AppendText("============================================\n")));
            m_textbox_result_two.Dispatcher.Invoke(new Action(() => m_textbox_result_two.AppendText("关闭端口：\n")));
            for (int i = min; i <= max; i++)
            {
                if (!is_active_two[i - 1]) m_textbox_result_two.Dispatcher.Invoke(new Action(() => m_textbox_result_two.AppendText(string.Format("{0}\n", i))));
            }
        }
        
        private void Button_start_two_Click(object sender, RoutedEventArgs e)
        {
            m_textbox_result_two.Text = "";
            Thread start = new Thread(new ThreadStart(Thread_control_two));
            start.Start();
            m_button_start_two.IsEnabled = false;
            m_textbox_target_ip_two.IsEnabled = false;
            m_textbox_target_port_max_two.IsEnabled = false;
            m_textbox_target_port_min_two.IsEnabled = false;
        }

        //
        //多线程扫描，方案二
        //

        public bool[] is_active_three = new bool[65535];
        public int target_num_min_three;
        public int now_scan_port_three;
        public int target_num_max_three;
        public int port_all_num_three;
        public int finish_scan_num_three;
        public Mutex mutex_three = new Mutex();

        public void Start_multi_thread_scan_three()
        {
            int now_scan_port=target_num_min_three;
            bool is_finish = false;
            bool is_connected;
            Socket test;
            IPAddress target_ip = IPAddress.Parse(m_textbox_target_ip_one.Dispatcher.Invoke(new Func<string>(() => m_textbox_target_ip_three.Text.Trim())));
            while (!is_finish)
            {
                mutex_three.WaitOne();
                if (now_scan_port_three <= target_num_max_three)
                {
                    now_scan_port = now_scan_port_three;
                    now_scan_port_three += 1;
                }
                else
                {
                    is_finish = true;
                }
                mutex_three.ReleaseMutex();

                if (!is_finish)
                {
                    test = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    is_connected = true;
                    try
                    {
                        test.Connect(target_ip, now_scan_port);
                    }
                    catch (Exception)
                    {
                        mutex_three.WaitOne();
                        finish_scan_num_three += 1;
                        m_textbox_result_three.Dispatcher.Invoke(new Action(() => m_textbox_result_three.Text = string.Format("{0}/{1} 已完成", finish_scan_num_three, port_all_num_three)));
                        mutex_three.ReleaseMutex();
                        is_connected = false;
                    }
                    finally
                    {
                        test.Dispose();
                    }
                    if (is_connected)
                    {
                        mutex_three.WaitOne();
                        is_active_three[now_scan_port-1] = true;
                        finish_scan_num_three += 1;
                        m_textbox_result_three.Dispatcher.Invoke(new Action(() => m_textbox_result_three.Text = string.Format("{0}/{1} 已完成", finish_scan_num_three, port_all_num_three)));
                        mutex_three.ReleaseMutex();
                    }
                }
            }
        }

        public void Thread_control_three()
        {
            //初始化
            int i;
            target_num_min_three = int.Parse(m_textbox_target_port_min_three.Dispatcher.Invoke(new Func<string>(() => m_textbox_target_port_min_three.Text.Trim())));
            now_scan_port_three = target_num_min_three;
            target_num_max_three = int.Parse(m_textbox_target_port_max_three.Dispatcher.Invoke(new Func<string>(() => m_textbox_target_port_max_three.Text.Trim())));
            port_all_num_three = target_num_max_three - target_num_min_three + 1;
            for (i = 0; i < 65535; i++) is_active_three[i] = false;
            finish_scan_num_three = 0;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            //开始扫描
            for(i=0;i<int.Parse(m_textbox_thread_num_three.Dispatcher.Invoke(new Func<string>(()=>m_textbox_thread_num_three.Text.Trim())));i++)
            {
                new Thread(new ThreadStart(Start_multi_thread_scan_three)).Start();
            }

            //判断是否扫描完毕
            bool is_finish = false;
            while (true)
            {
                mutex_three.WaitOne();
                if (finish_scan_num_three == port_all_num_three) is_finish = true;
                mutex_three.ReleaseMutex();

                if (is_finish)
                {
                    sw.Stop();
                    m_textbox_result_three.Dispatcher.Invoke(new Action(() => m_textbox_result_three.Text = string.Format("扫描已完成,用时{0}ms，结果如下\n", sw.ElapsedMilliseconds)));
                    Print_result_three();
                    m_button_start_three.Dispatcher.Invoke(new Action(() => m_button_start_three.IsEnabled = true));
                    m_textbox_target_ip_three.Dispatcher.Invoke(new Action(() => m_textbox_target_ip_three.IsEnabled = true));
                    m_textbox_target_port_max_three.Dispatcher.Invoke(new Action(() => m_textbox_target_port_max_three.IsEnabled = true));
                    m_textbox_target_port_min_three.Dispatcher.Invoke(new Action(() => m_textbox_target_port_min_three.IsEnabled = true));
                    m_textbox_thread_num_three.Dispatcher.Invoke(new Action(() => m_textbox_thread_num_three.IsEnabled = true));
                    return;
                }
            }
        }

        private void Print_result_three()
        {
            m_textbox_result_three.Dispatcher.Invoke(new Action(() => m_textbox_result_three.AppendText("============================================\n")));
            m_textbox_result_three.Dispatcher.Invoke(new Action(() => m_textbox_result_three.AppendText("开放端口：\n")));
            for (int i = target_num_min_three; i <= target_num_max_three; i++)
            {
                if (is_active_three[i - 1]) m_textbox_result_three.Dispatcher.Invoke(new Action(() => m_textbox_result_three.AppendText(string.Format("{0}\n", i))));
            }
            m_textbox_result_three.Dispatcher.Invoke(new Action(() => m_textbox_result_three.AppendText("============================================\n")));
            m_textbox_result_three.Dispatcher.Invoke(new Action(() => m_textbox_result_three.AppendText("关闭端口：\n")));
            for (int i = target_num_min_three; i <= target_num_max_three; i++)
            {
                if (!is_active_two[i - 1]) m_textbox_result_three.Dispatcher.Invoke(new Action(() => m_textbox_result_three.AppendText(string.Format("{0}\n", i))));
            }
        }

        private void Button_start_three_Click(object sender, RoutedEventArgs e)
        {
            m_textbox_result_three.Text = "";
            Thread start = new Thread(new ThreadStart(Thread_control_three));
            start.Start();
            m_button_start_three.IsEnabled = false;
            m_textbox_target_ip_three.IsEnabled = false;
            m_textbox_target_port_max_three.IsEnabled = false;
            m_textbox_target_port_min_three.IsEnabled = false;
            m_textbox_thread_num_three.IsEnabled = false;
        }
    }
}
