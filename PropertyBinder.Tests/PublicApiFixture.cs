using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace PropertyBinder.Tests
{
    [TestFixture]
    public class PublicApiFixture
    {
        [Test]
        public void ShouldPreservePublicApi()
        {
            const string fileName = @"..\..\PublicApi.txt";
            const string proposedFileName = @"..\..\PublicApi.txt.proposed";

            var api = PublicApiGenerator.PublicApiGenerator.GetPublicApi(typeof (PropertyBinder<>).Assembly);
            //var api = PublicApiGenerator.PublicApiGenerator.GetPublicApi(Assembly.LoadFrom(@"w:\Projects\github\PropertyBinder\output\PropertyBinder.dll"));
            if (File.Exists(fileName))
            {
                var currentApi = File.ReadAllText(fileName);
                if (!string.Equals(api, currentApi))
                {
                    File.WriteAllText(proposedFileName, api);
                    throw new Exception("API mismatch");
                }
            }
            else
            {
                File.WriteAllText(fileName, api);
            }
        }
    }
}