using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreTable : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI killText;
    [SerializeField] TextMeshProUGUI deathText;
    [SerializeField] TextMeshProUGUI suicideText;

    public string playerName { get; set; }
    public int score { get; set; }
    public int kill { get; set; }
    public int death { get; set; }
    public int suicide { get; set; }

    private void Start()
    {
        nameText.text = playerName;
    }

    public void AddKill()
    {
        kill++;
        score++;
        
        scoreText.text = score.ToString();
    }

    public void AddDeath()
    {
        death++;
    }

    public void AddSuicide()
    {
        suicide++;
        score--;

        scoreText.text = score.ToString();
    }

    public void DisplayMainScoreTable()
    {
        nameText.text = playerName;
        scoreText.text = score.ToString();
        killText.text = kill.ToString();
        deathText.text = death.ToString();
        suicideText.text = suicide.ToString();
    }

    public void SetScore(int newScore)
    {
        score = newScore;
    }

}