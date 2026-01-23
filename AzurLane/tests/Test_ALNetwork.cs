using System.Threading.Tasks;
using TCG.Tests;

public partial class Test_ALNetwork : ALNetwork
{
    TestHandler testHandler;

    public override void _Ready()
    {
        base._Ready();
        testHandler = TestRunner.RunSequential(this,
            TestOnALDrawCardEvent,
            TestOnSyncFlagshipEvent,
            TestOnSendMatchPhaseEvent
        );
    }

    void TriggerALDrawCard(string cardId, ALDrawType drawType) => OnALDrawCard(cardId, drawType);
    void TriggerSyncFlagship(string cardId) => OnSyncFlagship(cardId);
    void TriggerSendMatchPhase(int matchPhase) => OnSendMatchPhase(matchPhase);

    public Task TestOnALDrawCardEvent(Test test)
    {
        bool called = false;
        string calledCardId = "";
        ALDrawType calledDrawType = ALDrawType.Cube;
        ALPlayerDrawEvent handler = (peerId, cardId, drawType) =>
        {
            called = true;
            calledCardId = cardId;
            calledDrawType = drawType;
        };

        OnDrawCardEvent += handler;
        TriggerALDrawCard("test-card", ALDrawType.Deck);
        OnDrawCardEvent -= handler;

        test.Assert(called, true);
        test.Assert(calledCardId, "test-card");
        test.Assert((int)calledDrawType, (int)ALDrawType.Deck);
        return Task.CompletedTask;
    }

    public Task TestOnSyncFlagshipEvent(Test test)
    {
        bool called = false;
        string calledCardId = "";
        ALPlayerSyncCardEvent handler = (peerId, cardId) =>
        {
            called = true;
            calledCardId = cardId;
        };

        OnSyncFlagshipEvent += handler;
        TriggerSyncFlagship("flagship-01");
        OnSyncFlagshipEvent -= handler;

        test.Assert(called, true);
        test.Assert(calledCardId, "flagship-01");
        return Task.CompletedTask;
    }

    public Task TestOnSendMatchPhaseEvent(Test test)
    {
        bool called = false;
        int calledPhase = -1;
        ALPlayerMatchPhaseEvent handler = (peerId, matchPhase) =>
        {
            called = true;
            calledPhase = matchPhase;
        };

        OnSendMatchPhaseEvent += handler;
        TriggerSendMatchPhase(2);
        OnSendMatchPhaseEvent -= handler;

        test.Assert(called, true);
        test.Assert(calledPhase, 2);
        return Task.CompletedTask;
    }
}
