using System;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using uvTrayIcon;

namespace uvTrayIcon
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TaskbarIcon());
        }
    }
}

public class TaskbarIcon : ApplicationContext
{
    public static NotifyIcon trayIcon;
    private static System.Threading.Timer timer;
    public TaskbarIcon ()
    {
        // Initialize Tray Icon
        trayIcon = new NotifyIcon()
        {
            ContextMenu = new ContextMenu(new MenuItem[]
            {
                new MenuItem("Exit", Exit)
            }),
            Visible = true,
            
        };
            
        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            #if DEBUG
            AttachConsole(-1);
            #endif
            Alarm();

        }).Start();

    }

    
    private void Alarm()
    {
        DateTime now = DateTime.Now;
        TimeSpan alarm = new TimeSpan(7, 0, 0);
        TimeSpan nextTimerWait = alarm - now.TimeOfDay;
        if (nextTimerWait <= TimeSpan.Zero)
        {
            // after 7am
            Update();
            timer = new System.Threading.Timer(x => { Update(); }, null, new TimeSpan(24,0,0) + nextTimerWait, Timeout.InfiniteTimeSpan);
        }
        else
        {
            // before 7am
            timer = new System.Threading.Timer(x => { Alarm(); }, null, nextTimerWait, Timeout.InfiniteTimeSpan);
        }

    }

    public static bool Update()
    {
        using (WebClient wc = new WebClient())
        {
            #if DEBUG
            AttachConsole(-1);
            int queryCnt = 0;
            #endif
            while (true)
            {
                DateTime now = DateTime.Now;
                if (now.Hour >= 19 || now.Hour < 7)
                {
                    OfflineTrayIcon();
                    return true;
                }

                try
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    
                    wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.3";
                    var json = wc.DownloadString("https://www.nea.gov.sg/api/ultraviolet/getall/"+Guid.NewGuid());
                    dynamic response = JsonConvert.DeserializeObject<Root>(json);
                    #if DEBUG
                    queryCnt++;
                    #endif
                    var latestTimestamp = response.CurrentUV.Hour;
                    //DateTime dt = DateTime.Parse(latestTimestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    if (Globals.LatestTimestamp != latestTimestamp)
                    {
                        Globals.LatestTimestamp = latestTimestamp;
                        DateTime currentTimestamp = DateTime.ParseExact(response.CurrentUV.Hour, "h:mmtt",
                            CultureInfo.InvariantCulture); //4:00pm
                        int currentUvIndex = response.CurrentUV.Value;
                        UpdateTrayIcon(currentUvIndex, currentTimestamp);
#if DEBUG
                        Console.WriteLine("\nReading for " + currentTimestamp + " updated at " + DateTime.Now +
                                          " with " + queryCnt + " queries");
                        Console.WriteLine("===================================================");
                        queryCnt = 0;
#endif

                        int waitDuration=0;
                        // assumption: updates take place every 15 minutes in an hour, so we wait for the next update first
                        if (now.Minute < 60) { waitDuration = 60 - now.Minute; }
                        else if (now.Minute < 45) { waitDuration = 45 - now.Minute; }
                        else if (now.Minute < 30) { waitDuration = 30 - now.Minute; }
                        else if (now.Minute < 15) { waitDuration = 15 - now.Minute; }
                        Thread.Sleep(waitDuration*60000);
                    }
                    Thread.Sleep(5000); // we're just going to be dumb and check once every 5 seconds
                }
                catch (Exception e)
                {
                    #if DEBUG
                    Console.WriteLine(e);
                    #endif
                }
            }
        }
        
    }

    
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int pid);
    
    private static void UpdateTrayIcon(int currentUvIndex, DateTime currentTimestamp)
    {
        trayIcon.Icon = new Icon("assets/" + currentUvIndex + ".ico");
        if (currentUvIndex > 13)
        {
            trayIcon.Icon = new Icon("assets/13.ico");
            trayIcon.Text = "Last updated: " + currentTimestamp;
            trayIcon.Visible = true;
        }
    }

    public static void OfflineTrayIcon(){
        trayIcon.Icon = new Icon("assets/offline.ico");
        trayIcon.Text = "It's past 7pm sunset, wait for 7am sunrise";
        trayIcon.Visible = true;
    }

    void Exit(object sender, EventArgs e)
    {
        // Hide tray icon, otherwise it will remain shown until user mouses over it
        trayIcon.Visible = false;

        Application.Exit();
    }
}

public static class Globals
{
    public static string LatestTimestamp = null;
}
