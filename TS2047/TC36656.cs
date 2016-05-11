namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.ICWS.Configuration.System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Client.Navbar;
    using ININ.Testing.Automation.Lib.Client.Navbar.AppSettings;
    using ININ.Testing.Automation.Lib.Client.Queues.MyInteractions;
    using ININ.Testing.Automation.Lib.Common.LogonForm;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC36656 - Persistent logon information via connection configuration
    /// </summary>
    public class TC36656 : ClientTestCase
    {
        private AuthForm _authForm;
        private ConnectionSettings _connectionSettings;
        private MyInteractionsView _interactionsView;
        private LogonForm _logon;
        private UserMenuPopover _userMenu;

        public TC36656()
        {
            TSNum = "2047";
            TCNum = "36656.2";
        }

        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (Rm = ResourceManagerRuntime.AllocateResources(1, 1))
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

                                // Initialize logon object
                                _logon = new LogonForm();

                                // add driver
                                Drivers = WebDriverManager.Instance.AddDriver(Rm.Users.Count);
                                return UserLogon(Rm.Users[0], Rm.Stations[0]);
                            }, "Pre run setup failed.");
                        }
                        #endregion

                        #region STEP 1: Navigate to the configuration settings view.
                        using (Trace.TestCase.scope("Step 1: Navigate to the configuration settings view."))
                        {
                            //Step 1 Verify: The configuration settings view is displayed.
                            TraceTrue(() =>
                            {
                                _userMenu = new UserMenuPopover();
                                _userMenu.Toggle();
                                _userMenu.OpenSettingsMenu();
                                _connectionSettings = new ConnectionSettings();
                                return WaitFor(() => _connectionSettings.AppSettingsModalDisplayed);
                            }, "Step 1 - The app settings was not presented.");
                        }
                        #endregion

                        #region STEP 2: Under \'Connection\', select the configuration items to persist the server selection, user ID selection and station selection.
                        using (Trace.TestCase.scope("Step 2: Under \'Connection\', select the configuration items to persist the server selection, user ID selection and station selection."))
                        {
                            //Step 2 Verify: Configuration item selected.
                            TraceTrue(() =>
                            {
                                _connectionSettings.SelectMenu(AppSettingsModal.Menu.Connection);
                                _connectionSettings.SaveServer = true;
                                _connectionSettings.RememberStation = true;
                                _connectionSettings.RememberUserId = true;
                                _connectionSettings.Save();

                                _userMenu.Toggle();
                                _userMenu.OpenSettingsMenu();
                                _connectionSettings.SelectMenu(AppSettingsModal.Menu.Connection);
                                return WaitFor(() => _connectionSettings.SaveServer);
                            }, "Step 2 - The server option was not saved.");
                            TraceTrue(WaitFor(() => _connectionSettings.RememberUserId), "Step 2 - The user id option was not saved.");
                            TraceTrue(WaitFor(() => _connectionSettings.RememberStation), "Step 2 - The save station option was not saved.");
                        }
                        #endregion

                        #region STEP 3: Log off.
                        using (Trace.TestCase.scope("Step 3: Log off."))
                        {
                            //Step 3 Verify: Log off page is displayed.
                            _connectionSettings.Cancel();
                            TraceTrue(UserLogoff, "Step 3 - The user was not logged off.");
                        }
                        #endregion

                        #region STEP 4: Close and open the browser window and navigate to Interaction Connect.
                        using (Trace.TestCase.scope("Step 4: Close and open the browser window and navigate to Interaction Connect."))
                        {
                            //Step 4 Verify: The server selection should be bypassed and the user authentication page should be displayed with the user ID selection persisted.
                            TraceTrue(() =>
                            {
                                // Do the best emulation of reopening the browser
                                WebDriverManager.Instance.CurrentDriver.RefreshPage();
                                _logon.GoTo();
                                _authForm = new AuthForm();
                                return WaitFor(() => _authForm.Displayed);
                            }, "Step 4 - IC auth logon form could not be found.");

                            TraceTrue(() => WaitFor(() => _authForm.User.Equals(Rm.Users[0])), "Step 4 - User ID not remembered");
                            TraceTrue(() => WaitFor(() => _authForm.RememberUser), "Step 4 - The checkbox to remember the user ID was not selected.");
                        }
                        #endregion

                        #region STEP 5: Proceed.
                        using (Trace.TestCase.scope("Step 5: Proceed."))
                        {
                            //Step 5 Verify: Station selection page is bypassed and user is logged on to Interaction Connect.
                            TraceTrue(() =>
                            {
                                // Set and submit auth form
                                _authForm.Set(Rm.Users[0], UserPassword).LogOn();
                                _interactionsView = new MyInteractionsView();
                                return WaitFor(() => _interactionsView.Displayed);
                            }, "Step 5 - The user was not logged on the 2nd time");
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
        public void Test36656_PersistentLogonInformationViaConnectionConfiguration()
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