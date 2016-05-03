namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.Configuration;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;
    using StaleElementReferenceException = OpenQA.Selenium.StaleElementReferenceException;

    /// <summary>
    ///     TC25856 - Return to the server selection page
    /// </summary>
    public class TC25856 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC25856()
        {
            this.TSNum = "2047";
            this.TCNum = "25856.1";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     The logon page object
        /// </summary>
        private readonly Logon _logon = Logon.Get();
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
                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            this.Drivers = WebDriverManager.Instance.AddDriver(this.Rm.Users.Count);

                            Logon.GoToLogon();

                            var wait = new WebDriverBaseWait();
                            wait.IgnoreExceptionTypes(typeof (StaleElementReferenceException));

                            wait.Until(_ => this._logon.ServerTextField.CanInteract);
                        }
                        #endregion

                        #region STEP 1: From the server selection page, selection of the servers and select Choose Server.
                        using (Trace.TestCase.scope("Step 1: From the server selection page, selection of the servers and select Choose Server."))
                        {
                            //Step 1 Verify: The user advances to the login page.
                            this._logon.ServerTextField.WaitUntil(WaitUntilType.CanInteract);
                            this._logon.ServerTextField.SendKeys(GlobalConfiguration.Instance.IcServerConfiguration.ServerName, true);

                            this._logon.ChooseServerButton.Click();
                            this.TraceTrue(Logon.SetIcAuthForm(), "The user didn't go to the IC auth form.");
                        }
                        #endregion

                        #region STEP 2: Click on the servername link.
                        using (Trace.TestCase.scope("Step 2: Click on the servername link."))
                        {
                            //Step 2 Verify: The user is returned to the server selection page.
                            this._logon.SwitchToServerFormButton.Click();
                            this._logon.ServerTextField.WaitUntil(WaitUntilType.CanInteract);
                            this.TraceTrue(Logon.IsAtServerForm(), "The user didn't return to the server selection page.");
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