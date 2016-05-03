namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.ICWS.Configuration.System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;
    using StaleElementReferenceException = OpenQA.Selenium.StaleElementReferenceException;

    /// <summary>
    ///     TC24456 - Invalid Login Scenarios
    /// </summary>
    public class TC24456 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC24456()
        {
            this.TSNum = "2047";
            this.TCNum = "24456.5";
        }
        #endregion

        #region  Constants and Fields
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
                            // make sure the user is added to the right role.
                            SetUserDefaultRole(this.Rm.Users);

                            // setup the driver
                            this.Drivers = WebDriverManager.Instance.AddDriver(1);

                            // set the login authentication configuration to disable SSO
                            SetLoginAuthentication(null);

                            // set the Logon object
                            this._logon = Logon.Get();

                            Logon.GoToLogon();
                            this._logon.SetServerForm(this.IcServer);
                        }
                        #endregion

                        #region STEP 1: Attempt to Logon with an invalid user id.
                        var waiter = new WebDriverBaseWait();
                        waiter.IgnoreExceptionTypes(typeof (StaleElementReferenceException));
                        using (Trace.TestCase.scope("Step 1: Attempt to login with an invalid user id."))
                        {
                            this._logon.UserIDTextField.SendKeys(_INVALID_ID, true);
                            this._logon.PasswordTextField.SendKeys(this.UserPassword, true);
                            this._logon.LogonButton.Click();

                            //Step 1 Verify: An error appears at the top of the form
                            //Comment: Currently, the error says: \'The authentication process failed.\'
                            this.AssertIcAuthError(waiter, "The error message was not found or incorrect for user ID.");
                        }
                        #endregion

                        #region STEP 2: Attempt to login with an invalid password.
                        using (Trace.TestCase.scope("Step 2: Attempt to login with an invalid password."))
                        {
                            this._logon.UserIDTextField.SendKeys(this.Rm.Users[0], true);
                            this._logon.PasswordTextField.SendKeys(_INVALID_ID, true);
                            this._logon.LogonButton.Click();
                            //Step 2 Verify: An error appears at the top of the form.
                            //Comment: Currently, the error says: \'The authentication process failed.\'
                            this.AssertIcAuthError(waiter, "The error message was not found or incorrect for password");
                        }
                        #endregion

                        #region STEP 3: Authenticate with a valid user id and password.
                        using (Trace.TestCase.scope("Step 3: Authenticate with a valid user id and password."))
                        {
                            this._logon.UserIDTextField.SendKeys(this.Rm.Users[0], true);
                            this._logon.PasswordTextField.SendKeys(this.UserPassword, true);
                            this._logon.LogonButton.Click();

                            //Step 3 Verify: The station selection page is displayed.
                            this.TraceTrue(ChangeStation.ChangeStationViewIsShown(), "The change station view was not shown.");
                        }
                        #endregion

                        #region STEP 4: Attempt to login with an invalid station name.
                        using (Trace.TestCase.scope("Step 4: Attempt to login with an invalid station name."))
                        {
                            ChangeStation.SetStation(_DEFAULT_STATION_TYPE, _INVALID_ID);
                            ChangeStation.ClickChooseStation();
                            //Step 4 Verify: An error appears at the top of the form.
                            //Comment: Currently, the error says: \'The specified station name is invalid.\'
                            this._changeStation = ChangeStation.Get();
                            this._expectedErrorMessage = "The specified station name is invalid.";
                            Func<bool> assertFunc = () =>
                            {
                                return waiter.Until(d =>
                                {
                                    Trace.TestCase.note("Actual: {}, Expected: {}", this._changeStation.ChangeStationErrorView.Text.Trim(), this._expectedErrorMessage);
                                    return this._changeStation.ChangeStationErrorView.Text.Contains(this._expectedErrorMessage);
                                });
                            };
                            this.TraceTrue(assertFunc, "The error message was not found or incorrect for password.");
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
                        Trace.TestCase.always("HTML Dump: \n{}", WebDriverManager.Instance.HtmlDump);

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
        [Trait("Priority", "P1")]
        [Trait("BFT", "hazard")]
        [Trait("Patch", "true")]
        public void Test24456_InvalidLogonScenarios()
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
        ///     Verify if the error is correct
        /// </summary>
        /// <param name="waiter">Waiter object</param>
        /// <param name="assertionErrorMessage">Error message</param>
        private void AssertIcAuthError(WebDriverBaseWait waiter, string assertionErrorMessage)
        {
            this.TraceTrue(() => waiter.Until(d => this._logon.IcAuthLogonForm.Displayed), "IC auth logon form could not be found.");
            this.TraceTrue(() => waiter.Until(d => this._logon.IcAuthErrorMessageLabel.Displayed), "IC auth error message could not be found.");

            this.TraceTrue(!string.IsNullOrWhiteSpace(this._logon.UserIDTextField.Text), "When logging in with an invalid user name, the page just refreshes without displaying an error.");
            this.TraceTrue(this._logon.IcAuthErrorMessageLabel.Text.Contains(this._expectedErrorMessage), assertionErrorMessage);
        }
        #endregion
    }
}