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

    /// <summary>
    ///     TC22025 - Valid Workstation Logon/Logout
    /// </summary>
    public class TC22025 : ClientTestCase
    {
        public TC22025()
        {
            TSNum = "2047";
            TCNum = "22025.6";
        }

        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                // 1 User (User under test), 1 Station (User under test)
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

                                // Get driver(s)
                                Drivers = WebDriverManager.Instance.AddDriver(1);
                                return true;
                            }, "Pre run setup failed.");
                        }
                        #endregion

                        #region STEP 1: Logon User1 with Station1.
                        using (Trace.TestCase.scope("Step 1: Logon User1 with Station1."))
                        {
                            //Step 1 Verify: The use is successfully logged in.
                            //TraceTrue(UserLogon(Rm.Users[0], Rm.Stations[0]), "The user did not Logon as expected.");
                            TraceTrue(() =>
                            {
                                UserLogon(Rm.Users[0], Rm.Stations[0]);
                                return WaitFor(() => Util.IsLoggedIn());
                            }, "The user did not Logon as expected.");
                        }
                        #endregion

                        #region STEP 2: Refresh the page.
                        using (Trace.TestCase.scope("Step 2: Refresh the page."))
                        {
                            WebDriverManager.Instance.CurrentDriver.RefreshPage();
                            //NOTE: Connect prompts for confirmation before a page refresh
                            WebDriverManager.Instance.CurrentDriver.AcceptAlert();

                            //Step 2 Verify: The user is successfully auto-logged in.
                            TraceTrue(Util.IsLoggedIn(), "The user did not auto-Logon. If this is run in the grid, there is a good chance that the refresh just took too long and you can safely ignore if it is happening intermittently.  Usually 5 seconds is too long and the session will expire. ");
                        }
                        #endregion

                        #region STEP 3: Logout User1.
                        using (Trace.TestCase.scope("Step 3: Logout User1."))
                        {
                            //Step 3 Verify: The user is successfully taken back to the logoff page.
                            TraceTrue(UserLogoff, "The user did not logout as expected.");
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
        [Trait("Priority", "P1")]
        //[Trait("BFT", "false")]
        [Trait("BFT", "staging")]
        [Trait("Patch", "true")]
        public void Test22025_ValidWorkstationLogonLogout()
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