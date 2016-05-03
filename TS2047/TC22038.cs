namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;
    using StaleElementReferenceException = OpenQA.Selenium.StaleElementReferenceException;

    /// <summary>
    ///     Multiple Login Sessions on same browser
    /// </summary>
    public class TC22038 : ClientTestCase
    {
        public TC22038()
        {
            TSNum = "2047";
            TCNum = "22038.6";
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
                            // make sure the user is added to the right role.
                            Users.SetRole(Rm.Users[0], _DEFAULT_ROLE);
                            Status.Set(Rm.Users[0], "Available");

                            // get a driver for the test.
                            Drivers = WebDriverManager.Instance.AddDriver(3);
                        }
                        #endregion

                        #region STEP 1: Open a web browser and navigate to the webclient webpage for your IC server.
                        using (Trace.TestCase.scope("Step 1: Open a web browser and navigate to the webclient webpage for your IC server. "))
                        {
                            // Step 1 Verify: Webclient homepage opens without error.
                            WebDriverManager.Instance.SwitchBrowser(Drivers[0]);
                            Logon.GoToLogon();
                            TraceTrue(() => Logon.IsAtServerForm(), "Step 1 - Couldn't get to the server form.");
                        }
                        #endregion

                        #region STEP 2: Logon to the ic server with a user.
                        using (Trace.TestCase.scope("Step 2: Logon to the ic server with a user. "))
                        {
                            // Step 2 Verify: User logs in to IC server through webclient.
                            TraceTrue(() => UserLogonAndStatusSet(Rm.Users[0], Rm.Stations[0], Drivers[0]), "Step 2 - Did not Logon successfully");
                            TraceTrue(() =>
                            {
                                WebDriverManager.Instance.SwitchBrowser(Drivers[0]);
                                return WaitFor(() => Util.IsLoggedIn());

                            }, "Step 2 - Default views not displayed. Client does not appear to be connected.");
                        }
                        #endregion

                        #region STEP 3: In the web browser, duplicate the webclient tab to create a duplicate session.
                        using (Trace.TestCase.scope("Step 3: In the web browser, duplicate the webclient tab to create a duplicate session. "))
                        {
                            // Step 3 Verify: Tab is duplicated and the user is asked to Logon again.
                            WebDriverManager.Instance.SwitchBrowser(Drivers[1]);
                            Logon.GoToLogon();
                            TraceTrue(() => Logon.IsAtServerForm(), "Step 3 - Couldn't get to the server page.");
                        }
                        #endregion

                        #region STEP 4: Log the user in a second time using the same computer, user and station.
                        using (Trace.TestCase.scope("Step 4: Log the user in a second time using the same computer, user and station."))
                        {
                            // Step 4 Verify: The user is logged in and both sessions are active.
                            TraceTrue(() => UserLogonAndStatusSet(Rm.Users[0], Rm.Stations[0], Drivers[1]), "Did not Logon successfully the second time.");
                            TraceTrue(() =>
                            {
                                WebDriverManager.Instance.SwitchBrowser(Drivers[0]);
                                return WaitFor(() => Util.IsLoggedIn());

                            }, "Step 4 - Default views not displayed. Client does not appear to be connected.");
                        }
                        #endregion

                        #region STEP 5: Close the duplicated tab.
                        using (Trace.TestCase.scope("Step 5: Close the duplicated tab. "))
                        {
                            // Step 5 Verify: Original session is still open and does not close. 
                            WebDriverManager.Instance.SwitchBrowser(Drivers[1]);
                            WebDriverManager.Instance.CurrentDriver.Quit();
                            TraceTrue(() =>
                            {
                                WebDriverManager.Instance.SwitchBrowser(Drivers[0]);
                                return WaitFor(() => Util.IsLoggedIn());

                            }, "Step 5 - Default views not displayed. Client does not appear to be connected.");
                        }
                        #endregion

                        #region STEP 6: In the web browser, duplicate the webclient tab to create a duplicate session.
                        using (Trace.TestCase.scope("Step 6: In the web browser, duplicate the webclient tab to create a duplicate session. "))
                        {
                            // Step 6 Verify: Tab is duplicated and the user is asked to Logon again.
                            WebDriverManager.Instance.SwitchBrowser(Drivers[2]);
                            Logon.GoToLogon();
                            TraceTrue(() => Logon.IsAtAnyLogonForm(), "Step 6 - Couldn't get to the Logon page.");
                        }
                        #endregion

                        #region STEP 7: Log the user in a second time using the same computer and user, but a different station.
                        using (Trace.TestCase.scope("Step 7: Log the user in a second time using the same computer and user, but a different station."))
                        {
                            //Step 7 Verify: The user is logged in and the original session is disconnected.
                            TraceTrue(() => UserLogonAndStatusSet(Rm.Users[0], Rm.Stations[1], Drivers[2]), "Step 7 - Did not Logon successfully");
                            WebDriverManager.Instance.SwitchBrowser(Drivers[2]);
                            TraceTrue(() =>
                            {
                                WebDriverManager.Instance.SwitchBrowser(Drivers[2]);
                                return WaitFor(() => Util.IsLoggedIn());

                            }, "Step 7 - Default views not displayed. Client does not appear to be connected.");
                            WebDriverManager.Instance.SwitchBrowser(Drivers[0]);
                            TraceTrue(() => Logoff.IsAtLogoff(), "User still logged into both stations at the same time");
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
                    }
                }
            }
        }

        [ConnectFact]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "false")]
        public void Test22038_MultipleLogonSessionsOnSameBrowser()
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