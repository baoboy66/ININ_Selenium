namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.ICWS.Configuration;
    using ININ.ICWS.Configuration.People;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC36663 - Default Station information during logon
    /// </summary>
    public class TC36663 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC36663()
        {
            this.TSNum = "2047";
            this.TCNum = "36663.3";
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
                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            SetUserDefaultRole(this.Rm.Users);

                            // Setting the default workstation
                            Users.Set(new UserDataContract
                            {
                                ConfigurationId = new ConfigurationIdDataContract
                                {
                                    Id = this.Rm.Users[0]
                                },
                                DefaultWorkstation = new ConfigurationIdDataContract
                                {
                                    Id = this.Rm.Stations[0]
                                }
                            });

                            this.Drivers = WebDriverManager.Instance.AddDriver(this.Rm.Users.Count);

                            Logon.GoToLogon();
                        }
                        #endregion

                        #region STEP 1: Enter the appropriate server name, user ID and proceed.
                        using (Trace.TestCase.scope("Step 1: Enter the appropriate server name, user ID and proceed."))
                        {
                            Logon.Get().SetServerForm(this.IcServer);
                            Logon.SetIcAuthForm(this.Rm.Users[0], this.UserPassword);
                            Logon.Get().LogonButton.Click();
                            //Step 1 Verify: The station selection page is displayed and the Default Station information is correct.
                            this.TraceTrue(ChangeStation.ChangeStationViewIsShown(), "The change station view was not shown.");
                        }
                        #endregion

                        #region STEP 2: Choose station and proceed.
                        using (Trace.TestCase.scope("Step 2: Choose station and proceed."))
                        {
                            ChangeStation.ClickChooseStation();
                            //Step 2 Verify: User is logged on to Interaction Connect.
                            this.TraceTrue(Util.HasStation(), "The user does not have a station");
                            this.TraceTrue(NavBar.Get().CanFindNavbarMenuToggleButton(), "The user was not logged on.");
                        }
                        #endregion

                        #region STEP 3: Navigate to the user configuration menu.
                        using (Trace.TestCase.scope("Step 3: Navigate to the user configuration menu."))
                        {
                            NavBar.Get().NavbarMenuToggleButton.Click();
                            //Step 3 Verify: Default Station name is displayed and will match what was selected at the station selection page.
                            this.TraceTrue(NavBar.Get().StationLabel.Text == this.Rm.Stations[0], "The user is not connected to the right station.");
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