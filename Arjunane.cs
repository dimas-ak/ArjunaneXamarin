using System;
using System.Threading;
using Xamarin.Forms;
using System.Globalization;
using Xamarin.Essentials;

namespace ArjunaneXamarin
{
    public class Arjunane
    {
        private static int number_grid, how_many_grid;

        public class SetGrid { public int column { get; set; } public int row { get; set; } }
        public static SetGrid sg { get; set; }

        private CancellationTokenSource cancellation = new CancellationTokenSource();

        public Arjunane SetGridCount(int how_many)
        {

            sg = new SetGrid
            {
                column  = 0,
                row     = -1
            };

            // berapa jumlah perkalian (contoh dibawah ialah 3)
            how_many_grid   = how_many;

            // number_grid perkalian (misal how_many = 3)
            // berarti grid 3, 6, 9
            number_grid     = how_many;
            
            return this;
        }

        public SetGrid GetGridCount(int i)
        {
            sg.row += 1;

            if (number_grid == i)
            {
                number_grid         += how_many_grid;

                sg.column   += 1;
                sg.row      = 0;
            }

            return sg;
        }

        public string GetFileName(string path)
        {
            string[] filenames = path.Split('/');
            return filenames[filenames.Length - 1].Split('?')[0];
        }
        
        public string SetRangeDate(string _date)
        {
            DateTime date = DateTime.ParseExact(_date, "yyyy-MM-dd HH:mm:ss", new CultureInfo("id-ID"));
            TimeSpan sec = DateTime.UtcNow.Subtract(date);

            string result = "";


            int dayDiff = (int)sec.TotalDays;
            int secDiff = (int)sec.TotalSeconds;
            int minDiff = (int)sec.TotalMinutes;
            int hourDiff = (int)sec.TotalHours;

            // jarak waktu yang error jika string date nya tidak sesuai
            if (dayDiff < 0 || dayDiff >= 31)
            {
                result = "";
            }

            if (dayDiff == 0)
            {
                // kurang dari satu menit
                if (secDiff < 60)
                {
                    result = "Beberapa detik yang lalu.";
                }
                else if (minDiff < 60)
                {
                    result = minDiff + " menit yang lalu";
                }
                else
                {
                    if (hourDiff < 24) result = hourDiff + " jam yang lalu";
                }
            }
            else
            {
                result = SetDate(_date, true);
            }
            return result;

        }

        public string SetDate(string date, bool time = false, bool IsFromMySQL = false)
        {
            string[] _split = date.Split(' ');
            string[] split = _split[0].Split('-');

            string result_time = "";

            string[] months = new string[12];
            months[0] = "Januari";
            months[1] = "Februari";
            months[2] = "Maret";
            months[3] = "April";
            months[4] = "Mei";
            months[5] = "Juni";
            months[6] = "Juli";
            months[7] = "Agustus";
            months[8] = "September";
            months[9] = "Oktober";
            months[10] = "November";
            months[11] = "Desember";

            int indexMonth = IsFromMySQL ? int.Parse(split[1]) - 1 : int.Parse(split[1]);

            string month = months[indexMonth];

            if (time && _split.Length > 1)
            {
                string[] split_time = _split[1].Split(':');
                result_time = " " + split_time[0] + ":" + split_time[1] + " WIB";
            }

            return split[2] + " " + month + " " + split[0] + result_time;
        }

        public bool IsNull(Entry en)
        {
            return string.IsNullOrEmpty(en.Text);
        }
        public bool IsNull(Editor en)
        {
            return string.IsNullOrEmpty(en.Text);
        }
        public bool IsNull(Picker en)
        {
            return en.SelectedIndex == -1;
        }
        public bool IsNull(string en)
        {
            return string.IsNullOrEmpty(en) || en.Length == 0;
        }

        public void ShowError(Label label, string error)
        {
            label.IsVisible = true;
            label.Text = error;
        }

        public string ToRupiah(int angka)
        {
            return string.Format(CultureInfo.CreateSpecificCulture("id-id"), "Rp. {0:N}", angka);
        }
        public string ToRupiah(string angka)
        {
            return string.Format(CultureInfo.CreateSpecificCulture("id-id"), "Rp. {0:N}", int.Parse(angka));
        }

        public void SetTimeout(Action act, double miliseconds)
        {
            Device.StartTimer(TimeSpan.FromMilliseconds(miliseconds), () => {
                act();
                return false;
            });
        }

        public void SetInterval(Action act, double miliseconds)
        {
            CancellationTokenSource cts = cancellation;
            Device.StartTimer(TimeSpan.FromMilliseconds(miliseconds), () => {
                if (cts.IsCancellationRequested) return false;
                act();
                return true;
            });
        }
        public void ClearInterval()
        {
            Interlocked.Exchange(ref cancellation, new CancellationTokenSource()).Cancel();
        }
        public bool IsConnectedInternet()
        {
            var connect = Connectivity.NetworkAccess;
            return connect == NetworkAccess.Internet;
        }
        
    }
   
}