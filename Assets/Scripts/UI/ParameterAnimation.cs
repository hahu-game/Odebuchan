using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ステータス欄の数字に対する疑似アニメーション処理を提供
/// 0.5秒で数値が段階的に変化するアニメーションを実行
/// </summary>
public class ParameterAnimation : MonoBehaviour
{
    // 各TextMeshProUGUIごとのアニメーション中のコルーチン参照（複数同時実行対応）
    private Dictionary<TextMeshProUGUI, Coroutine> _runningAnimations = new Dictionary<TextMeshProUGUI, Coroutine>();

    /// <summary>
    /// パラメータの変動アニメーションを実行
    /// </summary>
    /// <param name="targetText">アニメーション対象のTextMeshProUGUI</param>
    /// <param name="fromValue">現在の値</param>
    /// <param name="toValue">変動後の値</param>
    public void AnimateParameter(TextMeshProUGUI targetText, int fromValue, int toValue)
    {
        if (targetText == null)
        {
            Debug.LogWarning("[ParameterAnimation] targetText is null");
            return;
        }

        // 既にこのTextMeshProUGUIでアニメーション実行中の場合は停止
        if (_runningAnimations.ContainsKey(targetText) && _runningAnimations[targetText] != null)
        {
            StopCoroutine(_runningAnimations[targetText]);
        }

        // 新しいアニメーションを開始
        Coroutine coroutine = StartCoroutine(AnimateCoroutine(targetText, fromValue, toValue, false));
        _runningAnimations[targetText] = coroutine;
    }

    /// <summary>
    /// パラメータの変動アニメーションを実行（+符号付き表示）
    /// </summary>
    /// <param name="targetText">アニメーション対象のTextMeshProUGUI</param>
    /// <param name="fromValue">現在の値</param>
    /// <param name="toValue">変動後の値</param>
    public void AnimateParameterWithSign(TextMeshProUGUI targetText, int fromValue, int toValue)
    {
        if (targetText == null)
        {
            Debug.LogWarning("[ParameterAnimation] targetText is null");
            return;
        }

        // 既にこのTextMeshProUGUIでアニメーション実行中の場合は停止
        if (_runningAnimations.ContainsKey(targetText) && _runningAnimations[targetText] != null)
        {
            StopCoroutine(_runningAnimations[targetText]);
        }

        // 新しいアニメーションを開始（+符号付き）
        Coroutine coroutine = StartCoroutine(AnimateCoroutine(targetText, fromValue, toValue, true));
        _runningAnimations[targetText] = coroutine;
    }

    /// <summary>
    /// アニメーション処理のコルーチン
    /// 0.1秒×5回で合計0.5秒のアニメーションを実行
    /// </summary>
    /// <param name="withSign">trueの場合、正の値に+符号を付ける</param>
    private IEnumerator AnimateCoroutine(TextMeshProUGUI targetText, int fromValue, int toValue, bool withSign)
    {
        try
        {
            // 変動がない場合はそのまま表示
            if (fromValue == toValue)
            {
                if (withSign)
                {
                    string sign = toValue >= 0 ? "+" : "";
                    targetText.text = $"{sign}{toValue}";
                }
                else
                {
                    targetText.text = toValue.ToString();
                }
                targetText.color = Color.black;
                yield break;
            }

            // 6つの数値を算出（初期値、中間4つ、最終値）
            // 計算のずれを防ぐため、小数切り捨ては表示時のみ行う
            float[] values = new float[6];
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5.0f; // 0.0, 0.2, 0.4, 0.6, 0.8, 1.0
                values[i] = Mathf.Lerp(fromValue, toValue, t);
            }

            // 上昇か減少かで色を決定（上昇=青、減少=赤）
            Color animationColor = (toValue > fromValue) ? Color.blue : Color.red;
            targetText.color = animationColor;

            // 6つの値を0.1秒ずつ表示（合計0.5秒）
            for (int i = 0; i < 6; i++)
            {
                // 小数を切り捨てて表示
                int displayValue = Mathf.FloorToInt(values[i]);

                if (withSign)
                {
                    string sign = displayValue >= 0 ? "+" : "";
                    targetText.text = $"{sign}{displayValue}";
                }
                else
                {
                    targetText.text = displayValue.ToString();
                }

                // 最後の値でない場合は0.1秒待機
                if (i < 5)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            // アニメーション終了後、黒色に戻す
            targetText.color = Color.black;

            // 最終値を確実に設定（計算誤差防止）
            if (withSign)
            {
                string sign = toValue >= 0 ? "+" : "";
                targetText.text = $"{sign}{toValue}";
            }
            else
            {
                targetText.text = toValue.ToString();
            }
        }
        finally
        {
            // アニメーション終了時に辞書から削除（正常終了・中断の両方で実行）
            if (_runningAnimations.ContainsKey(targetText))
            {
                _runningAnimations.Remove(targetText);
            }

            // 中断された場合も色を黒に戻す（安全対策）
            if (targetText != null)
            {
                targetText.color = Color.black;
            }
        }
    }
}
