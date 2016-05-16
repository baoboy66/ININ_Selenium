namespace ININ.Testing.Automation.Test.Client.TestCases.TS2493
{
    using System;
    using System.Collections.Generic;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.Configuration;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Client;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;

    using Xunit;

    /// <summary>
    ///     TC29593 - Dial Home Phone from Properties view
    /// </summary>
    public class TC29593 : TS2493TestCase
    {
        #region Constructors and Destructors
        public TC29593()
        {
            this.TSNum = "2493";
            this.TCNum = "29593.1";
        }
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (this.Rm = ResourceManagerRuntime.AllocateResources(2, 2))
                {
                    try
                    {
                        ContactProperties propertiesPage;

                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            SetUserDefaultRole(this.Rm.Users);
                            UpdateUserDirectoryEntry(this.Rm.Users[1], true);

                            this.Drivers = WebDriverManager.Instance.AddDriver(this.Rm.Users.Count);
                            this.UsersLogonAndStatusSet(this.Rm.Users, this.Rm.Stations, this.Drivers);

                            // The \'Properties\' action should activate in the Directory Toolbar.
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[0]);

                            this.SelectUserFromFilter(this.Rm.Users[1]);

                            var propertiesButton = DirectoryView.Get().GetButton(DirectoryAction.Properties,
                                ActionTarget.Default);
                            this.TraceTrue(propertiesButton.WaitUntil(WaitUntilType.CanInteract),
                                "Properties button didn't become enabled after selecting user");

                            // The properties for the user selected will display.
                            propertiesButton.Click();
                            propertiesPage = ContactProperties.Get();
                        }
                        #endregion

                        #region STEP 1: Dial the Home Number from the \'General\' view.
                        using (Trace.TestCase.scope("Step 1: Dial the Home Number from the \'General\' view."))
                        {
                            var server = GlobalConfiguration.Instance.IcServerConfiguration.ServerName;
                            var session = Core.SessionFactory(server, Rm.Users[1], "1234", Rm.Stations[1]);
                            var extension = string.Format("{0}^{0}", Users.GetExtension(Rm.Users[1]));

                            //Step 1 Verify: The correct Home Phone number configured is dialed and appears in the My Interactions queue.
                            this.TraceTrue(propertiesPage.General.HomePhone.DialButton.WaitUntil(WaitUntilType.CanInteract),
                                "The Home Phone dial button was not enabled/visible");

                            propertiesPage.General.HomePhone.DialButton.Click();

                            // Check our queue for the interaction
                            var row = FilteredQueue.WaitForInteraction(new Dictionary<string, string>
                            {
                                { InteractionAttribute.State.AttributeId, InteractionState.CONNECTED },
                                { InteractionAttribute.Number.AttributeId, extension }
                            });

                            TraceTrue(row != null, "The interaction was not found");

                            session.Disconnect();
                            session.Dispose();
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

                        TCDBResults.SendResultsToXml(this.TCNum, this.Passed, this.SCRs, this.Stopwatch.Elapsed.TotalSeconds, this.Attributes);
                        TCDBResults.SubmitResult(this.TCNum, this.Passed, this.SCRs, attributes: this.Attributes);

                        #region Cleanup
                        using (Trace.TestCase.scope("Post Run Clean Up"))
                        {
                            this.ClearAllQueues();
                        }
                        #endregion
                    }
                }
            }
        }

        [ConnectFact]
        [Trait("TestSuite", "2493")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "false")]
        public void Test29593_DialHomePhoneFromPropertiesView()
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