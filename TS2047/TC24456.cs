namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.ICWS.Configuration.System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC24456 - Invalid Login Scenarios
    /// </summary>
    public class TC24456 : ClientTestCase
    {
        /// <summary>
        ///     A random invalid string that will be used to simulate an invalid user id, password, server, and station.
        /// </summary>
        private const string _INVALID_ID = "fdjskaljfdklsjfdjskla";

        /// <summary>
        ///     Change station page object
        /// </summary>
        private ChangeStation _changeStation;

        /// <summary>
        ///     The default expected error message.
        /// </summary>
        private string _expectedErrorMessage = "The authentication process failed.";

        /// <summary>
        ///     Logon page object
        /// </summary>
        private Logon _logon;

        public TC24456()
        {
            TSNum = "2047";
            TCNum = "24456.5";
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
                            // make sure the user is added to the right role.
                            Users.SetRole(Rm.Users[0], _DEFAULT_ROLE);
                            Status.Set(Rm.Users[0], "Available");

                            // setup the driver
                            Drivers = WebDriverManager.Instance.AddDriver(1);

                            // set the Logon object
                            _logon = Logon.Get();
                            Logon.GoToLogon();
                            _logon.SetServerForm(IcServer);
                        }
                        #endregion

                        #region STEP 1: Attempt to Logon with an invalid user id.
                        using (Trace.TestCase.scope("Step 1: Attempt to login with an invalid user id."))
                        {
                            _logon.UserIDTextField.SendKeys(_INVALID_ID, true);
                            _logon.PasswordTextField.SendKeys(UserPassword, true);
                            _logon.LogonButton.Click();

                            //Step 1 Verify: An error appears at the top of the form
                            //Comment: Currently, the error says: \'The authentication process failed.\'
                            AssertIcAuthError("Step 1 - IC auth logon form could not be found.");
                        }
                        #endregion

                        #region STEP 2: Attempt to login with an invalid password.
                        using (Trace.TestCase.scope("Step 2: Attempt to login with an invalid password."))
                        {
                            _logon.UserIDTextField.SendKeys(Rm.Users[0], true);
                            _logon.PasswordTextField.SendKeys(_INVALID_ID, true);
                            _logon.LogonButton.Click();

                            //Step 2 Verify: An error appears at the top of the form.
                            //Comment: Currently, the error says: \'The authentication process failed.\'
                            AssertIcAuthError("Step 2 - The error message was not found or incorrect for password");
                        }
                        #endregion

                        #region STEP 3: Authenticate with a valid user id and password.
                        using (Trace.TestCase.scope("Step 3: Authenticate with a valid user id and password."))
                        {
                            _logon.UserIDTextField.SendKeys(Rm.Users[0], true);
                            _logon.PasswordTextField.SendKeys(UserPassword, true);
                            _logon.LogonButton.Click();

                            //Step 3 Verify: The station selection page is displayed.
                            TraceTrue(() => ChangeStation.ChangeStationViewIsShown(), "Step 3 - The change station view was not shown.");
                        }
                        #endregion

                        #region STEP 4: Attempt to login with an invalid station name.
                        using (Trace.TestCase.scope("Step 4: Attempt to login with an invalid station name."))
                        {
                            ChangeStation.SetStation(_DEFAULT_STATION_TYPE, _INVALID_ID);
                            ChangeStation.ClickChooseStation();
                            //Step 4 Verify: An error appears at the top of the form.
                            //Comment: Currently, the error says: \'The specified station name is invalid.\'

                            _changeStation = ChangeStation.Get();
                            _expectedErrorMessage = "Step 4 - The specified station name is invalid.";
                            TraceTrue(() => { return WaitFor(() => _changeStation.ChangeStationErrorView.Text.Contains(_expectedErrorMessage)); }, "The error message was not found for invalid station name.");
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
                        Trace.TestCase.always("HTML Dump: \n{}", WebDriverManager.Instance.HtmlDump);

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
        [Trait("Priority", "P1")]
        [Trait("BFT", "hazard")]
        [Trait("Patch", "true")]
        public void Test24456_InvalidLogonScenarios()
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

        /// <summary>
        ///     Verify if the error is correct
        /// </summary>
        /// <param name="assertionErrorMessage">Error message</param>
        private void AssertIcAuthError(string assertionErrorMessage)
        {
            TraceTrue(() => WaitFor(() => _logon.IcAuthLogonForm.Displayed), "IC auth logon form could not be found.");
            TraceTrue(() => WaitFor(() => _logon.IcAuthErrorMessageLabel.Displayed), "IC auth error message could not be found.");
            TraceTrue(!string.IsNullOrWhiteSpace(_logon.UserIDTextField.Text), "When logging in with an invalid user name, the page just refreshes without displaying an error.");
            TraceTrue(_logon.IcAuthErrorMessageLabel.Text.Contains(_expectedErrorMessage), assertionErrorMessage);
        }
    }
}