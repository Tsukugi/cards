using System.Collections.Generic;
using Godot;

namespace TCG.Tests
{
    public static class TestRunner
    {
        public static TestHandler RunSequential(Node node, params TestHandler.TestImplAsync[] tests)
        {
            var handler = new TestHandler(node);
            _ = handler.RunTestsSequentially(new List<TestHandler.TestImplAsync>(tests));
            return handler;
        }

        public static TestHandler RunParallel(Node node, params TestHandler.TestImplAsync[] tests)
        {
            var handler = new TestHandler(node);
            _ = handler.RunTestsParallel(new List<TestHandler.TestImplAsync>(tests));
            return handler;
        }
    }
}
