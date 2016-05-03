namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.ICWS.Configuration.System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.Common.Configuration;
    using ININ.Testing.Automation.Lib.Common.Storage;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC36656 - Persistent logon information via connection configuration
    /// </summary>
    public class TC36656 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC36656()
        {
            this.TSNum = "2047";
            this.TCNum = "36656.2";
        }
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
                        Logon logon;
                        ConnectionConfigurationDialogView connectionConfiguration;

                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            // make sure the user is added to the right role.
                            SetUserDefaultRole(this.Rm.Users);

                            this.Drivers = WebDriverManager.Instance.AddDriver(this.Rm.Users.Count);

                            SetLoginAuthentication(null);
                            logon = Logon.Get();
                            Logon.DoLogon(this.Rm.Users[0], this.UserPassword, this.IcServer, _DEFAULT_STATION_TYPE, this.Rm.Stations[0]);
                            this.TraceTrue(NavBar.Get().CanFindNavbarMenuToggleButton(), "The user was not logged on");
                        }
                        #endregion

                        #region STEP 1: Navigate to the configuration settings view.
                        using (Trace.TestCase.scope("Step 1: Navigate to the configuration settings view."))
                        {
                            NavBar.SelectSettings();
                            //Step 1 Verify: The configuration settings view is displayed.
                            connectionConfiguration = ConnectionConfigurationDialogView.Get();
                            this.TraceTrue(connectionConfiguration.Dialog.ConfigurationModal.WaitUntil(WaitUntilType.DoneMoving), "The app settings was not presented.");
                        }
                        #endregion

                        #region STEP 2: Under \'Connection\', select the configuration items to persist the server selection, user ID selection and station selection.
                        using (Trace.TestCase.scope("Step 2: Under \'Connection\', select the configuration items to persist the server selection, user ID selection and station selection."))
                        {
                            connectionConfiguration.SwitchTo();
                            connectionConfiguration.SaveServerCheckBox.Click(true);
                            connectionConfiguration.SaveIcAuthUserIdCheckBox.Click(true);
                            connectionConfiguration.SaveStationCheckBox.Click(true);
                            connectionConfiguration.Dialog.Save();
                            NavBar.SelectSettings();
                            connectionConfiguration.SwitchTo();
                            //Step 2 Verify: Configuration item selected.
                            this.TraceTrue(connectionConfiguration.SaveServerCheckBox.Selected, "The server option was not saved.");
                            this.TraceTrue(connectionConfiguration.SaveIcAuthUserIdCheckBox.Selected, "The user id option was not saved.");
                            this.TraceTrue(connectionConfiguration.SaveStationCheckBox.Selected, "The save station option was not saved.");
                        }
                        #endregion

                        #region STEP 3: Log off.
                        using (Trace.TestCase.scope("Step 3: Log off."))
                        {
                            connectionConfiguration.Dialog.Cancel();
                            NavBar.ApplicationLogout();
                            //Step 3 Verify: Log off page is displayed.
                            this.TraceTrue(Logoff.IsAtLogoff(), "The user was not logged off.");
                        }
                        #endregion

                        #region STEP 4: Close and open the browser window and navigate to Interaction Connect.
                        using (Trace.TestCase.scope("Step 4: Close and open the browser window and navigate to Interaction Connect."))
                        {
                            // Do the best emulation of reopening the browser
                            Util.PageRefresh();
                            Logon.GoToLogon();
                            BrowserStorage.Get().Session.Clear();
                            Util.PageRefresh();
                            Logon.GoToLogon();
                            //Step 4 Verify: The server selection should be bypassed and the user authentication page should be displayed with the user ID selection persisted.
                            this.TraceTrue(Logon.IsAtIcAuthForm(), "User is not at IC auth form");
                            this.TraceTrue(logon.UserIDTextField.Text == this.Rm.Users[0], "User ID not remembered");
                            this.TraceTrue(logon.RememberIcAuthCheckBox.Selected, "The checkbox to remember the user ID was not selected.");
                        }
                        #endregion

                        #region STEP 5: Proceed.
                        using (Trace.TestCase.scope("Step 5: Proceed."))
                        {
                            logon.PasswordTextField.SendKeys(this.UserPassword);
                            logon.LogonButton.Click();
                            //Step 5 Verify: Station selection page is bypassed and user is logged on to Interaction Connect.
                            this.TraceTrue(NavBar.Get().CanFindNavbarMenuToggleButton(), "The user was not logged on the 2nd time");
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