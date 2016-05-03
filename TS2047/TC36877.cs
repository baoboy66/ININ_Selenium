namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Threading;
    using ININ.ICWS.Configuration.System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC36877 - Cancel logon from progress indicator
    /// </summary>
    public class TC36877 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC36877()
        {
            this.TSNum = "2047";
            this.TCNum = "36877.2";
        }
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (this.Rm = ResourceManagerRuntime.AllocateResources(1))
                {
                    try
                    {
                        Logon logon;

                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            // make sure the user is added to the right role.
                            SetUserDefaultRole(this.Rm.Users);

                            this.Drivers = WebDriverManager.Instance.AddDriver(this.Rm.Users.Count);

                            SetLoginAuthentication(null);
                            logon = Logon.Get();
                            Logon.GoToLogon();
                        }
                        #endregion

                        #region STEP 1: Enter the Interaction Center server name, user ID and proceed.
                        using (Trace.TestCase.scope("Step 1: Enter the Interaction Center server name, user ID and proceed."))
                        {
                            logon.SetServerForm(this.IcServer);

                            Logon.SetMaxLogonTimeServerParameter(0);
                            logon.UserIDTextField.SendKeys(this.Rm.Users[0], true);
                            logon.PasswordTextField.SendKeys(this.UserPassword, true);
                            logon.LogonButton.Click();

                            //Step 1 Verify: The logon progress indicator will display.
                            this.TraceTrue(logon.CancelLogonButton.WaitUntil(WaitUntilType.CanInteract), "The cancel button was not able to be interacted with; hence, the progress indicator isn't shown");
                        }
                        #endregion

                        #region STEP 2: Cancel the logon.
                        using (Trace.TestCase.scope("Step 2: Cancel the logon."))
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(4));
                            logon.CancelLogonButton.Click();
                            //Step 2 Verify: User ID authentication page is displayed.
                            this.TraceTrue(Logon.IsAtIcAuthForm(), "The use was not put back at the IC auth form.");
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
                            Logon.ClearMaxLogonTimeServerParameter();
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
        public void Test36877_CancelLogonFromProgressIndicator()
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