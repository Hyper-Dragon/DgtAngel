using DgtCherub.Controllers;
using DgtCherub.Helpers;
using DgtCherub.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Text.Json;

namespace DgtCherubTest
{
    /// <summary>
    /// Log sanitisation. Everything written to the console box goes through this, and the
    /// input includes text scraped from a web page, so it must not be able to break a line.
    /// </summary>
    [TestClass]
    public class SanitiseTextTests
    {
        [TestMethod(DisplayName = "Quotes become dots so they cannot terminate a quoted field")]
        public void QuotesAreReplaced()
        {
            Assert.AreEqual("a.b", "a\"b".SanitiseText());
        }

        [TestMethod(DisplayName = "Line breaks are removed so one event stays one line")]
        [DataRow("a\rb", "ab")]
        [DataRow("a\nb", "ab")]
        [DataRow("a\r\nb", "ab")]
        [DataRow("a\tb", "ab")]
        public void LineBreaksAreRemoved(string input, string expected)
        {
            Assert.AreEqual(expected, input.SanitiseText());
        }

        [TestMethod(DisplayName = "A null character is removed")]
        public void NullCharacterIsRemoved()
        {
            Assert.AreEqual("ab", "a\0b".SanitiseText());
        }

        [TestMethod(DisplayName = "Ordinary text is untouched")]
        public void OrdinaryTextIsUntouched()
        {
            Assert.AreEqual("rnbqkbnr/pppppppp", "rnbqkbnr/pppppppp".SanitiseText());
        }

        [TestMethod(DisplayName = "Empty input stays empty")]
        public void EmptyStaysEmpty()
        {
            Assert.AreEqual("", "".SanitiseText());
        }
    }

    /// <summary>
    /// The plain HTTP API: what the CLI and the browser extension's diagnostics use.
    /// </summary>
    [TestClass]
    public class CherubHttpApiControllerTests
    {
        private static (CherubHttpApiController controller, Mock<IAngelHubService> hub) Create()
        {
            Mock<IAngelHubService> hub = new(MockBehavior.Loose) { DefaultValue = DefaultValue.Empty };

            return (new CherubHttpApiController(NullLogger<CherubHttpApiController>.Instance, hub.Object), hub);
        }

        [TestMethod(DisplayName = "AreYouThere answers without touching the hub")]
        public void AreYouThereAnswers()
        {
            (CherubHttpApiController controller, Mock<IAngelHubService> hub) = Create();

            string json = JsonSerializer.Serialize(controller.AreYouThere());

            StringAssert.Contains(json, "\"isSuccess\":true");
            hub.VerifyNoOtherCalls();
        }

        [TestMethod(DisplayName = "MessageUser forwards the message to the console")]
        public void MessageUserForwards()
        {
            (CherubHttpApiController controller, Mock<IAngelHubService> hub) = Create();

            _ = controller.MessageUser("CLI", "hello");

            hub.Verify(h => h.UserMessageArrived("CLI", "hello"), Times.Once);
        }

        [TestMethod(DisplayName = "MessageUser uses the documented defaults")]
        public void MessageUserDefaults()
        {
            (CherubHttpApiController controller, Mock<IAngelHubService> hub) = Create();

            _ = controller.MessageUser();

            hub.Verify(h => h.UserMessageArrived("Unknown", "Message Empty"), Times.Once);
        }
    }

    /// <summary>
    /// The embedded-resource loader. A missing resource is a programming error, but it must
    /// report itself: GetManifestResourceStream returns null and the loader dereferences it,
    /// so callers see a NullReferenceException with no clue which resource was missing.
    /// </summary>
    [TestClass]
    public class ResourceLoaderTests
    {
        [TestMethod(DisplayName = "A missing resource reports itself rather than a null reference")]
        public void MissingResourceIsReported()
        {
            Exception? thrown = null;

            try
            {
                _ = ResourceLoader.LoadResource("DgtCherub.Assets.DoesNotExist.png");
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            Assert.IsNotNull(thrown, "a missing resource must fail loudly");
            Assert.IsFalse(
                thrown is NullReferenceException,
                $"a missing resource should say so, not throw {thrown.GetType().Name}");
        }
    }
}
