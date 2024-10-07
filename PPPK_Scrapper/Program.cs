using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

Console.WriteLine("Scrapper started");
var chromeOptions = new ChromeOptions();
// chromeOptions.AddArguments("--headless=new");
var driver = new ChromeDriver(chromeOptions);
driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
var startPage = "https://xenabrowser.net/datapages/?hub=https://tcga.xenahubs.net:443";
driver.Url = startPage;
driver.Navigate();


var list = driver.FindElement(By.CssSelector("ul.Datapages-module__list___2yM9o"));
if (list == null)
    throw new Exception($"List on starting page {startPage} not found");

var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(1));
wait.Until(webDriver =>
{
    try
    {
     webDriver.FindElement(By.CssSelector("ul.Datapages-module__list___2yM9o > li > a"));
    }
    catch (Exception e)
    {
        return false;
    }

    return true;

});

var items = list.FindElements(By.CssSelector("li > a"));
if (items == null || items.Count == 0)
    throw new Exception($"Items on starting page {startPage} not found");

List<string> urls = new List<string>();
foreach (var item in items)
{
    urls.Add(item.GetAttribute("href"));
}


foreach (var url in urls)
{
    driver.Url = url;
    driver.Navigate();
    var dataSetName = "pancan normalized";
   
    Thread.Sleep(500);
    var da = driver.FindElements(By.CssSelector("a"));
    var dataSets = da
        .Where(l => l.Text.Contains(dataSetName)).Select(l => l.GetAttribute("href")).ToList();

    foreach (var set in dataSets)
    {
        driver.Url = set;
        driver.Navigate();
        Thread.Sleep(500);
        // var downloadButton = driver.FindElement(By.CssSelector("button.MuiButtonBase-root-2823"));
    }
}

Console.WriteLine("Scrapper finished");
driver.Quit();