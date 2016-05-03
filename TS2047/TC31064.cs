namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.ICWS.Configuration.System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC31064 - Login with "Allow IC Auth" disabled
    /// </summary>
    public class TC31064 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC31064()
        {
            this.TSNum = "2047";
            this.TCNum = "31064.5";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     Expected error message
        /// </summary>
        private const string _ALLOW_IC_AUTH_DISABLED_ERROR = "None of the server's accepted logon types are supported by this application. You will not be able to log on. Check your server selection again, and contact your administrator if problems persist.";

        /// <summary>
        ///     Logon page object
        /// </summary>
        private Logon _logon;

        private WebDriverBaseWait _wait;
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
                //  We can leave this commented out because this test verifies an error message given before trying to log a user on
                // using (Rm = ResourceManagerRuntime.AllocateResources(users: 0, stations: 0, skills: 0, workgroups: 0, roles: 0))
            {
                try
                {
                    #region Pre Run Setup
                    using (Trace.TestCase.scope("Pre Run Setup"))
                    {
                        this.Drivers = WebDriverManager.Instance.AddDriver(1);

                        this._wait = new WebDriverBaseWait();
                        this._wait.IgnoreExceptionTypes(typeof (StaleElementReferenceException));

                        SetLoginAuthentication(new LoginAuthenticationDataContract
                        {
                            DisableAlternateWindowsAuth = false,
                            DisableCachedCredentials = false,
                            DisableIcAuth = true,
                            DisableSingleSignOn = true,
                            DisableWindowsAuth = false
                        });
                    }
                    #endregion

                    #region STEP 1: Navigate to the Web Client logon page.
                    using (Trace.TestCase.scope("Step 1: Navigate to the Web Client logon page."))
                    {
                        this._logon = Logon.Get();

                        Logon.GoToLogon();

                        this._logon.SetServerForm(this.IcServer);

                        //Step 1 Verify: The user under test is denied access with an error message.
                        //Comment: \'None of the server\'s accepted logon types are supported by this application. You will not be able to log on. Check your server selection again, and contact your administrator if problems persist.
                        this._wait.Until(_ => this._logon.ServerErrorMessageLabel.Exists);
                        this.TraceTrue(this._logon.ServerErrorMessageLabel.Text == _ALLOW_IC_AUTH_DISABLED_ERROR, "The error message was not presented.");
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

        [ConnectFact]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P3")]
        [Trait("BFT", "hazard")]
        public void Test31064_LoginWithAllowIcAuthDisabled()
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