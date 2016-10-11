using System;
using System.Collections.Generic;
using System.IO.Ports;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace lidademo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort ComPort = new SerialPort();//声明一个串口      
        IList<customer> comList = new List<customer>();//可用串口集合
        private string[] ports;//可用串口数组
        private bool ComPortIsOpen = false;//COM口开启状态字，在打开/关闭串口中使用，这里没有使用自带的ComPort.IsOpen，因为在串口突然丢失的时候，ComPort.IsOpen会自动false，逻辑混乱
        private bool Listening = false;//用于检测是否没有执行完invoke相关操作，仅在单线程收发使用，但是在公共代码区有相关设置，所以未用#define隔离
        private bool WaitClose = false;//invoke里判断是否正在关闭串口是否正在关闭串口，执行Application.DoEvents，并阻止再次invoke ,解决关闭串口时，程序假死，具体参见http://news.ccidnet.com/art/32859/20100524/2067861_4.html 仅在单线程收发使用，但是在公共代码区有相关设置，所以未用#define隔离
        public MainWindow()
        {
            InitializeComponent();
        }

        private void AvailableComCbobox_PreviewMouseDown(object sender, MouseButtonEventArgs e)//刷新可用串口
        {
            GetPort();//刷新可用串口
        }

        private void GetPort()//刷新可用串口
        {

            comList.Clear();//情况控件链接资源
            AvailableComCbobox.DisplayMemberPath = "com1";
            AvailableComCbobox.SelectedValuePath = null;//路径都指为空，清空下拉控件显示，下面重新添加

            ports = new string[SerialPort.GetPortNames().Length];//重新定义可用串口数组长度
            ports = SerialPort.GetPortNames();//获取可用串口
            if (ports.Length > 0)//有可用串口
            {
                for (int i = 0; i < ports.Length; i++)
                {
                    comList.Add(new customer() { com = ports[i] });//下拉控件里添加可用串口
                }
                AvailableComCbobox.ItemsSource = comList;//可用串口下拉控件资源路径
                AvailableComCbobox.DisplayMemberPath = "com";//可用串口下拉控件显示路径
                AvailableComCbobox.SelectedValuePath = "com";//可用串口下拉控件值路径
            }
        }

        private void defaultSet_Click(object sender, RoutedEventArgs e)//重置按钮click事件
        {
            RateListCbobox.SelectedValue = "9600";//波特率默认设置9600
            ParityComCbobox.SelectedValue = "0";//校验位默认设置值为0，对应NONE
            DataBitsCbobox.SelectedValue = "8";//数据位默认设置8位
            StopBitsCbobox.SelectedValue = "1";//停止位默认设置1
        }

        public class customer//各下拉控件访问接口
        {

            public string com { get; set; }//可用串口
            public string com1 { get; set; }//可用串口
            public string BaudRate { get; set; }//波特率
            public string Parity { get; set; }//校验位
            public string ParityValue { get; set; }//校验位对应值
            public string Dbits { get; set; }//数据位
            public string Sbits { get; set; }//停止位
        }

        private void Button_Open(object sender, RoutedEventArgs e)//打开/关闭串口事件
        {
            if (AvailableComCbobox.SelectedValue == null)//先判断是否有可用串口
            {
                MessageBox.Show("无可用串口，无法打开!");
                return;//没有串口，提示后直接返回
            }
            #region 打开串口
            if (ComPortIsOpen == false)//ComPortIsOpen == false当前串口为关闭状态，按钮事件为打开串口
            {

                try//尝试打开串口
                {
                    ComPort.PortName = AvailableComCbobox.SelectedValue.ToString();//设置要打开的串口
                    ComPort.BaudRate = Convert.ToInt32(RateListCbobox.SelectedValue);//设置当前波特率
                    ComPort.Parity = (Parity)Convert.ToInt32(ParityComCbobox.SelectedValue);//设置当前校验位
                    ComPort.DataBits = Convert.ToInt32(DataBitsCbobox.SelectedValue);//设置当前数据位
                    ComPort.StopBits = (StopBits)Convert.ToDouble(StopBitsCbobox.SelectedValue);//设置当前停止位                    
                    ComPort.Open();//打开串口

                }
                catch//如果串口被其他占用，则无法打开
                {
                    MessageBox.Show("无法打开串口,请检测此串口是否有效或被其他占用！");
                    GetPort();//刷新当前可用串口
                    return;//无法打开串口，提示后直接返回
                }

                //↓↓↓↓↓↓↓↓↓成功打开串口后的设置↓↓↓↓↓↓↓↓↓
                openBtn.Content = "关闭串口";//按钮显示改为“关闭按钮”
                OpenImage.Source = new BitmapImage(new Uri("image\\On.png", UriKind.Relative));//开关状态图片切换为ON
                ComPortIsOpen = true;//串口打开状态字改为true
                WaitClose = false;//等待关闭串口状态改为false                
                defaultSet.IsEnabled = false;//打开串口后失能重置功能
                AvailableComCbobox.IsEnabled = false;//失能可用串口控件
                RateListCbobox.IsEnabled = false;//失能可用波特率控件
                ParityComCbobox.IsEnabled = false;//失能可用校验位控件
                DataBitsCbobox.IsEnabled = false;//失能可用数据位控件
                StopBitsCbobox.IsEnabled = false;//失能可用停止位控件
                //↑↑↑↑↑↑↑↑↑成功打开串口后的设置↑↑↑↑↑↑↑↑↑
            }
            #endregion
            #region 关闭串口
            else//ComPortIsOpen == true,当前串口为打开状态，按钮事件为关闭串口
            {
                try//尝试关闭串口
                {
                    ComPort.DiscardOutBuffer();//清发送缓存
                    ComPort.DiscardInBuffer();//清接收缓存
                    WaitClose = true;//激活正在关闭状态字，用于在串口接收方法的invoke里判断是否正在关闭串口
                    while (Listening)//判断invoke是否结束
                    {
                        //DispatcherHelper.DoEvents(); //循环时，仍进行等待事件中的进程，该方法为winform中的方法，WPF里面没有，这里在后面自己实现
                    }
                    ComPort.Close();//关闭串口
                    WaitClose = false;//关闭正在关闭状态字，用于在串口接收方法的invoke里判断是否正在关闭串口
                    SetAfterClose();//成功关闭串口或串口丢失后的设置
                }

                catch//如果在未关闭串口前，串口就已丢失，这时关闭串口会出现异常
                {
                    if (ComPort.IsOpen == false)//判断当前串口状态，如果ComPort.IsOpen==false，说明串口已丢失
                    {
                        SetComLose();
                    }
                    else//未知原因，无法关闭串口
                    {
                        MessageBox.Show("无法关闭串口，原因未知！");
                        return;//无法关闭串口，提示后直接返回
                    }
                }
            }
            #endregion

        }

        private void SetAfterClose()//成功关闭串口或串口丢失后的设置
        {

            openBtn.Content = "打开串口";//按钮显示为“打开串口”
            OpenImage.Source = new BitmapImage(new Uri("image\\Off.png", UriKind.Relative));
            ComPortIsOpen = false;//串口状态设置为关闭状态
            defaultSet.IsEnabled = true;//打开串口后使能重置功能
            AvailableComCbobox.IsEnabled = true;//使能可用串口控件
            RateListCbobox.IsEnabled = true;//使能可用波特率下拉控件
            ParityComCbobox.IsEnabled = true;//使能可用校验位下拉控件
            DataBitsCbobox.IsEnabled = true;//使能数据位下拉控件
            StopBitsCbobox.IsEnabled = true;//使能停止位下拉控件
        }

        private void SetComLose()//成功关闭串口或串口丢失后的设置
        {
            WaitClose = true;//;//激活正在关闭状态字，用于在串口接收方法的invoke里判断是否正在关闭串口
            while (Listening)//判断invoke是否结束
            {
                //DispatcherHelper.DoEvents(); //循环时，仍进行等待事件中的进程，该方法为winform中的方法，WPF里面没有，这里在后面自己实现
            }
            MessageBox.Show("串口已丢失");
            WaitClose = false;//关闭正在关闭状态字，用于在串口接收方法的invoke里判断是否正在关闭串口
            GetPort();//刷新可用串口
            SetAfterClose();//成功关闭串口或串口丢失后的设置
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //↓↓↓↓↓↓↓↓↓可用串口下拉控件↓↓↓↓↓↓↓↓↓
            ports = SerialPort.GetPortNames();//获取可用串口
            if (ports.Length > 0)//ports.Length > 0说明有串口可用
            {
                for (int i = 0; i < ports.Length; i++)
                {
                    comList.Add(new customer() { com = ports[i] });//下拉控件里添加可用串口
                }
                AvailableComCbobox.ItemsSource = comList;//资源路劲
                AvailableComCbobox.DisplayMemberPath = "com";//显示路径
                AvailableComCbobox.SelectedValuePath = "com";//值路径
                AvailableComCbobox.SelectedValue = ports[0];//默认选第1个串口
            }
            //↑↑↑↑↑↑↑↑↑可用串口下拉控件↑↑↑↑↑↑↑↑↑
            //↓↓↓↓↓↓↓↓↓波特率下拉控件↓↓↓↓↓↓↓↓↓
            IList<customer> rateList = new List<customer>();//可用波特率集合
            rateList.Add(new customer() { BaudRate = "9600" });
            rateList.Add(new customer() { BaudRate = "14400" });
            rateList.Add(new customer() { BaudRate = "19200" });
            rateList.Add(new customer() { BaudRate = "28800" });
            rateList.Add(new customer() { BaudRate = "38400" });
            rateList.Add(new customer() { BaudRate = "57600" });
            rateList.Add(new customer() { BaudRate = "115200" });
            RateListCbobox.ItemsSource = rateList;
            RateListCbobox.DisplayMemberPath = "BaudRate";
            RateListCbobox.SelectedValuePath = "BaudRate";
            RateListCbobox.SelectedValue = "115200";//波特率默认设置115200
            //↑↑↑↑↑↑↑↑↑波特率下拉控件↑↑↑↑↑↑↑↑↑

            //↓↓↓↓↓↓↓↓↓校验位下拉控件↓↓↓↓↓↓↓↓↓
            IList<customer> comParity = new List<customer>();//可用校验位集合
            comParity.Add(new customer() { Parity = "None", ParityValue = "0" });
            comParity.Add(new customer() { Parity = "Odd", ParityValue = "1" });
            comParity.Add(new customer() { Parity = "Even", ParityValue = "2" });
            comParity.Add(new customer() { Parity = "Mark", ParityValue = "3" });
            comParity.Add(new customer() { Parity = "Space", ParityValue = "4" });
            ParityComCbobox.ItemsSource = comParity;
            ParityComCbobox.DisplayMemberPath = "Parity";
            ParityComCbobox.SelectedValuePath = "ParityValue";
            ParityComCbobox.SelectedValue = "0";//校验位默认设置值为0，对应NONE
                                                //↑↑↑↑↑↑↑↑↑校验位下拉控件↑↑↑↑↑↑↑↑↑

            //↓↓↓↓↓↓↓↓↓数据位下拉控件↓↓↓↓↓↓↓↓↓
            IList<customer> dataBits = new List<customer>();//数据位集合
            dataBits.Add(new customer() { Dbits = "8" });
            dataBits.Add(new customer() { Dbits = "7" });
            dataBits.Add(new customer() { Dbits = "6" });
            DataBitsCbobox.ItemsSource = dataBits;
            DataBitsCbobox.SelectedValuePath = "Dbits";
            DataBitsCbobox.DisplayMemberPath = "Dbits";
            DataBitsCbobox.SelectedValue = "8";//数据位默认设置8位
            //↑↑↑↑↑↑↑↑↑数据位下拉控件↑↑↑↑↑↑↑↑↑

            //↓↓↓↓↓↓↓↓↓停止位下拉控件↓↓↓↓↓↓↓↓↓
            IList<customer> stopBits = new List<customer>();//停止位集合
            stopBits.Add(new customer() { Sbits = "1" });
            stopBits.Add(new customer() { Sbits = "1.5" });
            stopBits.Add(new customer() { Sbits = "2" });
            StopBitsCbobox.ItemsSource = stopBits;
            StopBitsCbobox.SelectedValuePath = "Sbits";
            StopBitsCbobox.DisplayMemberPath = "Sbits";
            StopBitsCbobox.SelectedValue = "1";//停止位默认设置1
            //↑↑↑↑↑↑↑↑↑停止位下拉控件↑↑↑↑↑↑↑↑↑

        }

        private void info_click(object sender, RoutedEventArgs e)//帮助-关于click事件
        {
        }

        private void feedBack_Click(object sender, RoutedEventArgs e)//帮助-反馈click事件
        {
        }
    }
}
