using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System.Threading;
using OpenQA.Selenium.Interactions;

namespace asuClassRegistration
{
    class Program
    {
        public static string uName = Properties.Settings.Default.username;
        public static string pword = Properties.Settings.Default.password;
        public static List<FirefoxDriver> drivs = new List<FirefoxDriver>();
        public static FirefoxDriver driver;
        public static List<string> dropClasses = new List<string>();
        public const string asuUrl = "https://webapp4.asu.edu/myasu/";
        public const string swapUrl = "https://www.asu.edu/go/swapclass/?STRM=2211&ACAD_CAREER=GRAD";
        public const string schedUrl = "https://webapp4.asu.edu/myasu/student/schedule?term=2211";
        public static string path = AppContext.BaseDirectory + "\\failureScreens\\";
        public static string path2 = AppContext.BaseDirectory + "\\SuccessScreen\\";
        public static int specialWait = 0;

        static void Main(string[] args)
        {
            try
            {
                System.IO.Directory.Delete(path);
                System.IO.Directory.Delete(path2);
            }
            catch (Exception w)
            { }
            var startTime = DateTime.Now.ToString();
            while (true)
            {
                startTime = DateTime.Now.ToString();
                try
                {
                    List<string> classes = Properties.Settings.Default.classNumberToPick.Split(',').ToList();
                    dropClasses = Properties.Settings.Default.CourseNoToDrop.Split(',').ToList();
                    string subjectNo = search(Properties.Settings.Default.subjectCode, classes, Properties.Settings.Default.location);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Execution started at : " + startTime);
                    Console.WriteLine("Failed at: " + DateTime.Now.ToString());
                    Console.Beep();
                    Thread.Sleep(10 * 1000);
                    string dateToday = "_" + DateTime.Now.ToString("yyyy-MM-dd") + "_" + DateTime.Now.ToString("HH-mm-ss");
                    try
                    {
                        System.IO.Directory.CreateDirectory(path);
                        string screenFile = System.IO.Path.Combine(path, "Failed" + dateToday + ".png");
                        Capture(screenFile);
                        
                    }
                    catch (Exception w)
                    {
                        
                    }
                    try
                    {
                        drivs.Clear();
                        driver.Close();
                    }
                    catch (Exception q) { }
                    Console.WriteLine(e.Message);
                }
            }
            Console.ReadLine();
        }

        public static void init()
        {
            var fo = new FirefoxOptions();
            fo.AddArguments("no-sandbox");
            //fo.AddArguments("--headless");
            driver = new FirefoxDriver(fo);
            drivs.Add(driver);
        }

        public static void Capture(String filePathName)
        {
            Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
            screenshot.SaveAsFile(filePathName, ScreenshotImageFormat.Png);
            Console.WriteLine("Screenshot saved as : " + filePathName);
        }

        public static void login(string username, string password)
        {
            if (driver.Title.Trim().Equals("Login", StringComparison.InvariantCultureIgnoreCase))
            {
                IWebElement userName = driver.FindElementById("username");
                IWebElement passWord = driver.FindElementById("password");
                userName.SendKeys(username);
                passWord.SendKeys(password);
                driver.FindElement(By.Name("submit")).Click();
            }

        }

        public static void nav(string url)
        {
            driver.Navigate().GoToUrl(url);
        }

        public static IWebElement waitForElement(By by, bool clickacble = false)
        {
            int attempt = 0;
            IWebElement element = null;
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
            while (element == null && attempt < 7) 
            {
                try
                {
                    element = driver.FindElement(by);
                    element = wait.Until(ExpectedConditions.ElementIsVisible(by));
                    if (element.Displayed)
                    {
                        if(!clickacble)
                        {
                            return element;
                        }
                        else if (clickacble)
                        {

                            wait.Until(ExpectedConditions.ElementToBeClickable(by));
                        }
                    }
                    else throw new Exception();
                }
                catch(Exception e)
                {
                    attempt++;
                    Thread.Sleep(attempt * 1000);
                }
            }
            
            Actions actions = new Actions(driver);
            actions.MoveToElement(element);
            actions.Perform();
            return element;
        }

