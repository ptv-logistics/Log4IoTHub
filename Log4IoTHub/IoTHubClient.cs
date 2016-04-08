//--------------------------------------------------------------
// Copyright (c) 2016 PTV Group
// 
// For license details, please refer to the file LICENSE, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Log4IoTHub
{
    public sealed class IoTHubClient
    {
        private static object _lock = new object();
        private string _deviceId;
        private string _deviceKey;
        private string _apiVersion;
        private string _iotHubName;
        private string _proxyHost;
        private int? _proxyPort;
        private string _sasToken = string.Empty;
        private int _sasTokenExpiry;
        private static UTF8Encoding encoding = new UTF8Encoding();
        private const string PROTOCOLL = "https";
        private const int VALIDITY_IN_SECONDS = 60;
        private Func<string> iotHubURI = () => $"{instance._iotHubName}/devices/{instance._deviceId}";
        private Func<string> iotHubURL = () => $"{PROTOCOLL}://{instance.iotHubURI()}/messages/events?api-version={instance._apiVersion}";
        private Func<TimeSpan> sinceEpoche = () => DateTime.UtcNow - new DateTime(1970, 1, 1);
        protected static readonly Random Random1 = new Random();
        // Minimal delay between attempts to reconnect in milliseconds. 
        protected const int MinDelay = 500;

        // Maximal delay between attempts to reconnect in milliseconds. 
        protected const int MaxDelay = 16000;

        private static volatile IoTHubClient instance;

        private IoTHubClient() { }

        public static IoTHubClient Instance(string deviceId, string deviceKey, string apiVersion = "2015-08-15-preview", string iotHubName = "ads-common-iothub.azure-devices.net", string proxyHost = null, int? proxyPort = null)
        {
            if (instance == null)
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new IoTHubClient();
                        instance._deviceId = deviceId;
                        instance._deviceKey = deviceKey;
                        instance._apiVersion = apiVersion;
                        instance._iotHubName = iotHubName;
                        instance._proxyHost = proxyHost;
                        instance._proxyPort = proxyPort;
                    }
                }
            }

            return instance;
        }


        public void IoTHubRequestAsync(string jsonRequest)
        {

            try
            {

                HttpWebRequest myHttpWebRequest = DoHttpWebRequest(jsonRequest);

                myHttpWebRequest.GetResponseAsync().ContinueWith((t) => RetrySendMessage2IoTHubAync(t, jsonRequest, iotHubURI, instance._deviceKey));

            }
            catch (WebException e)
            {
                Log4IoTHubAppender.Error($"(Error IoTHubRequestAsync [{e}]");
            }
        }

        private HttpWebRequest DoHttpWebRequest(string jsonRequest)
        {

            HttpWebRequest myHttpWebRequest;
            byte[] data;

            CreateTokenIfExpired(iotHubURI(), instance._deviceKey);

            data = encoding.GetBytes(jsonRequest);

            myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(iotHubURL());
            myHttpWebRequest.Method = "POST";

            if (!string.IsNullOrWhiteSpace(_proxyHost))
            {
                myHttpWebRequest.Proxy = new WebProxy(_proxyHost, _proxyPort ?? 80);
            }
            myHttpWebRequest.Headers.Add("Authorization", instance._sasToken);
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.ContinueTimeout = 10000;
            myHttpWebRequest.Timeout = 10000;
            myHttpWebRequest.ReadWriteTimeout = 10000;


            myHttpWebRequest.ContentLength = data.Length;

            try
            {
                using (Stream requestStream = myHttpWebRequest.GetRequestStream())
                {
                    requestStream.Write(data, 0, data.Length);
                }

            }
            catch (WebException e)
            {
                Log4IoTHubAppender.Error($"Invalid iotHub url [{iotHubURL()}] - Error [{e.Message}]", false);
            }

            return myHttpWebRequest;

        }

        private void RetrySendMessage2IoTHubAync(Task<WebResponse> t, string jsonRequest, Func<string> iotHubURI, string _deviceKey, int connectRetries = 0, int rootDelay = MinDelay)
        {
            using (Task<WebResponse> getResponseTask = t)
            {
                if (t.IsCompleted && !t.IsFaulted)
                {
                    using (WebResponse response = getResponseTask.Result)
                    {
                        int statusCode = (int)((HttpWebResponse)response).StatusCode;

                        if (statusCode != 204 && statusCode != 200)
                        {
                            if (connectRetries <= 5)
                            {
                                Retry(t, jsonRequest, iotHubURI, connectRetries, rootDelay);
                            }
                            else
                            {
                                Log4IoTHubAppender.Error($"(Max sendAsync Retries to {iotHubURI} exceeded");
                            }

                        }

                        // Use the GetResponseStream to get the content
                        // See http://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.getresponse.aspx
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            using (StreamReader myStreamReader = new StreamReader(responseStream, encoding))
                            {
                                string pageContent = myStreamReader.ReadToEnd();
                            }
                        }
                    }
                    //Log4IoTHubAppender.Info($"OnSendComplete(SUCCESS to {iotHubURI}");
                }
                else if (t.IsFaulted)
                {
                    if (connectRetries <= 5)
                    {
                        Retry(t, jsonRequest, iotHubURI, connectRetries, rootDelay);
                    }
                    else
                    {
                        Log4IoTHubAppender.Error($"(Max sendAsync Retries to {iotHubURI} exceeded");
                    }

                }
                else
                {
                    // this should not happen 
                    // as you don't pass a CancellationToken into your task
                    Log4IoTHubAppender.Error($"CANCELED sendAsync to {iotHubURI}");
                }
            }

        }

        private void Retry(Task<WebResponse> t, string jsonRequest, Func<string> iotHubURI, int connectRetries, int rootDelay)
        {
            try
            {
                rootDelay *= 2;
                if (rootDelay > MaxDelay)
                    rootDelay = MaxDelay;

                var waitFor = rootDelay + Random1.Next(rootDelay);
                try
                {
                    Thread.Sleep(waitFor);
                }
                catch
                {
                }
                connectRetries++;

                Log4IoTHubAppender.Error($"Retry number [{connectRetries}] send Message async because of [{t.Exception?.Message}]");

                HttpWebRequest myHttpWebRequest = DoHttpWebRequest(jsonRequest);

                myHttpWebRequest.GetResponseAsync().ContinueWith((tt) => RetrySendMessage2IoTHubAync(tt, jsonRequest, iotHubURI, instance._deviceKey, connectRetries, rootDelay));
            }
            catch (Exception ee)
            {
                Log4IoTHubAppender.Error($"RetrySendMessage2IoTHubAync.Exception: {ee.Message}");
            }
        }

        private static bool CreateTokenIfExpired(string resourceUri, string deviceKey)
        {
            lock (instance._sasToken)
            {

                try
                {
                    if (instance._sasToken != null && (int)instance.sinceEpoche().TotalSeconds <= instance._sasTokenExpiry)
                    {
                        return true;
                    }

                    HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(deviceKey));
                    instance._sasTokenExpiry = (int)instance.sinceEpoche().TotalSeconds + VALIDITY_IN_SECONDS;
                    var expiry = Convert.ToString(instance._sasTokenExpiry);
                    string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
                    var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
                    var sasToken = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry);
                    instance._sasToken = sasToken;
                    return true;
                }
                catch (FormatException e)
                {
                    Log4IoTHubAppender.Error($"Error create SASToken - invalid deviceKey [{deviceKey}] - error [{e.Message}]", false);
                    return false;
                }
                catch (Exception e)
                {
                    Log4IoTHubAppender.Error($"Error create SASToken {e.Message}");
                    return false;
                }
            }
        }

    }
}
