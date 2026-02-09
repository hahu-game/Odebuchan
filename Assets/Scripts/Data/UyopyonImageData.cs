using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// うーぴょんの画像データを管理するScriptableObject
/// Assets/Illustration/uyopyon配下の画像をInspectorから設定する
/// </summary>
[CreateAssetMenu(fileName = "UyopyonImageData", menuName = "Odebuchan/Uyopyon Image Data")]
public class UyopyonImageData : ScriptableObject
{
    [Header("進化前（Before）デフォルト画像")]
    public Sprite beforeNormal;
    public Sprite beforeAbnormal;

    [Header("進化後（After）デフォルト画像")]
    public Sprite afterNormal;
    public Sprite afterAbnormal;

    [Header("進化前（Before）行動画像")]
    public ActionImageSet beforeEat;
    public ActionImageSet beforeSleep;
    public ActionImageSet beforePlay;
    public ActionImageSet beforeClinic;

    [Header("進化後（After）基本行動画像")]
    public ActionImageSet afterEat;
    public ActionImageSet afterSleep;
    public ActionImageSet afterPlay;
    public ActionImageSet afterClinic;

    [Header("進化後（After）特殊能力画像")]
    public ActionImageSet afterGaishoku;
    public ActionImageSet afterKintre;
    public ActionImageSet afterGamushara;
    public ActionImageSet afterBenkyou;
    public ActionImageSet afterJukusui;
    public ActionImageSet afterDokagui;

    /// <summary>
    /// 行動ごとの画像セット（progress + complete）
    /// </summary>
    [Serializable]
    public class ActionImageSet
    {
        [Tooltip("行動中の画像（複数枚対応）")]
        public Sprite[] progressSprites;

        [Tooltip("行動完了の画像（複数枚対応）")]
        public Sprite[] completeSprites;

        /// <summary>
        /// 全画像を順番に取得（progress → complete）
        /// </summary>
        public Sprite[] GetAllSprites()
        {
            int totalCount = (progressSprites?.Length ?? 0) + (completeSprites?.Length ?? 0);
            Sprite[] result = new Sprite[totalCount];

            int index = 0;
            if (progressSprites != null)
            {
                for (int i = 0; i < progressSprites.Length; i++)
                {
                    result[index++] = progressSprites[i];
                }
            }
            if (completeSprites != null)
            {
                for (int i = 0; i < completeSprites.Length; i++)
                {
                    result[index++] = completeSprites[i];
                }
            }

            return result;
        }

        /// <summary>
        /// 画像の総数を取得
        /// </summary>
        public int TotalSpriteCount => (progressSprites?.Length ?? 0) + (completeSprites?.Length ?? 0);
    }

    /// <summary>
    /// 進化状態に応じたデフォルト画像を取得
    /// </summary>
    /// <param name="hasEvolved">進化済みか</param>
    /// <param name="hasAilment">状態異常を持っているか</param>
    /// <returns>対応するSprite</returns>
    public Sprite GetDefaultSprite(bool hasEvolved, bool hasAilment)
    {
        if (hasEvolved)
        {
            return hasAilment ? afterAbnormal : afterNormal;
        }
        else
        {
            return hasAilment ? beforeAbnormal : beforeNormal;
        }
    }

    /// <summary>
    /// 行動タイプに応じた画像セットを取得
    /// </summary>
    /// <param name="actionType">行動タイプ</param>
    /// <param name="hasEvolved">進化済みか</param>
    /// <param name="specialAbilityType">特殊能力タイプ（特殊能力の場合）</param>
    /// <returns>対応する画像セット、見つからない場合はnull</returns>
    public ActionImageSet GetActionImageSet(ActionType actionType, bool hasEvolved, SpecialAbilityType? specialAbilityType = null)
    {
        if (actionType == ActionType.SpecialAbility && specialAbilityType.HasValue)
        {
            // 特殊能力は進化後のみ
            return specialAbilityType.Value switch
            {
                SpecialAbilityType.Gaishoku => afterGaishoku,
                SpecialAbilityType.Kintre => afterKintre,
                SpecialAbilityType.Gamushara => afterGamushara,
                SpecialAbilityType.Benkyou => afterBenkyou,
                SpecialAbilityType.Jukusui => afterJukusui,
                SpecialAbilityType.Dokagui => afterDokagui,
                _ => null
            };
        }

        // 基本行動
        return actionType switch
        {
            ActionType.Eat => hasEvolved ? afterEat : beforeEat,
            ActionType.Sleep => hasEvolved ? afterSleep : beforeSleep,
            ActionType.Play => hasEvolved ? afterPlay : beforePlay,
            ActionType.Clinic => hasEvolved ? afterClinic : beforeClinic,
            _ => null
        };
    }

    /// <summary>
    /// 1枚あたりの表示時間を計算（総時間2秒を画像数で均等割り）
    /// </summary>
    /// <param name="imageSet">画像セット</param>
    /// <returns>1枚あたりの表示時間（秒）</returns>
    public static float GetDisplayDurationPerSprite(ActionImageSet imageSet)
    {
        if (imageSet == null || imageSet.TotalSpriteCount == 0)
        {
            return 0f;
        }

        const float totalDuration = 2.0f;
        return totalDuration / imageSet.TotalSpriteCount;
    }
}
