namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Collections.Generic;
    using ININ.ICWS.Configuration;
    using ININ.ICWS.Configuration.Hardware;
    using ININ.ICWS.Configuration.People;
    using ININ.ICWS.Configuration.System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Configuration.Hardware;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC24512 - Invalid Login Scenarios Licensing and ACLs
    /// </summary>
    public class TC24512 : ClientTestCase
    {
        /// <summary>
        ///     String object used to hold the expected error message for each step
        /// </summary>
        private const string _EXPECTED_ERROR_MESSAGE = "You were logged off because there was a problem connecting to the specified station.";
        private const string _EXPECTED_ERROR_MESSAGE_2 = "You are not allowed to log on to this station.";

        public TC24512()
        {
            TSNum = "2047";
            TCNum = "24512.7";
        }

        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (Rm = ResourceManagerRuntime.AllocateResources(1, 1))
                {
                    try
                    {
                        Drivers = WebDriverManager.Instance.AddDriver(Rm.Users.Count);

                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            // Set default role
                            Users.SetRole(Rm.Users[0], _DEFAULT_ROLE);
                            Status.Set(Rm.Users[0], "Available");

                            // Disable Client Access License
                            Users.SetClientAccessLicense(Rm.Users[0], false);

                            // Disable Client Accesss Right for workstation
                            var getStationRequestParameters = new StationsResource.GetStationRequestParameters
                            {
                                Select = "*", Id = Rm.Stations[0]
                            };
                            var stationDataContract = Stations.Get(getStationRequestParameters);
                            if (stationDataContract.StationLicensePropertiesHasValue == false)
                            {
                                stationDataContract.StationLicenseProperties = new StationLicensePropertiesDataContract
                                {
                                    LicenseActive = true
                                };
                            }
                            if (stationDataContract.StationLicenseProperties.HasClientAccessHasValue && stationDataContract.StationLicenseProperties.HasClientAccess == true)
                            {
                                stationDataContract.StationLicenseProperties.HasClientAccess = false;
                                // udpate the station
                                Stations.Set(stationDataContract);
                            }
                        }
                        #endregion

                        #region STEP 1: Attempt to Logon without a Client Access License
                        using (Trace.TestCase.scope("Step 1: Attempt to Logon without a Client Access License"))
                        {
                            // log in
                            Logon.DoLogon(Rm.Users[0], UserPassword, IcServer, shouldSetStation: false);
                            ChangeStation.SetStation(_DEFAULT_STATION_TYPE, Rm.Stations[0]);
                            ChangeStation.ClickChooseStation();

                            //Step 1 Verify: An error appears at the top of the form.
                            //Comment: The station and user both cannot have the license.  Currently, the error says: \'You were logged off because there was a problem connecting to the specified station. The following licenses were not available: I3_ACCESS_CLIENT.\'
                            TraceTrue(() => Logoff.IsAtLogoff(), "Step 1 - The user is not at the logoff view.");
                            var logoff = Logoff.Get();
                            TraceTrue(() => WaitFor(() => logoff.MessageElement.Text.Contains(_EXPECTED_ERROR_MESSAGE)), "Step 1 - There was an error with verifying the error message");
                        }
                        #endregion

                        #region STEP 2: Reassign the Client Access License to either Station1 or User1. Remove the ACL from User1 for Station Logon to Station1. Attempt to login.
                        using (Trace.TestCase.scope("Step 2: Reassign the Client Access License to either Station1 or User1. Remove the ACL from User1 for Station Logon to Station1. Attempt to login."))
                        {
                            //Step 2 Verify: An error appears at the top of the form.

                            Logoff.Get().ReturnToLogonButton.Click();

                            // Enable Client Access License
                            Users.SetClientAccessLicense(Rm.Users[0], true);

                            // remove the ability to Logon to the station
                            // to get around possible inheritance of ACLs, we will 
                            // just remove all role and workgroups
                            // NOTE: Hopefully we are not inheriting Station Logon from Default User

                            var getUserRequestParameters = new UsersResource.GetUserRequestParameters
                            {
                                Select = "*",
                                ActualValues = "true",
                                Id = Rm.Users[0]
                            };
                            var userDataContract = Users.Get(getUserRequestParameters);
                            userDataContract.Roles = new InheritableConfigurationIdCollectionDataContract {ActualValue = new List<ConfigurationIdDataContract>()};
                            userDataContract.Workgroups = new List<ConfigurationIdDataContract>();

                            // udpate the user
                            Users.Set(userDataContract);

                            Logon.DoLogon(Rm.Users[0], UserPassword, IcServer, shouldSetStation: false);
                            ChangeStation.SetStation(_DEFAULT_STATION_TYPE, Rm.Stations[0]);
                            ChangeStation.ClickChooseStation();

                            //Verify the expected Error Message
                            TraceTrue(() => WaitFor(() => ChangeStation.Get().ChangeStationErrorView.Text.Equals(_EXPECTED_ERROR_MESSAGE_2, StringComparison.OrdinalIgnoreCase)), "Step 2 - There was an error with verifying the expected error message");
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
                        Trace.TestCase.always("HTML Dump: \n{}", WebDriverManager.Instance.HtmlDump);

                        // Get the browser type that was used during this test case and add an execution to TestFile.xml
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
        public void Test24512_InvalidLogonScenariosLicensingAndAcLs()
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