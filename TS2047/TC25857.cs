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
    ///     TC25857 - Manually enter server name on Server Selection page
    /// </summary>
    public class TC25857 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC25857()
        {
            this.TSNum = "2047";
            this.TCNum = "25857.2";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     Error message to expect for an invalid IC Server
        /// </summary>
        private const string _SERVER_ERROR_MESSAGE = "There was a problem communicating with the server. Check your server selection again, and contact your administrator if problems persist.";
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
                            // Make sure that SSO is disabled
                            SetLoginAuthentication(null);

                            // make sure the user is added to the right role.
                            SetUserDefaultRole(this.Rm.Users);

                            // get a driver for the test.
                            this.Drivers = WebDriverManager.Instance.AddDriver(1);

                            // navigate to the Logon page
                            Logon.GoToLogon();
                        }
                        #endregion

                        #region STEP 1: Manually enter an invalid IC server name on the Server Selection page and select Choose Server.
                        using (Trace.TestCase.scope("Step 1: Manually enter an invalid IC server name on the Server Selection page and select Choose Server."))
                        {
                            // Step 1 Verify: An error message displays, stating \' There was a problem communicating with the server. Check your server selection again, and contact your administrator if problems persist.\'
                            Logon.Get().SetServerForm("bogus");

                            var waiter = new WebDriverBaseWait();
                            waiter.IgnoreExceptionTypes(typeof (StaleElementReferenceException));

                            this.TraceTrue(() => waiter.Until(d => string.Equals(Logon.Get().ServerErrorMessageLabel.Text.Trim(), _SERVER_ERROR_MESSAGE, StringComparison.InvariantCultureIgnoreCase)),
                                "The invalid server message was not displayed.");
                        }
                        #endregion

                        #region STEP 2: Now enter a valid IC server name on the Server Selection page and select Choose Server.
                        using (Trace.TestCase.scope("Step 2: Now enter a valid IC server name on the Server Selection page and select Choose Server."))
                        {
                            // Step 2 Verify: The user advances to the Logon screen.
                            Logon.Get().SetServerForm(this.IcServer);

                            Logon.Get().ChooseServerButton.WaitUntil(WaitUntilType.CanInteract);

                            this.TraceTrue(Logon.Get().ServerErrorMessageLabel.WaitUntil(WaitUntilType.Displayed, false),
                                "The invalid server message was not hidden as expected.");

                            this.TraceTrue(Logon.IsAtIcAuthForm(),
                                "The IC auth form was not displayed as expected.");
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

                        #region Cleanup
                        using (Trace.TestCase.scope("Post Run Clean Up"))
                        {
                            // Nothing to cleanup.
                        }
                        #endregion
                    }
                }
            }
        }

        [ConnectFact]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "hazard")]
        public void Test25857_ManuallyEnterServerNameOnServerSelectionPage()
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