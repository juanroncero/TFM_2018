using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
//using consumer.ENTITIES;

namespace Twitter
{
    public class cargaTweets
    {
        public BDD bdd = new BDD();

        public void test()
        {
            try
            {


            }
            catch (Exception ex)
            {


            }
        }

        public void guardaTweets()
        {
            List<TWEET> tweets = new List<TWEET>();

            try
            {
                // 1. LEE CUENTAS BDD
                List<TWEETER_ACCOUNT> cuentas = new List<TWEETER_ACCOUNT>();

                cuentas = bdd.getAccountsTwitter();

                foreach (TWEETER_ACCOUNT a in cuentas)
                {
                    Console.WriteLine("");
                    Console.WriteLine("CUENTA " + a.accountName);

                    // 2.1 GUARDA ULTIMOS TWEETS.


                    var httpClient = new HttpClient();
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitter.com/oauth2/token ");
                    var customerInfo = Convert.ToBase64String(new UTF8Encoding().GetBytes("xx" + ":" + "yy"));
                    request.Headers.Add("Authorization", "Basic " + customerInfo);
                    request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                    HttpResponseMessage response = httpClient.SendAsync(request).Result;

                    string json = response.Content.ReadAsStringAsync().Result;
                    var serializer = new JavaScriptSerializer();
                    dynamic item = serializer.Deserialize<object>(json);
                    var token = item["access_token"];



                    var requestUserTimeline = new HttpRequestMessage(HttpMethod.Get, string.Format("https://api.twitter.com/1.1/statuses/user_timeline.json?count={0}&screen_name={1}&trim_user=1&exclude_replies=1", 20, a.accountName));
                    requestUserTimeline.Headers.Add("Authorization", "Bearer " + token);
                    httpClient = new HttpClient();
                    HttpResponseMessage responseUserTimeLine = httpClient.SendAsync(requestUserTimeline).Result;
                    serializer = new JavaScriptSerializer();
                    dynamic json2 = serializer.Deserialize<object>(responseUserTimeLine.Content.ReadAsStringAsync().Result);
                    var enumerableTwitts = (json2 as IEnumerable<dynamic>);


                    //IEnumerable<string> twitts = Twitter_ .GetTwitts(a.accountName, 20).Result;


                    foreach (var t in enumerableTwitts)
                    {
                        TWEET tt = new TWEET();
                        tt.idCuenta = a.id;

                        tweetElemet pepe = (tweetElemet)t;


                        tt.texto = t[3].Value;
                        tweets.Add(tt);
                        Console.WriteLine(tt.texto);
                    }



                }

                // bdd.altaTweets(tweets);

            }
            catch (Exception ex)
            {

                throw;
            }

        }





    }

    public class tweetElemet
    {
        public string Key = string.Empty;
        public string Value = string.Empty;
    
    }


}
