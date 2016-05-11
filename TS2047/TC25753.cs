namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.Common.LogonForm;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC25753 - Non-ACD call rolls to voice mail - Case 2
    /// </summary>
    public class TC25753 : ClientTestCase
    {
        /// <summary>
        ///     The expected message displayed when javascript is disabled
        /// </summary>
        private const string _EXPECTED_ALERT = "Please enable JavaScript in your web browser";

        /// <summary>
        ///     The filename of the expected image file for the logo
        /// </summary>
        private const string _EXPECTED_LOGO = "i3logo-horizontal-black.jpg";

        public TC25753()
        {
            TSNum = "2047";
            TCNum = "25753.1";
        }

        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                try
                {
                    #region Pre Run Setup
                    using (Trace.TestCase.scope("Pre Run Setup"))
                    {
                        TraceTrue(() =>
                        {
                            // get driver
                            Drivers = WebDriverManager.Instance.AddDriver(1);
                            return true;
                        }, "Pre run setup failed.");
                    }
                    #endregion

                    #region STEP 1: With JavaScript disabled, launch the Basl Web Client.
                    using (Trace.TestCase.scope("Step 1: With JavaScript disabled, launch the Basl Web Client."))
                    {
                        // Step 1 Verify: The user is taken to a page containing the Interactive Intelligence logo with the message \'Please enable JavaScript in your web browser.\'
                        // Note: Due to the fact that disabling javascript is not actually supported in Chrome and IE about
                        // the best we can do is check for the existence and expected contents of the <noscript> element.
                        TraceTrue(() =>
                        {
                            var logon = new LogonForm();
                            logon.GoTo();
                            return WaitFor(() => NoScript.Get().NoScriptElement.WaitUntil(WaitUntilType.Exists));
                        }, "The noscript element was not found.");
                        TraceTrue(() => WaitFor(() => NoScript.Get().NoScriptElement.Html.Contains(_EXPECTED_LOGO)), "The noscript logo was not found.");
                        TraceTrue(() => WaitFor(() => NoScript.Get().NoScriptElement.Html.Contains(_EXPECTED_ALERT)), "The noscript alert was not found.");
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

        [ConnectFact]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P3")]
        [Trait("BFT", "true")]
        [Trait("Patch", "true")]
        public void Test25753_AppropriateMessageIsDisplayedWhenJavsScriptIsNotEnabled()
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