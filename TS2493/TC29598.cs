namespace ININ.Testing.Automation.Test.Client.TestCases.TS2493
{
    using System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Client;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;
    using StaleElementReferenceException = OpenQA.Selenium.StaleElementReferenceException;

    /// <summary>
    ///     TC29598 - Properties - Home view information is correct
    /// </summary>
    public class TC29598 : TS2493TestCase
    {
        #region Constructors and Destructors
        public TC29598()
        {
            TSNum = "2493";
            TCNum = "29598.1";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     Instance of a private waiter
        /// </summary>
        private WebDriverBaseWait _waiter;

        /// <summary>
        ///     Instance of Contact Properties
        /// </summary>
        private ContactProperties _propertiesPage;
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (Rm = ResourceManagerRuntime.AllocateResources(2, 1))
                {
                    try
                    {
                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            foreach (var user in Rm.Users)
                            {
                                Users.SetRole(user, _DEFAULT_ROLE);
                            }
                            UpdateUserDirectoryEntry(Rm.Users[1], false);

                            Drivers = WebDriverManager.Instance.AddDriver(1);

                            _waiter = new WebDriverBaseWait();
                            _waiter.IgnoreExceptionTypes(typeof(StaleElementReferenceException));

                            TraceTrue(() => UserLogonAndStatusSet(Rm.Users[0], Rm.Stations[0], Drivers[0]), "User failed to log on.");

                            // The 'Properties' action should activate in the Directory Toolbar.
                            WebDriverManager.Instance.SwitchBrowser(Drivers[0]);

                            SelectUserFromFilter(Rm.Users[1]);

                            var propertiesButton = DirectoryView.Get().GetButton(DirectoryAction.Properties, ActionTarget.Default);

                            TraceTrue(() => propertiesButton.WaitUntil(WaitUntilType.CanInteract), "Properties button didn't become enabled after selecting user");
                            // The properties for the user selected will display.
                            propertiesButton.Click();
                            TraceTrue(() => _waiter.Until(d =>
                            {
                                _propertiesPage = ContactProperties.Get();
                                return _propertiesPage.General.DisplayNameTextField.Displayed;
                            }), "Contact Properties didn't show in due time");

                            // make sure the contact properties actually show before we continue
                            // assumption: if the display name text field is displayed, then rest of the form is displayed as well
                            //TraceTrue(() => _waiter.Until(d => ContactProperties.Get().General.DisplayNameTextField.Displayed), "Contact Properties didn't show in due time");
                        }
                        #endregion

                        #region STEP 1: Select the 'Home' view.
                        using (Trace.TestCase.scope("Step 1: Select the 'Home' view."))
                        {
                            //Step 1 Verify: All previously configured user information should appear in the appropriate fields.
                            TraceTrue(_propertiesPage.SwitchToView(ContactPropertyTab.Home), "Couldn't switch to the Home tab");

                            _propertiesPage.Home.AddressTextField.WaitUntil(WaitUntilType.Displayed);

                            ValidateString(_propertiesPage.Home.AddressTextField.Text, HOME_ADDRESS, "Home address");
                            ValidateString(_propertiesPage.Home.CityTextField.Text, HOME_CITY, "Home city");
                            ValidateString(_propertiesPage.Home.StateTextField.Text, HOME_STATE, "Home state");
                            ValidateString(_propertiesPage.Home.ZipCodeTextField.Text, HOME_POSTALCODE, "Home postal code");
                            ValidateString(_propertiesPage.Home.CountryTextField.Text, HOME_COUNTRY, "Home country");

                            TraceTrue(ValidatePhoneNumber(_propertiesPage.Home.HomePhone2Number, HomePhone2), "Home Phone 2 did not match");
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
        [Trait("TestSuite", "2493")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "false")]
        public void Test29598_PropertiesHomeViewInformationIsCorrect()
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
        #endregion
    }
}