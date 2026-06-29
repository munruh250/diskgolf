using DiskGolf.CourseEditor;
using NUnit.Framework;

namespace DiskGolf.Tests.EditMode
{
    public sealed class ValidationMessageCopyTests
    {
        [Test]
        public void E001_PlayerCopy_IsPlainLanguage()
        {
            var msg = ValidationMessageCopy.ForCode("E001");
            Assert.That(msg, Does.Contain("basket").IgnoreCase);
            Assert.That(msg, Does.Not.Contain("E001"));
        }
    }
}
