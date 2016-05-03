namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Collections.Generic;
    using ININ.ICWS.Configuration;
    using ININ.ICWS.Configuration.Hardware;
    using ININ.ICWS.Configuration.People;
    using ININ.ICWS.Configuration.People.Accessrights;
    using ININ.ICWS.Configuration.System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS.Configuration.Hardware;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;
    using StaleElementReferenceException = OpenQA.Selenium.StaleElementReferenceException;

    /// <summary>
    ///     TC24512 - Invalid Login Scenarios Licensing and ACLs
    /// </summary>
    public class TC24512 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC24512()
        {
            this.TSNum = "2047";
            this.TCNum = "24512.7";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     String object used to hold the expected error message for each step
        /// </summary>
        private string _expectedErrorMessage = "";
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
                        this.Drivers = WebDriverManager.Instance.AddDriver(this.Rm.Users.Count);

                        var waiter = new WebDriverBaseWait();
                        waiter.IgnoreExceptionTypes(typeof (StaleElementReferenceException));

                        var getUserRequestParameters = new UsersResource.GetUserRequestParameters
                        {
                            Select = "*",
                            ActualValues = "true"
                        };
                        UserDataContract userDataContract;
                        var getStationRequestParameters = new StationsResource.GetStationRequestParameters
                        {
                            Select = "*"
                        };
                        StationDataContract stationDataContract;

                        Logon.Get();

                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            SetLoginAuthentication(null);
                            
                            // make sure the user doesn't have the Client Access License
                            getUserRequestParameters.Id = this.Rm.Users[0];
                            userDataContract = Users.Get(getUserRequestParameters);
                            if (userDataContract.LicensePropertiesHasValue == false)
                            {
                                userDataContract.LicenseProperties = new LicensePropertiesDataContract
                                {
                                    LicenseActive = true
                                };
                            }
                            if (userDataContract.LicenseProperties.HasClientAccessHasValue && userDataContract.LicenseProperties.HasClientAccess == true)
                            {
                                userDataContract.LicenseProperties.HasClientAccess = false;
                                // udpate the user
                                Users.Set(userDataContract);
                            }

                            // make sure the station doesn't have the Client Access License
                            getStationRequestParameters.Id = this.Rm.Stations[0];
                            stationDataContract = Stations.Get(getStationRequestParameters);
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

                            // make sure we are set to the default role
                            // Doing it here because of ICWS-246
                            SetUserDefaultRole(this.Rm.Users);

                            Logon.GoToLogon();
                        }
                        #endregion

                        #region STEP 1: Attempt to Logon without a Client Access License
                        using (Trace.TestCase.scope("Step 1: Attempt to Logon without a Client Access License"))
                        {
                            Logon.DoLogon(this.Rm.Users[0], this.UserPassword, this.IcServer, shouldSetStation: false);
                            ChangeStation.SetStation(_DEFAULT_STATION_TYPE, this.Rm.Stations[0]);
                            ChangeStation.ClickChooseStation();

                            this._expectedErrorMessage = "You were logged off because there was a problem connecting to the specified station.";
                            //Step 1 Verify: An error appears at the top of the form.
                            //Comment: The station and user both cannot have the license.  Currently, the error says: \'You were logged off because there was a problem connecting to the specified station. The following licenses were not available: I3_ACCESS_CLIENT.\'
                            this.TraceTrue(Logoff.IsAtLogoff(), "The user is not at the logoff view.");
                            var logoff = Logoff.Get();
                            this.TraceTrue(waiter.Until(d => logoff.MessageElement.Text.Contains(this._expectedErrorMessage)), "There was an error with verifying the error message");
                        }
                        #endregion

                        #region STEP 2: Reassign the Client Access License to either Station1 or User1. Remove the ACL from User1 for Station Logon to Station1. Attempt to login.
                        using (Trace.TestCase.scope("Step 2: Reassign the Client Access License to either Station1 or User1. Remove the ACL from User1 for Station Logon to Station1. Attempt to login."))
                        {
                            Logoff.Get().ReturnToLogonButton.Click();

                            //Step 2 Verify: An error appears at the top of the form.
                            //Comment: Currently, the error says: \'You are not allowed to log on to this station.\'
                            this._expectedErrorMessage = "You are not allowed to log on to this station.";
                            // readd the users's client access license
                            userDataContract.LicenseProperties.HasClientAccess = true;

                            // remove the ability to Logon to the station
                            // to get around possible inheritance of ACLs, we will 
                            // just remove all role and workgroups
                            // NOTE: Hopefully we are not inheriting Station Logon from Default User
                            userDataContract.Roles = new InheritableConfigurationIdCollectionDataContract {ActualValue = new List<ConfigurationIdDataContract>()};
                            userDataContract.Workgroups = new List<ConfigurationIdDataContract>();
                            if (userDataContract.AccessRightsHasValue == false)
                            {
                                userDataContract.AccessRights = new AccessRightsPropertiesDataContract();
                            }
                            userDataContract.AccessRights.LoginStation = new InheritableGroupedConfigurationIdCollectionDataContract
                            {
                                ActualValue = new List<GroupedConfigurationIdDataContract>()
                            };

                            // udpate the user
                            Users.Set(userDataContract);

                            // readd the stations' client access license
                            stationDataContract.StationLicenseProperties.HasClientAccess = true;

                            // update the station
                            Stations.Set(stationDataContract);

                            Logon.DoLogon(this.Rm.Users[0], this.UserPassword, this.IcServer, shouldSetStation: false);
                            ChangeStation.SetStation(_DEFAULT_STATION_TYPE, this.Rm.Stations[0]);
                            ChangeStation.ClickChooseStation();

                            ChangeStation.Get().ChangeStationErrorView.WaitUntil(WaitUntilType.Displayed);
                            //Verify the expected Error Message
                            this.TraceTrue(() => waiter.Until(d => ChangeStation.Get().ChangeStationErrorView.Text.Equals(this._expectedErrorMessage, StringComparison.OrdinalIgnoreCase)), "There was an error with verifying the expected error message");
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
                        Trace.TestCase.always("HTML Dump: \n{}", WebDriverManager.Instance.HtmlDump);

                        // Get the browser type that was used during this test case and add an execution to TestFile.xml
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