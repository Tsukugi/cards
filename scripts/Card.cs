using Godot;
using System;

public partial class Card : Node3D
{
    bool isSelected = false;

    Node3D selectedIndicator;

    [Export]
    public bool IsSelected
    {
        get => isSelected; set
        {
            isSelected = value;
        }
    }

    public override void _Ready()
    {
        selectedIndicator = GetNode<Node3D>("SelectedIndicator");
    }


    public override void _Process(double delta)
    {
        OnSelectHandler(isSelected);
    }
    void OnSelectHandler(bool isSelected)
    {
        selectedIndicator.Visible = isSelected;
    }
}
