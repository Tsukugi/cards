using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TCG.Tests;

namespace ALTCG.Tests
{
    public partial class Test_Async : Node
    {
        TestHandler testHandler;
        private AsyncHandler async;

        public override void _Ready()
        {
            base._Ready();
            testHandler = new(this);
            async = new(this);

            _ = testHandler.RunTestsSequentially(new List<TestHandler.TestImplAsync>(){
                    TestAwaitBefore,
                    TestAwaitForCheck,
                    TestAwaitForCheckTimeout,
                    TestDebounce,
                    TestRunAsyncFunctionsSequentially
                });
        }

        public async Task TestAwaitBefore(Test test)
        {
            bool boolTest = false;
            Task testAwait = async.AwaitBefore(() => { boolTest = true; }, 0.1f);
            test.Assert(boolTest, false);
            await testAwait;
            test.Assert(boolTest, true);
        }

        public async Task TestAwaitForCheck(Test test)
        {
            bool boolTest = false, finished = false;

            test.Assert(async.GetIsLoading(), false);

            Task testAwait = async.AwaitForCheck(
                () => { finished = true; },
                () => boolTest
            );

            test.Assert(finished, false);
            test.Assert(async.GetIsLoading(), true);
            await this.Wait(0.5f);

            boolTest = true;

            // Bool test is not immediately reflected on finished as it needs to go to the next check iteration
            test.Assert(finished, false);
            test.Assert(async.GetIsLoading(), true);

            await this.Wait(0.5f);

            test.Assert(finished, true);
            test.Assert(async.GetIsLoading(), false);
        }
        public async Task TestAwaitForCheckTimeout(Test test)
        {
            var timeoutTime = 1f;

            test.Assert(async.GetIsLoading(), false);

            Task testAwait = async.AwaitForCheck(
                null,
                () => false,
                timeoutTime
            );

            test.Assert(async.GetIsLoading(), true);

            await this.Wait(timeoutTime + 1);  // Add a bit more time for the next check iteration

            test.Assert(async.GetIsLoading(), false);
        }

        public async Task TestDebounce(Test test)
        {

            var timeoutTime = 1f;
            test.Assert(async.GetIsLoading(), false);
            Task testAwait = async.Debounce(() =>
            {
                test.Assert(async.GetIsLoading(), false);
            }, timeoutTime);

            test.Assert(async.GetIsLoading(), true);

            await this.Wait(timeoutTime + 1);  // Add a bit more time for the next check iteration

            test.Assert(async.GetIsLoading(), false);
        }
        public async Task TestRunAsyncFunctionsSequentially(Test test)
        {

            int lastResolved = 0;
            test.Assert(lastResolved, 0);
            Task testAwait = AsyncHandler.RunAsyncFunctionsSequentially(new List<Func<Task>>() {
                async () => { await this.Wait(1f); lastResolved = 1; test.Assert(lastResolved, 1);},
                async () => { await this.Wait(1f); lastResolved = 2; test.Assert(lastResolved, 2);},
                async () => { await this.Wait(1f); lastResolved = 3; test.Assert(lastResolved, 3);},
            });

            await this.Wait(5f);
            test.Assert(lastResolved, 3);
        }
    }
}