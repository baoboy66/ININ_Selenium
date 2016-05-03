namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Globalization;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.Configuration;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;
    using StaleElementReferenceException = OpenQA.Selenium.StaleElementReferenceException;

    /// <summary>
    ///     TC33871 - MaximumHttpSessions Server Parameter
    /// </summary>
    public class TC33871 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC33871()
        {
            this.TSNum = "2047";
            this.TCNum = "33871.2";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     Expected error message when Session Manager is not accepting new HTTP connections.
        /// </summary>
        private const string _ERROR_MSG = "This Session Manager is not currently accepting connections.";

        /// <summary>
        ///     ID of Server Parameter for max HTTP sessions
        /// </summary>
        private const string _SERVER_PARAMETER_HTTP_SESSIONS = "MaximumHttpSessions";

        /// <summary>
        ///     Object used to wait for specific conditions on various objects
        /// </summary>
        private WebDriverBaseWait _waiter;
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (this.Rm = ResourceManagerRuntime.AllocateResources(2, 2))
                {
                    try
                    {
                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            // Add drivers
                            this.Drivers = WebDriverManager.Instance.AddDriver(this.Rm.Users.Count);

                            this._waiter = new WebDriverBaseWait();
                            this._waiter.IgnoreExceptionTypes(typeof (StaleElementReferenceException));

                            // Set roles
                            SetUserDefaultRole(this.Rm.Users);

                            // Set the server parameter to only allow 2 HTTP sessions; 1 for the tests connection and 1 for user1
                            this.SetServerParameter(_SERVER_PARAMETER_HTTP_SESSIONS, (Core.SessionCount + 1).ToString(CultureInfo.InvariantCulture));

                            // Log User1 on
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[0]);
                            this.UserLogonAndStatusSet(this.Rm.Users[0], this.Rm.Stations[0], this.Drivers[0]);

                            this.TraceTrue(Util.IsLoggedIn(), "Failed to log user 1 on.");
                        }
                        #endregion

                        #region STEP 1: Navigate to the Interaction Connect logon page and attempt to log in User2.
                        using (Trace.TestCase.scope("Step 1: Navigate to the Interaction Connect logon page and attempt to log in User2."))
                        {
                            //Step 1 Verify: An error message is seen indicating that the session manager is currently not accepting connections.  User1\'s logon session is not affected in any way.

                            // Attempt to log user2 on
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[1]);
                            Logon.DoLogon(this.Rm.Users[1], this.UserPassword, GlobalConfiguration.Instance.IcServerConfiguration.ServerName, StationType.Workstation.ToString(), this.Rm.Stations[1], false, false, "", false, false /* shouldSetStation should be false since it should show the error message before the station set step. */);
                            this.TraceTrue(() => this._waiter.Until(d => Logon.Get().IcAuthErrorMessageLabel.Text.Equals(_ERROR_MSG, StringComparison.OrdinalIgnoreCase)), "Error message was wrong or was not displayed.");
                        }
                        #endregion

                        #region STEP 2: Delete the MaximumHttpSessions server parameter from the IC server the user is connected to.
                        using (Trace.TestCase.scope("Step 2: Delete the MaximumHttpSessions server parameter from the IC server the user is connected to."))
                        {
                            //Step 2 Verify: The server parameter is successfully deleted.

                            // Reset the server parameter for max HTTP sessions.
                            // This would ideally be a DELETE, but that ICWS API has not been implemented.
                            this.SetServerParameter(_SERVER_PARAMETER_HTTP_SESSIONS, "");
                        }
                        #endregion

                        #region STEP 3: Attempt to log User2 into Interaction Connect again.
                        using (Trace.TestCase.scope("Step 3: Attempt to log User2 into Interaction Connect again."))
                        {
                            //Step 3 Verify: Login is successful.
                            Logon.GoToLogon();
                            Logon.DoLogon(this.Rm.Users[1], this.UserPassword, GlobalConfiguration.Instance.IcServerConfiguration.ServerName, StationType.Workstation.ToString(), this.Rm.Stations[1]);
                            this.TraceTrue(Util.IsLoggedIn(), "Failed to log user2 on.");
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
                            // Reset the server parameter for max HTTP sessions.
                            // This would ideally be a DELETE, but that ICWS API has not been implemented.
                            this.SetServerParameter(_SERVER_PARAMETER_HTTP_SESSIONS, "");
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
        public void Test33871_MaximumHttpSessionsServerParameter()
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