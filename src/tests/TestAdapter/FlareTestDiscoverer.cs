using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Flare.Tests.TestAdapter
{
    [DefaultExecutorUri(FlareTestExecutor.ExecutorUri)]
    [FileExtension(".dll")]
    public sealed class FlareTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            foreach (var test in FlareTestLoader.Tests.Values)
                discoverySink.SendTestCase(test.Convert());
        }
    }
}
