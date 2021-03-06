﻿namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common.LogonForm;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC25856 - Return to the server selection page
    /// </summary>
    public class TC25856 : ClientTestCase
    {
        private LogonForm _logon;
        private ServerForm _serverForm;

        public TC25856()
        {
            TSNum = "2047";
            TCNum = "25856.1";
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
                                // get drivers for the test.
                                Drivers = WebDriverManager.Instance.AddDriver(1);

                                // initialize logon object
                                _logon = new LogonForm();
                                return true;
                            }, "Pre run setup failed.");
                        }
                        #endregion

                        #region STEP 1: From the server selection page, selection of the servers and select Choose Server.
                        using (Trace.TestCase.scope("Step 1: From the server selection page, selection of the servers and select Choose Server."))
                        {
                            //Step 1 Verify: The user advances to the login page.
                            TraceTrue(() =>
                            {
                                _logon.GoTo();
                                _serverForm = new ServerForm();
                                WaitFor(() => _serverForm.Displayed);
                                _serverForm.Set(IcServer);
                                _serverForm.Submit();
                                var authForm = new AuthForm();
                                return WaitFor(() => authForm.Displayed);
                            }, "Step 1 - The user didn't go to the IC auth form.");
                        }
                        #endregion

                        #region STEP 2: Click on the servername link.
                        using (Trace.TestCase.scope("Step 2: Click on the servername link."))
                        {
                            //Step 2 Verify: The user is returned to the server selection page.
                            TraceTrue(() =>
                            {
                                _logon.GoBackToServerForm();
                                return WaitFor(() => _serverForm.Displayed);
                            }, "Step 2 - The user didn't return to the server selection page.");
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
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P1")]
        [Trait("BFT", "true")]
        [Trait("Patch", "true")]
        public void Test25856_ReturnToTheServerSelectionPage()
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