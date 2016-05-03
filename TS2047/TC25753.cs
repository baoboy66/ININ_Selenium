namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC25753 - Non-ACD call rolls to voice mail - Case 2
    /// </summary>
    public class TC25753 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC25753()
        {
            this.TSNum = "2047";
            this.TCNum = "25753.1";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     The expected message displayed when javascript is disabled
        /// </summary>
        private const string _EXPECTED_ALERT = "Please enable JavaScript in your web browser";

        /// <summary>
        ///     The filename of the expected image file for the logo
        /// </summary>
        private const string _EXPECTED_LOGO = "i3logo-horizontal-black.jpg";
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                try
                {
                    #region Pre Run Setup
                    using (Trace.TestCase.scope("Pre Run Setup"))
                    {
                        this.Drivers = WebDriverManager.Instance.AddDriver(1);
                    }
                    #endregion

                    #region STEP 1: With JavaScript disabled, launch the Basl Web Client.
                    using (Trace.TestCase.scope("Step 1: With JavaScript disabled, launch the Basl Web Client."))
                    {
                        // Step 1 Verify: The user is taken to a page containing the Interactive Intelligence logo with the message \'Please enable JavaScript in your web browser.\'
                        Logon.GoToLogon();

                        // Note: Due to the fact that disabling javascript is not actually supported in Chrome and IE about
                        // the best we can do is check for the existence and expected contents of the <noscript> element.
                        this.TraceTrue(NoScript.Get().NoScriptElement.WaitUntil(WaitUntilType.Exists), "The noscript element was not found.");
                        this.TraceTrue(NoScript.Get().NoScriptElement.Html.Contains(_EXPECTED_LOGO), "The noscript logo was not found.");
                        this.TraceTrue(NoScript.Get().NoScriptElement.Html.Contains(_EXPECTED_ALERT), "The noscript alert was not found.");
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

                    this.Attributes.Add(TestCaseAttribute.WebBrowser_Desktop, WebDriverManager.Instance.GetBrowserVersion());
                    TCDBResults.SendResultsToXml(this.TCNum, this.Passed, this.SCRs, this.Stopwatch.Elapsed.TotalSeconds, this.Attributes);
                    TCDBResults.SubmitResult(this.TCNum, this.Passed, this.SCRs, attributes: this.Attributes);

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