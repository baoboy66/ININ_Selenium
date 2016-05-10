namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.ICWS.Configuration;
    using ININ.ICWS.Configuration.People;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Client.Navbar;
    using ININ.Testing.Automation.Lib.Client.Queues.MyInteractions;
    using ININ.Testing.Automation.Lib.Common.LogonForm;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC36663 - Default Station information during logon
    /// </summary>
    public class TC36663 : ClientTestCase
    {
        /// <summary>
        ///     Logon page object
        /// </summary>
        private LogonForm _logon;

        private StationForm _station;

        public TC36663()
        {
            TSNum = "2047";
            TCNum = "36663.3";
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

                                // Setting the default workstation
                                Users.Set(new UserDataContract
                                {
                                    ConfigurationId = new ConfigurationIdDataContract
                                    {
                                        Id = Rm.Users[0]
                                    },
                                    DefaultWorkstation = new ConfigurationIdDataContract
                                    {
                                        Id = Rm.Stations[0]
                                    }
                                });
                                // get driver for the test.
                                Drivers = WebDriverManager.Instance.AddDriver(Rm.Users.Count);

                                // Go to logon page
                                _logon = new LogonForm();
                                _logon.GoTo();
                                return true;
                            }, "Pre run setup failed.");
                        }
                        #endregion

                        #region STEP 1: Enter the appropriate server name, user ID and proceed.
                        using (Trace.TestCase.scope("Step 1: Enter the appropriate server name, user ID and proceed."))
                        {
                            //Step 1 Verify: The station selection page is displayed and the Default Station information is correct.
                            TraceTrue(() =>
                            {
                                // set and submit server form
                                var serverForm = new ServerForm();
                                if (WaitFor(() => serverForm.Displayed))
                                    serverForm.Set(IcServer).Submit();

                                // Set and submit auth form
                                var authForm = new AuthForm();
                                if (WaitFor(() => authForm.Displayed))
                                    authForm.Set(Rm.Users[0], UserPassword).LogOn();

                                _station = new StationForm();
                                return WaitFor(() => _station.Displayed);
                            }, "Step 1 - The change station form was not shown.");
                        }
                        #endregion

                        #region STEP 2: Choose station and proceed.
                        using (Trace.TestCase.scope("Step 2: Choose station and proceed."))
                        {
                            //Step 2 Verify: User is logged on to Interaction Connect.
                            TraceTrue(() =>
                            {
                                _station.Submit();
                                var interation = new MyInteractionsView();
                                return WaitFor(() => interation.Displayed);
                            }, "Step 2 - User is not logged on.");
                        }
                        #endregion

                        #region STEP 3: Navigate to the user configuration menu.
                        using (Trace.TestCase.scope("Step 3: Navigate to the user configuration menu."))
                        {
                            //Step 3 Verify: Default Station name is displayed and will match what was selected at the station selection page.
                            TraceTrue(() =>
                            {
                                var menu = new UserMenuPopover();
                                menu.Toggle();
                                return WaitFor(() => menu.Station.Equals(Rm.Stations[0]));
                            }, "Step 3 - The user is not connected to the right station.");
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
        public void Test36663_DefaultStationInformationDuringLogon()
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