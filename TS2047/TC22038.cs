namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;
    using StaleElementReferenceException = OpenQA.Selenium.StaleElementReferenceException;

    /// <summary>
    ///     Multiple Login Sessions on same browser
    /// </summary>
    public class TC22038 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC22038()
        {
            this.TSNum = "2047";
            this.TCNum = "22038.6";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     Expected message when connection to the IC server is lost
        /// </summary>
        private const string _CONNECTION_LOST_MESSAGE = "Your connection to the server was lost, and the application is unable to reconnect automatically.";
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (this.Rm = ResourceManagerRuntime.AllocateResources(1, 2))
                {
                    try
                    {
                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            // make sure the user is added to the right role.
                            SetUserDefaultRole(this.Rm.Users);

                            // get a driver for the test.
                            this.Drivers = WebDriverManager.Instance.AddDriver(3);
                        }
                        #endregion

                        #region STEP 1: Open a web browser and navigate to the webclient webpage for your IC server.
                        using (Trace.TestCase.scope("Step 1: Open a web browser and navigate to the webclient webpage for your IC server. "))
                        {
                            // Step 1 Verify: Webclient homepage opens without error.
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[0]);
                            Logon.GoToLogon();
                            this.TraceTrue(Logon.IsAtServerForm(), "Step 1 - Couldn't get to the servre form.");
                        }
                        #endregion

                        #region STEP 2: Logon to the ic server with a user.
                        using (Trace.TestCase.scope("Step 2: Logon to the ic server with a user. "))
                        {
                            // Step 2 Verify: User logs in to IC server through webclient.
                            this.TraceTrue(this.UserLogonAndStatusSet(this.Rm.Users[0], this.Rm.Stations[0], this.Drivers[0], shouldPersist: true), "Step 2 - Did not Logon successfully");
                            this.TraceTrue(VerifyDriverIsLoggedIntoClient(this.Drivers[0]), "Step 2 - Default views not displayed. Client does not appear to be connected.");
                        }
                        #endregion

                        #region STEP 3: In the web browser, duplicate the webclient tab to create a duplicate session.
                        using (Trace.TestCase.scope("Step 3: In the web browser, duplicate the webclient tab to create a duplicate session. "))
                        {
                            // Step 3 Verify: Tab is duplicated and the user is asked to Logon again.
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[1]);
                            Logon.GoToLogon();
                            this.TraceTrue(Logon.IsAtServerForm(), "Step 3 - Couldn't get to the server page.");
                        }
                        #endregion

                        #region STEP 4: Log the user in a second time using the same computer, user and station.
                        using (Trace.TestCase.scope("Step 4: Log the user in a second time using the same computer, user and station."))
                        {
                            // Step 4 Verify: The user is logged in and both sessions are active.
                            this.TraceTrue(Logon.DoLogon(this.Rm.Users[0], this.UserPassword, this.IcServer, _DEFAULT_STATION_TYPE, this.Rm.Stations[0], true), "did not Logon successfully");
                            this.TraceTrue(VerifyDriverIsLoggedIntoClient(this.Drivers[0]), "Step 4 - Default views not displayed. Client does not appear to be connected.");
                        }
                        #endregion

                        #region STEP 5: Close the duplicated tab.
                        using (Trace.TestCase.scope("Step 5: Close the duplicated tab. "))
                        {
                            // Step 5 Verify: Original session is still open and does not close. 
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[1]);
                            WebDriverManager.Instance.CurrentDriver.Close();

                            this.TraceTrue(VerifyDriverIsLoggedIntoClient(this.Drivers[0]), "Step 5 - Default views not displayed. Client does not appear to be connected.");
                        }
                        #endregion

                        #region STEP 6: In the web browser, duplicate the webclient tab to create a duplicate session.
                        using (Trace.TestCase.scope("Step 6: In the web browser, duplicate the webclient tab to create a duplicate session. "))
                        {
                            // Step 6 Verify: Tab is duplicated and the user is asked to Logon again.
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[2]);
                            Logon.GoToLogon();
                            this.TraceTrue(Logon.IsAtAnyLogonForm(), "Step 6 - Couldn't get to the Logon page.");
                        }
                        #endregion

                        #region STEP 7: Log the user in a second time using the same computer and user, but a different station.
                        using (Trace.TestCase.scope("Step 7: Log the user in a second time using the same computer and user, but a different station."))
                        {
                            //Step 7 Verify: The user is logged in and the original session is disconnected.
                            this.TraceTrue(Logon.DoLogon(this.Rm.Users[0], this.UserPassword, this.IcServer, _DEFAULT_STATION_TYPE, this.Rm.Stations[1], true), "Step 7 - Did not Logon successfully");
                            this.TraceTrue(VerifyDriverIsLoggedIntoClient(this.Drivers[2]), "Step 7 - Default views not displayed. Client does not appear to be connected.");

                            this.TraceTrue(VerifyDriverIsNotLoggedIntoClient(this.Drivers[0]), "User still logged into both stations at the same time");
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
        public void Test22038_MultipleLogonSessionsOnSameBrowser()
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

        #region Methods
        /// <summary>
        ///     Verify that the given web driver appears to be logged in and have a connected session
        /// </summary>
        /// <param name="webDriver">The driver to check</param>
        /// <returns>True if the driver is logged onto the client, false otherwise</returns>
        private static bool VerifyDriverIsLoggedIntoClient(string webDriver)
        {
            var waiter = new WebDriverBaseWait();
            waiter.IgnoreExceptionTypes(typeof (StaleElementReferenceException));

            WebDriverManager.Instance.SwitchBrowser(webDriver);

            return waiter.Until(d => Util.IsLoggedIn(0));
        }

        /// <summary>
        ///     Verify that the given web driver appears to be no longer have a connected session by checking to make sure the
        ///     expected connection lost message is displayed.
        /// </summary>
        /// <param name="webDriver">The driver to check</param>
        /// <returns>True if the driver is not logged onto the client, false otherwise</returns>
        private static bool VerifyDriverIsNotLoggedIntoClient(string webDriver)
        {
            WebDriverManager.Instance.SwitchBrowser(webDriver);

            var logoff = Logoff.Get();

            return logoff.MessageElement.WaitUntil(WaitUntilType.Displayed) &&
                   string.Equals(logoff.MessageElement.Text.Trim(), _CONNECTION_LOST_MESSAGE, StringComparison.InvariantCultureIgnoreCase);
        }
        #endregion
    }
}