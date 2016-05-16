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
    ///     TC29597 - Dial Pager from Properties view
    /// </summary>
    public class TC29597 : TS2493TestCase
    {
        #region Constructors and Destructors
        public TC29597()
        {
            TSNum = "2493";
            TCNum = "29597.1";
        }
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (Rm = ResourceManagerRuntime.AllocateResources(2, 2))
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
                            UpdateUserDirectoryEntry(Rm.Users[1], true);

                            Drivers = WebDriverManager.Instance.AddDriver(1);
                            TraceTrue(() => UserLogonAndStatusSet(Rm.Users[0], Rm.Stations[0], Drivers[0]), "User failed to log on.");

                            // The 'Properties' action should activate in the Directory Toolbar.
                            WebDriverManager.Instance.SwitchBrowser(Drivers[0]);

                            SelectUserFromFilter(Rm.Users[1]);

                            var propertiesButton = DirectoryView.Get().GetButton(DirectoryAction.Properties,
                                ActionTarget.Default);
                            TraceTrue(propertiesButton.WaitUntil(WaitUntilType.CanInteract),
                                "Properties button didn't become enabled after selecting user");

                            // The properties for the user selected will display.
                            propertiesButton.Click();
                            propertiesPage = ContactProperties.Get();
                        }
                        #endregion

                        #region STEP 1: Dial the Pager from the 'More Business' view.
                        using (Trace.TestCase.scope("Step 1: Dial the Pager from the 'More Business' view."))
                        {
                            //Step 1 Verify: The correct Pager number configured is dialed and appears in the My Interactions queue.
                            var server = GlobalConfiguration.Instance.IcServerConfiguration.ServerName;
                            var session = Core.SessionFactory(server, Rm.Users[1], "1234", Rm.Stations[1]);
                            var extension = string.Format("{0}^{0}", Users.GetExtension(Rm.Users[1]));

                            propertiesPage.SwitchToView(ContactPropertyTab.Business);

                            TraceTrue(propertiesPage.Business.PagerNumber.DialButton.WaitUntil(WaitUntilType.CanInteract),
                                "The Pager dial button was not enabled/visible");

                            propertiesPage.Business.PagerNumber.DialButton.Click();

                            // Check our queue for the interaction
                            var row = FilteredQueue.WaitForInteraction(new Dictionary<string, string>
                            {
                                { InteractionAttribute.Number.AttributeId, extension },
                                { InteractionAttribute.State.AttributeId, InteractionState.CONNECTED }
                            });

                            TraceTrue(row != null, "The interaction was not dialing");

                            session.Disconnect();
                            session.Dispose();
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

                        #region Cleanup
                        using (Trace.TestCase.scope("Post Run Clean Up"))
                        {
                            ClearAllQueues();
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
        public void Test29597_DialPagerFromPropertiesView()
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