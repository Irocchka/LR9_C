using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace grafica
{
    public struct City
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public struct Weather
    {
        public string Country { get; set; }
        public string Name { get; set; }
        public double Temp { get; set; }
        public string Description { get; set; }
    }

    public partial class Form1 : Form
    {

        City _city;
        static String _string = "Press Button Again";
        static bool flag = false;

        static string API_KEY = "6876035b49da589210a0c76608d5da29";

        public class API_call
        {
            public static async Task<Weather> GetWeather(City city)
            {

                var url = $"https://api.openweathermap.org/data/2.5/weather";
                var parameters = $"?lat={city.Latitude}&lon={city.Longitude}&appid={API_KEY}";

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(url);

                // Запрос, пока не получим ответ
                HttpResponseMessage response = await client.GetAsync(parameters).ConfigureAwait(false);

                Console.WriteLine("Fetching data...");
                try
                {
                    response = await client.GetAsync(parameters);
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    Console.WriteLine($"{city.Name} : Connection troubles, try again");
                }

                // Считываем ответ как стрингу
                Weather result = new Weather();

                if (response.IsSuccessStatusCode)
                {
                    string rawResponse = await response.Content.ReadAsStringAsync();

                    Regex rx = new Regex("(?<=\"country\":\")[^\"]+(?=\")");
                    result.Country = rx.Match(rawResponse).ToString();
                    rx = new Regex("(?<=\"name\":\")[^\"]+(?=\")");
                    result.Name = rx.Match(rawResponse).ToString();
                    rx = new Regex("(?<=\"temp\":)[^\"]+(?=,)");
                    result.Temp = Math.Round(Convert.ToDouble(rx.Match(rawResponse).ToString()) - 273);
                    rx = new Regex("(?<=\"description\":\")[^\"]+(?=\")");
                    result.Description = rx.Match(rawResponse).ToString();

                }

                // Если смогли достать хоть какие-то данные, считаем среднее. Иначе - возвращаем null.
                //MessageBox.Show(city.Name + ": " + result.Temp.ToString() + " degrees, " + result.Description);

                _string = city.Name + ": " + result.Temp.ToString() + " degrees, " + result.Description;
                flag = true;
                return result;

            }
        }

        public Form1()
        {
            InitializeComponent();
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            listBox1.Items.AddRange(File.ReadAllLines("C:\\Users\\79526\\Desktop\\Ирочка\\city.txt"));
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            string item = listBox1.SelectedItem.ToString();


            var line = item.Split(new String[] { "\t", ",", " " }, StringSplitOptions.RemoveEmptyEntries);

            City newCity = new City();
            newCity.Name = line[0];

            for (int i = 1; i < line.Length - 2; i++)
                newCity.Name += $" {line[i]}";

            newCity.Latitude = Convert.ToDouble(line[line.Length - 2]);
            newCity.Longitude = Convert.ToDouble(line[line.Length - 1]);
            _city = newCity;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                API_call.GetWeather(_city);
            });

            while (!flag) { };
            listBox2.Items.Clear();
            listBox2.Items.Insert(0, _string);
            flag = false;
        }
    }
}