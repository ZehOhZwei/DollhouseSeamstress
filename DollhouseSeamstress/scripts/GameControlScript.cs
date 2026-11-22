using Godot;
using System;
using System.Collections.Generic;

public partial class GameControlScript : Node3D
{
    public enum GameState { TitleScreen, ResultScreen, EndScreen, InGame, Playing }

    [Export] public Control TitleScreen;
    [Export] public Control InGameUi;
    [Export] public Control ResultScreen;
    [Export] public Control EndScreen;
    [Export] public Node3D Player;
    private GameState _gameState;
    private Node currentLevel;
    private int currentLevelIdx = 0;
    public List<string> LevelPaths = [
        "res://scenes/levels/level_1_tent.tscn",
        "res://scenes/levels/level_2_house.tscn",
        "res://scenes/levels/level_3_villa.tscn"
        ];

    public override void _Ready()
    {
        var startButton = GetNode<Button>("../UI/TitleScreenUi/StartGameButton");
        _gameState = GameState.TitleScreen;
        startButton.ButtonDown += () => { NextLevel(0); };
    }

    public void NextLevel(int idx)
    {
        currentLevelIdx = idx;
        var parent = GetParent();
        if (currentLevel != null)
        {
            parent.RemoveChild(currentLevel);
        }

        TitleScreen.Visible = false;
        ResultScreen.Visible = false;
        EndScreen.Visible = false;
        InGameUi.Visible = true;
        _gameState = GameState.InGame;

        if (idx < LevelPaths.Count - 1)
        {
            var levelScene = ResourceLoader.Load<PackedScene>(LevelPaths[idx]);
            var level = levelScene.Instantiate();
            currentLevel = level;
            GetParent().AddChild(level);
        }
        else
        {
            TitleScreen.Visible = false;
            ResultScreen.Visible = false;
            EndScreen.Visible = true;
            InGameUi.Visible = false;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("play_level") && _gameState == GameState.InGame)
        {
            PlayLevel();
        }
    }


    public void PlayLevel()
    {
        _gameState = GameState.Playing;


        currentLevel.Call("PlayLevel");

        var timer = new Timer();
        AddChild(timer);
        timer.WaitTime = 5.0f;
        timer.Timeout += LevelResults;
        timer.OneShot = true;
        timer.Start();
    }

    public void LevelResults()
    {
        _gameState = GameState.ResultScreen;

        var score = (float)currentLevel.Call("GetLevelScore");

        TitleScreen.Visible = false;
        ResultScreen.Visible = true;
        EndScreen.Visible = false;
        InGameUi.Visible = false;

        var nextButton = ResultScreen.GetNode<Button>("NextButton");
        var restartButton = ResultScreen.GetNode<Button>("RestartButton");

        ResultScreen.GetNode<Label>("Score").Text = $"Score: {(int)score}%";

        if (score > 50f)
        {
            nextButton.Visible = true;
            restartButton.Visible = false;

            nextButton.ButtonDown += () => { NextLevel(currentLevelIdx + 1); };
        }
        else
        {
            nextButton.Visible = false;
            restartButton.Visible = true;

            restartButton.ButtonDown += () => { RestartLevel(); };
        }

    }


    public void RestartLevel()
    {
        NextLevel(currentLevelIdx);
    }
}
