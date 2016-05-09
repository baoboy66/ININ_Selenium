namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.ICWS.Configuration.System;
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
    ///     TC36657 - Persistent logon information
    /// </summary>
    public class TC36657 : ClientTestCase
    {
        private LogonForm _logon;
        private AuthForm _authForm;
        private MyInteractionsView _interactionsView;

        public TC36657()
        {
            TSNum = "2047";
            TCNum = "36657.2";
        }

        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (Rm = ResourceManagerRuntime.AllocateResources(1, 2))
                {
                    try
                    {
                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            TraceTrue(() =>
                            {
                                // make sure the user is added to the right role.
                                Users.SetRole(Rm.Users[0], _DEFAULT_ROLE);
                                Status.Set(Rm.Users[0], "Available");

                                // get drivers for the test.
                                Drivers = WebDriverManager.Instance.AddDriver(1);
                                return true;
                            }, "Pre run setup failed.");
                        }
                        #endregion

                        #region STEP 1: Log on with appropriate server name, user ID, password and station select to persist each selection.
                        using (Trace.TestCase.scope("Step 1: Log on with appropriate server name, user ID, password and station select to persist each selection."))
                        {
                            //Step 1 Verify: User is logged on to Interaction Connect.
                            TraceTrue(() =>
                            {
                                WebDriverManager.Instance.SwitchBrowser(Drivers[0]);
                                _logon = new LogonForm();
                                _logon.GoTo();

                                // set and submit server form
                                var serverForm = new ServerForm();
                                if (WaitFor(() => serverForm.Displayed))
                                {
                                    serverForm.SaveServer = true;
                                    serverForm.Set(IcServer).Submit();
                                }

                                // Set and submit auth form
                                _authForm = new AuthForm();
                                if (WaitFor(() => _authForm.Displayed))
                                {
                                    _authForm.RememberUser = true;
                                    _authForm.Set(Rm.Users[0], UserPassword).LogOn();
                                }
                                // Set and submit station form
                                var station = new StationForm();
                                if (WaitFor(() => station.Displayed))
                                {
                                    station.RememberStation = true;
                                    station.Set(Rm.Stations[0]).Submit();
                                }
                                _interactionsView = new MyInteractionsView();
                                return WaitFor(() => _interactionsView.Displayed);
                            }, "Step 1 - The user was not logged on");
                        }
                        #endregion

                        #region STEP 2: Log off.
                        using (Trace.TestCase.scope("Step 2: Log off."))
                        {
                            //Step 2 Verify: Log off page is displayed.
                            TraceTrue(UserLogoff, "Step 2 - The user did not logout as expected.");
                        }
                        #endregion

                        #region STEP 3: Close and open the browser window and navigate to Interaction Connect.
                        using (Trace.TestCase.scope("Step 3: Close and open the browser window and navigate to Interaction Connect."))
                        {
                            // Step 3 Reopen the browser
                            // Do the best emulation of reopening the browser
                            TraceTrue(() =>
                            {
                                WebDriverManager.Instance.CurrentDriver.RefreshPage();
                                _logon.GoTo();
                                return WaitFor(() => _authForm.Displayed);
                            }, "Step 3 - The user was not logged on");

                            TraceTrue(() => WaitFor(() => _authForm.User.Equals(Rm.Users[0])), "Step 3 - User ID not remembered");
                            TraceTrue(() => WaitFor(() => _authForm.RememberUser), "Step 3 - The checkbox to remember the user ID was not selected.");
                        }
                        #endregion

                        #region STEP 4: Proceed.
                        using (Trace.TestCase.scope("Step 4: Proceed."))
                        {
                            //Step 4 Verify: Station selection page is bypassed and user is logged on to Interaction Connect.
                            TraceTrue(() =>
                            {
                                // Set and submit auth form
                                _authForm.Set(Rm.Users[0], UserPassword).LogOn();
                                return WaitFor(() => _interactionsView.Displayed);
                            }, "Step 4 - The user was not logged on the 2nd time");
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
                            SetLoginAuthentication(new LoginAuthenticationDataContract
                            {
                                DisableAlternateWindowsAuth = false,
                                DisableCachedCredentials = false,
                                DisableIcAuth = false,
                                DisableSingleSignOn = true,
                                DisableWindowsAuth = false
                            });
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
        public void Test36657_PersistentLogonInformation()
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