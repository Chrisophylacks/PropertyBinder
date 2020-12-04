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

            var api = PublicApiGenerator.ApiGenerator.GeneratePublicApi(typeof (Binder<>).Assembly);
            if (File.Exists(fileName))
            {
                var currentApi = File.ReadAllText(fileName);
                if (!string.Equals(api, currentApi))
                {
                    File.WriteAllText(fileName, api);
                    throw new Exception("API mismatch, check git diff");
                }
            }
            else
            {
                File.WriteAllText(fileName, api);
            }
        }
    }
}