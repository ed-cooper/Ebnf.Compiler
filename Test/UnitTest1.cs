using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices.ComTypes;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void RegexRule_ShouldPass()
        {
            var r = Validation.IsRange("abc", out var rem, out var pt);

            var s = Validation.IsStruct("struct hello :", out rem, out pt);

            Assert.IsTrue(r);
            Assert.IsTrue(s);
        }
    }
}
