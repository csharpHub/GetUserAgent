# :eyeglasses: GetUserAgent
Get default browser user agent in C#

# :hammer: Usage:
``` C#
static void Main(string[] args)
{
    string browser = GetDefaultBrowser();
    string location = GetBrowserLocation(browser);
    string version = GetBrowserVersion(location);
    string useragent = GetUserAgent(browser);

    Console.WriteLine("Browser : " + browser);
    Console.WriteLine("Location : " + location);
    Console.WriteLine("Version : " + version);
    Console.WriteLine("User-Agent : " + useragent);

    Console.ReadLine();
}
```
