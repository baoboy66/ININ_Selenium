namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.ICWS.Configuration.System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common.LogonForm;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC31064 - Login with "Allow IC Auth" disabled
    /// </summary>
    public class TC31064 : ClientTestCase
    {
        /// <summary>
        ///     Expected error message
        /// </summary>
        private const string _ALLOW_IC_AUTH_DISABLED_ERROR = "None of the server's accepted logon types are supported by this application. You will not be able to log on. Check your server selection again, and contact your administrator if problems persist.";

        /// <summary>
        ///     Logon page object
        /// </summary>
        private LogonForm _logon;

        public TC31064()
        {
            TSNum = "2047";
            TCNum = "31064.5";
        }

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
                        TraceTrue(() =>
                        {
                            // Get driver(s)
                            Drivers = WebDriverManager.Instance.AddDriver(1);

                            SetLoginAuthentication(new LoginAuthenticationDataContract
                            {
                                DisableAlternateWindowsAuth = false,
                                DisableCachedCredentials = false,
                                DisableIcAuth = true,
                                DisableSingleSignOn = true,
                                DisableWindowsAuth = false
                            });

                            return true;
                        }, "Pre run setup failed.");
                    }
                    #endregion

                    #region STEP 1: Navigate to the Web Client logon page.
                    using (Trace.TestCase.scope("Step 1: Navigate to the Web Client logon page."))
                    {
                        //Step 1 Verify: The user under test is denied access with an error message.
                        //Comment: \'None of the server\'s accepted logon types are supported by this application. You will not be able to log on. Check your server selection again, and contact your administrator if problems persist.
                        TraceTrue(() =>
                        {
                            _logon = new LogonForm();
                            _logon.GoTo();
                            var serverForm = new ServerForm();
                            WaitFor(() => serverForm.Displayed);
                            serverForm.Set(IcServer);
                            serverForm.Submit();
                            return WaitFor(() => !string.IsNullOrWhiteSpace(serverForm.Error) && serverForm.Error.Equals(_ALLOW_IC_AUTH_DISABLED_ERROR));
                        }, "The error message was not presented.");
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

        [ConnectFact]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P3")]
        [Trait("BFT", "hazard")]
        public void Test31064_LoginWithAllowIcAuthDisabled()
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