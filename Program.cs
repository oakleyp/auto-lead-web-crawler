using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScrapySharp.Core;
using ScrapySharp.Html.Parsing;
using ScrapySharp.Network;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Html.Forms;
using System.Text.RegularExpressions;
using System.Web;

namespace FacultyContactCrawler
{
    class ContactInfo
    {
        private string name;
        private string school;
        private string department;
        private string address;
        private string position;
        private string course;
        private string phone;
        private string email;
        private string link;
        private int credct = 0; //Stores up to date number of non-"Unknown" credentials stored


        public ContactInfo(string Name = "Unknown", string School = "Unknown", string Department = "Unknown",
            string Job = "Unknown", string Course = "Unknown", string Address = "Unknown", string Phone = "Unknown", string Email = "Unknown",
            string Link = "Unknown")
        {
            this.name = Name;
            this.school = School;
            this.department = Department;
            this.position = Job;
            this.course = Course;
            this.address = Address;
            this.phone = Phone;
            this.email = Email;
            this.link = Link;
        }
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                credct++;
            }
        }
        public string School
        {
            get
            {
                return school;
            }
            set
            {
                school = value;
                credct++;
            }
        }
        public string Department
        {
            get
            {
                return department;
            }
            set
            {
                department = value;
                credct++;

            }
        }
        public string Position
        {
            get
            {
                return position;
            }
            set
            {
                if (position == "Unknown") credct++;
                position = value;

            }
        }
        public string Course
        {
            get
            {
                return course;
            }
            set
            {
                if (course == "Unknown") credct++;
                course = value;

            }
        }
        public string Address
        {
            get
            {
                return address;
            }
            set
            {
                if (address == "Unknown") credct++;
                address = value;
            }
        }
        public string Phone
        {
            get
            {
                return phone;
            }
            set
            {
                if (phone == "Unknown") credct++;
                phone = value;
            }
        }
        public string Email
        {
            get
            {
                return email;
            }
            set
            {
                if (email == "Unknown") credct++;
                email = value;
            }
        }
        public string Link
        {
            get
            {
                return link;
            }
            set
            {
                if (link == "Unknown") credct++;
                link = value;
            }
        }
        public int CredCount()
        {
            return credct;
        }
        public void PrintToConsole()
        {
            Console.WriteLine("Full Name: " + Name);
            Console.WriteLine("School: " + School);
            Console.WriteLine("Department: " + Department);
            Console.WriteLine("Position: " + Position);
            Console.WriteLine("Course: " + Course);
            Console.WriteLine("Address: " + Address);
            Console.WriteLine("Email: " + Email);
            Console.WriteLine("Phone: " + Phone);
            Console.WriteLine("Contact Link: " + Link);
        }
    }
    class CrawlLink
    {
        private Uri url; // Direct link to page
        private int weight;  // Determines precedence in crawl order 
        private string title; // Title given by <a> tag
        private bool crawled; // Whether page was already crawled for links
        private bool scraped; // Whether page was already scraped for links
        private List<ContactInfo> foundcontacts; // List of all relevant contacts found on page

        //Initializers
        public CrawlLink(Uri url, string title, int weight = 0, bool hasBeenCrawled = false)
        {
            this.title = title;
            this.url = url;
            this.weight = weight;
            this.crawled = hasBeenCrawled;
            this.scraped = false;
            this.foundcontacts = new List<ContactInfo>();
        }
        public CrawlLink(string url, string title, int weight = 0, bool hasBeenCrawled = false)
        {
            this.title = title;
            this.url = new Uri(url);
            this.weight = 0;
            this.crawled = hasBeenCrawled;
            this.scraped = false;
            this.foundcontacts = new List<ContactInfo>();
        }

        //Accessors & Modifiers
        public Uri URL
        {
            get { return url; }
            set { url = value; }
        }
        public string Title
        {
            get { return title; }
        }
        public int Weight
        {
            get { return weight; }
            set { weight = value; }
        }
        public List<ContactInfo> getContactList()
        {
            return this.foundcontacts;
        }
        public void AddContact(ContactInfo contactinfo)
        {
            foundcontacts.Add(contactinfo);
        }
        public bool hasBeenCrawled
        {
            get { return crawled; }
            set { crawled = value; }
        }
        public bool hasBeenScraped
        {
            get { return scraped; }
            set { scraped = value; }
        }

    }
    class CrawlLinkList
    {
        private List<CrawlLink> linklist;
        public CrawlLinkList()
        {
            linklist = new List<CrawlLink>();
        }
        public CrawlLink At(int index)
        {
            return linklist.ElementAt(index);
        }
        public int Count()
        {
            return linklist.Count();
        }

        public void PrioritizeAndWeedOut()
        {
            linklist = new List<CrawlLink>(linklist.OrderByDescending(link => link.Weight));
            if (Count() > 30)
            {
                int sum = 0;
                for (int i = 0; i < linklist.Count(); i++) sum += linklist.ElementAt(i).Weight;
                int avg = sum / linklist.Count();
                for (int i = linklist.Count() - 1; i >= 0; i--) if (linklist.ElementAt(i).Weight < avg) linklist.RemoveAt(i);
            }
        }
        public void Add(CrawlLink link)
        {
            linklist.Add(link);
        }
        public void Add(Uri link, string title, int weight = 0, bool hasBeenCrawled = false)
        {
            this.linklist.Add(new CrawlLink(link, title, weight, hasBeenCrawled));
        }
        public void RemoveAt(int index)
        {
            if (Count() > 0 && index < Count() && index >= 0)
            {
                if (index == 0) linklist = linklist.Skip(index).ToList();
                else linklist.RemoveAt(index);
            }
        }
        public Uri getURL(int index)
        {
            return this.linklist.ElementAt(index).URL;
        }
        public void Clear()
        {
            linklist = new List<CrawlLink>();
        }
    }

    class Program
    {

        public static int IndexOfAny(string input, string[] keywords, int startAtIndex = 0, bool caseSensitive = false)
        //Returns Index of first keyword in an array of keywords found in a given string 
        {
            if (!caseSensitive) { input = input.ToLower(); }
            for (int i = startAtIndex; i < keywords.Count(); i++)
            {
                if (!caseSensitive) keywords[i] = keywords[i].ToLower();

                if (input.Contains(keywords[i])) return i;
            }
            return -1;
        }
        public static bool isUSState(string content)
        {
            content = content.ToLower().Trim();
            for (int i = 0; i < 50; i++)
            {
                if (content.Contains((" " + StateArray.Abbreviations().ElementAt(i) + " ").ToLower())) return true;
                else if (content.Contains((" " + StateArray.Names().ElementAt(i) + " ").ToLower())) return true;
            }
            return false;
        }
        public static bool isUSAddress(string content)
        {
            content = content.ToLower().Trim();
            if (isUSState(content)) return true;
            else
            {
                if (Regex.IsMatch(content, @"(\d{2,}?\D{0,3}\s+)+((\D\S*[^\.]\s+){1,5})+(\D\S*)\s+")) return true;
                else if (isUSState(content)) return true;
            }
            return false;
        }
        public static bool isPhoneNumber(string content)
        {
            try
            {
                content = content.Trim();
                char[] tokens = { '+', '(', ')', '-', '.', ' ' };
                content = Regex.Replace(content, @"\D", "");
                /*bool unclean = true;
                while (unclean)
                {
                    for (int i = 0; i < tokens.Count(); i++)
                    {
                        // Remove all formatting from phone number
                        if (content.Contains(tokens[i])) {
                            while (content.Contains(tokens[i])) { content.Remove(tokens[i], 1); }
                        }
                        else unclean = false;
                    }
                }*/
                if (Regex.IsMatch(content, @"^\d+$") && content.Length > 5) return true; //It's a string of numbers and longer than 5 digits

                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Phone detection has shit itself... Press any key to continue."); Console.ReadLine();
                return false;
            }
        }
        public static string parseEmail(string content)
        {
            content = content.ToLower().Trim();
            // Attempt to purge email censors
            if (content.Contains("email"))
            {
                //Replace any leading label (ex. "Email: scrub 'at' itttech.edu" -> "scrub 'at' itttech.edu")
                content = Regex.Replace(content, @"(^(\S*)+\s){1}?", "");
            }
            string[] searchkeys = { " at ", " -at- ", " - at - ", "-at-", "- at -", " -(at)- ", "-(at)-",
            " (at) ", " 'at' ",  " \"at\" ", "'at'", "'@'", " @ "};
            int index = IndexOfAny(content, searchkeys);
            if (index != -1) content = content.Replace(searchkeys[index], "@");
            else if (content.Contains(" ") && !content.Contains("@")) content.Replace(" ", "@");

            // Validate email address half-assedly
            if (content.Contains("@") && content.Contains(".") && content.Length > 5) return content; // Fuck IDK, It might be

            return "";
        }
        public static bool isPositionName(string content)
        {
            content = content.ToLower().Trim();
            string PositionKeys = "Chair,Head,Professor,Instructor,Lead,Admistrator,Organizer,Director";
            List<string> KeyMatch = PositionKeys.Split(',').ToList();
            for (int i = 0; i < KeyMatch.Count(); i++)
            {
                if (content.Contains(KeyMatch.ElementAt(i).ToLower())) return true; //Could be, fuck it
            }
            return false;
        }
        public static bool isDepartmentName(string content)
        {
            content = content.ToLower().Trim();
            string LinkKeys = "CS,Computer Science,Department,College,CE,Engineering,Programming,Software,C++,Java,Development";
            List<string> LinkMatch = LinkKeys.Split(',').ToList();

            for (int i = 0; i < LinkMatch.Count(); i++)
            {
                if (content.Contains(LinkMatch.ElementAt(i).ToLower())) return true; //Could be, fuck it
            }
            return false;
        }
        public static bool isFullName(string content)
        {
            content = content.ToLower().Trim();
            string NameKeys = "Mr,Mrs,Dr,Prof,Phd,Doctor";
            List<string> Keywords = NameKeys.Split(',').ToList();
            int spacect = 0; // If it contains 1 to 4 spaces, that's a good start
            foreach (char letter in content) if (letter == ' ') { spacect++; }
            if (spacect > 2)
            {
                if ((IndexOfAny(content, Keywords.ToArray()) != -1)) return true;
            }
            return false;
        }
        public static bool isRelevantInfo(string content)
        {
            content = content.ToLower().Trim();
            string NameKeys = " Mr, Mrs, Dr, Prof, Phd, Doctor";
            List<string> Keywords = NameKeys.Split(',').ToList();
            int spacect = 0; // If it contains 1 to 4 spaces, that's a good start
            foreach (char letter in content) if (letter == ' ') { spacect++; }
            if ((IndexOfAny(content, Keywords.ToArray()) != -1)) return true; // It's gotta be a name at least
            else if (spacect > 4) return false; // 4 names and no title? Either irrelevant or hispanic and irrelevant de facto, ignoring the unicornesque Mexican CS Professors
            else if (spacect > 1 && spacect < 5) return true;
            else return false;


        }
        public static int getKeywordFrequency(List<string> keywords, string context)
        {
            string haystack = context.ToLower();
            int matchct = 0;

            for (int i = 0; i < keywords.Count(); i++)
            {
                string needle = keywords.ElementAt(i).ToLower();
                bool match = false;
                do
                {
                    if (haystack.Contains(needle))
                    {
                        match = true;
                        matchct++;
                        haystack = haystack.Replace(needle, " ");
                    }
                    else match = false;
                } while (match == true);
            }
            return matchct;
        }

        public static KeyValuePair<string,string> scrapeContactInfo(string text) // Returns true if any given type of tag on page contains contact info, and 
        {
            Console.WriteLine("Relevant String: ");
            Console.WriteLine(text + ";\n ");
            if (isUSAddress(text))
            {
                Console.WriteLine("Credential Type: Address\n");
                return new KeyValuePair<string, string>("Address", text);
            }
            else if (isPhoneNumber(text))
            {
                Console.WriteLine("Credential Type: Phone Number\n");
                //Remove any text or spaces
                string phonenum = text.Trim();
                phonenum = Regex.Replace(phonenum, @"[^\d]", "");
                return new KeyValuePair<string, string>("Phone", phonenum);
            }
            else if (!String.IsNullOrEmpty(parseEmail(text)))
            {
                Console.WriteLine("Credential Type: Email Address\n");
                return new KeyValuePair<string, string>("Email", parseEmail(text));
            }
            else if (isFullName(text))
            {
                Console.WriteLine("Credential Type: Full Name\n");
                //If another name appears, start the next entry
                return new KeyValuePair<string, string>("Name", text);
            }
            else if (isPositionName(text))
            {
                Console.WriteLine("Credential Type: Position Name\n");
                return new KeyValuePair<string, string>("Position", text);
            }
            else if (isDepartmentName(text))
            {
                Console.WriteLine("Credential Type: Department Name\n");
                return new KeyValuePair<string, string>("Department", text);
            }
            else return new KeyValuePair<string, string>("null", "");
        }
        public static bool getContact(HtmlNode node)
        {

            return false;
        }
        public static List<HtmlNode> ExtractAllAHrefTags(HtmlDocument htmlSnippet)
        {
            List<HtmlNode> hrefTags = new List<HtmlNode>();
            if (htmlSnippet.DocumentNode.SelectNodes("//a[@href]").Count() > 0)
            {
                foreach (HtmlNode link in htmlSnippet.DocumentNode.SelectNodes("//a[@href]"))
                {
                    hrefTags.Add(link);
                }
            }

            return hrefTags;
        }

        public static string ToAbsoluteUrl(string relativeUrl, WebPage httpcontext)
        {
            /*string tempUrl = relativeUrl;
            if (string.IsNullOrEmpty(relativeUrl) & string.IsNullOrEmpty(httpcontext.BaseUrl)) return relativeUrl;

            if (httpcontext.Html.InnerHtml.ToString() == null) return relativeUrl;
            string AbsoluteUrl = httpcontext.AbsoluteUrl.Host.ToString();

            if (httpcontext.AbsoluteUrl.AbsolutePath == ("/" + relativeUrl)) return relativeUrl;

            if (relativeUrl.ElementAt(0) != '/') 



            if (Uri.IsWellFormedUriString(AbsoluteUrl + tempUrl, UriKind.Absolute)) return (AbsoluteUrl + tempUrl);
            else return relativeUrl;*/
            if (string.IsNullOrEmpty(relativeUrl))
                return relativeUrl;

            if (httpcontext.Html.InnerHtml.ToString() == null)
                return relativeUrl;

            if (!relativeUrl.StartsWith("/"))
                relativeUrl = relativeUrl.Insert(0, "/");
            var url = httpcontext.AbsoluteUrl;
            string port = url.Port != 80 ? (":" + url.Port) : String.Empty;

            return String.Format("{0}://{1}{2}{3}",
                url.Scheme, url.Host, port, relativeUrl);
        }
        static void Main(string[] args)
        {
            //--------------------------------------------------------------------
            // ---------------Begin Configuration Variables-----------------------
            //--------------------------------------------------------------------

            // Title Keywords - dictate crawler decision to analyze page further for contact info
            string TitleKeys = "CS,Contact,Computer Science,Computer Engineering,Department,Faculty," +
                "Programming,Instructor,Department of Computer Science,Department of Engineering,School," +
                "College,Institute,University";
            List<string> TitleMatchList = TitleKeys.Split(',').ToList();

            // Link Keywords - dictate crawler decision to crawl linked page 
            string LinkKeys = "CS,Contact,Computer Science,Department,Faculty,Programming," +
                "Instructor,Department of Computer Science,Department of Engineering,College of Engineering," +
                "College of Computer Science,Academics,Courses,Schools,Classes,Colleges,People,Staff";
            List<string> LinkKeywordList = LinkKeys.Split(',').ToList();

            // Link Keywords that strongly suggest destination could be end goal - Give these links top priority in crawl precedence
            string EndLinkKeys = "Contact,Faculty,People";
            List<string> EndLinkKeywordList = EndLinkKeys.Split(',').ToList();

            // Heading keywords that suggest a contact has been found on an end node
            string ProfTitleKeys = "Chair,Director,Computer Science,Head,Coordinator,Department Chair,Dr,Prof,Professor,Doctor,Phone,Email,'at',@, at ," +
                "Office";
            List<string> ProfTitleMatchList = ProfTitleKeys.Split(',').ToList();

            // Table Content Heading Keywords - determine whether to scrape data contained under given column headings
            // Only useful for basic HTML pages, info on more heavily layered or formatted pages will be identified by regex pattern
            string TableKeys = "Email,State,Phone";
            List<string> TableMatchList = TableKeys.Split(',').ToList();



            // Initial Links to crawl
            //string RootLinksStr = "web.mit.edu,uky.edu";
            //List<string> RootLinkListStr = RootLinksStr.ToString().Split(',').ToList();
            List<Uri> RootLinkList = new List<Uri>();
            //for (int i = 0; i < RootLinkListStr.Count(); i++) RootLinkList.Add(new System.Uri(RootLinkListStr.ElementAt(i)));
            string input = "";
            Console.Out.WriteLine("Enter each URL to be crawled followed by {Enter}; Type 'go' followed by {Enter} to start:\n");
            do
            {
                input = Console.ReadLine();
                if (!Uri.IsWellFormedUriString(input, UriKind.Absolute) & !input.StartsWith("http")) Console.WriteLine("Invalid URL");
                else RootLinkList.Add(new Uri(input));
            } while (input.ToLower() != "go");


            //--------------------------------------------------------------------
            // -------------End Configuration Variables---------------------------
            //--------------------------------------------------------------------

            // setup the browser
            ScrapingBrowser Browser = new ScrapingBrowser();
            Browser.AllowAutoRedirect = true; // Browser has many settings you can access in setup
            Browser.AllowMetaRedirect = true;
            CrawlLinkList crawllinks = new CrawlLinkList();

            for (int urinum = 0; urinum < RootLinkList.Count(); urinum++) // Crawls Root Links, loops back after each root website has been crawled
            {
                crawllinks.Clear(); // Stores interesting links on first crawl of each page
                CrawlLink RootLink = new CrawlLink(RootLinkList.ElementAt(urinum), "Root Link");
                crawllinks.Add(RootLink); // Add root link to start off
                int totalcrawled = 0; //Debugging
                //-----------------------Start Crawling-------------------------------//
                for (int linknum = 0; linknum < crawllinks.Count() && linknum < 45 && totalcrawled < 200; linknum++)
                {
                    Console.Out.WriteLine("Link started: " + linknum + " " + crawllinks.getURL(linknum).ToString() + " \n");

                    if (!crawllinks.At(linknum).hasBeenCrawled)
                    {
                        totalcrawled++; //Debugging
                        bool resultisHTML = false;
                        WebPage PageResult;
                        //go to the home page
                        try
                        {
                            PageResult = Browser.NavigateToPage(crawllinks.getURL(linknum));
                            if (PageResult.RawResponse.Headers.Count > 0)
                            {
                                foreach (KeyValuePair<string, string> kvp in PageResult.RawResponse.Headers)
                                {
                                    if (kvp.Key == "Content-Type" && kvp.Value.Contains("text/html")) resultisHTML = true;

                                }
                                if (!resultisHTML) { crawllinks.RemoveAt(linknum); continue; }
                            }
                        }
                        catch (System.AggregateException e) //Probably 404, page not found
                        {
                            Console.WriteLine(e.InnerException.Message);
                            continue;
                        }


                        if (resultisHTML)
                        {
                            try
                            {
                                HtmlDocument page = new HtmlDocument();
                                if (string.IsNullOrWhiteSpace(PageResult.Html.InnerHtml.ToString())) continue;
                                page.LoadHtml(PageResult.Html.InnerHtml.ToString());

                                // get first piece of data, the page title
                                if (PageResult.Html.CssSelect("title").First().InnerHtml.Length == 0) continue;
                                HtmlNode TitleNode = PageResult.Html.CssSelect("title").First();
                                /*if ((PageResult.Html.CssSelect(".navbar-brand").First().InnerText.ToString().Length < 5))
                                {
                                    // No title text in logo space or logo title is less descriptive, use page title element
                                    TitleNode = PageResult.Html.CssSelect("title").First();
                                }
                                else
                                {
                                    if (PageResult.Html.CssSelect(".navbar-brand").First().InnerText.ToString().Length < PageResult.Html.CssSelect("title").First().InnerText.Length)
                                        TitleNode = PageResult.Html.CssSelect("title").First();
                                    else TitleNode = PageResult.Html.CssSelect(".navbar-brand").First();
                                }*/

                                string PageTitle = TitleNode.InnerText;

                                // Scrape for links, determined by number of link Title Matches 
                                int MatchCount = 0;
                                for (int i = 0; i < TitleMatchList.Count(); i++) if (PageTitle.Contains(TitleMatchList[i])) MatchCount++;
                                if (MatchCount > 0) // It's at least named something relevant, scrape links to crawl later
                                {
                                    //System.Console.WriteLine("Title Match: " + PageTitle + " " + crawllinks.getURL(linknum).ToString()); System.Console.ReadLine();
                                    List<HtmlNode> linknodes = ExtractAllAHrefTags(page);
                                    foreach (HtmlNode linknode in linknodes)
                                    {

                                        string rawurlstr = linknode.Attributes["href"].Value;

                                        //Ensure the same link is not followed twice
                                        if (!rawurlstr.Contains("http") && PageResult.AbsoluteUrl.AbsolutePath.Split('/').Last().Contains(rawurlstr.TrimEnd('/').Split('/').Last()))
                                            continue;

                                        if (!Uri.IsWellFormedUriString(rawurlstr, UriKind.Absolute)
                                            && !rawurlstr.Contains(PageResult.AbsoluteUrl.ToString()))
                                        {
                                            rawurlstr = ToAbsoluteUrl(rawurlstr, PageResult);
                                        }


                                        //Superficial relevance search to see if anyt URLs are worth scraping or crawling deeper 
                                        int keywordmatches = 0;
                                        for (int i = 0; i < LinkKeywordList.Count(); i++)
                                        {
                                            if (linknode.InnerText.ToLower().Contains(LinkKeywordList.ElementAt(i).ToLower())) keywordmatches++;
                                        }

                                        //Takes seemingly relevant links, makes sure we haven't crawled them yet, then designates a more meaningful prioritization weight
                                        if (keywordmatches > 0 && Uri.IsWellFormedUriString(rawurlstr, UriKind.Absolute))
                                        {
                                            //Prevents relative link following recursion or following of duplicate relative links 
                                            Uri linkhref = new Uri(rawurlstr);
                                            bool isduplicate = false;
                                            for (int i = 0; i < crawllinks.Count(); i++)
                                            {
                                                if (crawllinks.At(i).URL == linkhref) isduplicate = true;
                                            }
                                            if (!isduplicate)
                                            {
                                                keywordmatches += getKeywordFrequency(EndLinkKeywordList, rawurlstr) * 3;
                                                if (rawurlstr.EndsWith("contact")) keywordmatches += 2; //Just common sense
                                                if (keywordmatches > 2) crawllinks.Add(new CrawlLink(linkhref, linknode.InnerText, keywordmatches));
                                                Console.Out.WriteLine("Link found: " + linkhref.ToString() + " \n");
                                            }
                                        }

                                    }

                                }
                            }



                            catch (Exception e)
                            {
                                Console.WriteLine("Failed to Parse HTML");
                                continue;
                            }
                        }
                        else { crawllinks.RemoveAt(linknum); continue; }
                        crawllinks.At(linknum).hasBeenCrawled = true;
                        if (crawllinks.Count() > 5) crawllinks.PrioritizeAndWeedOut(); //Refresh link list with most keyword relevant links first
                    } // Links have been scraped, now look for contact info - God speed little bot


                }
                //-----------------------Stop Crawling--------------------------------//

                crawllinks.PrioritizeAndWeedOut();

                Console.WriteLine("Done Crawling for Links, Totalling " + totalcrawled + " crawls. Scraping target prioritization:");
                for (int i = 0; i < crawllinks.Count(); i++)
                {
                    Console.WriteLine("Link: " + crawllinks.At(i).URL + " | Weight: " + crawllinks.At(i).Weight);
                }

                //-----------------------Start Scraping-------------------------------//
                for (int linknum = 0; linknum < crawllinks.Count(); linknum++)
                {
                    Console.Out.WriteLine("Link scraping started at: " + linknum + " " + crawllinks.getURL(linknum).ToString() + " \n");

                    if (!crawllinks.At(linknum).hasBeenScraped)
                    {
                        crawllinks.At(linknum).hasBeenScraped = true;
                        WebPage PageResult;
                        //go to the home page
                        try
                        {
                            bool resultisHTML = false;
                            PageResult = Browser.NavigateToPage(crawllinks.getURL(linknum));
                            foreach (KeyValuePair<string, string> kvp in PageResult.RawResponse.Headers)
                            {
                                if (kvp.Key == "Content-Type" && kvp.Value.Contains("text/html")) resultisHTML = true;

                            }
                            if (!resultisHTML) { crawllinks.RemoveAt(linknum); continue; }

                        }
                        catch (System.AggregateException e) //Probably 404, page not found
                        {
                            Console.WriteLine(e.InnerException.Message);
                            continue;
                        }
                        HtmlDocument page = new HtmlDocument();
                        if (string.IsNullOrEmpty(PageResult.Html.InnerHtml.ToString())) { crawllinks.RemoveAt(linknum); continue; }
                        page.LoadHtml(PageResult.Html.InnerHtml.ToString().Replace(">", ">\n"));

                        HtmlNodeCollection pageBody;
                        if (page.DocumentNode.SelectNodes("//body") != null) pageBody = page.DocumentNode.SelectNodes("//body");
                        else
                        {
                            crawllinks.RemoveAt(linknum);
                            continue;
                        }

                        IEnumerable<HtmlNode> ContentNodes;
                        if (pageBody.CssSelect("div").Count() > 0)
                            ContentNodes = pageBody.CssSelect("div");
                        else if (pageBody.CssSelect("td").Count() > 0)
                            ContentNodes = pageBody.CssSelect("td");
                        else { crawllinks.RemoveAt(linknum); continue; }
                        List<ContactInfo> Professors = new List<ContactInfo>();
                        ContactInfo professor = new ContactInfo();
                        try
                        { //Working shitty code was here, improvement in progress
                            for (int i = 0; i < ContentNodes.Count(); i++)
                            {
                                HtmlNode rootNode = ContentNodes.ElementAt(i);
                                IEnumerable<HtmlNode> childNodes = rootNode.ChildNodes;

                                int maxDepth = 0; //Tracks number of Nodes to branch down until there is relevant or useful contact info
                                int seqInt = 0; //Tracks number of times the following recursively generative loop has run since the last piece of contact info was found at current depth
                                for (int x = 0; x < childNodes.Count(); x++)
                                {
                                    HtmlNode childNode = childNodes.ElementAt(x);

                                    //If this node is a text element, see if it contains anything useful
                                    if ((maxDepth == 0 || maxDepth / seqInt == 3) && childNode.Name == "#text")
                                    {
                                        if (childNode.InnerHtml.Length > 0 && childNode.InnerHtml.CleanInnerHtmlAscii().Length > 0)
                                        {
                                            string innerText = "";
                                            string content = "";

                                            if (string.IsNullOrWhiteSpace(childNode.InnerHtml.CleanInnerHtmlAscii())) continue;
                                            else { innerText = childNode.InnerText; }

                                            string[] textstrings = innerText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                            foreach (string text in textstrings)
                                            {
                                                KeyValuePair<string, string> credential = scrapeContactInfo(text);
                                                switch(credential.Key)
                                                {
                                                    case "Name":
                                                        professor.Name = credential.Key;
                                                        break;
                                                    case "Address":
                                                        professor.Address = credential.Key;
                                                        break;
                                                    case "Phone":
                                                        professor.Address = credential.Key;
                                                        break;
                                                    case "Email":
                                                        professor.Address = credential.Key;
                                                        break;
                                                    case "Position":
                                                        professor.Address = credential.Key;
                                                        break;
                                                    case "Department":
                                                        professor.Address = credential.Key;
                                                        break;
                                                }

                                            }
                                        }
                                    }
                                    seqInt++;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\n\nFuck...");
                            Console.WriteLine(e.Message + "\n\nFuck that link anyway man, it's not worth it. Next link.\n\n");
                        }
                        try
                        {
                            //Professors.Add(professor);
                            string universalDepartmentName = "";
                            string universalDepartmentAddress = "";

                            if (Professors.Count == 0) Console.WriteLine("Page Scraped, No valid contacts. Moving on.");
                            else
                            {
                                Console.WriteLine("Page Scraped. List of valid contact entries: ");

                                if (Professors.ElementAt(0).Name == "Unknown" &&
                                    (Professors.ElementAt(0).Address != "Unknown" & Professors.ElementAt(0).Department != "Unknown"))
                                {
                                    //General Department info captured on page
                                    universalDepartmentAddress = Professors.ElementAt(0).Address;
                                    universalDepartmentName = Professors.ElementAt(0).Department;
                                    if (Professors.Count() > 1) Professors.RemoveAt(0);

                                }
                            }
                            if (Professors.Count() > 1)
                            {
                                foreach (ContactInfo prof in Professors)
                                {
                                    if (prof.Address == "Unknown") prof.Address = universalDepartmentAddress;
                                    if (prof.Department == "Unknown") prof.Department = universalDepartmentName;

                                    if (prof.CredCount() >= 3 && prof.Name != "Unknown")
                                    {
                                        prof.PrintToConsole();
                                        Console.WriteLine();

                                    }
                                    else
                                    {
                                        if (Professors.Count() == 1) Professors.Remove(prof);

                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\n\nException adding contacts... Big Deal right. Here's the message if you even give a shit:");
                            Console.WriteLine(e.Message + "\nBreaking to next link...\n\n");
                        }

                        Console.ReadLine();
                        //if (Professors.Count() > 0) break; //Contact Page found, Professor contacts scraped, work here is done; Go back to root links


                    }
                }
                //-----------------------Stop Scraping--------------------------------//
            }

            Console.WriteLine("done....\n\nLinks Scraped:\n");
            for (int i = 0; i < crawllinks.Count(); i++)
            {
                Console.WriteLine("Link title:" + crawllinks.At(i).Title + "; Href: " + crawllinks.getURL(i) + "; Weight: " + crawllinks.At(i).Weight);
            }
            System.Console.ReadLine();
        }
    }
}


