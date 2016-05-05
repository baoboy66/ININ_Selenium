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
    using ININ.Testing.Automation.Lib.Common.LogonForm;
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
        private const string _EXPECTED_ERROR_MESSAGE = "The following licenses were not available: I3_ACCESS_CLIENT";
        private const string _EXPECTED_ERROR_MESSAGE_2 = "You are not allowed to log on to this station.";

        /// <summary>
        ///     Logon page object
        /// </summary>
        private LogonForm _logonForm;
        private AuthForm _authForm;
        private StationForm _stationForm;

        /// <summary>
        ///     Logon page object
        /// </summary>
        private LogoffForm _logoffForm;
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
                            TraceTrue(() =>
                            {
                                // Set default role
                                Users.SetRole(Rm.Users[0], _DEFAULT_ROLE);
                                Status.Set(Rm.Users[0], "Available");

                                // Disable Client Access License
                                Users.SetClientAccessLicense(Rm.Users[0], false);

                                // Disable Client Accesss Right for workstation
                                var getStationRequestParameters = new StationsResource.GetStationRequestParameters
                                {
                                    Select = "*",
                                    Id = Rm.Stations[0]
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

                                // set logon object
                                _logonForm = new LogonForm();
                                return true;
                            }, "Pre run setup failed.");

                        }
                        #endregion

                        #region STEP 1: Attempt to Logon without a Client Access License
                        using (Trace.TestCase.scope("Step 1: Attempt to Logon without a Client Access License"))
                        {
                            //Step 1 Verify: An error appears at the top of the form.
                            //Comment: The station and user both cannot have the license.  Currently, the error says: \'You were logged off because there was a problem connecting to the specified station. The following licenses were not available: I3_ACCESS_CLIENT.\'

                            TraceTrue(() =>
                            {
                                _logonForm.GoTo();
                                var serverForm = new ServerForm();
                                if (WaitFor(() => serverForm.Displayed))
                                    serverForm.Set(IcServer).Submit();

                                // Set and submit auth form
                                _authForm = new AuthForm();
                                if (WaitFor(() => _authForm.Displayed))
                                    _authForm.Set(Rm.Users[0], UserPassword).LogOn();

                                // Set station and submit 
                                _stationForm = new StationForm();
                                if (WaitFor(() => _stationForm.Displayed))
                                    _stationForm.Set(Rm.Stations[0]).Submit();

                                _logoffForm = new LogoffForm();
                                return WaitFor(() => _logoffForm.Displayed);
                            }, "Step 1 - The user is not at the logoff view.");

                            TraceTrue(() =>
                            {
                                return WaitFor(() => _logoffForm.Error.Equals(_EXPECTED_ERROR_MESSAGE));
                            }, "Step 1 - There was an error with verifying the error message");
                        }
                        #endregion

                        #region STEP 2: Reassign the Client Access License to either Station1 or User1. Remove the ACL from User1 for Station Logon to Station1. Attempt to login.
                        using (Trace.TestCase.scope("Step 2: Reassign the Client Access License to either Station1 or User1. Remove the ACL from User1 for Station Logon to Station1. Attempt to login."))
                        {
                            //Step 2 Verify: An error appears at the top of the form.
                            TraceTrue(() =>
                            {
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
                                userDataContract.Roles = new InheritableConfigurationIdCollectionDataContract { ActualValue = new List<ConfigurationIdDataContract>() };
                                userDataContract.Workgroups = new List<ConfigurationIdDataContract>();

                                // udpate the user's license
                                Users.Set(userDataContract);

                                return true;
                            }, "Step 2- Update user's license failed.");

                            TraceTrue(() =>
                            {

                                // Log in
                                _logoffForm.Return();
                                _authForm = new AuthForm();
                                if (WaitFor(() => _authForm.Displayed))
                                    _authForm.Set(Rm.Users[0], UserPassword).LogOn();

                                // Set station and submit 
                                _stationForm = new StationForm();
                                if (WaitFor(() => _stationForm.Displayed))
                                    _stationForm.Set(Rm.Stations[0]).Submit();
                                return WaitFor(() => _stationForm.Error.Equals(_EXPECTED_ERROR_MESSAGE_2, StringComparison.OrdinalIgnoreCase));
                            }, "Step 2 - There was an error with verifying the expected error message");
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