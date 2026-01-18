
using System.Threading.Tasks;
using Godot;
using TCG.Tests;

namespace ALTCG.Tests
{
    public partial class Test_ALBoard : ALBoard
    {
        ALPlayer player = new();
        TestHandler testHandler;
        public override void _Ready()
        {
            base._Ready();
            testHandler = TestRunner.RunSequential(this,
                TestPlaceCardInBoardFromHand
            );
        }

        public async Task TestPlaceCardInBoardFromHand(Test test)
        {
            ALCard mockCard = new();

            SelectCardField(player, Vector2I.One);
            PlaceCardInBoardFromHand(player, mockCard);
            test.Assert(GetSelectedCardPosition(player), Vector2I.One);
            test.Assert(GetSelectedCard<ALCard>(player).Name, mockCard.Name);
            await Task.CompletedTask;
        }
    }
}
