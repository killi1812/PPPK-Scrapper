using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.IO.Compression;

var chromeOptions = new ChromeOptions();
chromeOptions.AddArguments("--headless=new");
var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads",
    $"Data{DateTime.Now.ToString("yyyy-MM-dd")}");
if (Directory.Exists(path))
{
    Directory.Delete(path, true);
}

Directory.CreateDirectory(path);

chromeOptions.AddUserProfilePreference("download.default_directory", path);
chromeOptions.AddUserProfilePreference("download.prompt_for_download", false);
chromeOptions.AddUserProfilePreference("download.directory_upgrade", true);

Console.WriteLine("Scrapper started");
var driver = new ChromeDriver(chromeOptions);
// driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

var startPage = "https://xenabrowser.net/datapages/?hub=https://tcga.xenahubs.net:443";
driver.Url = startPage;
driver.Navigate();


var list = driver.FindElement(By.CssSelector("ul.Datapages-module__list___2yM9o"));
if (list == null)
    throw new Exception($"List on starting page {startPage} not found");

var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
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

var downloadList = new List<string>();

foreach (var url in urls)
{
    driver.Url = url;
    driver.Navigate();
    var dataSetName = "pancan normalized";

    var dataSet = new List<string>();
    try
    {
        dataSet = wait.Until(d =>
        {
            var da = d.FindElements(By.CssSelector("a"))
                .Where(l => l.Text.Contains(dataSetName)).Select(l => l.GetAttribute("href")).ToList();
            if (da.Count == 0) return null;
            return da;
        });
    }
    catch (Exception e)
    {
    }

    foreach (var set in dataSet)
    {
        driver.Url = set;
        driver.Navigate();
        try
        {

            var downloadLink = wait.Until(d =>
            {
                try
                {
                    //*[@id="main"]/div/div/div/span[6]/span/a[1] 
                    var rez = d.FindElement(By.XPath("//*[@id=\"main\"]/div/div/div/span[6]/span/a[1]"));
                    return rez;
                }
                catch (Exception e)
                {
                    return null;
                }
            });
            downloadList.Add(downloadLink.GetAttribute("href"));
        }
        catch
        {
        }
    }
}

Console.WriteLine($"Downloads started count:{downloadList.Count}");
foreach (var url in downloadList)
{
    driver.Url = url;
    driver.Navigate();
    Console.WriteLine($"Downloaded from {url}");
}

int downloadTimeout = 10;
var stopwatch = Stopwatch.StartNew();

while (true)
{
    var files = new DirectoryInfo(path).GetFiles();
    bool isDownloading = files.Any(f => f.LastWriteTime > DateTime.Now.AddSeconds(-3));
    if (!isDownloading)
        break;

    if (stopwatch.Elapsed.Minutes > downloadTimeout)
    {
        Console.WriteLine("Download timed out.");
        break;
    }

    Thread.Sleep(1000);
}

Console.WriteLine("Downloads finished.");


// {
//     var processStartInfo = new ProcessStartInfo
//     {
//         FileName = "/bin/bash",
//         RedirectStandardInput = true,
//         UseShellExecute = false,
//         CreateNoWindow = true,
//     };
//
//     var process = new Process { StartInfo = processStartInfo };
//     process.Start();
//
//     using (StreamWriter sw = process.StandardInput)
//     {
//         if (sw.BaseStream.CanWrite)
//         {
//             sw.WriteLine($"chmod 777 {Path.Combine(path,"*")}");
//         }
//     }
//
//     process.WaitForExit();
// }


{
    var files = new DirectoryInfo(path).GetFiles().Select(f => f.FullName);
    foreach (var gzipFilePath in files)
    {
        using (FileStream originalFileStream = new FileStream(gzipFilePath, FileMode.Open, FileAccess.Read))
        {
            using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
            {
                // Create a new file using the decompressed stream
                using (FileStream decompressedFileStream = new FileStream(gzipFilePath.Replace(".gz", ".txt"),
                           FileMode.Create, FileAccess.Write))
                {
                    decompressionStream.CopyTo(decompressedFileStream);
                    Console.WriteLine($"Decompressed: {gzipFilePath} to {path}");
                }
            }
        }
    }
}

string[] gzFiles = Directory.GetFiles(path, "*.gz");
foreach (string gzFile in gzFiles)
{
    File.Delete(gzFile);
}

Console.WriteLine("Scrapper finished");
driver.Quit();