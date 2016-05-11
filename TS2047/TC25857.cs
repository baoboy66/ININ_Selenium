namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common.LogonForm;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC25857 - Manually enter server name on Server Selection page
    /// </summary>
    public class TC25857 : ClientTestCase
    {
        private const string _INVALID_ID = "LSDFSLDKFJDLSJFLSJ";

        /// <summary>
        ///     Error message to expect for an invalid IC Server
        /// </summary>
        private const string _SERVER_ERROR_MESSAGE = "There was a problem communicating with the server. Check your server selection again, and contact your administrator if problems persist.";

        private LogonForm _logon;
        private ServerForm _serverForm;

        public TC25857()
        {
            TSNum = "2047";
            TCNum = "25857.2";
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
                                // get a driver for the test.
                                Drivers = WebDriverManager.Instance.AddDriver(1);

                                // navigate to the Logon page
                                _logon = new LogonForm();
                                _logon.GoTo();
                                return true;
                            }, "Pre run setup failed.");
                        }
                        #endregion

                        #region STEP 1: Manually enter an invalid IC server name on the Server Selection page and select Choose Server.
                        using (Trace.TestCase.scope("Step 1: Manually enter an invalid IC server name on the Server Selection page and select Choose Server."))
                        {
                            // Step 1 Verify: An error message displays, stating \' There was a problem communicating with the server. Check your server selection again, and contact your administrator if problems persist.\'
                            TraceTrue(() =>
                            {
                                _serverForm = new ServerForm();
                                WaitFor(() => _serverForm.Displayed);
                                _serverForm.Set(_INVALID_ID);
                                _serverForm.Submit();
                                return WaitFor(() => _serverForm.Error.Equals(_SERVER_ERROR_MESSAGE));
                            }, "Step 1 - The invalid server message was not displayed.");
                        }
                        #endregion

                        #region STEP 2: Now enter a valid IC server name on the Server Selection page and select Choose Server.
                        using (Trace.TestCase.scope("Step 2: Now enter a valid IC server name on the Server Selection page and select Choose Server."))
                        {
                            // Step 2 Verify: The user advances to the Logon screen.
                            TraceTrue(() =>
                            {
                                _serverForm = new ServerForm();
                                WaitFor(() => _serverForm.Displayed);
                                _serverForm.Set(IcServer);
                                _serverForm.Submit();
                                var authForm = new AuthForm();
                                return WaitFor(() => authForm.Displayed);
                            }, "Step 2 - The IC auth form was not displayed as expected.");
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

                        #region Cleanup
                        using (Trace.TestCase.scope("Post Run Clean Up"))
                        {
                            // Nothing to cleanup.
                        }
                        #endregion
                    }
                }
            }
        }

        [ConnectFact]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "hazard")]
        public void Test25857_ManuallyEnterServerNameOnServerSelectionPage()
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