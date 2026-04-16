using System.Collections;
using UnityEngine;
using TMPro;

public class ReelController : MonoBehaviour
{
    public TextMeshProUGUI reelText;
    public int finalNumber { get; private set; }

    public IEnumerator Spin(float duration)
    {
        float elapsed = 0f;
        float tickRate = 0.07f;

        while (elapsed < duration)
        {
            reelText.text = Random.Range(0, 10).ToString();
            elapsed += tickRate;
            yield return new WaitForSeconds(tickRate);
        }

        finalNumber = Random.Range(0, 10);
        reelText.text = finalNumber.ToString();
    }

    public void ResetReel()
    {
        StopAllCoroutines();
        reelText.text = "?";
        finalNumber = 0;
    }
}