        public static string search(string subjectCode, List<string> classNos, string location)
        {
            string url = "";
            string message = "";
            //int noOfClass = 0;
            
            for (int i = 0; i < classNos.Count; i++)
            {
                init();
            }
            int j = 0;
            for (int i = 0; i < classNos.Count;i++)
            {
                bool isOpen = false;
                if (dropClasses.Count < 1)
                {
                    Console.WriteLine("Classes to drop has exhausted");
                    return message;
                }
                driver = drivs[i];
                url = "https://" + "webapp4.asu.edu/catalog/classlist?t=2211&s=" + subjectCode.ToUpper() + "&hon=F&promod=F&c=" + location + "&e=all&page=1&k=" + classNos[i].Trim();
                nav(url);
                IWebElement classAvailable = waitForElement(By.XPath("//*[@class='availableSeatsColumnValue']"));
                try
                {
                    isOpen = classAvailable.FindElement(By.XPath(".//i[@title='seats available']")).Displayed;
                }
                catch (Exception)
                { }
                //noOfClass = int.Parse(classAvailable.Text.Split('o')[0].Trim());
                if (isOpen)
                {
                    Console.Beep();
                    IWebElement classNumber = waitForElement(By.XPath("//*[@class='classNbrColumnValue']/a"));
                    string classno = classNumber.Text.Trim();
                    int attempt = 0;
                    do
                    {
                        specialWait = 1000 * attempt;
                        message = swap(dropClasses[j], classno);
                        attempt++;
                    } while (message.Equals("Failed") && attempt < 4);
                    Console.WriteLine(message);
                    if(message.Contains("has been added to your schedule"))
                    {
                        message = "Class: " +classno+" - "+ message+ "\n";
                        drivs[i].Close();
                        drivs.Remove(drivs[i]);
                        classNos.Remove(classno);
                        dropClasses.Remove(dropClasses[j]);
                        j++;
                    }
                }
                else
                {
                    Console.Write("..");
                }
                if(classNos.Count <=0)
                {
                    return message;
                }
                if (i >= classNos.Count - 1)
                {
                    i = -1;
                }
            }
            return "";

        }

        public static string swap(string subjectNumber1, string subjectNumber2)
        {
            nav(swapUrl);
            login(uName, pword);
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
            try
            {
                string dateToday = "_" + DateTime.Now.ToString("yyyy-MM-dd") + "_" + DateTime.Now.ToString("HH-mm-ss");
                //driver = (FirefoxDriver)driver.SwitchTo().Frame(targetElement);
                IWebElement newSub = waitForElement(By.Id("DERIVED_REGFRM1_CLASS_NBR"),true);
                newSub.SendKeys(subjectNumber2);
                IWebElement sel = waitForElement(By.XPath(".//*[@id='DERIVED_REGFRM1_DESCR50$4$']"));
                SelectElement subSelect = new SelectElement(sel);
                subSelect.SelectByValue(subjectNumber1);
                Thread.Sleep(specialWait);
                IWebElement next1 = waitForElement(By.Id("SSR_SWAP_FL_WRK_SSR_PB_SRCH"), true);
                next1.Click();
                IWebElement next2 = waitForElement(By.Id("PT_NEXT"), true);
                next2.Click();
                IWebElement reviewSwap = waitForElement(By.Id("DERIVED_SSR_FL_SSR_SELECT"), true);
                reviewSwap.Click();
                IWebElement finishSwap = waitForElement(By.Id("SSR_ENRL_FL_WRK_SUBMIT_PB"), true);
                
                finishSwap.Click();
                IWebElement confirm = waitForElement(By.Id("#ICYes"), true);
                confirm.Click();
                IWebElement message = waitForElement(By.Id("win14divDERIVED_REGFRM1_SS_MESSAGE_LONG$0"));
                try
                {
                    System.IO.Directory.CreateDirectory(path2);
                    string screenFile = System.IO.Path.Combine(path2, "Sucess_2_" + dateToday + ".png");
                    Capture(screenFile);
                }
                catch (Exception w)
                { }
                nav(schedUrl);
                try
                {
                    System.IO.Directory.CreateDirectory(path2);
                    string screenFile = System.IO.Path.Combine(path2, "Sucess_1_" + dateToday + ".png");
                    Capture(screenFile);
                }
                catch (Exception w)
                { }
                return message.Text;
            }
            catch (Exception e)
            {
                return "Failed";
            }
            
        }

    }
}
