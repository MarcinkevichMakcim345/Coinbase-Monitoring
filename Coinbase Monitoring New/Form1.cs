using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using xNet;
using System.Collections.Generic;
using LiveCharts;
using LiveCharts.Wpf;

namespace Coinbase_Monitoring_New
{
    public partial class Form1 : Form
    {
        public static double CurrentRate;
        public Form1()
        {
            InitializeComponent();
            new Thread(() => RefreshToken()) { IsBackground = true }.Start();
            timer1.Enabled = true;
            timer1_Tick(null, null);
        }
        private void guna2Button1_Click(object sender, EventArgs e)
        {
            try
            {
                Form2 form2 = new Form2();
                form2.ShowDialog();
                string _code = form2.AccessToken;
                if (!String.IsNullOrEmpty(_code))
                    new Thread(() => LoadAuthorizationData(_code)) { IsBackground = true }.Start();
                else
                    MessageBox.Show("Авторизация не удалась");
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }

        #region Метод Обмена Кода/Рефреш токена на Access Token
        /// <summary>
        /// Метод Обмена Кода/Рефреш токена на Access Token и сохранение
        /// </summary>
        /// <param name="_code"></param>
        /// <param name="refreshToken"></param>
        public void LoadAuthorizationData(string _code, bool refreshToken = false)
        {
            try
            {
                using (HttpRequest request = new HttpRequest())
                {
                    var UrlParams = new RequestParams();
                    if (!refreshToken)
                    {
                        UrlParams["grant_type"] = "authorization_code";
                        UrlParams["code"] = _code;
                        UrlParams["redirect_uri"] = "https://google.com/1";
                    }
                    else
                    {
                        UrlParams["grant_type"] = "refresh_token";
                        UrlParams["refresh_token"] = _code;
                    }
                    UrlParams["client_id"] = "3843dcdb8a84798dbf01aab35cf04ca702db111da7e1c65a89c2210122aff2b0";
                    UrlParams["client_secret"] = "0cd09349b3c296478cee710a85a444d1bcd24fc4256a0b906980a1a38decbd77";

                    string Response = request.Post("https://api.coinbase.com/oauth/token", UrlParams).ToString();

                    Properties.Settings.Default._access_token = Response.Substring("access_token\":\"", "\"");
                    Properties.Settings.Default._token_type = Response.Substring("token_type\":\"", "\"");
                    Properties.Settings.Default._expires_in = Response.Substring("expires_in\":", ",");
                    Properties.Settings.Default._refresh_token = Response.Substring("refresh_token\":\"", "\"");
                    Properties.Settings.Default.Save();
                    LoadDataToUi();
                }
            }
            catch { LoadDataToUi(); }
        }
        #endregion

        #region Метод загрузки данных о пользователе на форму
        /// <summary>
        /// Метод загрузки данных о пользователе на форму
        /// </summary>
        public void LoadDataToUi()
        {
            try
            {
                using (HttpRequest request = new HttpRequest())
                {
                    request.AddHeader("Authorization", $"{Properties.Settings.Default._token_type} {Properties.Settings.Default._access_token}");
                    request.AddHeader("Accept", "application/json");
                    string Response = request.Get("https://api.coinbase.com/v2/user").ToString();

                    string name = Response.Substring("name\":\"", "\"");
                    string login = Response.Substring("username\":", ",");
                    string UrlAvatar = Response.Substring("avatar_url\":\"", "\"");

                    request.AddHeader("Authorization", $"{Properties.Settings.Default._token_type} {Properties.Settings.Default._access_token}");
                    request.AddHeader("Accept", "application/json");
                    Response = request.Get("https://api.coinbase.com/v2/accounts/").ToString();
                    string balance = Response.Substring("\"amount\":\"", "\"");

                    NameLabel.Invoke((MethodInvoker)(() => NameLabel.Text = $"Имя: {name}"));
                    LoginLabel.Invoke((MethodInvoker)(() => LoginLabel.Text = $"Логин: {login}"));
                    BalanceLabel.Invoke((MethodInvoker)(() => BalanceLabel.Text = $"Баланс: {balance}"));
                    AvatarBox.Invoke((MethodInvoker)(() =>
                    {
                        AvatarBox.ImageLocation = UrlAvatar;
                        AvatarBox.SizeMode = PictureBoxSizeMode.StretchImage;
                    }));
                }
            }
            catch
            {
                NameLabel.Invoke((MethodInvoker)(() => NameLabel.Text = $"Имя: "));
                LoginLabel.Invoke((MethodInvoker)(() => LoginLabel.Text = $"Логин: "));
                BalanceLabel.Invoke((MethodInvoker)(() => BalanceLabel.Text = $"Баланс: "));
                AvatarBox.Invoke((MethodInvoker)(() =>
                {
                    AvatarBox.ImageLocation = "https://images.coinbase.com/avatar?h=603b6edb6f02b60c5d1abQBRgy5sBj%2BqpPwHSEPGUgbjjtWBHQxWGVZBGZyw%0Ae4rs&s=128";
                    AvatarBox.SizeMode = PictureBoxSizeMode.StretchImage;
                }));
                Properties.Settings.Default._access_token = "";
                Properties.Settings.Default._token_type = "";
                Properties.Settings.Default._expires_in = "";
                Properties.Settings.Default._refresh_token = "";
                Properties.Settings.Default.Save();
            }
        }
        #endregion

        #region Обновление Токенов
        public void RefreshToken()
        {
            try
            {
                while (true)
                {
                    if (!String.IsNullOrEmpty(Properties.Settings.Default._expires_in))
                    {
                        if (Properties.Settings.Default._refresh_token != null)
                            LoadAuthorizationData(Properties.Settings.Default._refresh_token, true);
                        Thread.Sleep(Convert.ToInt32(Properties.Settings.Default._expires_in) * 1000);
                    }
                    else
                    {
                        Thread.Sleep(60000);
                    }
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }
        #endregion

        #region Кнопка выхода с аккаунта
        private void guna2Button2_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default._access_token = null;
            Properties.Settings.Default._token_type = null;
            Properties.Settings.Default._expires_in = null;
            Properties.Settings.Default._refresh_token = null;
            Properties.Settings.Default.Save();
            LoadDataToUi();
        }
        #endregion

        #region Метод парсинга данных для построения графика
        public class Data
        {
            public double values { get; set; }
            public DateTime date { get; set; }
        }
        /// <summary>
        /// Метод парсинга данных для построения графика
        /// </summary>
        public List<Data> ParseData()
        {
            List<Data> data = new List<Data>();
            try
            {
                using (HttpRequest request = new HttpRequest())
                {
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.192 Safari/537.36";
                    request.AddHeader("Accept", "application/json");

                    // Получаем актуальный курс для конвертера
                    string Response = request.Get("https://api.coinbase.com/v2/prices/" + ProductIdBox.Text + "/spot").ToString();
                    CurrentRate = Convert.ToDouble(Response.Substring("amount\":\"", "\"").Replace(".", ","));

                    var UrlParams = new RequestParams();
                    if (PeriodBox.SelectedIndex == 0)
                    {
                        UrlParams["start"] = DateTime.Now.AddHours(-1).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffZ", CultureInfo.InvariantCulture);
                        UrlParams["granularity"] = "60";
                    }
                    else
                    {
                        UrlParams["start"] = DateTime.Now.AddHours(-24).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffZ", CultureInfo.InvariantCulture);
                        UrlParams["granularity"] = "300";
                    }
                    UrlParams["end"] = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffZ", CultureInfo.InvariantCulture);

                    Response = request.Get("https://api.pro.coinbase.com/products/" + ProductIdBox.Text + "/candles", UrlParams).ToString().Substring("[");

                    while (Response.Contains("[") && Response.Contains("]"))
                    {
                        string source = Response.Substring("[", "]");
                        data.Add(new Data() { values = Convert.ToDouble(source.Split(',')[3].Replace(".", ",")), date = UnixToDateTime(source.Substring(0, source.IndexOf(","))) });
                        Response = Response.Replace($"[{Response.Substring("[", "]")}]", " ");
                    }
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return data;
        }
        #endregion

        #region Метод перевода Unix Time в обычное время
        /// <summary>
        /// Метод перевода Unix Time в обычное время
        /// </summary>
        /// <param name="epoch">Время от начала эпохи</param>
        /// <returns></returns>
        public static DateTime UnixToDateTime(string epoch)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            try
            {
                dateTime = dateTime.AddSeconds(Convert.ToDouble(epoch)).ToLocalTime();
            }
            catch { }
            return dateTime;
        }
        #endregion

        #region Таймер для рисования графика
        /// <summary>
        /// Таймер для рисования графика
        /// </summary>
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                LiveCharts.SeriesCollection series = new LiveCharts.SeriesCollection();
                LiveCharts.Wpf.LineSeries line = new LineSeries();
                cartesianChart1.DisableAnimations = true;        // Отключаем анимацию

                List<Data> data = ParseData();
                series.Clear();
                ChartValues<double> zp = new ChartValues<double>();
                List<string> date = new List<string>();
                for (int i = data.Count - 1; i >= 0; i--)
                {
                    zp.Add(data[i].values);
                    date.Add(data[i].date.ToString("HH:mm:ss"));
                }

                cartesianChart1.AxisX.Clear();
                cartesianChart1.AxisX.Add(new LiveCharts.Wpf.Axis
                {
                    Labels = date
                });
                line.Title = "Курс Bitcoin";
                line.Values = zp;
                line.PointGeometrySize = 5;                      // Уменьшаем размер точек

                series.Add(line);
                cartesianChart1.Series = series;
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }
        #endregion

        #region кнопка обновления графика
        private void guna2Button3_Click_1(object sender, EventArgs e)
        {
            try
            {
                timer1_Tick(null, null);
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }
        #endregion

        #region Конвертер валюты
        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (double.TryParse(guna2TextBox1.Text, out double result))
                {
                    guna2TextBox2.Text = $"{(Convert.ToDouble(guna2TextBox1.Text) * CurrentRate).ToString("#.##")} {ProductIdBox.Text.Substring("-")}";
                }
                else
                {
                    if (guna2TextBox1.Text.Length > 0)
                        guna2TextBox1.Text = guna2TextBox1.Text.Remove(guna2TextBox1.Text.Length - 1, 1);
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }
        #endregion
    }
}
