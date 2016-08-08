using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace PropertyBinder.Tests
{
    [TestFixture]
    public class PublicApiFixture
    {
        [Test]
        public void ShouldPreservePublicApi()
        {
            string fileName = Path.Combine(Environment.CurrentDirectory, @"..\..\PublicApi.txt");
            string proposedFileName = Path.Combine(Environment.CurrentDirectory, @"PublicApi.txt.proposed");

            var api = PublicApiGenerator.PublicApiGenerator.GetPublicApi(typeof (Binder<>).Assembly);
            if (File.Exists(fileName))
            {
                var currentApi = File.ReadAllText(fileName);
                if (!string.Equals(api, currentApi))
                {
                    File.WriteAllText(proposedFileName, api);
                    Process.Start(new ProcessStartInfo("winmergeu", string.Format("\"{0}\" \"{1}\"", proposedFileName, fileName)));
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