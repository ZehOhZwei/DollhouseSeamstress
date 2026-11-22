using Godot;
using System;
using System.Collections.Generic;

public partial class GameControlScript : Node3D
{
    public enum GameState { titleScreen, resultScreen, EndScreen, levelOne, levelTwo, levelThree }

    [Export] public Control TitleScreen;
    public string TitleScreenPath = "res://scenes/levels/titleScreen";
    public string ResultScreenPath = "res://scenes/levels/levelResultScreen";
    public List<string> LevelPaths = [
        "res://scenes/levels/level_1_tent",
        "res://scenes/level_2_house",
        "res://scenes/level_3_villa"
        ];
    public string EndScreenPath = "res://scenes/levels/endScreen";



    public override void _Ready()
    {
        var startButton = GetNode<Button>("../TitleScreenUi/StartGameButton");

        startButton.ButtonDown += OnStartButtonPressed;
    }

    private void OnStartButtonPressed()
    {
        NextLevel(0);
    }

    public void NextLevel(int idx)
    {

    }

    public void RestartLevel(int idx)
    {

    }
}
