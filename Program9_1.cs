using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace lr9
{

    class Program
    {
        //part1
        public class Avreage
        {
            public static async Task<double?> GetAvreage(string stockName)
            {
                var url = $"https://query1.finance.yahoo.com/v7/finance/download/";
                double ndata = 1609464600;
                double kdata = 1640995200;
                var parameters = $"{stockName}?period1={ndata}&period2={kdata}&interval=1d&events=history&includeAdjustedClose=true";

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(url);

                // Делаем запрос, пока не получим ответ
                HttpResponseMessage? response = null;
                while (response == null)
                {
                    try
                    {
                        response = await client.GetAsync(parameters);
                    }
                    catch (HttpRequestException)
                    {
                        Console.WriteLine($"{stockName} : connection troubles, tryin' again");
                    }
                }

                var stream = await response.Content.ReadAsStringAsync();
                var data = stream.Split('\n');
                double sum = 0;
                double count = 0;

                foreach (string line in data)
                {
                    // try, потому что не по всем акциям есть норм данные - в таком случае их не получится спарсить
                    try
                    {
                        // Пробуем делить строку запятыми
                        string[] dayData = line.Split(',');

                        // Нам нужны значения High и Low - в строке они стоят 3 и 4 соответственно. Достаём их, считаем среднее и плюсуем к сумме
                        double low = Convert.ToDouble(dayData[2].Replace('.', ','));
                        double hight = Convert.ToDouble(dayData[3].Replace('.', ','));

                        sum += (hight + low) / 2;
                        count++;
                    }
                    // Это исключение появляется, когда по акции вообще нет данных
                    catch (IndexOutOfRangeException)
                    {
                        Console.WriteLine($"{stockName} : data is missing");
                    }
                    // Это исключение появляется для заголовочной строки и данных null
                    catch (FormatException) { }
                }

                // Если смогли достать хоть какие-то данные, считаем среднее. Иначе - возвращаем null.
                if (count != 0) return sum / count;
                else return null;
            }
        }

        // Объявляем мьютекс, чтобы безопасно писать в файл
        static Mutex mutOut = new Mutex();
        // Счётчик параллельных потоков
        static int ThreadCount = 0;

        // Функция для получения и записи данных об одной акции
        public static void GetAndWriteData(string name, StreamWriter outputWriter)
        {
            // Получаем среднее значение цены
            double? avgValue = Avreage.GetAvreage(name).GetAwaiter().GetResult();

            //Console.WriteLine($"{name} : {avgValue}!!!!");

            // Ждём освобождения мьютекса и лочим его
            mutOut.WaitOne();
            // Пишем название акции и цену в поток, используемый для записи в файл
            outputWriter.WriteLine($"{name} : {(avgValue)}");
            // Передаём содержимое потока в файл
            outputWriter.Flush();
            // Освобождаем мьютекс
            mutOut.ReleaseMutex();
            // Уменьшаем счётчик потоков
            ThreadCount--;
        }

        // Асинхронная функция, запускающая функцию GetAndWriteData асинхронно
        public static async Task<Task> GetAndWriteDataAsync(string name, StreamWriter outputWriter)
        {
            return Task.Run(() => GetAndWriteData(name, outputWriter));
        }

        static void Main()
        {

            //Part1
            Console.WriteLine("Part1:");
            // Открываем наши файлы через using
            using (FileStream input = File.Open("C:\\Users\\79526\\Desktop\\Ирочка\\ticker.txt", FileMode.Open),
                output = File.Open("C:\\Users\\79526\\Desktop\\Ирочка\\Avreage.txt", FileMode.Create))
            {
                // Создаём потоки для чтения и записи
                StreamReader inputReader = new StreamReader(input);
                StreamWriter outputWriter = new StreamWriter(output);

                // Считываем строку (имя акции) и асинхронно вызываем метод получения и записи данных
                while (!inputReader.EndOfStream)
                {
                    string name = inputReader.ReadLineAsync().GetAwaiter().GetResult();
                    // Увеличиваем счётчик потоков
                    ThreadCount++;
                    GetAndWriteDataAsync(name, outputWriter);
                    // Без задержки не работает/работает плохо. Возможно из-за сети
                    Thread.Sleep(50);
                }

                // Ждём завершения всех потоков
                while (ThreadCount > 0) { }
                Console.WriteLine("Done!");
                // Закрываем потоки
                inputReader.Close();
                outputWriter.Close();
            }

            Console.ReadLine();
        }
    }
}
