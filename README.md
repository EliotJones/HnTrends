# HackerNews Trends #

This is a site designed to display trends in [HackerNews](https://news.ycombinator.com/) posting over time. It can be used to identify long term trending of topics being posted and whether a topic is currently having an 'attention bubble'.

The site supports viewing data grouped by Day, Week and Month. It also supports viewing values as % of total count rather than just a straightforward count.

The site is built using .NET Core 2.1 MVC. The backing data is stored in a SQLite database and the data is indexed using [Lucene.NET](https://lucenenet.apache.org/). In addition is uses the .NET `IMemoryCache` to cache frequently accessed values.

An example of a trend search result is shown below:

![Trend plot displays an increasing frequency of Rust related posts](https://raw.githubusercontent.com/EliotJones/HnTrends/master/docs/example-screen.png)

The site is hosted on an Ubuntu VM here (no HTTPS at present): http://eliot-jones.com:5690/.

## High Level Overview ##

The site has the following components grouped by C# project:

+ HnTrends - the website itself. This uses MVC to display the results. Results are gather based on a regular polling interval using a .NET BackgroundService.
+ HnTrends.Crawler - runs a task which polls the HackerNews API to find the latest stories. Since the HackerNews API returns all item types from the same endpoint it is necessary to step through them sequentially. In order to speed this up it uses multithreading from the console based launcher `HnTrends.ConsoleCrawler`. The website is limited to a single thread when using this.
+ HnTrends.Database - Writes and reads data from a SQLite database using plain ADO.NET style code.
+ HnTrends.Indexer - Maintains and updates a Lucene.NET index by reading data from the SQLite database and updating the index. The website's background poller also calls this after the data is updated.