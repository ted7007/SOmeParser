using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using AngleSharp;
using AngleSharp.Dom;
using BookParser;
using Newtonsoft.Json;

namespace Parser;

public class ParserBook
{
    public static IDocument GetDocument(string url)
    {
        var config = Configuration.Default.WithDefaultLoader().WithDefaultCookies();
        var context = BrowsingContext.New(config);
        return context.OpenAsync(url).Result;
    }
    private List<Book> ParseBookInfo(string urlWithCollection,int startPage, int endPage)
    {
        Console.WriteLine($"Started thread since {startPage} to {endPage}");
        var books = new List<Book>();
        DateTime nowThread = DateTime.Now;
        
        for(int i = startPage; i <= endPage; i++)
        {
            try
            {
                DateTime now = DateTime.Now;
                var document2 = GetDocument(urlWithCollection + Convert.ToString(i));


                var bookElementsFromPage =
                    document2.GetElementsByClassName("woo-entry-inner clr");
                
                if ((bookElementsFromPage.Length == 0))
                {
                    Console.WriteLine($"[{DateTime.Now}]Page - {i}: Книги не найдены в теле документа. Status code - {document2.StatusCode}");
                    continue;
                }
                Console.WriteLine($"Page - {i} was readed, count = {bookElementsFromPage.Length}");
               // Console.WriteLine("Page - "+(i+1));
                foreach (var bookFromList in bookElementsFromPage)
                {
                    var book = ParseBookElement(bookFromList);
                    if(book is null)
                        continue;
                    books.Add(book);
                    if(books.Count%100==0)
                        Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}]: was parsed {books.Count} books");
                }
                DateTime end = DateTime.Now;
                Console.WriteLine($"Page - {i} was parsed, count in thread = {books.Count}; time - {new TimeSpan((end-now).Ticks).TotalMinutes} min");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : ERROR for page- {ex.Message}");
            }
        }
        DateTime endThread = DateTime.Now;
        Console.WriteLine($"Thread parsed all pages.  time - {new TimeSpan((endThread-nowThread).Ticks).TotalMinutes} min");

        return books;
    }

    private Book? ParseBookElement(IElement element)
    {
        string refToBook = "";
        string BoookName ="";
        int Remainder =0;
        int Price=0;
        string Description = "";
        String Author = "";
        string Genre = "";
        String Image = "";
        int NumberPages = 0;
        string ISBN = "";
        string PublisherName = "";
        try
        {
            refToBook = element.GetElementsByClassName("woo-entry-image clr")[0].Children[0].Attributes["href"].Value;
            //Console.WriteLine("AddToRef is " + refToBook);
            var BookInfo = GetDocument(refToBook);
            BoookName = BookInfo.GetElementsByClassName("single-post-title product_title entry-title")[0].TextContent;
            var priceRes = element.GetElementsByClassName("woocommerce-Price-amount amount")[0].Children[0].TextContent.Split(',')[0];
            Price = int.Parse(priceRes);
            var wordWithPrice = BookInfo.GetElementsByClassName("stock in-stock")[0].TextContent.Split(' ');
            foreach (var i in wordWithPrice)
            {
                if (int.TryParse(i, out Remainder))
                    break;
            }
            Description = BookInfo.GetElementsByClassName("woocommerce-product-details__short-description")[0].TextContent
                .Replace("\t", "")
                .Replace("\n", "");;
            Genre = BookInfo.GetElementsByClassName("posted_in")[0].GetElementsByTagName("a")[0].TextContent
                .Replace("\t", "")
                .Replace("\n", "");
            
            Image = BookInfo.GetElementsByClassName("woocommerce-product-gallery__wrapper")[0].GetElementsByTagName("img")[0].Attributes["src"]
                .Value
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\n", "");;
            var properties = BookInfo.GetElementsByClassName("woocommerce-product-attributes shop_attributes")[0].GetElementsByTagName("tr");
            foreach (var i in properties)
            {
                var label = i.GetElementsByTagName("th")[0].TextContent;
                var value = i.GetElementsByTagName("td")[0].TextContent
                    .Replace("\t", "")
                    .Replace("\n", "");
                switch (label.ToLower())
                {
                    case "автор":
                        Author = value;
                        break;
                    case "isbn/issn":
                        ISBN = value;
                        break;
                    case "кол-во страниц":
                        NumberPages = int.Parse(value);
                        break;
                    case "издательство":
                        PublisherName = value;
                        break;
                    
                }
            }

        }
        catch (Exception ex)
        {
       //     Console.WriteLine($"{DateTime.Now} : ERROR for book - {ex.Message}");
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
            ParsingDate = DateTime.Today.ToUniversalTime()
        };
        
        book.SourceName = refToBook;
        return book;
    }


    public void StartParsingAsyncNew(string address, int firstPage, int lastPage, int countTasks)
    {
        if(lastPage-firstPage < countTasks)
           for(int i = 0; i < 10; i++) Console.WriteLine("WARNING!!! Count task more than count parsing pages. CAN ME ERROR");
        DateTime start = DateTime.Now;  // Если счет страниц начинается с 1, то количество страниц = ( последняя страница - 1 )
         // 305 по 500 книг. Если выбрать 0 страницу - покажется 1-ая. Если выбрать 306-ю - не выдаст книг.

         int countPages = lastPage;
        if (firstPage > 0)
            countPages = lastPage - firstPage;
        Task[] tasks = new Task[countTasks+1];
        int countPagesForOneTask = countPages / countTasks;
        int lastParsedPage = 0;
        int startPage = firstPage;
        var finalBooks = new List<Book>();
        for (int i = 0; i < countTasks; i++)
        {
            int endPage = startPage + countPagesForOneTask;
            int numberTask = i;
            int curStartPage = startPage; // чтоб не изменялось после старта потока
            var task = Task.Factory.StartNew(() =>
            {
                var parsedBooks = ParseBookInfo(address, curStartPage, endPage - 1);
                lock (finalBooks)
                {
                    finalBooks.AddRange(parsedBooks);
                }
            });
            lastParsedPage = endPage-1;
            startPage = lastParsedPage + 1;
            tasks[i] = task;
        }

        Task lastTask = null;
        if (lastParsedPage != lastPage)
        {
            lastTask =  Task.Factory.StartNew(() =>
            {
                var parsedBooks = ParseBookInfo(address, lastParsedPage + 1, lastPage);
                lock (finalBooks)
                {
                    finalBooks.AddRange(parsedBooks);
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
        Console.WriteLine($"Parsing finished. It got {(end-start).TotalMinutes} minutes; ");
        WriteToJSON($"Igra-Slov-{DateTime.Today.ToShortDateString()}.json", finalBooks);
    }

    private void SaveSite(string address, int firstPage, int lastPage, int countTasks)
    {
          //TODO
    }
    
    private void WriteToJSON(string path, List<Book> books)
    {
        var json = JsonConvert.SerializeObject(books);
        File.WriteAllText(path, json, Encoding.UTF8);
        Console.WriteLine("Books writed to json, count "+books.Count);
        File.WriteAllText(path+"Count",Convert.ToString(books.Count));
    }
    
    private string GetValue(IHtmlCollection<IElement> collection)
    {
        if (collection.Length > 0)
            return collection[0].TextContent;
        return "";
    }

    private IElement GetElement(IHtmlCollection<IElement> collection)
    {
        if (collection.Length > 0)
            return collection[0];
        return null;
    }
}