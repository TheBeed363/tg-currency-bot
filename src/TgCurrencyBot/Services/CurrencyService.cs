using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using TgCurrencyBot.Models;

namespace TgCurrencyBot.Services;

public class CurrencyService : BackgroundService
{
    public readonly IMemoryCache _memoryCache;
    public readonly ILogger _logger;

    public CurrencyService(IMemoryCache memoryCache, ILogger<CurrencyService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // так как эта задача выполняется в другом потоке, то велика вероятность, что
                // локаль по умолчанию может отличаться от той, которая установлена в нашем приложении,
                // поэтому явно укажем нужную нам, чтобы не было проблем с разделителями, названиями и т.д.
                Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU"); // <== нужная вам локаль

                // кодировка файла xml с сайта ЦБ - windows-1251
                // по умолчанию она недоступна в .NET Core, поэтому регистрируем нужный провайдер 
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                // т.к. мы знаем что данные к нам приходят именно в файле, именно в формате XML,
                // поэтому нет необходимости использовать WebRequest,
                // используем в работе класс XDocument и сразу забираем файл с удаленного сервера
                var xml = XDocument.Load("http://www.cbr.ru/scripts/XML_daily.asp");

                // далее парсим файл и находим нужные нам валюты по их коду ID, и заполняем класс-модель
                var converter = new Currency();

                // доллары
                converter.USD = xml.Elements("ValCurs").Elements("Valute").FirstOrDefault(x => x.Element("NumCode").Value == "840").Elements("Value").FirstOrDefault().Value;
                // евро
                converter.EUR = xml.Elements("ValCurs").Elements("Valute").FirstOrDefault(x => x.Element("NumCode").Value == "978").Elements("Value").FirstOrDefault().Value;

                converter.CNY = xml.Elements("ValCurs").Elements("Valute").FirstOrDefault(x => x.Element("NumCode").Value == "156").Elements("Value").FirstOrDefault().Value;

                _memoryCache.Set("key_currency", converter, TimeSpan.FromMinutes(1440));
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }

            // если указаний о завершении данной задачи не поступало,
            // то запрашиваем обновление данных каждый час
            await Task.Delay(3600000, cancellationToken);
        }
    }
}