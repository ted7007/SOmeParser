using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using AngleSharp;
using AngleSharp.Dom;
using BookParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Parser;

public class BookParser
{
    public static IDocument GetDocument(string url)
    {
        var config = Configuration.Default.WithDefaultLoader().WithDefaultCookies();
        var context = BrowsingContext.New(config);
        var document = context.OpenAsync(url).Result;
        return document;

    }

    private IHtmlCollection<IElement>? GetColelctionOfBooks(string url, string className, string siteName, int countTries = 10)
    {
        IHtmlCollection<IElement> bookCollection = null;
        while (true)
        {
            var document = GetDocument(url);
            bookCollection = document.GetElementsByClassName(className);
            if (bookCollection.Length != 0)
            {
                break;
            }
    
            if (countTries < 10)
            {
                Console.WriteLine($"page wasnt got on url - {url}; Reloading..");
                continue;
            }
    
            break;
        }

        return bookCollection;

    }

    private (List<Book>, List<Url>) ParseBookInfo(string urlWithCollection,string siteName, int startPage, int endPage)
    {
        Console.WriteLine($"Started thread since {startPage} to {endPage}");
        var books = new List<Book>();
        List<Url> unloadedUrls = new List<Url>();
        DateTime nowThread = DateTime.Now;

        for (int i = startPage; i <= endPage; i++)
        {
            try
            {
                DateTime now = DateTime.Now;
                string pageUrl = urlWithCollection + Convert.ToString(i);
                var bookElementsFromPage = GetColelctionOfBooks(pageUrl,
                    "catalog-section-item intec-grid-item-3 intec-grid-item-700-2 intec-grid-item-720-3 intec-grid-item-950-2",
                    siteName);

                if (bookElementsFromPage is null || bookElementsFromPage.Length == 0)
                {
                    Console.WriteLine(
                        $"[{DateTime.Now}]Page - {i}: Книги не найдены в теле документа.");
                    unloadedUrls.Add(new Url(){Value = pageUrl});
                    continue;
                }

                Console.WriteLine($"Page - {i} was readed, count = {bookElementsFromPage.Length}");
                foreach (var bookFromList in bookElementsFromPage)
                {
                    var book = ParseBookElement(bookFromList);
                    if (book is null)
                        continue;
                    books.Add(book);
                    if (books.Count % 100 == 0)
                        Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}]: was parsed {books.Count} books");
                }

                DateTime end = DateTime.Now;
                Console.WriteLine(
                    $"Page - {i} was parsed, count in thread = {books.Count}; time - {new TimeSpan((end - now).Ticks).TotalMinutes} min");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : ERROR for page- {ex.Message}");
            }
        }

        DateTime endThread = DateTime.Now;
        Console.WriteLine(
            $"Thread parsed all pages.  time - {new TimeSpan((endThread - nowThread).Ticks).TotalMinutes} min");

        return (books, unloadedUrls);
    }

        private Book? ParseBookElement(IElement element)
        {
            string refToBook = "";
            string BoookName = "";
            int Remainder = 0;
            int Price = 0;
            string Description = "";
            String Author = "";
            string Genre = "";
            String Image = "";
            int NumberPages = 0;
            string ISBN = "";
            string PublisherName = "";
            try
            {
                refToBook = "https://primbook.ru" +
                            element.GetElementsByClassName("catalog-section-item-image-wrapper intec-image-effect")[0]
                                .Attributes["href"].Value;
                var BookInfo = GetDocument(refToBook);
                GetProps(element.Attributes["data-data"].Value, out BoookName, out Price, out Remainder);
                Description =
                    BookInfo.GetElementsByClassName("catalog-element-section-description intec-ui-markup-text")[0]
                        .TextContent
                        .Replace("\t", "")
                        .Replace("\n", "");
                ;
                Genre = BookInfo.GetElementsByClassName("breadcrumb-item")[^1].TextContent
                    .Replace("\t", "")
                    .Replace("\n", "");

                Image = "https://primbook.ru" +
                        BookInfo.GetElementsByClassName("catalog-element-gallery-picture intec-image")[0]
                            .Attributes["href"]
                            .Value
                            .Replace(" ", "")
                            .Replace("\t", "")
                            .Replace("\n", "");
                ;

            }
            catch (Exception ex)
            {
                //Console.WriteLine($"{DateTime.Now} : ERROR for book - {ex.Message}");
            }

            if (string.IsNullOrEmpty(refToBook))
                return null;
            Book book = new Book()
            {
                Author = Author,
                Description = Description,
                Genre = Genre,
                Image = Image,
                Name = BoookName,
                Remainder = Remainder,
                Price = Price,
                NumberOfPages = NumberPages,
                ISBN = ISBN,
                PublisherName = PublisherName,
                ParsingDate = DateTime.UtcNow
            };

            book.SourceName = refToBook;
            return book;
        }

        private void GetProps(string json, out string bookName, out int price, out int quantity)
        {
            var info = JObject.Parse(json);
            bookName = info.GetValue("name").ToString();
            price = (int)info["prices"].First["discount"]["value"].ToObject<float>();
            bool available = info["available"].ToObject<bool>();
            if (available)
                quantity = info["quantity"]["value"].ToObject<int>();
            else
                quantity = 0;

        }

        public (List<Book>, List<Url>) StartParsingAsyncNew(string address,string siteName, int firstPage, int lastPage, int countTasks)
        {
            if (lastPage - firstPage < countTasks && countTasks != 1)
                for (int i = 0; i < 10; i++)
                    Console.WriteLine("WARNING!!! Count task more than count parsing pages. CAN ME ERROR");
            DateTime
                start = DateTime
                    .Now; // Если счет страниц начинается с 1, то количество страниц = ( последняя страница - 1 )
            // 305 по 500 книг. Если выбрать 0 страницу - покажется 1-ая. Если выбрать 306-ю - не выдаст книг.
            Console.WriteLine($"[{start.ToShortTimeString()}] start parsing address {address}");
            int countPages = lastPage;
            if (firstPage > 0 && lastPage != 1)
                countPages = lastPage - firstPage;
            Task[] tasks = new Task[countTasks + 1];
            int countPagesForOneTask = countPages / countTasks;
            int lastParsedPage = 0;
            int startPage = firstPage;
            var finalBooks = new List<Book>();
            var unloadedPages = new List<Url>();
            for (int i = 0; i < countTasks; i++)
            {
                int endPage = startPage + countPagesForOneTask;
                int numberTask = i;
                int curStartPage = startPage; // чтоб не изменялось после старта потока
                var task = Task.Factory.StartNew(() =>
                {
                    
                    var resCortage = ParseBookInfo(address,siteName, curStartPage, endPage - 1);
                    lock (finalBooks)
                    {
                        finalBooks.AddRange(resCortage.Item1);
                    }

                    lock (unloadedPages)
                    {
                        unloadedPages.AddRange(resCortage.Item2);
                    }
                });
                lastParsedPage = endPage - 1;
                startPage = lastParsedPage + 1;
                tasks[i] = task;
            }

            Task lastTask = null;
            if (lastParsedPage < lastPage)
            {
                lastTask = Task.Factory.StartNew(() =>
                {
                    var resCortage = ParseBookInfo(address,siteName, lastParsedPage + 1, lastPage);
                    lock (finalBooks)
                    {
                        finalBooks.AddRange(resCortage.Item1);
                    }

                    lock (unloadedPages)
                    {
                        unloadedPages.AddRange(resCortage.Item2);
                    }
                });
                tasks[countTasks] = lastTask;
            }
            else
            {
                tasks[countTasks] = Task.FromResult(new List<Book>());
            }

            // 1 task - 0,50 min - x1 work
            // 8 tasks - 0,55 min - x8 work
            // 16 tasks - 0,68 min but x16 work
            Task.WaitAll(tasks);
            DateTime end = DateTime.Now;
            Console.WriteLine($"Parsing finished. It got {(end - start).TotalMinutes} minutes; ");
            return (finalBooks, unloadedPages);
        }

        private void WriteToJSON(string path, List<Book> books)
        {
            var json = JsonConvert.SerializeObject(books);
            File.WriteAllText(path, json, Encoding.UTF8);
            Console.WriteLine("Books writed to json, count " + books.Count);
            File.WriteAllText(path + "Count", Convert.ToString(books.Count));
        }


        private void WriteUrlToJson(Url url, string path)
        {
            List<Url> urls = new List<Url>() { url };
            bool isFileExists = File.Exists(path);
            if (isFileExists)
            {
                var urlsText = File.ReadAllText(path, Encoding.UTF8);
                urls.AddRange(JsonConvert.DeserializeObject<List<Url>>(urlsText));
            }

            var urlJson = JsonConvert.SerializeObject(urls);
            File.WriteAllText(path, urlJson);
        }

    } 