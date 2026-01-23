using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace TCG.Tests
{
    public class TestHandler
    {
        public delegate Task TestImplAsync(Test test);
        Node rootNode;
        int failedAsserts = 0;
        public TestHandler(Node _rootNode)
        {
            rootNode = _rootNode;
        }
        public async Task RunTestsSequentially(List<TestImplAsync> tests)
        {
            List<Func<Task>> tasks = [];
            tests.ForEach(test => tasks.Add(() => RunTest(test)));
            await AsyncHandler.RunAsyncFunctionsSequentially(tasks);
            GD.Print($"[RunTestsSequentially] Tests complete! {rootNode.GetType()} Total {tests.Count}");
            PrintSummary(tests.Count);
            QuitAfterTests();
        }

        public async Task RunTestsParallel(List<TestImplAsync> tests)
        {
            List<Task> taskResults = [];
            tests.ForEach(test => taskResults.Add(RunTest(test)));
            await Task.WhenAll(taskResults.ToArray());
            GD.Print($"[RunTestsSequentially] Tests complete! {rootNode.GetType()} Total {tests.Count}");
            PrintSummary(tests.Count);
            QuitAfterTests();
        }

        public Task RunTest(TestImplAsync impl)
        {
            return new Test(rootNode, impl, this).RunTest();
        }

        internal void RegisterAssertResult(bool success)
        {
            if (!success)
            {
                failedAsserts++;
            }
        }

        private void QuitAfterTests()
        {
            int exitCode = failedAsserts > 0 ? 1 : 0;
            rootNode.GetTree().Quit(exitCode);
        }

        private void PrintSummary(int totalTests)
        {
            GD.Print($"[TestsSummary] {rootNode.GetType()} Tests {totalTests} - FailedAsserts {failedAsserts}");
        }
    }

    public class Test
    {
        Node rootNode;
        TestHandler.TestImplAsync impl;
        TestHandler handler;
        int failedAsserts = 0, successfulAsserts = 0;

        public Test(Node _rootNode, TestHandler.TestImplAsync _impl, TestHandler _handler)
        {
            rootNode = _rootNode;
            impl = _impl;
            handler = _handler;
        }
        public Task RunTest()
        {
            GD.Print($"[RunTest] Starting test for {impl.GetMethodInfo().Name}");
            Task testResult = impl(this);
            testResult.ContinueWith((testResult) => OnTestComplete());
            return testResult;
        }

        void OnTestComplete()
        {
            GD.Print($"[OnTestComplete] {impl.GetMethodInfo().Name} Success {successfulAsserts} - Failed {failedAsserts} - Total {failedAsserts + successfulAsserts}");
        }

        void HandleAssert<T>(bool success, T value, T expected)
        {
            handler.RegisterAssertResult(success);
            if (success)
            {
                successfulAsserts++;
                return;
            }
            string msg = $"[Assert] {impl.GetMethodInfo().Name} Error - Value: {value} - Expected: {expected} ";
            GD.PrintErr(msg);
            GD.PushError(msg);
            failedAsserts++;
        }

        public void Assert(bool value, bool expected) => HandleAssert(value == expected, value, expected);
        public void Assert(int value, int expected) => HandleAssert(value == expected, value, expected);
        public void Assert(Vector2 value, Vector2 expected) => HandleAssert(value == expected, value, expected);
        public void Assert(string value, string expected) => HandleAssert(value == expected, value, expected);
    }
}
