using UnityEngine;
using UnityEngine.UI; 
using TMPro;

public class TimeCounter : MonoBehaviour
{
    [Header("绑定UI文本组件")]
    public TMP_Text timeText;

    private float totalTime; // 累计总秒数
    private int hour, minute, second;

    void Update()
    {
        totalTime += Time.deltaTime;
        CalculateTime();
        ShowTime();
    }

    // 把总秒数换算成 时、分、秒
    void CalculateTime()
    {
        // 总秒数换算小时
        hour = Mathf.FloorToInt(totalTime / 3600);
        // 扣除小时后剩余秒数，换算分钟
        minute = Mathf.FloorToInt((totalTime % 3600) / 60);
        // 剩余部分为秒
        second = Mathf.FloorToInt(totalTime % 60);
    }

    // 格式化显示：00:00:00
    void ShowTime()
    {
        timeText.text = $"{hour:D2}:{minute:D2}:{second:D2}";
    }

    // 外部调用：重置计时器
    public void ResetTimer()
    {
        totalTime = 0;
    }

    // 暂停计时
    public void PauseTimer()
    {
        enabled = false;
    }

    // 继续计时
    public void ContinueTimer()
    {
        enabled = true;
    }
}