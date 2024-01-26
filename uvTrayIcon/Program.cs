using System;
using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
            AttachConsole(-2);
            #endif
            while (true)
            {
                // when is the next update
                var timeOfDay = DateTime.Now.TimeOfDay;
                var nextHour = Math.Ceiling(timeOfDay.TotalHours);

                if (nextHour > 19 || nextHour < 7)
                {
                    nextHour = 7; // the api only has data from 7am-7pm, let's wait for tmr 7am
                    // display offline icon when 7pm data ends
                    Task.Delay(TimeSpan.FromMilliseconds(3600000)).ContinueWith(task => OfflineTrayIcon());
                } else {
                    // within api operating hours, we show the current hour index (even though its not freshly updated)
                    Update(false);
                }
                
                // we wait till the next time where api will be updated to start checking for changes every second
                var nextFullHour = TimeSpan.FromHours(nextHour);
                var nextUpdate = (nextFullHour - timeOfDay);
                var delta = nextUpdate.TotalMilliseconds;
                #if DEBUG
                Console.WriteLine("Next update in "+nextUpdate);
                Console.WriteLine("===================================================");
                #endif
                Thread.Sleep((int) Math.Ceiling(delta));

                /*
                 * start timer to fetch data from NEA
                 * nvm timer wont work cuz the request duration is indeterminate
                System.Timers.Timer checkApiTimer = new System.Timers.Timer();
                checkApiTimer.Elapsed += new ElapsedEventHandler(CheckApiEvent);
                checkApiTimer.Interval = 1000;
                checkApiTimer.Enabled = true;
                */
                
                // it's the start of the next hour, within api operation hours, lets watch for changes
                Update(true);
            }

        }).Start();

    }

    /*
    private static void CheckApiEvent(object source, ElapsedEventArgs e)
    {
    }
    */

    public static bool Update(bool watchForChanges)
    {
        using (WebClient wc = new WebClient())
        {
            #if DEBUG
            AttachConsole(-1);
            int queryCnt = 0;
            #endif
            while (true)
            {
                try
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    var json = wc.DownloadString("https://api.data.gov.sg/v1/environment/uv-index");
                    dynamic response = JsonConvert.DeserializeObject<RootObject>(json);
                    #if DEBUG
                    queryCnt++;
                    #endif
                    var latestTimestamp = response.Root[0].LatestTimestamp;
                    // their timestamp is ISO 8601
                    DateTime dt = DateTime.Parse(latestTimestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    if (dt.Hour > Globals.CurrentHour || !watchForChanges)
                    {
                        Globals.CurrentHour = dt.Hour;
                        string currentTimestamp = response.Root[0].DataPoints[0].IndexTimestamp;
                        int currentUvIndex = response.Root[0].DataPoints[0].IndexValue;
                        UpdateTrayIcon(currentUvIndex, currentTimestamp);
                        #if DEBUG
                        Console.WriteLine("\nUpdated at "+currentTimestamp+" with "+queryCnt+" queries");
                        #endif
                        return true;
                    }
                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    #if DEBUG
                    Console.WriteLine(e);
                    #endif
                    return false;
                }
            }
        }
        
    }

    
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int pid);
    
    private static void UpdateTrayIcon(int currentUvIndex, string indexTimestamp)
    {
        trayIcon.Icon = new Icon("assets/" + currentUvIndex + ".ico");
        if (currentUvIndex > 11)
        {
            trayIcon.Icon = new Icon("assets/11.ico");
            trayIcon.Text = "Last updated: " + indexTimestamp;
            trayIcon.Visible = true;
        }
    }

    private void OfflineTrayIcon(){
        trayIcon.Icon = new Icon("assets/offline.ico");
        trayIcon.Text = "I't past 7pm sunset, wait for 7am sunrise";
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
    public static int CurrentHour = -1;
}
