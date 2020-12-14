using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System.Threading;

namespace asuClassRegistration
{
    class Program
    {

        public static FirefoxDriver driver;
        public const string asuUrl = "https://webapp4.asu.edu/myasu/";
        public const string swapUrl = "https://www.asu.edu/go/swapclass/?STRM=2211&ACAD_CAREER=GRAD";
        public static string path = AppContext.BaseDirectory + "\\failureScreens\\";


        static void Main(string[] args)
        {
            var startTime = DateTime.Now.ToString();
            while (true)
            {
                try
                {
                    init();
                    if (driver != null)
                    {
                        List<string> classes = Properties.Settings.Default.classNumberToPick.Split(',').ToList();
                        foreach(String clas in classes)
                        {
                            string subjectNo = search(Properties.Settings.Default.subjectCode, clas , Properties.Settings.Default.location);
                            nav(asuUrl);
                            String uName = Properties.Settings.Default.username;
                            string pword = Properties.Settings.Default.password;
                            login(uName, pword);
                            string message = swap(Properties.Settings.Default.CourseNoToDrop, subjectNo);
                            classes.Remove(clas);
                            Console.WriteLine(message + "\n\npress key to continue..");
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Execution started at : " + startTime);
                    Console.WriteLine("Failed at: " + DateTime.Now.ToString());
                    Thread.Sleep(10 * 1000);
                    string dateToday = "_" + DateTime.Now.ToString("yyyy-MM-dd") + "_" + DateTime.Now.ToString("HH-mm-ss");
                    try
                    {
                        System.IO.Directory.CreateDirectory(path);
                        string screenFile = System.IO.Path.Combine(path, "Failed" + dateToday + ".png");
                        Capture(screenFile);
                    }
                    catch (Exception w)
                    { }
                    driver.Close();
                    Console.WriteLine(e.Message);
                }
            }
            Console.ReadLine();
        }

        public static void init()
        {
            var fo = new FirefoxOptions();
            fo.AddArgument("no-sandbox");
            driver = new FirefoxDriver(fo);

        }

        public static void Capture(String filePathName)
        {
            Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
            screenshot.SaveAsFile(filePathName, ScreenshotImageFormat.Png);
            Console.WriteLine("Screenshot saved as : " + filePathName);
        }

        public static void login(string username, string password)
        {
            IWebElement userName = driver.FindElementById("username");
            IWebElement passWord = driver.FindElementById("password");
            userName.SendKeys(username);
            passWord.SendKeys(password);
            driver.FindElement(By.Name("submit")).Click();

        }

        public static void nav(string url)
        {
            driver.Navigate().GoToUrl(url);
        }

        public static IWebElement waitForElement(By by, bool clickacble = false)
        {
            int attempt = 0;
            IWebElement element = null;
            while (element == null && attempt<10)
            {
                try
                {
                    element = driver.FindElement(by);
                    if (element.Displayed)
                    {
                        return element;
                    }
                    else throw new Exception();
                }
                catch(Exception e)
                {
                    attempt++;
                    Thread.Sleep(attempt * 1000);
                }
            }
            if (clickacble)
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(25));
                wait.Until(ExpectedConditions.ElementToBeClickable(by));
            }
            return element;
        }

        public static string search(string subjectCode, string subjNo, string location)
        {
            string url = "http://"+"webapp4.asu.edu/catalog/classlist?t=2211&s="+subjectCode.ToUpper()+"&n="+subjNo+"&hon=F&promod=F&c="+location+"&e=all&page=1";
            int noOfClass = 0;
            bool isOpen = false;
            while(!(noOfClass>0 && isOpen))
            {
                nav(url);
                IWebElement classAvailable = waitForElement(By.XPath("//*[@class='availableSeatsColumnValue']"));
                try
                {
                    isOpen = classAvailable.FindElement(By.XPath(".//i[@title='seats available']")).Displayed;
                }
                catch(Exception)
                {}
                noOfClass = int.Parse(classAvailable.Text.Split('o')[0].Trim());
                if (noOfClass > 0 && isOpen)
                {
                    IWebElement classNumber = waitForElement(By.XPath("//*[@class='classNbrColumnValue']/a"));
                    string classno = classNumber.Text.Trim();
                    return classno;
                }
                else
                {
                    Console.Write("..");
                }
            }
            return "";

        }

        public static string swap(string subjectNumber1, string subjectNumber2)
        {
            nav(swapUrl);
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(25));
            //IWebElement targetElement = null;
            //int attempt = 0;
            //while (/*targetElement == null && */attempt<10)
            //{
            //    //try
            //    //{
            //    //    bool condition = false;
            //    //    while (condition != true)
            //    //    {
            //    //        driver = (FirefoxDriver)driver.SwitchTo().Frame(0);
            //    //        var frames = driver.FindElements(By.XPath("//frame"));
            //    //        foreach (var frame in frames)
            //    //        {
            //    //            if (((IWebElement)frame).GetAttribute("name").Equals("TargetContent"))
            //    //            {
            //    //                targetElement = frame;
            //    //                Console.WriteLine("\nfound frame in attempt no: " + (attempt+1));
            //    //                condition = true;
            //    //                break;
            //    //            }
            //    //        }
            //    //    }

            //    //}
            //    //catch (Exception e)
            //    //{
            //    //    attempt++;
            //    //    driver.SwitchTo().ParentFrame();
            //    //    Console.WriteLine("\nattempt: " + attempt);
            //    //    Thread.Sleep(attempt * 1000);
            //    //}
            //}
            try
            {
                //driver = (FirefoxDriver)driver.SwitchTo().Frame(targetElement);
                IWebElement sel = waitForElement(By.XPath(".//*[@id='DERIVED_REGFRM1_DESCR50$4$']"));
                SelectElement subSelect = new SelectElement(sel);
                subSelect.SelectByValue(subjectNumber1);
                IWebElement newSub = driver.FindElementById("DERIVED_REGFRM1_CLASS_NBR");
                newSub.SendKeys(subjectNumber2);
                driver.FindElementById("SSR_SWAP_FL_WRK_SSR_PB_SRCH").Click();
                IWebElement next = waitForElement(By.Id("PT_NEXT"), true);
                next.Click();
                IWebElement reviewSwap = waitForElement(By.Id("DERIVED_SSR_FL_SSR_SELECT"), true);
                reviewSwap.Click();
                IWebElement finishSwap = waitForElement(By.Id("SSR_ENRL_FL_WRK_SUBMIT_PB"), true);
                finishSwap.Click();
                IWebElement confirm = waitForElement(By.Id("#ICYes"), true);
                confirm.Click();
                IWebElement message = waitForElement(By.Id("win14divDERIVED_REGFRM1_SS_MESSAGE_LONG$0"));
                return message.Text;
            }
            catch (Exception e)
            {
                return "Failed";
            }
            
        }

    }
}
