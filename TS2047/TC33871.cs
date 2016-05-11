namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Globalization;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Client.Queues.MyInteractions;
    using ININ.Testing.Automation.Lib.Common.LogonForm;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC33871 - MaximumHttpSessions Server Parameter
    /// </summary>
    public class TC33871 : ClientTestCase
    {
        /// <summary>
        ///     Expected error message when Session Manager is not accepting new HTTP connections.
        /// </summary>
        private const string _ERROR_MSG = "This Session Manager is not currently accepting connections.";

        /// <summary>
        ///     ID of Server Parameter for max HTTP sessions
        /// </summary>
        private const string _SERVER_PARAMETER_HTTP_SESSIONS = "MaximumHttpSessions";

        /// <summary>
        ///     Logon page object
        /// </summary>
        private LogonForm _logon;

        public TC33871()
        {
            TSNum = "2047";
            TCNum = "33871.2";
        }

        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (Rm = ResourceManagerRuntime.AllocateResources(2, 2))
                {
                    try
                    {
                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            TraceTrue(() =>
                            {
                                // Log User1 on
                                // Set roles
                                foreach (var user in Rm.Users)
                                {
                                    Users.SetRole(user, _DEFAULT_ROLE);
                                    Status.Set(user, "Available");
                                }
                                // Add drivers
                                Drivers = WebDriverManager.Instance.AddDriver(Rm.Users.Count);
                                WebDriverManager.Instance.SwitchBrowser(Drivers[0]);
                                WaitFor(() => UserLogon(Rm.Users[0], Rm.Stations[0]));

                                // Set the server parameter to only allow 2 HTTP sessions; 1 for the tests connection and 1 for user1
                                SetServerParameter(_SERVER_PARAMETER_HTTP_SESSIONS, (Core.SessionCount + 1).ToString(CultureInfo.InvariantCulture));
                                return true;
                            }, "Pre run setup failed.");
                        }
                        #endregion

                        #region STEP 1: Navigate to the Interaction Connect logon page and attempt to log in User2.
                        using (Trace.TestCase.scope("Step 1: Navigate to the Interaction Connect logon page and attempt to log in User2."))
                        {
                            //Step 1 Verify: An error message is seen indicating that the session manager is currently not accepting connections.  User1\'s logon session is not affected in any way.
                            TraceTrue(() =>
                            {
                                WebDriverManager.Instance.SwitchBrowser(Drivers[1]);
                                _logon = new LogonForm();
                                _logon.GoTo();
                                var serverForm = new ServerForm();
                                if (WaitFor(() => serverForm.Displayed))
                                {
                                    serverForm.Set(IcServer).Submit();
                                }

                                // Set and submit auth form
                                var authForm = new AuthForm();
                                if (WaitFor(() => authForm.Displayed))
                                {
                                    authForm.Set(Rm.Users[1], UserPassword).LogOn();
                                }
                                return WaitFor(() => !string.IsNullOrWhiteSpace(authForm.Error) && authForm.Error.Equals(_ERROR_MSG));
                            }, "The error message was not presented.");
                        }
                        #endregion

                        #region STEP 2: Delete the MaximumHttpSessions server parameter from the IC server the user is connected to.
                        using (Trace.TestCase.scope("Step 2: Delete the MaximumHttpSessions server parameter from the IC server the user is connected to."))
                        {
                            //Step 2 Verify: The server parameter is successfully deleted.

                            // Reset the server parameter for max HTTP sessions.
                            // This would ideally be a DELETE, but that ICWS API has not been implemented.
                            SetServerParameter(_SERVER_PARAMETER_HTTP_SESSIONS, "");
                        }
                        #endregion

                        #region STEP 3: Attempt to log User2 into Interaction Connect again.
                        using (Trace.TestCase.scope("Step 3: Attempt to log User2 into Interaction Connect again."))
                        {
                            //Step 3 Verify: Login is successful.
                            TraceTrue(() =>
                            {
                                _logon.GoTo();

                                // Set and submit auth form
                                var authForm = new AuthForm();
                                if (WaitFor(() => authForm.Displayed))
                                {
                                    authForm.Set(Rm.Users[1], UserPassword).LogOn();
                                }
                                // Set and submit station form
                                var station = new StationForm();
                                if (WaitFor(() => station.Displayed))
                                {
                                    station.Set(Rm.Stations[1]).Submit();
                                }
                                var interaction = new MyInteractionsView();
                                return WaitFor(() => interaction.Displayed);
                            }, "Step 3 - Failed to log user2 on.");
                        }
                        #endregion

                        Passed = true;
                    }
                    catch (KnownScrException exception)
                    {
                        Graphics.TakeScreenshot();
                        TraceTrue(
                            false,
                            "Failed due to known SCR: " + exception.SCR + ". SCR Description: " + exception.Message,
                            exception.SCR);
                        Passed = false;
                        throw;
                    }
                    catch (Exception e)
                    {
                        Graphics.TakeScreenshot();
                        Trace.TestCase.exception(e);
                        Passed = false;
                        throw;
                    }
                    finally
                    {
                        // Perform an HTML Dump into i3trace.
                        Trace.TestCase.always("Html dump: \n{}", WebDriverManager.Instance.HtmlDump);

                        Attributes.Add(TestCaseAttribute.WebBrowser_Desktop, WebDriverManager.Instance.GetBrowserVersion());
                        TCDBResults.SendResultsToXml(TCNum, Passed, SCRs, Stopwatch.Elapsed.TotalSeconds, Attributes);
                        TCDBResults.SubmitResult(TCNum, Passed, SCRs, attributes: Attributes);

                        #region Cleanup
                        using (Trace.TestCase.scope("Post Run Clean Up"))
                        {
                            // Reset the server parameter for max HTTP sessions.
                            // This would ideally be a DELETE, but that ICWS API has not been implemented.
                            SetServerParameter(_SERVER_PARAMETER_HTTP_SESSIONS, "");
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
                Run();
            }
            catch (Exception e)
            {
                if (Passed)
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
    }
}