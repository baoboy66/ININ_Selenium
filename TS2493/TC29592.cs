namespace ININ.Testing.Automation.Test.Client.TestCases.TS2493
{
    using System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Core.WebElements;
    using ININ.Testing.Automation.Lib.Client;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.ManagedICWS.Directories;

    using Xunit;
    using StaleElementReferenceException = OpenQA.Selenium.StaleElementReferenceException;

    /// <summary>
    ///     TC29592 - Properties - General view information is correct
    /// </summary>
    public class TC29592 : TS2493TestCase
    {
        #region Constructors and Destructors
        public TC29592()
        {
            TSNum = "2493";
            TCNum = "29592.1";
        }
        #endregion

        #region  Constants and Fields
        private WebDriverBaseWait _waiter;
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
                        Button propertiesButton;
                        ContactProperties propertiesPage;

                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            foreach (var user in Rm.Users)
                            {
                                Users.SetRole(user, _DEFAULT_ROLE);
                            }
                            Directories.UpdateUserDirectoryEntry(Rm.Users[1], false);

                            Drivers = WebDriverManager.Instance.AddDriver(1);
                            // Initialize waiter
                            _waiter = new WebDriverBaseWait();
                            _waiter.IgnoreExceptionTypes(typeof (StaleElementReferenceException));

                            TraceTrue(() => UserLogonAndStatusSet(Rm.Users[0], Rm.Stations[0], Drivers[0]), "User failed to log on.");
                        }
                        #endregion

                        #region STEP 1: Select the user with all fields configured in Interaction Administrator from the Company Directory.
                        using (Trace.TestCase.scope("Step 1: Select the user with all fields configured in Interaction Administrator from the Company Directory."))
                        {
                            //Step 1 Verify: The 'Properties' action should activate in the Directory Toolbar.
                            SelectUserFromFilter(Rm.Users[1]);

                            propertiesButton = DirectoryView.Get().GetButton(DirectoryAction.Properties,
                                ActionTarget.Default);
                            TraceTrue(() => _waiter.Until(d => propertiesButton.WaitUntil(WaitUntilType.CanInteract)), "Properties button didn't become enabled after selecting user");
                        }
                        #endregion

                        #region STEP 2: Select the 'Properties' action.
                        using (Trace.TestCase.scope("Step 2: Select the 'Properties' action."))
                        {
                            //Step 2 Verify: The properties for the user selected will display.
                            propertiesButton.Click();
                            propertiesPage = ContactProperties.Get();
                        }
                        #endregion

                        #region STEP 3: Select the 'General' view.
                        using (Trace.TestCase.scope("Step 3: Select the 'General' view."))
                        {
                            //Step 3 Verify: All previously configured user information should appear in the appropriate fields.
                            propertiesPage.SwitchToView(ContactPropertyTab.General);

                            TraceTrue(string.Equals(propertiesPage.General.FirstNameTextField.Text, FIRST_NAME,
                                StringComparison.InvariantCultureIgnoreCase),
                                "The first name text didn't match");
                            TraceTrue(string.Equals(propertiesPage.General.LastNameTextField.Text, LAST_NAME,
                                StringComparison.InvariantCultureIgnoreCase),
                                "The last name text didn't match");
                            TraceTrue(string.Equals(propertiesPage.General.CompanyTextField.Text, COMPANY,
                                StringComparison.InvariantCultureIgnoreCase),
                                "The company text didn't match");

                            TraceTrue(ValidatePhoneNumber(propertiesPage.General.HomePhone, HomePhone1),
                                "Home phone didn't match");

                            TraceTrue(ValidatePhoneNumber(propertiesPage.General.BusinessPhone, BusinessPhone1),
                                "Business phone didn't match");

                            TraceTrue(ValidatePhoneNumber(propertiesPage.General.MobileNumber, MobilePhone),
                                "Mobile phone didn't match");
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
        public void Test29592_PropertiesGeneralViewInformationIsCorrect()
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