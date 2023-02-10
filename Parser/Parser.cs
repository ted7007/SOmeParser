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
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);
        return context.OpenAsync(url).Result;
    }
    private async Task<List<Book>> ParserBookInfo(string urlWithCollection,int countPages)
    {
        var books = new List<Book>();
        for(int i = 0; i < countPages; i++)
        {
            try
            {
                var document2 = GetDocument(urlWithCollection + "?PAGEN_1=" + Convert.ToString(i));


                var textWitHResultSearchElements =
                    document2.GetElementsByClassName("item_block col-4");
                if ((textWitHResultSearchElements.Length == 0))
                {
                    Console.WriteLine("Книги не найдены");
                }

                Console.WriteLine("------------------------------");
                Console.WriteLine("Page - "+(i+1));
                foreach (var bookFromList in textWitHResultSearchElements)
                {
                    var book = ParseICollection(bookFromList);
                    books.Add(book);
                    Console.WriteLine($"Got book -  {book.Name}; Price - {book.Price}; Remainder - {book.Remainder}");
                    Console.WriteLine();
                }
                
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : ERROR for page- {ex.Message}");
            }
        }

        return books;
    }

    private Book ParseICollection(IElement element)
    {
        
        string addToRef = element.GetElementsByClassName("wrapper_fw")[0].GetElementsByTagName("a")[0]
            .Attributes["href"].Value;
        //Console.WriteLine("AddToRef is "+addToRef);
        var BookInfo = GetDocument("https://globusbooks.ru" + addToRef);
        string BoookName ="";
        int Remainder =0;
        int Price=0;
        string Description = "";
        String Author = "";
        string Genre = "";
        String Image = "";
        try
        {
            BoookName = BookInfo.GetElementById("pagetitle").TextContent; 
            Remainder = Int32.Parse(BookInfo.GetElementsByClassName("plus")[0].Attributes["data-max"].Value);
            Price = Int32.Parse(BookInfo.GetElementsByClassName("price")[0].Attributes["data-value"].Value);
            Description = BookInfo.GetElementsByClassName("detail_text")[0].TextContent;
            var mbAuthor = BookInfo.GetElementsByClassName("cml_right")[0].TextContent.Split(' ');
            for (int c = 0; c < mbAuthor.Length; c++)
            {
                if (mbAuthor[c] == "Автор:" && c != mbAuthor.Length - 1)
                    Author = mbAuthor[c + 1];
                if (mbAuthor[c] == "Жанр:" && c != mbAuthor.Length - 1)
                    Genre = mbAuthor[c + 1];
            }

            
            Image = "https://globusbooks.ru" +
                           BookInfo.GetElementById("photo-0").GetElementsByTagName("img")[0].Attributes["href"];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now} : ERROR for book - {ex.Message}");
        }
        Book book = new Book()
        {
            Author = Author,
            Description = Description,
            Genre = Genre,
            Image = Image,
            Name = BoookName,
            Remainder = Remainder,
            Price = Price,
            NumberOfPages = 0
        };
        
        book.SourceName = "globusbooks";
        return book;
    }
    
    public async Task StartParsingAsync(int maxCountISBNs)
    {
        var finalBooks = new List<Book>();
        var booksUch = await ParserBookInfo("https://globusbooks.ru/catalog/uchebnaya_literatura/", 311);   // 311
        finalBooks.AddRange(booksUch);
        var booksLit = await ParserBookInfo("https://globusbooks.ru/catalog/literatura/", 56);  //56
        finalBooks.AddRange(booksLit);
        
        var bookHyd = await ParserBookInfo("https://globusbooks.ru/catalog/khudozhestvennaya_literatura/", 97); //97
        finalBooks.AddRange(bookHyd);
        
        var bookDet = await ParserBookInfo("https://globusbooks.ru/catalog/detskaya_literatura/", 371);     //371
        finalBooks.AddRange(bookDet);
        
        var bookENc = await ParserBookInfo("https://globusbooks.ru/catalog/entsiklopedii/", 24);  //24
        finalBooks.AddRange(bookENc);
        
        WriteToJSON("BooksFromGlobus.json", finalBooks);
        
    }

    private void WriteToJSON(string path, List<Book> books)
    {
        var json = JsonConvert.SerializeObject(books);
        File.WriteAllText(path, json, Encoding.UTF8);
        Console.WriteLine("Books writed to json, count "+books.Count);
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