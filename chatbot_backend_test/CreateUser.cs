using System;
using NUnit.Framework;

namespace chatbot_backend_test
{
    [TestFixture]
    public class CreateUser
    {
        [Test]
        public void TestCreateUser()
        {
        }

        [TestCase("", true)]
        [TestCase("hej", true)]
        [TestCase("hej@", true)]
        [TestCase("hej@.com", true)]
        [TestCase("hej@meddig.com", false)]
        [TestCase("hej@meddig.dk", false)]
        [TestCase("hej@meddig.org", false)]
        [TestCase("hej@meddig.no", false)]
        [TestCase("hej@meddig.us", false)]
        public void TestTestEmail(string email, bool throws)
        {
            if (throws) {
                Assert.That(() => chatbot_backend.Controllers.CreateUser.TestEmail(email),
                Throws.Exception
                  .TypeOf<Exception>()
                  .With.Property("Message")
                  .EqualTo("Email is not valid."));
            } else {
                chatbot_backend.Controllers.CreateUser.TestEmail(email);
            }
        }

        [TestCase("", true, "The password must contain at least one character")]
        [TestCase("HELLOWORLD", true, "The password must contain at least one lowercase character")]
        [TestCase("helloworld", true, "The password must contain at least one uppercase character")]
        [TestCase("HelloWorld", true, "The password must contain at least one digit")]
        [TestCase("HelloWorld1", true, "The password must contain at least one special character")]
        [TestCase("HWd1+", true, "The password must be at least 8 characters long")]
        [TestCase("HelloWorld1+", false, "")]
        public void TestTestPassword(string password, bool throws, string exceptionMessage)
        {
            if (throws)
            {
                Assert.That(() => chatbot_backend.Controllers.CreateUser.TestPassword(password),
                Throws.Exception
                  .TypeOf<Exception>()
                  .With.Property("Message")
                  .EqualTo(exceptionMessage));
            } else {
                chatbot_backend.Controllers.CreateUser.TestPassword(password);
            }
        }

        [Test]
        public void TestCreateUserIfNotExists()
        {
        }
    }
}
