namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.ICWS.Configuration.System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common.LogonForm;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC36636 - Cancel logon at station selection
    /// </summary>
    public class TC36636 : ClientTestCase
    {
        private AuthForm _authForm;
        private LogonForm _logon;
        private StationForm _station;

        public TC36636()
        {
            TSNum = "2047";
            TCNum = "36636.3";
        }

        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (Rm = ResourceManagerRuntime.AllocateResources(1))
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
                                Drivers = WebDriverManager.Instance.AddDriver(Rm.Users.Count);
                                return true;
                            }, "Pre run setup failed.");
                        }
                        #endregion

                        #region STEP 1: Enter the Interaction Center server name, user ID and proceed to the station selection page.
                        using (Trace.TestCase.scope("Step 1: Enter the Interaction Center server name, user ID and proceed to the station selection page."))
                        {
                            //Step 1 Verify: Station selection page is displayed.
                            TraceTrue(() =>
                            {
                                _logon = new LogonForm();
                                _logon.GoTo();

                                // set and submit server form
                                var serverForm = new ServerForm();
                                if (WaitFor(() => serverForm.Displayed))
                                {
                                    serverForm.Set(IcServer).Submit();
                                }

                                // Set and submit auth form
                                _authForm = new AuthForm();
                                if (WaitFor(() => _authForm.Displayed))
                                {
                                    _authForm.Set(Rm.Users[0], UserPassword).LogOn();
                                }

                                _station = new StationForm();
                                return WaitFor(() => _station.Displayed);
                            }, "Step 1 - The change station form was not shown.");
                        }
                        #endregion

                        #region STEP 2: Cancel the station selection.
                        using (Trace.TestCase.scope("Step 2: Cancel the station selection."))
                        {
                            //Step 2 Verify: User ID authentication page is displayed.
                            TraceTrue(() =>
                            {
                                _station.Cancel();
                                return WaitFor(() => _authForm.Displayed);
                            }, "Step 2 - The user was not put back at the IC auth form.");
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
        public void Test36636_CancelLogonAtStationSelection()
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