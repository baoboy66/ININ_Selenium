namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Linq;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    public class TC25308 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC25308()
        {
            this.TSNum = "2047";
            this.TCNum = "25308.6";
        }
        #endregion

        #region  Constants and Fields
        private const string _POLISH = "polski";
        private ChangeLanguage _changeLanguage;
        private Logon _logon;
        private WebDriverBaseWait _waiter;
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (this.Rm = ResourceManagerRuntime.AllocateResources(1, 1))
                {
                    try
                    {
                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            this.Drivers = WebDriverManager.Instance.AddDriver(this.Rm.Users.Count);
                            this._waiter = new WebDriverBaseWait();
                            this._waiter.IgnoreExceptionTypes(typeof (StaleElementReferenceException));
                            SetUserDefaultRole(this.Rm.Users);
                        }
                        #endregion

                        #region STEP 1: On the login page, click the language menu.
                        using (Trace.TestCase.scope("Step 1: On the login page, click the language menu."))
                        {
                            //Step 1 Verify: A menu containing available language options is loaded.  All of the appropriate language options are shown in the menu.
                            //Comment: See Notes to determine what should be seen in the menu.
                            Logon.CanSelectLanguage();
                            this._changeLanguage = ChangeLanguage.Get();

                            this.TraceTrue(() => { return this._changeLanguage.GetAvailableLanguages().Any(lang => string.Equals(lang, _POLISH, StringComparison.OrdinalIgnoreCase)); }, "Polish was not an option in the list of languages.");
                            this._changeLanguage.SelectLanguageDropDownItems.ClickItemWithText(_POLISH);
                        }
                        #endregion

                        #region STEP 2: Inside the menu, choose an available language other than English and click on it.
                        using (Trace.TestCase.scope("Step 2: Inside the menu, choose an available language other than English and click on it."))
                        {
                            //Step 2 Verify: The login page reloads and the text on the page is now displayed in the language that was selected.
                            //Comment: We basically want to verify here that the page is not being displayed in English.
                            this._logon = Logon.Get();
                            this.TraceTrue(() => this._waiter.Until(d => this._logon.SelectLanguageButton.Text.IndexOf(_POLISH, StringComparison.OrdinalIgnoreCase) != -1), "Language was not changed.");
                        }
                        #endregion

                        this.Passed = true;
                    }
                    catch (KnownScrException exception)
                    {
                        Graphics.TakeScreenshot();
                        this.TraceTrue(
                            false,
                            "Failed due to known SCR: " + exception.SCR + ". SCR Description: " + exception.Message,
                            exception.SCR);
                        this.Passed = false;
                        throw;
                    }
                    catch (Exception e)
                    {
                        Graphics.TakeScreenshot();
                        Trace.TestCase.exception(e);
                        this.Passed = false;
                        throw;
                    }
                    finally
                    {
                        // Perform an HTML Dump into i3trace.
                        Trace.TestCase.always("Html dump: \n{}", WebDriverManager.Instance.HtmlDump);

                        this.Attributes.Add(TestCaseAttribute.WebBrowser_Desktop, WebDriverManager.Instance.GetBrowserVersion());
                        TCDBResults.SendResultsToXml(this.TCNum, this.Passed, this.SCRs, this.Stopwatch.Elapsed.TotalSeconds, this.Attributes);
                        TCDBResults.SubmitResult(this.TCNum, this.Passed, this.SCRs, attributes: this.Attributes);
                    }
                }
            }
        }

        [ConnectFact]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "false")]
        [Trait("Patch", "true")]
        public void Test25308_ChangeLanguageOnTheLoginPage()
        {
            try
            {
                this.Run();
            }
            catch (Exception e)
            {
                if (this.Passed)
                {
                    Trace.TestCase.exception(e, "Cleanup threw an exception. Make sure you are using ICWS APIs to do cleanup.");
                }
                else
                {
                    Trace.TestCase.exception(e, "The test case failed. Informing XUnit.");
                    throw;
                }
            }
        }
        #endregion
    }
}