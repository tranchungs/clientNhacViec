using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WMPLib;
using DDay.iCal;
using System.Collections;
using Ical.Net.DataTypes;
using System.Collections.Generic;
using xNet;
using Newtonsoft.Json;
using System.IO;
using System.Threading;

namespace App
{
    class Program
    {
       static List<int> listnotification = new List<int>();
        static void Main(string[] args)
        {


            //String a =   ConvertText("hihi", "hihi", "hihi", "hihi");
            //  Console.WriteLine(a);
            //  Console.ReadLine();

            while (true)
            {
                getDataJob();
                Thread.Sleep(200);

            }
        }

        
        static void getDataJob()
        {
            try
            {
                HttpRequest http = new HttpRequest();
                String html = http.Get("http://localhost:7454/api/getjob").ToString();
                Job result = JsonConvert.DeserializeObject<Job>(html);
                Console.WriteLine(result.message.Length);
                if (result.message.Length > 0)
                {
                    //   result.message[0].start 
                    long timenow = currentTime();
                    if (result.message[0].notification_date >= timenow && result.message[0].notification_date < (timenow + 5000))
                    {
                        bool checknotifi = true;
                        foreach (int num in listnotification)
                        {
                            if (result.message[0].id == num)
                            {
                                checknotifi = false;
                            }
                        }
                        if (checknotifi)
                        {
                            String summary = result.message[0].summary;
                            String location = result.message[0].location;
                            String description = result.message[0].description;
                            String timestart = convertMilisecondtoTime(result.message[0].start);
                            String end = convertMilisecondtoTime(result.message[0].end);

                            bool checkdownload = GetFileMP3(summary, location, description, timestart);
                            if (checkdownload)
                            {
                                ToastNotification(summary, location, description, timestart, end);
                                listnotification.Add(result.message[0].id);
                            }
                            else
                            {
                                Console.WriteLine("Chưa có file đc");
                            }
                          
                        }

                    }

                }
                else
                {

                }
            }
            catch
            {


            }


        }
        static string convertMilisecondtoTime(long miliseccond)
        {
            long timemili = miliseccond + 25200000;
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
            date = date.AddMilliseconds(timemili).ToLocalTime();
            return $"{date.Hour}:{date.Minute}";
           
        }
        static bool GetFileMP3(String sumary, String location, String descript, String starttime)
        {
            try
            {
                HttpRequest http = new HttpRequest();
                String result = ConvertText(sumary,location,descript,starttime);

                FPTAI fptai = JsonConvert.DeserializeObject<FPTAI>(result);
                var stream = http.Get($"{fptai.async}").ToMemoryStream().ToArray();
                File.WriteAllBytes($"file.mp3", stream);
                return true;
            }
            catch 
            {

                return false;
            }
           

        }
        static long currentTime()
        {
            long currentMiliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            DateTime epochStart = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);
            DateTime epochEnd = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);

            long elapsedMilliseconds = (long)((epochEnd - epochStart).TotalMilliseconds);

            long unixTimestamp = currentMiliseconds - elapsedMilliseconds - 25200000;
            return unixTimestamp;
        }
        static void ToastNotification(String sumary, String location, String descript,String timestart,String timeend)
        {

            WindowsMediaPlayer sound = new WindowsMediaPlayer();

            sound.URL = @"file.mp3";
            sound.controls.play(); //Play sound


            new ToastContentBuilder()
                .AddArgument("action", "viewConversation")
                .AddArgument("conversationId", 9813)
                .AddText(sumary)
                .AddText($"Địa điểm:{location}, ({timestart}-{timeend})")
                .AddText($"Mô tả:{descript}")
              .Show();
        }


        static String ConvertText(String sumary, String location, String descript,String starttime)
        {
            String result = Task.Run(async () =>
            {
                String payload = $"Bạn có lịch {sumary} vào lúc {starttime}, mô tả {descript}, địa điểm {location}";
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("api-key", "KmfuJcs0LQkndn14d70yF0uqM34OWh8J");
                client.DefaultRequestHeaders.Add("speed", "");
                client.DefaultRequestHeaders.Add("voice", "thuminh");
                var response = await client.PostAsync("https://api.fpt.ai/hmi/tts/v5", new System.Net.Http.StringContent(payload));
                return await response.Content.ReadAsStringAsync();
            }).GetAwaiter().GetResult();

            return result;
        }
    }

    public class Job
    {
        public bool status { get; set; }
        public MessageJob[] message { get; set; }
    }

    public class MessageJob
    {
        public int id { get; set; }
        public string summary { get; set; }
        public string description { get; set; }
        public string location { get; set; }
        public long start { get; set; }
        public long end { get; set; }
        public long notification_date { get; set; }
    }

    public class FPTAI
    {
        public string async { get; set; }
        public int error { get; set; }
        public string message { get; set; }
        public string request_id { get; set; }
    }


}
