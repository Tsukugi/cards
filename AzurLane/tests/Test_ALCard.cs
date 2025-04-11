
using System.Collections.Generic;
using System.Threading.Tasks;
using TCG.Tests;

namespace ALTCG.Tests
{
    public partial class Test_ALCard : ALCard
    {
        TestHandler testHandler;
        public override void _Ready()
        {
            base._Ready();
            testHandler = new(this);
            _ = testHandler.RunTestsSequentially(new List<TestHandler.TestImplAsync>(){
                    TestCanShowStackCount,
                    TestCanShowCardDetailsUI,
                    TestCanShowPowerLabel,
                    TestSetGetIsInActiveState,
                    TestCanBeAttacked
                });
        }

        public Task TestCanShowStackCount(Test test)
        {
            test.Assert(CanShowStackCount(), false);
            CardStack = 5;
            test.Assert(CanShowStackCount(), true);
            return Task.CompletedTask;
        }
        public Task TestCanShowCardDetailsUI(Test test)
        {
            IsEmptyField = true;
            SetIsFaceDown(true);
            test.Assert(CanShowCardDetailsUI(), false);
            IsEmptyField = false;
            SetIsFaceDown(false);
            test.Assert(CanShowCardDetailsUI(), true);
            return Task.CompletedTask;
        }
        public Task TestCanShowPowerLabel(Test test)
        {
            IsEmptyField = true;
            test.Assert(CanShowPowerLabel(), false);
            IsEmptyField = false;
            test.Assert(CanShowPowerLabel(), true);
            return Task.CompletedTask;
        }

        public Task TestSetGetIsInActiveState(Test test)
        {
            SetIsInActiveState(false);
            test.Assert(GetIsInActiveState(), false);
            SetIsInActiveState(true);
            test.Assert(GetIsInActiveState(), true);
            return Task.CompletedTask;
        }

        public Task TestCanBeAttacked(Test test)
        {
            SetAttackFieldType(EAttackFieldType.BackRow);
            test.Assert(CanBeAttacked(EAttackFieldType.CantAttackHere), false); // Expected error
            test.Assert(CanBeAttacked(EAttackFieldType.BackRow), false);
            test.Assert(CanBeAttacked(EAttackFieldType.FrontRow), true);
            SetAttackFieldType(EAttackFieldType.FrontRow);
            test.Assert(CanBeAttacked(EAttackFieldType.CantAttackHere), false); // Expected error
            test.Assert(CanBeAttacked(EAttackFieldType.BackRow), true);
            test.Assert(CanBeAttacked(EAttackFieldType.FrontRow), true);
            return Task.CompletedTask;
        }
    }
}