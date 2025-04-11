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
        }

        public async Task RunTestsParallel(List<TestImplAsync> tests)
        {
            List<Task> taskResults = [];
            tests.ForEach(test => taskResults.Add(RunTest(test)));
            await Task.WhenAll(taskResults.ToArray());
            GD.Print($"[RunTestsSequentially] Tests complete! {rootNode.GetType()} Total {tests.Count}");
        }

        public Task RunTest(TestImplAsync impl)
        {
            return new Test(rootNode, impl).RunTest();
        }
    }

    public class Test
    {
        Node rootNode;
        TestHandler.TestImplAsync impl;
        int failedAsserts = 0, successfulAsserts = 0;

        public Test(Node _rootNode, TestHandler.TestImplAsync _impl)
        {
            rootNode = _rootNode;
            impl = _impl;
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

        public void Assert(bool value, bool expected) => HandleAssert<bool>(value == expected, value, expected);
        public void Assert(int value, int expected) => HandleAssert<int>(value == expected, value, expected);
    }
}