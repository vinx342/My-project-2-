using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SlotMachineManager : MonoBehaviour
{
    public ReelController[] reels = new ReelController[6];
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI budgetText;
    public TextMeshProUGUI totalWonText;
    public TextMeshProUGUI multiplierLabel;
    public TextMeshProUGUI spinCountText;
    public TextMeshProUGUI goalText;
    public Button spinButton;
    public Slider betSlider;
    public TextMeshProUGUI betLabel;
    public TextMeshProUGUI betLabel2;
    public TextMeshProUGUI warnLbl;
    public GameObject warnLabelUI;
    public GameObject goalWinPanel;
    public TextMeshProUGUI goalWinText;

    [Header("Reel Timing")]
    public float spinDuration = 5.0f;

    [Header("Multiplier Range")]
    public int minMultiplier = 1;
    public int maxMultiplier = 10;

    [Header("Lose Conditions")]
    public int maxSpins = 20;
    public float autoResetDelay = 3f;

    [Header("Goal Range")]
    public int goalMin = 10000;
    public int goalMax = 100000;

    private float[] stopTimes;
    private int budget = 100;
    private int totalWon = 0;
    private int bet = 1;
    private int lastBet = 1;
    private int multiplier = 1;
    private int spinsUsed = 0;
    private int goal = 0;
    private bool isSpinning = false;
    private bool updatingSlider = false;
    private bool isGameOver = false;
    private Coroutine spinCoroutine = null;

    void Start()
    {
        stopTimes = new float[reels.Length];
        float gap = spinDuration / reels.Length;
        for (int i = 0; i < reels.Length; i++)
            stopTimes[i] = gap * (i + 1);

        if (goalWinPanel != null) goalWinPanel.SetActive(false);
        warnLabelUI.SetActive(false);

        betSlider.onValueChanged.RemoveAllListeners();
        betSlider.minValue = 1;
        betSlider.maxValue = budget;
        betSlider.value = 1;
        bet = 1;
        betSlider.onValueChanged.AddListener(OnBetChanged);

        multiplierLabel.text = "x?";
        spinButton.interactable = true;

        GenerateGoal();
        UpdateHUD();
        ShowWarnLabel();
    }

    void GenerateGoal()
    {
        // always positive goal only
        goal = Mathf.RoundToInt(Random.Range(goalMin, goalMax) / 1000f) * 1000;
        goalText.text = "Goal: Reach $" + goal.ToString("N0");
        goalText.color = Color.green;

        totalWon = 0;
        totalWonText.text = "Total Wins: $0 / $" + goal.ToString("N0");
    }

    public void OnBetChanged(float val)
    {
        if (isSpinning || updatingSlider || isGameOver) return;
        bet = Mathf.RoundToInt(val);
        betLabel.text = "$" + bet;
        betLabel2.text = "Bet: $" + bet;
        UpdateHUD();
        ShowWarnLabel();
    }

    public void OnSpinPressed()
    {
        if (budget < 1 || isSpinning || isGameOver) return;
        isSpinning = true;
        spinsUsed++;
        lastBet = bet;
        budget -= bet;

        multiplier = Random.Range(minMultiplier, maxMultiplier + 1);
        bool isNegativeMultiplier = Random.value > 0.75f;
        if (isNegativeMultiplier) multiplier = -multiplier;
        multiplierLabel.text = "x" + multiplier;

        spinButton.interactable = false;
        betSlider.interactable = false;

        resultText.text = "Spinning...";
        resultText.color = Color.white;
        warnLabelUI.SetActive(false);
        UpdateHUD();
        spinCoroutine = StartCoroutine(SpinAll());
    }

    IEnumerator SpinAll()
    {
        for (int i = 0; i < reels.Length; i++)
            StartCoroutine(reels[i].Spin(stopTimes[i]));

        yield return new WaitForSeconds(spinDuration + 0.1f);
        CheckWin();
        isSpinning = false;

        // budget hits 0 — game over
        if (budget <= 0)
        {
            budget = 0;
            StartCoroutine(GameOver("You ran out of budget!"));
            yield break;
        }

        // spin limit — game over
        if (spinsUsed >= maxSpins)
        {
            StartCoroutine(GameOver("You used all " + maxSpins + " spins!"));
            yield break;
        }

        // goal reached — win
        if (totalWon >= goal)
        {
            StartCoroutine(GoalWin(
                "GOAL REACHED!",
                "You reached $" + goal.ToString("N0") + " in wins!",
                Color.green
            ));
            yield break;
        }

        // continue game
        updatingSlider = true;
        betSlider.maxValue = Mathf.Max(1, budget);
        int keepBet = Mathf.Clamp(lastBet, 1, budget);
        betSlider.value = keepBet;
        updatingSlider = false;

        bet = keepBet;
        spinButton.interactable = true;
        betSlider.interactable = true;
        multiplierLabel.text = "x?";

        UpdateHUD();
        ShowWarnLabel();
    }

    void CheckWin()
    {
        var counts = new Dictionary<int, int>();
        foreach (var r in reels)
        {
            int n = r.finalNumber;
            if (!counts.ContainsKey(n)) counts[n] = 0;
            counts[n]++;
        }

        int maxMatch = 0;
        foreach (var v in counts.Values)
            if (v > maxMatch) maxMatch = v;

        int winAmount = 0;
        string label = "";

        switch (maxMatch)
        {
            case 6: winAmount = lastBet * multiplier * 6; label = "JACKPOT! x" + (multiplier * 6); break;
            case 5: winAmount = lastBet * multiplier * 5; label = "Five of a kind! x" + (multiplier * 5); break;
            case 4: winAmount = lastBet * multiplier * 4; label = "Four of a kind! x" + (multiplier * 4); break;
            case 3: winAmount = lastBet * multiplier; label = "Three of a kind! x" + multiplier; break;
            case 2: winAmount = lastBet * multiplier; label = "Pair! x" + multiplier; break;
            default: label = "No match. Lost $" + lastBet; break;
        }

        if (winAmount > 0)
        {
            budget += winAmount;
            totalWon += winAmount;
            resultText.text = label + " +$" + winAmount;
            resultText.color = Color.green;
        }
        else if (winAmount < 0)
        {
            int loss = Mathf.Abs(winAmount);
            budget -= loss;
            budget = Mathf.Max(0, budget);
            totalWon += winAmount;
            totalWon = Mathf.Max(0, totalWon);
            resultText.text = label + " -$" + loss + " (negative multiplier!)";
            resultText.color = Color.red;
            budgetText.text = "Budget: $" + budget.ToString("N0");
        }
        else
        {
            resultText.text = label;
            resultText.color = Color.white;
        }

        totalWonText.text = "Total Wins: $" + totalWon.ToString("N0") + " / $" + goal.ToString("N0");
    }

    IEnumerator GoalWin(string header, string message, Color col)
    {
        isGameOver = true;
        spinButton.interactable = false;
        betSlider.interactable = false;

        if (goalWinPanel != null)
        {
            goalWinPanel.SetActive(true);
            if (goalWinText != null)
            {
                goalWinText.text = header + "\n" + message;
                goalWinText.color = col;
            }
        }

        warnLabelUI.SetActive(true);
        warnLbl.text = header;
        warnLbl.color = col;
        resultText.color = col;

        float timer = autoResetDelay;
        while (timer > 0)
        {
            resultText.text = message + "\nResetting in " + Mathf.CeilToInt(timer) + "s...";
            if (goalWinText != null)
                goalWinText.text = header + "\n" + message + "\nResetting in " + Mathf.CeilToInt(timer) + "s...";
            timer -= Time.deltaTime;
            yield return null;
        }

        resultText.color = Color.white;
        if (goalWinPanel != null) goalWinPanel.SetActive(false);
        ResetGame();
    }

    IEnumerator GameOver(string reason)
    {
        isGameOver = true;
        spinButton.interactable = false;
        betSlider.interactable = false;

        warnLabelUI.SetActive(true);
        warnLbl.text = "GAME OVER!";
        ColorUtility.TryParseHtmlString("#CC100B", out Color loseColor);
        warnLbl.color = loseColor;

        float timer = autoResetDelay;
        while (timer > 0)
        {
            resultText.text = reason + "\nResetting in " + Mathf.CeilToInt(timer) + "s...";
            resultText.color = Color.red; 
            timer -= Time.deltaTime;
            yield return null;
        }

        ResetGame();
    }

    void ShowWarnLabel()
    {
        if (isSpinning || isGameOver) return;

        if (bet >= budget && budget > 0)
        {
            warnLabelUI.SetActive(true);
            warnLbl.text = "WARNING: ALL IN";
            warnLbl.color = Color.yellow;
        }
        else
        {
            warnLabelUI.SetActive(false);
        }
    }

    void UpdateHUD()
    {
        budgetText.text = "Budget: $" + budget.ToString("N0");
        totalWonText.text = "Total Wins: $" + totalWon.ToString("N0") + " / $" + goal.ToString("N0");
        betLabel.text = "$" + bet;
        betLabel2.text = "Bet: $" + bet;
        if (spinCountText != null)
            spinCountText.text = "Spins: " + spinsUsed + " / " + maxSpins;
    }

    public void ResetGame()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;
        }
        StopAllCoroutines();

        foreach (var r in reels)
            r.ResetReel();

        budget = 100;
        totalWon = 0;
        bet = 1;
        lastBet = 1;
        spinsUsed = 0;
        multiplier = 1;
        isSpinning = false;
        isGameOver = false;

        updatingSlider = true;
        betSlider.minValue = 1;
        betSlider.maxValue = 100;
        betSlider.value = 1;
        betSlider.interactable = true;
        updatingSlider = false;

        multiplierLabel.text = "x?";
        spinButton.interactable = true;

        resultText.text = "";
        resultText.color = Color.white;
        betLabel.color = Color.white;
        betLabel2.color = Color.white;

        warnLabelUI.SetActive(false);
        if (goalWinPanel != null) goalWinPanel.SetActive(false);

        GenerateGoal();
        UpdateHUD();
        ShowWarnLabel();
    }
}