namespace ININ.Testing.Automation.Test.Client.TestCases.TS2493
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using ININ.ICWS.Configuration;
    using ININ.ICWS.Configuration.People;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;

    public abstract class TS2493TestCase : ClientTestCase
    {
        #region  Constants and Fields
        protected const string BUSINESS_ADDRESS = "1234 Test Street\nSuite 1234";
        protected const string BUSINESS_ASSISTANT_NAME = "test-user-assistant-name";
        protected const string BUSINESS_EMAIL = "john.doe+asdf@example.com";
        protected const string COMPANY = "user-company";
        protected const string DEPARTMENT = "test-user-department";
        protected const string FIRST_NAME = "user-first-name";
        protected const string HOME_ADDRESS = "4321 Test Street\nApartment 1234";
        protected const string HOME_CITY = "test-user-home-city";
        protected const string HOME_COUNTRY = "test-user-business-country";
        protected const string HOME_POSTALCODE = "12345-ABCDE";
        protected const string HOME_STATE = "test-user-home-state";
        protected const string LAST_NAME = "user-last-name";
        protected const string TITLE = "test-user-title";
        protected const string USER_NOTES = "this\nis\na\ntest\nnote";

        protected static readonly BasicPhoneNumberDataContract AssistantPhone = new BasicPhoneNumberDataContract
        {
            DialExtensionAutomatically = true,
            Number = "3175558900",
            Extension = "9001"
        };

        protected static readonly BasicPhoneNumberDataContract BusinessPhone1 = new BasicPhoneNumberDataContract
        {
            DialExtensionAutomatically = true,
            Extension = "5678",
            Number = "3175552345"
        };

        protected static readonly BasicPhoneNumberDataContract BusinessPhone2 = new BasicPhoneNumberDataContract
        {
            DialExtensionAutomatically = false,
            Number = "3175554567",
            Extension = "6789"
        };

        protected static readonly BasicPhoneNumberDataContract FaxNumber = new BasicPhoneNumberDataContract
        {
            DialExtensionAutomatically = false,
            Number = "3175557890"
        };

        protected static readonly BasicPhoneNumberDataContract HomePhone1 = new BasicPhoneNumberDataContract
        {
            DialExtensionAutomatically = false,
            Number = "3175551234"
        };

        protected static readonly BasicPhoneNumberDataContract HomePhone2 = new BasicPhoneNumberDataContract
        {
            DialExtensionAutomatically = false,
            Number = "3175553456"
        };

        protected static readonly BasicPhoneNumberDataContract MobilePhone = new BasicPhoneNumberDataContract
        {
            DialExtensionAutomatically = false,
            Number = "3175555678"
        };

        protected static readonly BasicPhoneNumberDataContract PagerNumber = new BasicPhoneNumberDataContract
        {
            DialExtensionAutomatically = false,
            Number = "3175556789"
        };
        #endregion

        #region Methods
        private static string NormalizeDisplayNumber(string formattedNumber)
        {
            return string.Join("", formattedNumber.Where(char.IsDigit));
        }

        protected static void UpdateUserDirectoryEntry(string userId, bool dialableNumbers)
        {
            var requestParams = new UsersResource.GetUserRequestParameters
            {
                Id = userId,
                Select = "*",
                ActualValues = "true"
            };

            var userContract = Users.Get(requestParams);

            var dialableNumberContract = new BasicPhoneNumberDataContract
            {
                Number = string.Format("{0}^{0}", userContract.Extension)
            };

            userContract.PersonalInformationProperties = new PersonalInformationPropertiesDataContract
            {
                GivenName = FIRST_NAME,
                Surname = LAST_NAME,
                CompanyName = COMPANY,
                PhoneNumberOfMobile = dialableNumbers ? dialableNumberContract : MobilePhone,
                PhoneNumberOfBusiness1 = dialableNumbers ? dialableNumberContract : BusinessPhone1,
                PhoneNumberOfBusiness2 = dialableNumbers ? dialableNumberContract : BusinessPhone2,
                PhoneNumberOfHome1 = dialableNumbers ? dialableNumberContract : HomePhone1,
                PhoneNumberOfHome2 = dialableNumbers ? dialableNumberContract : HomePhone2,
                PhoneNumberOfFax = dialableNumbers ? dialableNumberContract : FaxNumber,
                PhoneNumberOfPager = dialableNumbers ? dialableNumberContract : PagerNumber,
                PhoneNumberOfAssistant = dialableNumbers ? dialableNumberContract : AssistantPhone,
                OfficeLocation = BUSINESS_ADDRESS,
                EmailAddress = BUSINESS_EMAIL,
                Title = TITLE,
                DepartmentName = DEPARTMENT,
                AssistantName = BUSINESS_ASSISTANT_NAME,
                Notes = USER_NOTES,
                StreetAddress = HOME_ADDRESS,
                City = HOME_CITY,
                StateOrProvince = HOME_STATE,
                Country = HOME_COUNTRY,
                PostalCode = HOME_POSTALCODE
            };

            Users.Set(userContract);
        }

        protected bool ValidatePhoneNumber(EditableNumber number, BasicPhoneNumberDataContract contract)
        {
            var extensionNumber = number.Extension.Text;
            this.TraceTrue(
                string.Equals(extensionNumber, contract.Extension, StringComparison.InvariantCultureIgnoreCase) ||
                (string.IsNullOrWhiteSpace(extensionNumber) && string.IsNullOrWhiteSpace(contract.Extension)),
                string.Format("Extension did not match what was expected (got '{0}', expected '{1}')",
                    extensionNumber,
                    contract.Extension));

            this.ValidateString(contract.Number, NormalizeDisplayNumber(number.DisplayString.Text), "Phone number");

            return true;
        }

        protected void ValidateString(string actual, string expected, string displayName)
        {
            // Normalize the newlines, since Selenium occasionally uses CRLF or LF
            var normalizedActual = Regex.Replace(actual, @"\r\n|\n\r", "\n");
            var normalizedExpected = Regex.Replace(expected, @"\r\n|\n\r", "\n");

            var waiter = new WebDriverBaseWait();
            waiter.IgnoreExceptionTypes(typeof(StaleElementReferenceException));
            this.TraceTrue(() => waiter.Until(d => 
                string.Equals(normalizedActual, normalizedExpected, StringComparison.InvariantCultureIgnoreCase)), 
                string.Format("{0} didn't match; expected '{1}', got '{2}'", displayName, normalizedExpected,normalizedActual));
        }
        #endregion
    }
}