using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ArjunaneXamarin
{
    public class ArjunaneWebRequest
    {
        private static string _url;

        private Dictionary<string, string> add_field = new Dictionary<string, string>();
        private Dictionary<string, Stream> add_file = new Dictionary<string, Stream>();
        private Dictionary<string, string> header_fields = new Dictionary<string, string>();


        public bool IsConnectedInternet()
        {
            var connect = Connectivity.NetworkAccess;
            return connect == NetworkAccess.Internet;
        }
        public ArjunaneWebRequest GetURL(string url)
        {
            add_field.Clear();
            add_file.Clear();
            header_fields.Clear();
            _url = url;
            return this;
        }
        public ArjunaneWebRequest AddField(string key, string value)
        {
            add_field.Add(key, value);
            return this;
        }
        public ArjunaneWebRequest AddFields(Dictionary<string, string> dict)
        {
            if(dict.Count > 0)
            {
                foreach(var ini in dict)
                {
                    add_field.Add(ini.Key, ini.Value);
                }
            }
            return this;
        }
        public ArjunaneWebRequest AddFile(string key, Stream value)
        {
            add_file.Add(key, value);
            return this;
        }
        public ArjunaneWebRequest AddHeaders(string key, string value)
        {
            header_fields.Add(key, value);
            return this;
        }
        public async Task SendWebRequest(Action<bool, string, string> func, int _TimeOut = 30)
        {
            bool _response;
            string _result = "";
            string _error = "";
            HttpClient client = new HttpClient();
            try
            {
                string url = _url;

                var cts = new CancellationTokenSource();

                cts.CancelAfter(TimeSpan.FromSeconds(_TimeOut));

                HttpResponseMessage response;

                if(header_fields.Count  > 0)
                {
                    foreach(var ini in header_fields)
                    {
                        client.DefaultRequestHeaders.Add(ini.Key, ini.Value);
                    }
                }

                if (add_field.Count > 0)
                {
                    var builder = new UriBuilder(_url)
                    {
                        Port = -1
                    };
                    var query = HttpUtility.ParseQueryString(builder.Query);
                    foreach (var ini in add_field)
                    {
                        query[ini.Key] = ini.Value;                    
                    }

                    builder.Query = query.ToString();
                    url = builder.ToString();

                    response = await client.GetAsync(url, cts.Token);
                }
                else
                {

                    response = await client.GetAsync(_url, cts.Token);
                }



                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                _response = true;
                _result = responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Error Web" + e.ToString());
                _response = false;
                _error = e.Message.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Exception : " + e.ToString());
                _response = false;
                _error = e.Message.ToString();

            }
            func(_response, _result, _error);
        }

        public void SendWebForm(Action<bool, string, string> func, int timeout_seconds = 30, string method = "POST", string contentType = "application/x-www-form-urlencoded")
        {
            Task.Run(() => {
                Device.BeginInvokeOnMainThread(() => {
                    bool _response;
                    string _result = "";
                    string _error = "";

                    try
                    {
                        var request = (HttpWebRequest)WebRequest.Create(_url);

                        string postData = "";

                        int i = 0;
                        int count = add_field.Count - 1;

                        foreach (var val in add_field)
                        {
                            string and = i != count ? "&" : "";
                            postData += val.Key + "=" + val.Value + and;
                            i++;
                        }

                        var data = Encoding.ASCII.GetBytes(postData);

                        request.Method = method.ToUpper();
                        request.ContentType = contentType;
                        request.ContentLength = data.Length;
                        request.Timeout = timeout_seconds * 1000;

                        if(header_fields.Count > 0)
                        {
                            foreach(var ini in header_fields)
                            {
                                request.Headers.Add(ini.Key, ini.Value);
                            }
                        }

                        //request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.97 Safari/537.36";

                        Stream stream;
                        using (stream = request.GetRequestStream())
                        {
                            stream.Write(data, 0, data.Length);
                        }

                        var response = (HttpWebResponse)request.GetResponse();

                        var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                        _result = responseString;

                        _response = true;
                        response.Close();
                        stream.Close();
                    }
                    catch (WebException e)
                    {
                        _response = false;
                        _error = e.Message;
                        StringBuilder sb1 = new StringBuilder();

                        sb1.AppendFormat("Exception: GetResponse: WebException Status={0}", e.Status);

                        Console.WriteLine("Error Line : " + sb1.ToString());
                    }
                    func(_response, _result, _error);


                });
            });

        }

        public async void SendWebFormMultipart(Action<bool, string, string> func, int timeout_seconds = 30)
        {
            bool _response;
            string _result = "";
            string _error = "";
            try
            {
                // Convert each of the three inputs into HttpContent objects

                // examples of converting both Stream and byte [] to HttpContent objects
                // representing input type file
                // HttpContent bytesContent = new ByteArrayContent(fileBytes);

                // Submit the form using HttpClient and 
                // create form data as Multipart (enctype="multipart/form-data")

                using (var client = new HttpClient())
                using (var formData = new MultipartFormDataContent())
                {
                    client.Timeout = TimeSpan.FromSeconds(timeout_seconds);
                    formData.Headers.ContentType.MediaType = "multipart/form-data";
                    // Add the HttpContent objects to the form data
                    if (add_field.Count != 0)
                    {
                        foreach (var val in add_field)
                        {
                            HttpContent stringContent = new StringContent(val.Value);
                            formData.Add(stringContent, val.Key);
                        }
                    }

                    if (header_fields.Count > 0)
                    {
                        foreach (var ini in header_fields)
                        {
                            client.DefaultRequestHeaders.Add(ini.Key, ini.Value);
                        }
                    }

                    if (add_file.Count != 0)
                    {
                        foreach (var val in add_file)
                        {

                            FileStream fs = val.Value as FileStream;
                            HttpContent _fs = new StreamContent(fs);

                            string[] filenames = fs.Name.Split('/');
                            string file_name = "new-file" + filenames[filenames.Length - 1];

                            string[] paths = new string[filenames.Length - 1];

                            for (int i = 0; i < filenames.Length; i++)
                            {
                                var ini = filenames[i];

                                if (i != (filenames.Length - 1)) paths[i] = ini;

                            }
                            string path = string.Join("/", paths);

                            //FileStream fs_path = new FileStream(path, FileMode.Open, FileAccess.Read);
                            // StreamContent streamcontent = new StreamContent(fs_path);

                            // streamcontent.Headers.Add("Content-Type", "application/octet-stream");
                            // streamcontent.Headers.Add("Content-Disposition", string.Format("form-data; name=\"" + val.Key + "\"; filename=\"{0}\"", file_name));

                            byte[] imagebytearraystring = ImageFileToByteArray(fs.Name);

                            formData.Add(new ByteArrayContent(imagebytearraystring, 0, imagebytearraystring.Count()), val.Key, file_name);
                            //formData.Add(_fs, val.Key, file_name);
                            //  fs_path.Dispose();
                            // streamcontent.Dispose();
                        }
                    }


                    // Invoke the request to the server

                    // equivalent to pressing the submit button on
                    // a form with attributes (action="{url}" method="post")
                    var response = await client.PostAsync(_url, formData);

                    // ensure the request was a success
                    _response = response.IsSuccessStatusCode;

                    _result = await response.Content.ReadAsStringAsync();
                }
            }
            catch (WebException e)
            {
                _response = false;
                _error = e.Message.ToString();

                Console.WriteLine("Web Exception : " + e.Message);
                Console.WriteLine("Web Exception : " + e.StackTrace);
            }
            catch (Exception e)
            {
                _response = false;
                _error = e.Message.ToString();
                Console.WriteLine("Exception : " + e.Message);
                Console.WriteLine("Exception : " + e.StackTrace);
            }

            func(_response, _result, _error);
        }
        private byte[] ImageFileToByteArray(string path)
        {
            FileStream fs = File.OpenRead(path);
            byte[] bytes = new byte[fs.Length];
            fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
            fs.Close();
            return bytes;
        }

        public async Task<byte[]> DownloadImage(string url, int timeout = 30)
        {
            var _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeout) };

            try
            {
                using (var httpResponse = await _httpClient.GetAsync(url))
                {
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        await Application.Current.MainPage.DisplayAlert("Info", "Photo berhasil di Unduh.", "Ok");
                        return await httpResponse.Content.ReadAsByteArrayAsync();
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Ops", "Terjadi kesalahan, \n\r URL photo tidak valid", "Ok");
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                //Handle Exception
                await Application.Current.MainPage.DisplayAlert("Ops", "Terjadi kesalahan : " + e.Message, "Ok");
                return null;
            }
        }
        public void DownloadData(Action<DownloadDatas> data, Action<bool, string> func = null, Action<bool, double> progressDownload = null)
        {
            DownloadDatas dd = new DownloadDatas();
            data(dd);
            int _timeout = (int)TimeSpan.FromSeconds(dd.timeout_seconds).TotalMilliseconds;

            if (dd.url == null) throw new ArgumentException("Please set the URL");

            ExWebClient webClient = new ExWebClient(_timeout);

            try
            {
                // mendapatkan nama berdasarkan url
                string _file_name = dd.url.Split('/').Last();


                var _path = dd.folder_directory;

                string path = "";

                if (Device.RuntimePlatform == Device.Android)
                {

                    path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/" + _path;
                }
                else
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/" + _path;
                }

                if (!Directory.Exists(path))
                {
                    // Console.WriteLine("directory tidak ada " + path);
                    Directory.CreateDirectory(path);
                }

                double progress = .0;

                if(progressDownload != null)
                {
                    webClient.DownloadProgressChanged += (s, e) => {
                        double prog = (double)e.ProgressPercentage;
                        progress = prog / 100;

                        progressDownload(false, progress);
                    };
                }

                webClient.DownloadDataCompleted += (s, e) => {
                    try
                    {

                        var bytes = e.Result;

                        string localFilename = dd.file_name ?? _file_name;

                        string localPath = Path.Combine(path, localFilename);

                        progressDownload?.Invoke(true, .0);

                        try
                        {
                            File.WriteAllBytes(localPath, bytes);

                            //Application.Current.MainPage.DisplayAlert("Info", "Data berhasil di unduh.\n\rFolder penyimpanan : " + _path, "Ok");
                        }
                        catch (DirectoryNotFoundException dx)
                        {
                            Console.WriteLine("Directory Not found : " + dx.StackTrace);
                            Console.WriteLine("Directory Not found : " + dx.Message);
                            //Application.Current.MainPage.DisplayAlert("Ops", "Terjadi kesalahan : " + dx.Message, "Ok");
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Directory Not found : " + ex.StackTrace);
                        Console.WriteLine("Directory Not found : " + ex.Message);

                        func(false, ex.Message);
                    }
                    

                        // IMPORTANT: this is a background thread, so any interaction with
                        // UI controls must be done via the MainThread
                    
                };
                func(true, null);
                webClient.DownloadDataAsync(new Uri(dd.url));
            }
            catch (Exception ex)
            {
                func(false, ex.Message);
                //Application.Current.MainPage.DisplayAlert("Ops", "Terjadi kersalahan : \n\r" + ex.Message, "Ok");
                Console.WriteLine("ERROR:" + ex.StackTrace);
                Console.WriteLine("ERROR:" + ex.Message);
            }
        }

        public class DownloadDatas 
        {
            public string url { get; set; } = null;
            public string file_name { get; set; } = null;
            public string folder_directory { get; set; } = "DownloadData";
            public int timeout_seconds { get; set; } = 60;
        }

        public class RequestWeb 
        { 
            public string Method { get; set; } = null;
            public string ContentType { get; set; } = null;
            public int Timeout { get; set; } = -1;
        }
        public class ExWebClient : WebClient
        {
            public int Timeout { get; set; }

            public ExWebClient()
            {
                this.Timeout = 60000;
            }

            public ExWebClient(int timeout)
            {
                this.Timeout = timeout;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var result = base.GetWebRequest(address);
                result.Timeout = this.Timeout;
                return result;
            }
        }
    }
}
