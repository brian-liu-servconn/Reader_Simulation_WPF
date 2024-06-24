using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
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
using rfmt;
using static Reader_Simulation_WPF.impjinData;

namespace Reader_Simulation_WPF
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        int tagqty = 0;
        bool canwork = false;
        ObservableCollection<Member> memberData = new ObservableCollection<Member>();

        public MainWindow()
        {
            InitializeComponent();

            dataGrid.DataContext = memberData;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            setlbl1();
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            setlbl1();
            tagqty = 0;
            lbl2.Content = "0";
            canwork = true;
            BtnSend.IsEnabled = false;
            BtnStop.IsEnabled = true;
            Thread th = new Thread(new ParameterizedThreadStart(goWork));
            th.Start(new int[] { (int)SendTag.Value, (int)SendCnt.Value, (int)SendMiliSec.Value });
        }

        private void goWork(Object obj)
        {
            int[] q = (int[])obj;
            for (int i = 0; i < q[1]; i++)
            {
                if (canwork)
                {
                    sendAPI(q[0]);
                    Thread.Sleep(q[2]);
                }
            }
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                canwork = false;
                BtnSend.IsEnabled = true;
                BtnStop.IsEnabled = false;
            }));
        }

        private void setlbl1()
        {
            try
            {
                lbl1.Content = "/" + ((int)SendTag.Value * (int)SendCnt.Value).ToString();
            }
            catch (Exception ex)
            {
            }
        }

        private void sendAPI(int qty)
        {
            List<impjinData.Tags> tags = new List<impjinData.Tags>();
            for (int i = 0; i < qty; i++)
            {
                DateTime now = DateTime.Now;
                impjinData.Tags t = new impjinData.Tags();
                t.timestamp = now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") + "00Z";
                t.eventType = "tagInventory";
                impjinData.data d = new impjinData.data();
                string sepc = "RS" + now.ToString("yyyyMMddHHmmssffff");
                string epc = clsRFMT.getEPCtoHEX(sepc);
                d.epc = Convert.ToBase64String(StringToByteArray(epc));
                d.epcHex = epc;
                d.tid = "E28000" + now.ToString("yyyyMMddHHmmssffff");
                d.tidHex = Convert.ToBase64String(StringToByteArray(d.tid));
                d.pc = Convert.ToBase64String(StringToByteArray(clsRFMT.getPC(1, 0, 1, "A5", epc)));
                d.antennaPort = 1;
                d.peakRssiCdbm = -5800;
                d.frequency = 926750;
                d.transmitPowerCdbm = 3000;
                d.phaseAngle = 33.75;
                t.tagInventoryEvent = d;
                tags.Add(t);
                setListText2("EPC: " + sepc + " / " + epc);
            }
            tagqty += qty;
            try
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    FetchDataAsync(IPHost.Text, tags);
                }));
            }
            catch
            {
                setListText("Server Timeout!");
            }
        }

        private Task FetchDataAsync(string url, List<Tags> tags)
        {
            return Task.Run(() =>
            {
                string j = Newtonsoft.Json.JsonConvert.SerializeObject(tags);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.KeepAlive = true;
                request.ContentType = "application/json";
                request.Timeout = 100000;
                string param = j;
                byte[] bs = Encoding.UTF8.GetBytes(param);
                try
                {
                    using (Stream reqStream = request.GetRequestStream())
                    {
                        reqStream.Write(bs, 0, bs.Length);
                    }
                }
                catch
                {
                    setListText("Server Timeout!");
                }
                setListText("送出" + tags.Count.ToString() + "個標籤。");
                var httpResponse = (HttpWebResponse)request.GetResponse();
                string aa = httpResponse.StatusCode.ToString();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    string theMessage = streamReader.ReadToEnd();
                    setListText("收到回應:" + aa);
                }
            });

        }

        private void setListText(string text)
        {
            Application.Current.Dispatcher.Invoke(new Action(delegate
            {
                string a = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + " - " + text;
                listBox1.Items.Add(a);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                lbl2.Content = tagqty.ToString();
            }));
        }

        private void setListText2(string text)
        {
            Application.Current.Dispatcher.Invoke(new Action(delegate
            {
                string datatime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");
                string a = datatime + " - " + text;
                memberData.Add(new Member()
                {
                    Date = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                    Name = text,
                });
                listBox2.Items.Add(a);
                listBox2.SelectedIndex = listBox2.Items.Count - 1;
                dataGrid.DataContext = memberData;
            }));
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            canwork = false;
            BtnSend.IsEnabled = true;
            BtnStop.IsEnabled = false;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            lbl2.Content = "0";
            listBox1.Items.Clear();
            listBox2.Items.Clear();
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private void SendTag_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            setlbl1();
        }

        private void SendCnt_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            setlbl1();
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
            {
                System.Text.StringBuilder copy_buffer = new System.Text.StringBuilder();
                foreach (object item in listBox1.SelectedItems)
                    copy_buffer.AppendLine(item.ToString());
                if (copy_buffer.Length > 0)
                    Clipboard.SetText(copy_buffer.ToString());
            }
        }

        private void listBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
            {
                System.Text.StringBuilder copy_buffer = new System.Text.StringBuilder();
                foreach (object item in listBox2.SelectedItems)
                    copy_buffer.AppendLine(item.ToString());
                if (copy_buffer.Length > 0)
                    Clipboard.SetText(copy_buffer.ToString());
            }

        }
    }

    public class Member
    {
        public string Date { get; set; }
        public string Name { get; set; }
    }
}
