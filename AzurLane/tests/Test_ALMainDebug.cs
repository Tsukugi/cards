using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TCG.Tests;

public partial class Test_ALMainDebug : Node
{
    TestHandler testHandler;

    public override void _Ready()
    {
        base._Ready();
        testHandler = TestRunner.RunSequential(this,
            TestAutoMatchDisabledDoesNothing,
            TestAutoHostFlow,
            TestAutoJoinFlow
        );
    }

    public Task TestAutoMatchDisabledDoesNothing(Test test)
    {
        var host = new FakeAutoMatchHost();
        var network = new FakeAutoMatchNetwork();
        var debug = new ALMainDebug(host, network);

        debug.AutoSyncStart();

        test.Assert(host.WaitCalls, 0);
        test.Assert(host.OpenJoinCalls, 0);
        test.Assert(host.OpenHostCalls, 0);
        test.Assert(host.StartMatchCalls, 0);
        return Task.CompletedTask;
    }

    public async Task TestAutoHostFlow(Test test)
    {
        var host = new FakeAutoMatchHost { AutoHostEnabled = true };
        var network = new FakeAutoMatchNetwork
        {
            IsServer = true,
            PlayerCount = 2
        };
        var debug = new ALMainDebug(host, network);

        await debug.AutoHostMatch();

        test.Assert(host.OpenHostCalls, 1);
        test.Assert(host.StartMatchCalls, 1);
        test.Assert(host.WaitSeconds.Count, 3);
        test.Assert(Mathf.IsEqualApprox(host.WaitSeconds[0], 1f), true);
        test.Assert(Mathf.IsEqualApprox(host.WaitSeconds[1], 1f), true);
        test.Assert(Mathf.IsEqualApprox(host.WaitSeconds[2], 1f), true);
    }

    public async Task TestAutoJoinFlow(Test test)
    {
        var host = new FakeAutoMatchHost { AutoJoinEnabled = true };
        var network = new FakeAutoMatchNetwork();
        var debug = new ALMainDebug(host, network);

        await debug.AutoJoinMatch();

        test.Assert(host.OpenJoinCalls, 1);
        test.Assert(host.OpenHostCalls, 0);
        test.Assert(host.StartMatchCalls, 0);
        test.Assert(host.WaitSeconds.Count, 2);
        test.Assert(Mathf.IsEqualApprox(host.WaitSeconds[0], 1f), true);
        test.Assert(Mathf.IsEqualApprox(host.WaitSeconds[1], 2f), true);
    }

    sealed class FakeAutoMatchHost : IALMainAutoMatchHost
    {
        public bool AutoHostEnabled { get; set; }
        public bool AutoJoinEnabled { get; set; }
        public bool IsGameCreated { get; private set; }
        public int WaitCalls { get; private set; }
        public int OpenJoinCalls { get; private set; }
        public int OpenHostCalls { get; private set; }
        public int StartMatchCalls { get; private set; }
        public int UpdateJoinInputsCalls { get; private set; }
        public string LastAddress { get; private set; } = "";
        public int LastPort { get; private set; }
        public List<float> WaitSeconds { get; } = [];
        LobbyMode mode = LobbyMode.None;

        public Task Wait(float seconds)
        {
            WaitCalls++;
            WaitSeconds.Add(seconds);
            return Task.CompletedTask;
        }

        public void OpenJoinLobby()
        {
            OpenJoinCalls++;
            mode = LobbyMode.Join;
        }

        public void OpenHostLobby()
        {
            OpenHostCalls++;
            mode = LobbyMode.Host;
        }

        public void UpdateJoinInputs(string address, int port)
        {
            UpdateJoinInputsCalls++;
            LastAddress = address;
            LastPort = port;
        }

        public ALConnectionSettings GetJoinConnectionSettings()
        {
            return new ALConnectionSettings
            {
                Address = "127.0.0.1",
                Port = 7000
            };
        }

        public Error TryConfirmJoinOrHost(out string message)
        {
            message = "";
            if (mode == LobbyMode.Join)
            {
                return Error.Ok;
            }
            if (mode == LobbyMode.Host)
            {
                IsGameCreated = true;
                return Error.Ok;
            }
            message = "No lobby mode set.";
            return Error.Failed;
        }

        public void StartMatch()
        {
            StartMatchCalls++;
        }

        public bool IsAutoHostMatchEnabled() => AutoHostEnabled;
        public bool IsAutoJoinMatchEnabled() => AutoJoinEnabled;
        public void SetAutoMatchInProgress(bool enabled) { }

        enum LobbyMode
        {
            None,
            Join,
            Host
        }
    }

    sealed class FakeAutoMatchNetwork : IAutoMatchNetwork
    {
        public int CreatePlayerCountCalls { get; private set; }
        public bool IsServer { get; set; }
        public int PlayerCount { get; set; } = 1;

        public int GetPlayerCount()
        {
            CreatePlayerCountCalls++;
            return PlayerCount;
        }
    }
}
