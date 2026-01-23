using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TCG.Tests;

namespace ALTCG.Tests
{
    public partial class Test_ALCard : ALCard
    {
        TestHandler testHandler;
        readonly Dictionary<string, Variant> errorSettingBackup = new();
        public override void _Ready()
        {
            cardDisplay = GetNodeOrNull<Node3D>("CardDisplay") ?? new Node3D { Name = "CardDisplay" };
            if (cardDisplay.GetParent() is null)
            {
                AddChild(cardDisplay);
            }
            SetProcess(false);
            SetTestAttributes();
            testHandler = TestRunner.RunSequential(this,
                TestCanShowStackCount,
                TestCanShowCardDetailsUI,
                TestCanShowPowerLabel,
                TestSetGetIsInActiveState,
                TestCanBeAttacked
            );
        }

        public override void _EnterTree()
        {
            SetProcess(false);
        }

        public override void _Process(double delta)
        {
            // No per-frame updates needed for tests.
        }

        void SetTestAttributes()
        {
            var field = typeof(Card).GetField("attributes", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field is null)
            {
                GD.PushError("[Test_ALCard] Failed to set test attributes.");
                return;
            }

            field.SetValue(this, new ALCardDTO());
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
            SetIsEmptyField(true);
            SetIsFaceDown(true);
            test.Assert(CanShowCardDetailsUI(), false);
            SetIsEmptyField(false);
            SetIsFaceDown(false);
            test.Assert(CanShowCardDetailsUI(), true);
            return Task.CompletedTask;
        }
        public Task TestCanShowPowerLabel(Test test)
        {
            SetIsEmptyField(true);
            test.Assert(CanShowPowerLabel(), false);
            SetIsEmptyField(false);
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
            SetErrorPrintsEnabled(false);
            test.Assert(CanBeAttacked(EAttackFieldType.CantAttackHere), false); // Expected error
            SetErrorPrintsEnabled(true);
            test.Assert(CanBeAttacked(EAttackFieldType.BackRow), false);
            test.Assert(CanBeAttacked(EAttackFieldType.FrontRow), true);
            SetAttackFieldType(EAttackFieldType.FrontRow);
            SetErrorPrintsEnabled(false);
            test.Assert(CanBeAttacked(EAttackFieldType.CantAttackHere), false); // Expected error
            SetErrorPrintsEnabled(true);
            test.Assert(CanBeAttacked(EAttackFieldType.BackRow), true);
            test.Assert(CanBeAttacked(EAttackFieldType.FrontRow), true);
            return Task.CompletedTask;
        }

        void SetErrorPrintsEnabled(bool enabled)
        {
            string[] settingKeys =
            {
                "debug/settings/stdout/print_errors",
                "debug/settings/stdout/print_error_messages"
            };

            foreach (string key in settingKeys)
            {
                if (!ProjectSettings.HasSetting(key))
                {
                    continue;
                }

                if (!errorSettingBackup.ContainsKey(key))
                {
                    errorSettingBackup[key] = ProjectSettings.GetSetting(key);
                }

                ProjectSettings.SetSetting(key, enabled);
            }

            if (enabled)
            {
                foreach (var entry in errorSettingBackup)
                {
                    ProjectSettings.SetSetting(entry.Key, entry.Value);
                }
                errorSettingBackup.Clear();
            }
        }
    }
}
