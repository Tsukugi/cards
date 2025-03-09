
using Godot;
using System.Collections.Generic;

public partial class Player : Node3D
{
    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    CardGroup selectedGroup;
    int selectedGroupIndex = 0;
    PlayerHand hand;
    PlayerBoard board;
    List<CardGroup> groups = new();
    public override void _Ready()
    {
        hand = GetNode<PlayerHand>("Hand");
        board = GetNode<PlayerBoard>("Board");
        groups = new() { hand, board };
        SelectGroup(0);
    }

    public override void _Process(double delta)
    {
        OnAxisChangeHandler(axisInputHandler.GetAxis());
    }

    void OnAxisChangeHandler(Vector2 axis)
    {
        if (axis.Y != 0) SelectGroup((int)axis.Y);
    }

    void SelectGroup(int index)
    {
        selectedGroupIndex = groups.Count.ApplyCircularBounds(selectedGroupIndex + index);
        selectedGroup = groups[selectedGroupIndex];
        groups.ForEach(group => group.SetIsGroupActive(false));
        selectedGroup.SetIsGroupActive(true);
        GD.Print(index);
        GD.Print(selectedGroup.Name);
    }
}
