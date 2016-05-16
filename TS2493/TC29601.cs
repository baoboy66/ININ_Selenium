namespace ININ.Testing.Automation.Test.Client.TestCases.TS2493
{
    using System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Client;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using Xunit;

    /// <summary>
    ///     TC29601 - Properties - Notes view information is correct
    /// </summary>
    public class TC29601 : TS2493TestCase
    {
        #region Constructors and Destructors
        public TC29601()
        {
            TSNum = "2493";
            TCNum = "29601.1";
        }
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
                        ContactProperties propertiesPage;

                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            foreach (var user in Rm.Users)
                            {
                                Users.SetRole(user, _DEFAULT_ROLE);
                            }
                            UpdateUserDirectoryEntry(Rm.Users[1], false);

                            Drivers = WebDriverManager.Instance.AddDriver(1);
                            TraceTrue(() => UserLogonAndStatusSet(Rm.Users[0], Rm.Stations[0], Drivers[0]), "User failed to log on.");

                            // The 'Properties' action should activate in the Directory Toolbar.

                            SelectUserFromFilter(Rm.Users[1]);

                            var propertiesButton = DirectoryView.Get().GetButton(DirectoryAction.Properties, ActionTarget.Default);
                            TraceTrue(propertiesButton.WaitUntil(WaitUntilType.CanInteract), "Properties button didn't become enabled after selecting user");

                            // The properties for the user selected will display.
                            propertiesButton.Click();
                            propertiesPage = ContactProperties.Get();
                        }
                        #endregion

                        #region STEP 1: Select the 'Notes' view.
                        using (Trace.TestCase.scope("Step 1: Select the 'Notes' view."))
                        {
                            //Step 1 Verify: All previously configured user information should appear in the appropriate fields.
                            TraceTrue(propertiesPage.SwitchToView(ContactPropertyTab.Notes), "Couldn't switch to the Notes tab");

                            ValidateString(propertiesPage.Notes.NotesTextField.Text, USER_NOTES, "Notes");
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
        public void Test29601_PropertiesNotesViewInformationIsCorrect()
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